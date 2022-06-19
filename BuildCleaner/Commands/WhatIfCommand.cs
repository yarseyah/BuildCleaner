namespace BuildCleaner.Commands;

public class WhatIfCommand : AsyncCommand<Settings>
{
    public WhatIfCommand(
        ILogger<WhatIfCommand> logger,
        RecursiveFolderLocator recursiveFolderLocator)
    {
        Logger = logger;
        RecursiveFolderLocator = recursiveFolderLocator;
    }

    private ILogger<WhatIfCommand> Logger { get; }

    private RecursiveFolderLocator RecursiveFolderLocator { get; }

    public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AnsiConsole.WriteLine("WhatIf shows the folders the would be deleted when using the 'delete' command");
        AnsiConsole.WriteLine();

        var options = RecursiveFolderLocator.DefaultOptions;
        options.DisplayAccessErrors = settings.DisplayAccessErrors;
        options.DisplayBaseFolder = settings.DisplayBaseFolder;

        var deleteAll = false;

        RecursiveFolderLocator.Visit(
            settings.RootLocation,
            (folder) =>
            {
                var action = deleteAll
                    ? Action.Delete
                    : Prompt(folder, settings);

                switch (action)
                {
                    case Action.Delete:
                        AnsiConsole.MarkupLine($"WhatIf: [red][[DELETE]][/] {folder}");
                        break;
                    case Action.DeleteAll:
                        AnsiConsole.MarkupLine($"WhatIf: [red][[DELETE]][/] {folder}");
                        deleteAll = true;
                        break;
                    case Action.DeleteNothing:
                        return false;
                }

                return true;
            },
            options);

        return Task.FromResult(0);
    }

    private Action Prompt(string folder, Settings settings) =>
        settings.Interactive
            ? AnsiConsole.Prompt(
                    new TextPrompt<string>($"Delete folder [yellow]{folder}[/]?")
                        .InvalidChoiceMessage("Not a valid action")
                        .DefaultValue("yes")
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