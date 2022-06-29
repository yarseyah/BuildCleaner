namespace BuildCleaner.Commands;

public abstract class AbstractDeleteCommand : AsyncCommand<DeleteCommandSettings>
{
    protected AbstractDeleteCommand(
        ILogger logger,
        RecursiveFolderLocator recursiveFolderLocator,
        IFolderSelector folderSelector,
        FolderSizeCalculator folderSizeCalculator
        )
    {
        Logger = logger;
        RecursiveFolderLocator = recursiveFolderLocator;
        FolderSelector = folderSelector;
        FolderSizeCalculator = folderSizeCalculator;
    }

    private ILogger Logger { get; }
    
    private RecursiveFolderLocator RecursiveFolderLocator { get; }

    private IFolderSelector FolderSelector { get; }
    
    private FolderSizeCalculator FolderSizeCalculator { get; }

    protected bool ShowSizes { get; private set; }
    
    protected long TotalSize { get; private set; }
    
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

        // Configure the 'activity' response from users, if non-interactive, default to 'Delete'
        Func<string, Activity> activity = settings.Interactive ? Prompt : _ => Activity.Delete;

        await RecursiveFolderLocator.VisitAsync(
            settings.RootLocation,
            async (folder) =>
            {
                Logger.LogTrace("Visiting {Folder}", folder);

                // Get the desired activity for this folder, if 'all' has previously been selected,
                // short-circuit the prompt and return 'Delete'
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
                                // TODO: we could easily use a callback to get the latest size
                                size = await FolderSizeCalculator.GetFolderSizeAsync(folder);
                                TotalSize += size;
                            });

                        var sizeOutput = size == 0 ? "empty" : size.Bytes().ToFullWords();
                        AnsiConsole.MarkupLine($"{CommandName}: [red][[DELETE]][/] {folder} [yellow]{sizeOutput}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"{CommandName}: [red][[DELETE]][/] {folder}");
                    }

                    try
                    {
                        await PerformCommand(folder);
                    }
                    catch (Exception)
                    {
                        Logger.LogError("Unable to process folder {Folder}", folder);
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
            AnsiConsole.MarkupLine($"Space recovered [yellow]{TotalSize.Bytes().ToFullWords()}[/].");
        }

        return 0;
    }

    protected abstract void ExplainCommand();

    protected abstract string CommandName { get; }

    protected abstract Task PerformCommand(string folder);

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

    protected enum Activity
    {
        Delete,
        Keep,
        DeleteAll,
        DeleteNothing,
        Unknown,
    }
}