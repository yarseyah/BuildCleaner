namespace BuildCleaner.Support;

public class FolderSizeCalculator
{
    public FolderSizeCalculator(ILogger<FolderSizeCalculator> logger)
    {
        Logger = logger;
    }

    private ILogger<FolderSizeCalculator> Logger { get; }

    public async Task<long> GetFolderSizeAsync(string folderPath)
    {
        long size = 0;
        
        foreach (var file in Directory.GetFiles(folderPath))
        {
            try
            {
                var fileInfo = new FileInfo(file);
                size += fileInfo.Length;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Could not get file size for file: {FileName}", file);
            }
        }

        foreach (string subFolder in Directory.GetDirectories(folderPath))
        {
            size += await GetFolderSizeAsync(subFolder);
        }

        return size;
    }
}