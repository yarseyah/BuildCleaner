namespace BuildCleaner.Commands;

[SuppressMessage(
    "ReSharper",
    "ClassNeverInstantiated.Global",
    Justification = "Invoked by Spectre.Console for 'WhatIf' command options")]
public class WhatIfCommand(
    ILogger<WhatIfCommand> logger,
    RecursiveFolderLocator recursiveFolderLocator,
    IFolderSelector folderSelector,
    FolderSizeCalculator folderSizeCalculator)
    : DeleteCommand(logger, recursiveFolderLocator, folderSelector, folderSizeCalculator)
{
    protected override string CommandName => "WhatIf";

    protected override void ExplainCommand()
    {
        AnsiConsole.WriteLine("WhatIf shows the folders the would be deleted when using the 'delete' command");
        AnsiConsole.WriteLine();
    }

    protected override Task DeleteFolder(DeleteCommandSettings settings, string folder)
    {
        // Non operation in WhatIf command
        return Task.CompletedTask;
    }
}