namespace BuildCleaner.Commands;

using System.Threading.Tasks;
using BuildCleaner.Support;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

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

        RecursiveFolderLocator.Visit(
            settings.RootLocation,
            (folder) =>
            {
                if (!settings.Interactive || AnsiConsole.Confirm($"Delete folder {folder}?"))
                {
                    AnsiConsole.MarkupLine($"WhatIf: [red][[DELETE]][/] {folder}");
                }
            },
            options);

        return Task.FromResult(0);
    }
}