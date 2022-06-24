namespace BuildCleaner.Setup;

public class FolderRulesConfiguration
{
    public string[] Include { get; set; } = Array.Empty<string>();
    
    public string[] Exclude { get; set; }= Array.Empty<string>();
}