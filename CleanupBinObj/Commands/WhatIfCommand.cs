namespace CleanupBinObj.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CleanupBinObj.Commands.Settings;
using CleanupBinObj.Rules.Exclude;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

public class WhatIfCommand : AsyncCommand<WhatIfSettings>
{
    private HashSet<string> TargetFolders { get; } = new(StringComparer.CurrentCultureIgnoreCase);

    private List<string> AccessErrors { get; } = new();

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
        var root = settings.RootLocation;

        AnsiConsole.WriteLine("WhatIf shows the folders the would be deleted when using the 'delete' command");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"Base folder : [yellow]{root}[/]");

        foreach (var folder in GetFoldersRecursively(root))
        {
            AnsiConsole.WriteLine(folder);
        }

        if (true && AccessErrors.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[aqua]The following locations reported an access problem[/]");
            AccessErrors.ForEach(e => AnsiConsole.MarkupLine($" * [red]{e}[/]"));
        }

        return Task.FromResult(0);
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
}