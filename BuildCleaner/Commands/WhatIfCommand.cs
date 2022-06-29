namespace BuildCleaner.Commands;

[SuppressMessage(
    "ReSharper",
    "ClassNeverInstantiated.Global",
    Justification = "Invoked by Spectre.Console for 'WhatIf' command options")]
public class WhatIfCommand : AbstractDeleteCommand
{
    public WhatIfCommand(
        ILogger<WhatIfCommand> logger,
        RecursiveFolderLocator recursiveFolderLocator,
        IFolderSelector folderSelector,
        FolderSizeCalculator folderSizeCalculator)
        : base(logger, recursiveFolderLocator, folderSelector, folderSizeCalculator)
    {
    }

    protected override string CommandName => "WhatIf";

    protected override void ExplainCommand()
    {
        AnsiConsole.WriteLine("WhatIf shows the folders the would be deleted when using the 'delete' command");
        AnsiConsole.WriteLine();
    }

    protected override Task PerformCommand(string folder)
    {
        // Non operation in WhatIf command
        return Task.CompletedTask;
    }
}