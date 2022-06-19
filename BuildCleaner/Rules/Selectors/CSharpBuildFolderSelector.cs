namespace BuildCleaner.Rules.Selectors;

public class CSharpBuildFolderSelector : IFolderSelector
{
    public CSharpBuildFolderSelector(ILogger<CSharpBuildFolderSelector> logger)
    {
        Logger = logger;
    }

    private ILogger<CSharpBuildFolderSelector> Logger { get; }

    private IReadOnlyCollection<string> TargetFolders { get; } =
        new HashSet<string>(StringComparer.CurrentCultureIgnoreCase)
        {
            "bin",
            "obj",
            "testresults",
        };

    public Task<bool> SelectFolderAsync(string fullFolderPath)
    {
        // Get the last part of the path (GetFileName will do this) and
        // see if it is in the list of folders to delete
        var folderName = Path.GetFileName(fullFolderPath);

        // Is the folder a special name matching the likely folders to delete?
        if (TargetFolders.Contains(folderName))
        {
            // Folder is a candidate for being a CSharp build folder, to be sure get the parent
            // folder of the folder we are looking at (if null, then it can't be a CSharp build folder)
            var parentFolder = Path.GetDirectoryName(fullFolderPath);
            if (parentFolder != null)
            {
                // Get the *.csproj files in the parent folder
                var csprojFiles = Directory.GetFiles(parentFolder, "*.csproj", SearchOption.TopDirectoryOnly);

                if (csprojFiles.Length == 0)
                {
                    Logger.LogDebug(
                        "Folder '{Folder}' does not appear to be a CSharp build folder (no *.csproj files found)",
                        fullFolderPath);
                }
                else if (csprojFiles.Length > 1)
                {
                    Logger.LogWarning("Folder '{Folder}' contains multiple *.csproj files found", fullFolderPath);
                }

                return Task.FromResult(csprojFiles.Any());
            }
        }

        return Task.FromResult(false);
    }
}