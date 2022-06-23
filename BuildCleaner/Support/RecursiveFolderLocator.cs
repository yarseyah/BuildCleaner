namespace BuildCleaner.Support;

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

    private List<(Exception Exception, string Folder)> AccessErrors { get; } = new();

    public Options DefaultOptions => new Options();

    public async Task VisitAsync(
        string rootLocation, 
        Func<string, Task<bool>> visitorFunc,
        Func<string, Task<bool>> selectorFunc,
        Options? options = null)
    {
        var root = EnsureAbsolutePath(rootLocation);
        options ??= DefaultOptions;

        if (options.DisplayBaseFolder)
        {
            AnsiConsole.MarkupLine($"Base folder : [yellow]{root}[/]");
            AnsiConsole.WriteLine();
        }

        await foreach (var folder in GetFoldersRecursively(root, selectorFunc))
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
                AnsiConsole.WriteLine();
                var table = new Table();
                table.AddColumns("Error", "Location");
                AccessErrors.ForEach(ae => table.AddRow(ae.Exception.GetType().Name, ae.Folder));
                AnsiConsole.Write(table);
            }
            else
            {
                AnsiConsole.MarkupLine(
                    "[aqua]No folders reported an access problem, however on deletion subfolders may[/]");
            }
        }
    }

    private async IAsyncEnumerable<string> GetFoldersRecursively(
        string root, 
        Func<string, Task<bool>> selectorFunc,
        int depth = 0)
    {
        var subFolders = GetFolders(root);
        var count = subFolders.Length;
        var countdown = subFolders.Length;
        var position = 0;
     
        foreach (var folder in subFolders)
        {
            Logger.LogTrace(
                "[{Pos}/{Total}] Processing folder '{Folder}' (Depth = {Depth}) ",
                position,
                count,
                folder, 
                depth);

            Exclusion excluded;
            
            try
            {
                excluded = ExclusionRules.Enforce(folder);
            }
            catch (Exception e)
            {
                AccessErrors.Add((e, folder));
                countdown--;
                position++;
                continue;
            }
            
            var excludeSelf = (excluded & Exclusion.ExcludeSelf) == Exclusion.ExcludeSelf;
            var excludeChildren = (excluded & Exclusion.ExcludeChildren) == Exclusion.ExcludeChildren;

            // Use the delegate to determine if we should visit this folder
            var isBuildFolder = await selectorFunc(folder);
            
            if (isBuildFolder && !excludeSelf)
            {
                Logger.LogTrace("Visiting folder {Folder} [{Depth}]", folder, depth);
                yield return folder;
            }
            else if (!excludeChildren)
            {
                Logger.LogTrace("Calling children of folder {Folder} [{Depth}]", folder, depth);
                await foreach (var child in GetFoldersRecursively(folder, selectorFunc, depth+1))
                {
                    yield return child;
                }
            }
            else
            {
                Logger.LogTrace("Folder {Name} is assumed to be excluded [{Depth}]", folder, depth);
            }

            countdown--;
            position++;
        }

        if (countdown > 0)
        {
            Logger.LogWarning("{FolderCount} folders remain to be visited", countdown);
        }
    }

    private string[] GetFolders(string parent)
    {
        try
        {
            var dirs = Directory.GetDirectories(parent);
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
            AccessErrors.Add((e, parent));
            return Array.Empty<string>();
        }
    }

    // TODO: this can be done in the validation of settings
    private string EnsureAbsolutePath(string root)
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