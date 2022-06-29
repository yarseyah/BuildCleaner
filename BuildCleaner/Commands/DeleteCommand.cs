namespace BuildCleaner.Commands;

public class DeleteCommand : AbstractDeleteCommand
{
    public DeleteCommand(
        ILogger<DeleteCommand> logger,
        RecursiveFolderLocator recursiveFolderLocator,
        IFolderSelector folderSelector,
        FolderSizeCalculator folderSizeCalculator)
        : base(logger, recursiveFolderLocator, folderSelector, folderSizeCalculator)
    {
        Logger = logger;
    }

    private ILogger Logger { get; }

    protected override void ExplainCommand()
    {
        AnsiConsole.Write(new FigletText("Delete (Build Cleaner)").Centered().Color(Color.Purple));
    }

    protected override string CommandName => "Delete";

    protected override Task PerformCommand(string folder)
    {
        try
        {
            DirectoryInfo di = new(folder);
            if (di.Exists)
            {
                di.Delete(true);
            }
        }
        catch (Exception e)
        {
            AnsiConsole.WriteException(e);
            throw;
        }

        return Task.CompletedTask;
    }
}