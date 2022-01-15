namespace BuildCleaner.Support;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BuildCleaner.Rules.Exclude;
using Microsoft.Extensions.Logging;
using Spectre.Console;

public class RecursiveFolderLocator
{
    public RecursiveFolderLocator(
        ILogger<RecursiveFolderLocator> logger,
        ExclusionRules exclusionRules)
    {
        Logger = logger;
        ExclusionRules = exclusionRules;

        var targetFolders = new[]
        {
            "bin",
            "obj",
            "testresults",
        };

        foreach (var targetFolder in targetFolders)
        {
            TargetFolders.Add(targetFolder);
        }
    }

    private ILogger<RecursiveFolderLocator> Logger { get; }

    private ExclusionRules ExclusionRules { get; }

    private HashSet<string> TargetFolders { get; } = new(StringComparer.CurrentCultureIgnoreCase);

    private List<string> AccessErrors { get; } = new();

    public Options DefaultOptions => new Options();

    public void Visit(string rootLocation, Func<string,bool> callback, Options? options = null)
    {
        var root = EnsureAbsolutePath(rootLocation);
        options ??= DefaultOptions;

        if (options.DisplayBaseFolder)
        {
            AnsiConsole.MarkupLine($"Base folder : [yellow]{root}[/]");
            AnsiConsole.WriteLine();
        }

        foreach (var folder in GetFoldersRecursively(root))
        {
            var @continue = callback(folder);
            if (!@continue)
            {
                break;
            }
        }

        if (options.DisplayAccessErrors && AccessErrors.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[aqua]The following locations reported an access problem[/]");
            AccessErrors.ForEach(e => AnsiConsole.MarkupLine($" * [red]{e}[/]"));
        }
    }

    private IEnumerable<string> GetFoldersRecursively(string root)
    {
        IEnumerable<(string Folder, bool IsTarget)> folders =
            SafelyGetSubDirectories(root)
                .Select(f => (Folders: f, IsTarget: TargetFolders.Contains(Path.GetFileName(f))));

        foreach (var folder in folders)
        {
            var excluded = ExclusionRules.Enforce(folder.Folder);
            var excludeSelf = (excluded & Exclusion.ExcludeSelf) == Exclusion.ExcludeSelf;
            var excludeChildren = (excluded & Exclusion.ExcludeSelf) == Exclusion.ExcludeSelf;

            if (folder.IsTarget && !excludeSelf)
            {
                yield return folder.Folder;
            }
            else if (!excludeChildren)
            {
                foreach (var child in GetFoldersRecursively(folder.Folder))
                {
                    yield return child;
                }
            }
        }
    }

    private string[] SafelyGetSubDirectories(string parent)
    {
        try
        {
            return Directory.GetDirectories(parent);
        }
        catch (Exception e)
        {
            var logging = e is UnauthorizedAccessException ? LogLevel.Warning : LogLevel.Error;
            Logger.Log(logging, e, "Problem getting directories: {Root}", parent);
            AccessErrors.Add(parent);
            return Array.Empty<string>();
        }
    }

    // TODO: this can be done in the validation of settings
    private string EnsureAbsolutePath(string root)
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        return (root, entryAssembly) switch
        {
            { root: ".", entryAssembly: not null } => (!string.IsNullOrWhiteSpace(entryAssembly.Location)
                                                          ? Path.GetDirectoryName(entryAssembly.Location)
                                                          : null) ??
                                                      throw new NotSupportedException(
                                                          "Seem to be missing entry assembly"),
            { root.Length: > 0 } when Directory.Exists(root) => Path.GetFullPath(root),
            { root.Length: > 0 } => throw new DirectoryNotFoundException($"Unable to find directory '{root}'"),
            _ => throw new ArgumentException("Unspecified issue with root folder supplied", nameof(root))
        };
    }

    public class Options
    {
        public bool DisplayAccessErrors { get; set; }

        public bool DisplayBaseFolder { get; set; } = true;
    }
}