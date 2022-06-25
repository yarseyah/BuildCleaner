namespace BuildCleaner.Commands;

[SuppressMessage(
    "ReSharper",
    "ClassNeverInstantiated.Global",
    Justification = "Invoked by Spectre.Console for 'WhatIf' command options")]
public class WhatIfCommand : AsyncCommand<WhatIfCommandSettings>
{
    private bool showSizes;

    private long totalSize;

    public WhatIfCommand(
        ILogger<WhatIfCommand> logger,
        RecursiveFolderLocator recursiveFolderLocator,
        FolderSizeCalculator folderSizeCalculator,
        IFolderSelector folderSelector)
    {
        Logger = logger;
        RecursiveFolderLocator = recursiveFolderLocator;
        FolderSizeCalculator = folderSizeCalculator;
        FolderSelector = folderSelector;
    }

    private ILogger<WhatIfCommand> Logger { get; }

    private RecursiveFolderLocator RecursiveFolderLocator { get; }

    private FolderSizeCalculator FolderSizeCalculator { get; }

    private IFolderSelector FolderSelector { get; }

    public override async Task<int> ExecuteAsync(CommandContext context, WhatIfCommandSettings whatIfCommandSettings)
    {
        Logger.LogTrace("Invoked {Command}", nameof(WhatIfCommand));

        AnsiConsole.WriteLine("WhatIf shows the folders the would be deleted when using the 'delete' command");
        AnsiConsole.WriteLine();

        var options = new RecursiveFolderLocator.Options
        {
            DisplayAccessErrors = whatIfCommandSettings.DisplayAccessErrors,
            DisplayBaseFolder = whatIfCommandSettings.DisplayBaseFolder
        };

        showSizes = whatIfCommandSettings.ShowSizes;
        var deleteAll = false;

        // Configure the 'activity' response from users, if non-interactive, default to 'Delete'
        Func<string, Activity> activity = whatIfCommandSettings.Interactive ? Prompt : _ => Activity.Delete;

        await RecursiveFolderLocator.VisitAsync(
            whatIfCommandSettings.RootLocation,
            async (folder) =>
            {
                Logger.LogTrace("Visiting {Folder}", folder);

                // Get the desired activity for this folder, if 'all' has previously been selected,
                // short-circuit the prompt and return 'Delete'
                var action = deleteAll ? Activity.Delete : activity(folder);
                deleteAll |= (action == Activity.DeleteAll);

                if (action is Activity.Delete or Activity.DeleteAll)
                {
                    await WhatIfDeleteFolder(folder);
                }
                else if (action == Activity.DeleteNothing)
                {
                    return false;
                }

                return true;
            },
            async folder => await FolderSelector.SelectFolderAsync(folder),
            options);

        if (showSizes)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"Total size to be recovered will be [yellow]{totalSize.Bytes().ToFullWords()}[/].");
        }

        return 0;
    }

    private async Task WhatIfDeleteFolder(string folder)
    {
        var size = 0L;

        if (showSizes)
        {
            await AnsiConsole.Status()
                .StartAsync("Calculating folder size...", async _ =>
                {
                    // TODO: we could easily use a callback to get the latest size
                    size = await FolderSizeCalculator.GetFolderSizeAsync(folder);
                    totalSize += size;
                });

            var sizeOutput = size == 0 ? "empty" : size.Bytes().ToFullWords();
            AnsiConsole.MarkupLine($"WhatIf: [red][[DELETE]][/] {folder} [yellow]{sizeOutput}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"WhatIf: [red][[DELETE]][/] {folder}");
        }

    }

    private static Activity Prompt(string folder) =>
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
 
    private enum Activity
    {
        Delete,
        Keep,
        DeleteAll,
        DeleteNothing,
        Unknown,
    }
}