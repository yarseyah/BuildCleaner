namespace BuildCleaner.Commands;

[UsedImplicitly]
public class DeleteCommand(
    ILogger<DeleteCommand> logger,
    RecursiveFolderLocator recursiveFolderLocator,
    IFolderSelector folderSelector,
    FolderSizeCalculator folderSizeCalculator,
    IList<IAccessIssue> accessIssues)
    : DeleteCommandBase(logger, recursiveFolderLocator, folderSelector, folderSizeCalculator, accessIssues)
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
                AccessIssues.Add(new GeneralAccessIssue("Skipped symbolic link", folder));
                return Task.CompletedTask;
            }

            // Prevent deletion outside allowed root
            if (!IsSafePath(folder, settings.RootLocation))
            {
                Logger.LogWarning("Skipping unsafe folder: {Folder}", folder);
                AccessIssues.Add(new GeneralAccessIssue("Skipped unsafe folder", folder));
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
            AccessIssues.Add(new ExceptionAccessIssue(e, folder));
            throw;
        }

        return Task.CompletedTask;
    }
}