﻿namespace BuildCleaner.Support;

public class RecursiveFolderLocator(
    ILogger<RecursiveFolderLocator> logger,
    ExclusionRules exclusionRules,
    IList<IAccessIssue> accessIssues)
{
    private ILogger<RecursiveFolderLocator> Logger { get; } = logger;
    private ExclusionRules ExclusionRules { get; } = exclusionRules;
    private IList<IAccessIssue> AccessIssues { get; } = accessIssues;

    public async Task VisitAsync(
        string rootLocation, 
        Func<string, Task<bool>> visitorFunc,
        Func<string, Task<bool>> selectorFunc,
        Options? options = null,
        CancellationToken cancellationToken = default)
    {
        var root = EnsureAbsolutePath(rootLocation);
        options ??= new();

        if (options.DisplayBaseFolder)
        {
            AnsiConsole.MarkupLine($"Base folder : [yellow]{root}[/]");
            AnsiConsole.WriteLine();
        }

        await foreach (var folder in GetFoldersRecursively(root, selectorFunc, cancellationToken))
        {
            // Visitor function will return false if it wants to stop the iteration.
            if (!await visitorFunc(folder))
            {
                break;
            }
        }

        if (options.DisplayAccessErrors)
        {
            AnsiConsole.WriteLine();
            if (AccessIssues.Count != 0)
            {
                AnsiConsole.MarkupLine("[aqua]The following locations reported an access problem[/]");
                AnsiConsole.WriteLine();
                var table = new Table();
                table.AddColumns("Type", "Message", "Location");
                foreach (var issue in AccessIssues)
                {
                    var type = issue is ExceptionAccessIssue ex ? ex.Exception.GetType().Name : "General";
                    table.AddRow(type, issue.Message, issue.Folder);
                }
                AnsiConsole.Write(table);
            }
        }
    }

    private async IAsyncEnumerable<string> GetFoldersRecursively(
        string root, 
        Func<string, Task<bool>> selectorFunc,
        [EnumeratorCancellation]
        CancellationToken cancellationToken,
        int depth = 0)
    {
        var subFolders = GetFolders(root);

        foreach (var folder in subFolders)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            
            Logger.LogTrace(
                "Processing folder '{Folder}' (Depth = {Depth}) ",
                folder,
                depth);

            Exclusion excluded;

            try
            {
                excluded = ExclusionRules.Enforce(folder);
            }
            catch (Exception e)
            {
                AccessIssues.Add(new ExceptionAccessIssue(e, folder));
                continue;
            }

            var excludeSelf = (excluded & Exclusion.ExcludeSelf) == Exclusion.ExcludeSelf;
            var excludeChildren = (excluded & Exclusion.ExcludeChildren) == Exclusion.ExcludeChildren;

            if (!excludeSelf && await selectorFunc(folder))
            {
                Logger.LogTrace("Visiting folder {Folder} [{Depth}]", folder, depth);
                yield return folder;
            }
            else if (!excludeChildren)
            {
                Logger.LogTrace("Calling children of folder {Folder} [{Depth}]", folder, depth);
                await foreach (var child in GetFoldersRecursively(folder, selectorFunc, cancellationToken, depth + 1))
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
            var dirs = Directory
                .GetDirectories(parent)
                .OrderBy(d => d, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            Logger.LogDebug(
                "From {Parent} found {Count} folders: {Dir}\n",
                parent,
                dirs.Length,
                string.Join(Environment.NewLine, dirs.Select((d,i) => $"{i} - {d}")));
            
            return dirs;
        }
        catch (Exception e)
        {
            var logging = e is UnauthorizedAccessException ? LogLevel.Trace : LogLevel.Error;
            Logger.Log(logging, e, "Problem getting directories: {Root}", parent);
            AccessIssues.Add(new ExceptionAccessIssue(e, parent));
            return [];
        }
    }

    // TODO: this can be done in the validation of settings
    private static string EnsureAbsolutePath(string root)
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        return (root, entryAssembly) switch
        {
            { root: ".", entryAssembly: not null } =>
                (!string.IsNullOrWhiteSpace(entryAssembly.Location)
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