namespace BuildCleaner.Commands;

using System.Threading.Tasks;
using BuildCleaner.Commands.Settings;
using BuildCleaner.Support;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

public class WhatIfCommand : AsyncCommand<WhatIfSettings>
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

    public override Task<int> ExecuteAsync(CommandContext context, WhatIfSettings settings)
    {
        AnsiConsole.WriteLine("WhatIf shows the folders the would be deleted when using the 'delete' command");
        AnsiConsole.WriteLine();

        RecursiveFolderLocator.Visit(settings.RootLocation, AnsiConsole.WriteLine);

        return Task.FromResult(0);
    }

}