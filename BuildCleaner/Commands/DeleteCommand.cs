namespace BuildCleaner.Commands;

public class DeleteCommand(
    ILogger<DeleteCommand> logger,
    RecursiveFolderLocator recursiveFolderLocator,
    IFolderSelector folderSelector,
    FolderSizeCalculator folderSizeCalculator)
    : AsyncCommand<DeleteCommandSettings>
{
    private bool ShowSizes { get; set; }

    private long TotalSize { get; set; }
    
    public override async Task<int> ExecuteAsync(CommandContext context, DeleteCommandSettings settings)
    {
        logger.LogTrace("Invoked {Command}", CommandName);
        ExplainCommand();
        
        var options = new RecursiveFolderLocator.Options
        {
            DisplayAccessErrors = settings.DisplayAccessErrors,
            DisplayBaseFolder = settings.DisplayBaseFolder
        };

        var deleteAll = false;
        ShowSizes = settings.ShowSizes;

        // Configure the 'activity' response from users, if non-interactive, default to 'Delete'
        Func<string, Activity> activity = settings.Interactive ? Prompt : _ => Activity.Delete;

        await recursiveFolderLocator.VisitAsync(
            settings.RootLocation,
            async (folder) =>
            {
                logger.LogTrace("Visiting {Folder}", folder);

                // Get the desired activity for this folder, if 'all' has previously been selected,
                // short-circuit the prompt and return 'Delete'
                var action = deleteAll ? Activity.Delete : activity(folder);
                deleteAll |= (action == Activity.DeleteAll);

                if (action is Activity.Delete or Activity.DeleteAll)
                {
                    var size = 0L;
                    if (ShowSizes)
                    {
                        await AnsiConsole.Status()
                            .StartAsync("Calculating folder size...", async _ =>
                            {
                                // TODO: we could easily use a callback to get the latest size
                                size = await folderSizeCalculator.GetFolderSizeAsync(folder);
                                TotalSize += size;
                            });

                        var sizeOutput = size == 0 ? "-" : FormatMb(size);
                        AnsiConsole.MarkupLine($"{CommandName}: [yellow]{sizeOutput,12}[/] : [red][[DELETE]][/] {folder}");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"{CommandName}: [red][[DELETE]][/] {folder}");
                    }

                    try
                    {
                        await DeleteFolder(settings, folder);
                    }
                    catch (Exception)
                    {
                        logger.LogError("Unable to process folder {Folder}", folder);
                    }
                }
                else if (action == Activity.DeleteNothing)
                {
                    return false;
                }

                return true;
            },
            async folder => await folderSelector.SelectFolderAsync(folder),
            options);

        if (ShowSizes)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"Space recovered [yellow]{FormatMb(TotalSize)}[/].");
        }

        return 0;
    }

    protected virtual void ExplainCommand()
    {
        AnsiConsole.Write(new FigletText("Delete (Build Cleaner)").Centered().Color(Color.Purple));
    }

    protected virtual string CommandName => "Delete";

    protected virtual Task DeleteFolder(DeleteCommandSettings settings, string folder)
    {
        try
        {
            DirectoryInfo di = new(folder);

            // Prevent deletion of symlinks
            if (IsSymlink(di))
            {
                logger.LogWarning("Skipping symbolic link: {Folder}", folder);
                return Task.CompletedTask;
            }

            // Prevent deletion outside allowed root
            if (!IsSafePath(folder, settings.RootLocation))
            {
                logger.LogWarning("Skipping unsafe folder: {Folder}", folder);
                return Task.CompletedTask;
            }

            if (di.Exists)
            {
                di.Delete(true);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unable to process folder {Folder}", folder);
            AnsiConsole.WriteException(e, ExceptionFormats.ShortenEverything | ExceptionFormats.ShowLinks);
            throw;
        }

        return Task.CompletedTask;
    }

    // Checks if the directory is a symbolic link
    private static bool IsSymlink(DirectoryInfo di)
    {
        return di.Attributes.HasFlag(FileAttributes.ReparsePoint);
    }

    // Checks if the folder is within the allowed root
    private bool IsSafePath(string folder, string rootLocation)
    {
        // Only allow deletion within a specific root directory
        // settings.RootLocation is available in ExecuteAsync scope, so pass it as a field if needed
        // For now, assume CommandContext or settings.RootLocation is accessible
        var allowedRoot = Path.GetFullPath(rootLocation);
        var fullPath = Path.GetFullPath(folder);
        return fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase);
    }

    protected static Activity Prompt(string folder) =>
        AnsiConsole.Prompt(
                new TextPrompt<string>($"Delete folder [yellow]{folder}[/]?")
                    .InvalidChoiceMessage("Not a valid action")
                    .DefaultValue("yes")
                    .AddChoice("yes")
                    .AddChoice("no")
                    .AddChoice("all")
                    .AddChoice("none")) switch
            {
                "yes" => Activity.Delete,
                "no" => Activity.Keep,
                "all" => Activity.DeleteAll,
                "none" => Activity.DeleteNothing,
                _ => Activity.Unknown,
            };

    private static string FormatMb(long bytes) => $"{(bytes / 1024.0 / 1024.0):0.##}mb";

    protected enum Activity
    {
        Delete,
        Keep,
        DeleteAll,
        DeleteNothing,
        Unknown,
    }
}