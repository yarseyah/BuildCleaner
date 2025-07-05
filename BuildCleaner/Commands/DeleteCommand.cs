namespace BuildCleaner.Commands;

[UsedImplicitly]
public class DeleteCommand(
    ILogger<DeleteCommand> logger,
    RecursiveFolderLocator recursiveFolderLocator,
    IFolderSelector folderSelector,
    FolderSizeCalculator folderSizeCalculator)
    : DeleteCommandBase(logger, recursiveFolderLocator, folderSelector, folderSizeCalculator)
{

    protected override string CommandName => "Delete";

    protected override void ExplainCommand()
    {
        AnsiConsole.Write(new FigletText("Delete (Build Cleaner)").Centered().Color(Color.Purple));
    }

    protected override Task DeleteFolder(DeleteCommandSettings settings, string folder)
    {
        try
        {
            DirectoryInfo di = new(folder);

            // Prevent deletion of symlinks
            if (IsSymlink(di))
            {
                Logger.LogWarning("Skipping symbolic link: {Folder}", folder);
                return Task.CompletedTask;
            }

            // Prevent deletion outside allowed root
            if (!IsSafePath(folder, settings.RootLocation))
            {
                Logger.LogWarning("Skipping unsafe folder: {Folder}", folder);
                return Task.CompletedTask;
            }

            if (di.Exists)
            {
                di.Delete(true);
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Unable to process folder {Folder}", folder);
            AnsiConsole.WriteException(e, ExceptionFormats.ShortenEverything | ExceptionFormats.ShowLinks);
            throw;
        }

        return Task.CompletedTask;
    }
}