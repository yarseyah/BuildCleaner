namespace BuildCleaner.Rules.Selectors;

using BuildCleaner.Setup;
using DotNet.Globbing;

public class CSharpBuildFolderSelector : IFolderSelector
{
    public CSharpBuildFolderSelector(
        ILogger<CSharpBuildFolderSelector> logger,
        IOptions<FolderRulesConfiguration> folderRulesOptions)
    {
        Logger = logger;

        var settings = folderRulesOptions.Value;
        IncludePatterns = settings.Include.Select(Glob.Parse).ToArray();
        ExcludePatterns = settings.Exclude.Select(Glob.Parse).ToArray();
    }

    private Glob[] IncludePatterns { get; }
    
    private Glob[] ExcludePatterns { get; }

    private ILogger<CSharpBuildFolderSelector> Logger { get; }
    
    public Task<bool> SelectFolderAsync(string fullFolderPath)
    {
        // Is the folder a special name matching the likely folders to delete?
        var include = IncludePatterns.Any(g => g.IsMatch(fullFolderPath));
        if (include)
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