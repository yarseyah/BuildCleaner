namespace BuildCleaner.Commands;

using System.Diagnostics.CodeAnalysis;
using BuildCleaner.Rules.Selectors;

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

        await RecursiveFolderLocator.VisitAsync(
            settings.RootLocation, async (folder) =>
            {
                var action = deleteAll
                    ? Action.Delete
                    : Prompt(folder, settings);

                switch (action)
                {
                    case Action.Delete:
                        await WhatIfDeleteFolder(folder);
                        break;
                    case Action.DeleteAll:
                        await WhatIfDeleteFolder(folder);
                        deleteAll = true;
                        break;
                    case Action.DeleteNothing:
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
}