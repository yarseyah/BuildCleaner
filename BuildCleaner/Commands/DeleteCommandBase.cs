namespace BuildCleaner.Commands;

public abstract class DeleteCommandBase(
    ILogger logger,
    RecursiveFolderLocator recursiveFolderLocator,
    IFolderSelector folderSelector,
    FolderSizeCalculator folderSizeCalculator,
    IList<IAccessIssue> accessIssues)
    : AsyncCommand<DeleteCommandSettings>
{
    protected readonly ILogger Logger = logger;
    protected readonly IList<IAccessIssue> AccessIssues = accessIssues;
    private bool ShowSizes { get; set; }
    private long TotalSize { get; set; }
    private RecursiveFolderLocator RecursiveFolderLocator { get; } = recursiveFolderLocator;
    private IFolderSelector FolderSelector { get; } = folderSelector;
    private FolderSizeCalculator FolderSizeCalculator { get; } = folderSizeCalculator;

    public override async Task<int> ExecuteAsync(CommandContext context, DeleteCommandSettings settings)
    {
        Logger.LogTrace("Invoked {Command}", CommandName);
        ExplainCommand();

        var options = new RecursiveFolderLocator.Options
        {
            DisplayAccessErrors = settings.DisplayAccessErrors,
            DisplayBaseFolder = settings.DisplayBaseFolder
        };

        var deleteAll = false;
        ShowSizes = settings.ShowSizes;
        Func<string, Activity> activity = settings.Interactive ? Prompt : _ => Activity.Delete;

        await RecursiveFolderLocator.VisitAsync(
            settings.RootLocation,
            async (folder) =>
            {
                Logger.LogTrace("Visiting {Folder}", folder);
                var action = deleteAll ? Activity.Delete : activity(folder);
                deleteAll |= (action == Activity.DeleteAll);

                if (action is Activity.Delete or Activity.DeleteAll)
                {
                    var size = 0L;
                    if (ShowSizes)
                    {
                        await AnsiConsole.Status()
                            .StartAsync("Calculating folder size...", async _ =>
                            {
                                size = await FolderSizeCalculator.GetFolderSizeAsync(folder);
                                TotalSize += size;
                            });
                        var sizeOutput = size == 0 ? "-" : FormatMb(size);
                        AnsiConsole.MarkupLine($"{CommandName}: [yellow]{sizeOutput,12}[/] : [red][[DELETE]][/] {folder}");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"{CommandName}: [red][[DELETE]][/] {folder}");
                    }

                    try
                    {
                        await DeleteFolder(settings, folder);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Unable to process folder {Folder}", folder);
                        AccessIssues.Add(new ExceptionAccessIssue(ex, folder));
                    }
                }
                else if (action == Activity.DeleteNothing)
                {
                    return false;
                }

                return true;
            },
            async folder => await FolderSelector.SelectFolderAsync(folder),
            options);

        if (ShowSizes)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"Space recovered [yellow]{FormatMb(TotalSize)}[/].");
        }

        return 0;
    }

    protected abstract void ExplainCommand();
    protected abstract string CommandName { get; }
    protected abstract Task DeleteFolder(DeleteCommandSettings settings, string folder);

    protected static Activity Prompt(string folder) =>
        AnsiConsole.Prompt(
                new TextPrompt<string>($"Delete folder [yellow]{folder}[/]?")
                    .InvalidChoiceMessage("Not a valid action")
                    .DefaultValue("yes")
                    .AddChoice("yes")
                    .AddChoice("no")
                    .AddChoice("all")
                    .AddChoice("none")) switch
            {
                "yes" => Activity.Delete,
                "no" => Activity.Keep,
                "all" => Activity.DeleteAll,
                "none" => Activity.DeleteNothing,
                _ => Activity.Unknown,
            };

    private static string FormatMb(long bytes) => $"{(bytes / 1024.0 / 1024.0):0.##}mb";

    protected enum Activity
    {
        Delete,
        Keep,
        DeleteAll,
        DeleteNothing,
        Unknown,
    }

    protected static bool IsSymlink(DirectoryInfo di)
    {
        return di.Attributes.HasFlag(FileAttributes.ReparsePoint);
    }

    protected static bool IsSafePath(string folder, string rootLocation)
    {
        var allowedRoot = Path.GetFullPath(rootLocation);
        var fullPath = Path.GetFullPath(folder);
        return fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase);
    }
}
