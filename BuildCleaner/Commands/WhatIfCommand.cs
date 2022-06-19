namespace BuildCleaner.Commands;

using Humanizer;

public class WhatIfCommand : AsyncCommand<Settings>
{
    private bool showSizes = false;
    
    private long totalSize = 0;
    
    public WhatIfCommand(
        ILogger<WhatIfCommand> logger,
        RecursiveFolderLocator recursiveFolderLocator,
        FolderSizeCalculator folderSizeCalculator)
    {
        Logger = logger;
        RecursiveFolderLocator = recursiveFolderLocator;
        FolderSizeCalculator = folderSizeCalculator;
    }

    private ILogger<WhatIfCommand> Logger { get; }

    private RecursiveFolderLocator RecursiveFolderLocator { get; }
    
    private FolderSizeCalculator FolderSizeCalculator { get; }

 public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AnsiConsole.WriteLine("WhatIf shows the folders the would be deleted when using the 'delete' command");
        AnsiConsole.WriteLine();

        var options = RecursiveFolderLocator.DefaultOptions;
        options.DisplayAccessErrors = settings.DisplayAccessErrors;
        options.DisplayBaseFolder = settings.DisplayBaseFolder;

        showSizes = settings.ShowSizes;
        var deleteAll = false;
        
        // TODO: use DI to bring this in
        FolderSelector selector = new();
        
        await RecursiveFolderLocator.VisitAsync(
            settings.RootLocation, async (folder) =>
            {
                var action = deleteAll
                    ? Action.Delete
                    : Prompt(folder, settings);

                switch (action)
                {
                    case Action.Delete:
                        await DeleteFolder(folder);
                        break;
                    case Action.DeleteAll:
                        await DeleteFolder(folder);
                        deleteAll = true;
                        break;
                    case Action.DeleteNothing:
                        return false;
                }

                return true;
            }, 
            async folder => await selector.SelectFolderAsync(folder),
            options);

        if (showSizes)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"Total size to be recovered will be [yellow]{totalSize.Bytes().ToFullWords()}[/].");
        }

        return 0;
    }

    private async Task DeleteFolder(string folder)
    {
        AnsiConsole.MarkupLine($"WhatIf: [red][[DELETE]][/] {folder}");
        if (showSizes)
        {
            long size = 0;
            await AnsiConsole.Status()
                .StartAsync("Calculating folder size...", async ctx =>
                {
                    // TODO: we could easily use a callback to get the latest size
                    size = await FolderSizeCalculator.GetFolderSizeAsync(folder);
                    totalSize += size;
                });

            AnsiConsole.MarkupLine($"WhatIf: {folder} consumes [yellow]({size.Bytes().ToFullWords()})[/]");
        }
    }

    private Action Prompt(string folder, Settings settings) =>
        settings.Interactive
            ? AnsiConsole.Prompt(
                    new TextPrompt<string>($"Delete folder [yellow]{folder}[/]?")
                        .InvalidChoiceMessage("Not a valid action")
                        .DefaultValue("yes")
                        .AddChoice("yes")
                        .AddChoice("no")
                        .AddChoice("all")
                        .AddChoice("none")) switch
                {
                    "yes" => Action.Delete,
                    "no" => Action.Keep,
                    "all" => Action.DeleteAll,
                    "none" => Action.DeleteNothing,
                    _ => Action.Unknown,
                }
            : Action.Delete;

    private enum Action
    {
        Delete,
        Keep,
        DeleteAll,
        DeleteNothing,
        Unknown,
    }

    private class FolderSelector
    {
        private IReadOnlyCollection<string> TargetFolders { get; } =
            new HashSet<string>(StringComparer.CurrentCultureIgnoreCase)
            {
                "bin",
                "obj",
                "testresults",
            };

        public Task<bool> SelectFolderAsync(string fullFolderPath)
        {
            // Get the last part of the path (GetFileName will do this) and
            // see if it is in the list of folders to delete
            var folderName = Path.GetFileName(fullFolderPath);
            return Task.FromResult(TargetFolders.Contains(folderName));
        }
    }
}