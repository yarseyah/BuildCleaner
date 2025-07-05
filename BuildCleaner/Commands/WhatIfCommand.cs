namespace BuildCleaner.Commands;

[UsedImplicitly]
public class WhatIfCommand(
    ILogger<WhatIfCommand> logger,
    RecursiveFolderLocator recursiveFolderLocator,
    IFolderSelector folderSelector,
    FolderSizeCalculator folderSizeCalculator,
    IList<IAccessIssue> accessIssues)
    : DeleteCommandBase(logger, recursiveFolderLocator, folderSelector, folderSizeCalculator, accessIssues)
{
    protected override string CommandName => "WhatIf";

    protected override void ExplainCommand()
    {
        AnsiConsole.WriteLine("WhatIf shows the folders that would be deleted when using the 'delete' command");
        AnsiConsole.WriteLine();
    }

    protected override Task DeleteFolder(DeleteCommandSettings settings, string folder)
    {
        // Non operation in WhatIf command
        return Task.CompletedTask;
    }
}