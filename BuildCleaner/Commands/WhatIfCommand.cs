namespace BuildCleaner.Commands;

[SuppressMessage(
    "ReSharper",
    "ClassNeverInstantiated.Global",
    Justification = "Invoked by Spectre.Console for 'WhatIf' command options")]
public class WhatIfCommand : AsyncCommand<Settings>
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

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        Logger.LogTrace("Invoked {Command}", nameof(WhatIfCommand));

        AnsiConsole.WriteLine("WhatIf shows the folders the would be deleted when using the 'delete' command");
        AnsiConsole.WriteLine();

        var options = RecursiveFolderLocator.DefaultOptions;
        options.DisplayAccessErrors = settings.DisplayAccessErrors;
        options.DisplayBaseFolder = settings.DisplayBaseFolder;

        showSizes = settings.ShowSizes;
        var deleteAll = false;

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

                switch (action)
                {
                    case Activity.Delete:
                        await WhatIfDeleteFolder(folder);
                        break;
                    case Activity.DeleteAll:
                        await WhatIfDeleteFolder(folder);
                        deleteAll = true;
                        break;
                    case Activity.DeleteNothing:
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