﻿namespace BuildCleaner.Support;

public class RecursiveFolderLocator
{
    public RecursiveFolderLocator(
        ILogger<RecursiveFolderLocator> logger,
        ExclusionRules exclusionRules)
    {
        Logger = logger;
        ExclusionRules = exclusionRules;
    }

    private ILogger<RecursiveFolderLocator> Logger { get; }

    private ExclusionRules ExclusionRules { get; }

    private List<string> AccessErrors { get; } = new();

    public Options DefaultOptions => new Options();

    public async Task VisitAsync(
        string rootLocation, 
        Func<string, Task<bool>> visitorFunc,
        Func<string, bool>? selectorFunc = null,
        Options? options = null)
    {
        var root = EnsureAbsolutePath(rootLocation);
        options ??= DefaultOptions;

        if (options.DisplayBaseFolder)
        {
            AnsiConsole.MarkupLine($"Base folder : [yellow]{root}[/]");
            AnsiConsole.WriteLine();
        }

        foreach (var folder in GetFoldersRecursively(root, selectorFunc))
        {
            var @continue = await visitorFunc(folder);
            if (!@continue)
            {
                break;
            }
        }

        if (options.DisplayAccessErrors)
        {
            AnsiConsole.WriteLine();

            if (AccessErrors.Any())
            {
                AnsiConsole.MarkupLine("[aqua]The following locations reported an access problem[/]");
                AccessErrors.ForEach(e => AnsiConsole.MarkupLine($" * [red]{e}[/]"));
            }
            else
            {
                AnsiConsole.MarkupLine(
                    "[aqua]No folders reported an access problem, however on deletion subfolders may[/]");
            }
        }
    }

    private IEnumerable<string> GetFoldersRecursively(string root, Func<string, bool>? selectorFunc)
    {
        var subFolders =
            GetFolders(root)
                .Select(folder => (
                    Name: folder,
                    IsTarget: selectorFunc?.Invoke(Path.GetFileName(folder)) ?? true));

        foreach (var (name, isTarget) in subFolders)
        {
            var excluded = ExclusionRules.Enforce(name);
            var excludeSelf = (excluded & Exclusion.ExcludeSelf) == Exclusion.ExcludeSelf;
            var excludeChildren = (excluded & Exclusion.ExcludeSelf) == Exclusion.ExcludeSelf;

            if (isTarget && !excludeSelf)
            {
                yield return name;
            }
            else if (!excludeChildren)
            {
                foreach (var child in GetFoldersRecursively(name, selectorFunc))
                {
                    yield return child;
                }
            }
        }
    }

    private string[] GetFolders(string parent)
    {
        try
        {
            return Directory.GetDirectories(parent);
        }
        catch (Exception e)
        {
            var logging = e is UnauthorizedAccessException ? LogLevel.Trace : LogLevel.Error;
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