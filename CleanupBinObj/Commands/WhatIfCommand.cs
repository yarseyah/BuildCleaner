﻿namespace CleanupBinObj.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CleanupBinObj.Commands.Settings;
using CleanupBinObj.Rules.Exclude;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

public class WhatIfCommand : AsyncCommand<WhatIfSettings>
{
    private HashSet<string> TargetFolders { get; } = new(StringComparer.CurrentCultureIgnoreCase);

    public WhatIfCommand(
        ILogger<WhatIfCommand> logger,
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

    private ILogger<WhatIfCommand> Logger { get; }

    private ExclusionRules ExclusionRules { get; }

    public override Task<int> ExecuteAsync(CommandContext context, WhatIfSettings settings)
    {
        var root = EnsureAbsolutePath(settings.RootLocation);

        AnsiConsole.WriteLine("WhatIf shows the folders the would be deleted when using the 'delete' command");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"Base folder : [yellow]{root}[/]");

        foreach (var folder in GetFoldersRecursively(root))
        {
            AnsiConsole.WriteLine(folder);
        }

        return Task.FromResult(0);
    }

    private IEnumerable<string> GetFoldersRecursively(string root)
    {
        IEnumerable<(string Folder, bool IsTarget)> folders = Directory.GetDirectories(root)
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

    // TODO: this can be done in the validation of settings
    private string EnsureAbsolutePath(string root)
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        return (root, entryAssembly) switch
        {
            { root: ".", entryAssembly: not null } => (!string.IsNullOrWhiteSpace(entryAssembly.Location)
                        ? Path.GetDirectoryName(entryAssembly.Location)
                        : null) ??
                    throw new NotSupportedException("Seem to be missing entry assembly"),
            { root.Length: > 0 } when Directory.Exists(root) => Path.GetFullPath(root),
            { root.Length: > 0 } => throw new DirectoryNotFoundException($"Unable to find directory '{root}'"),
            _ => throw new ArgumentException("Unspecified issue with root folder supplied", nameof(root))
        };
    }
}