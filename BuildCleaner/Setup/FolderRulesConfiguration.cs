namespace BuildCleaner.Startup;

public class FolderRulesConfiguration
{
    public string[] Include { get; set; } = [];
    
    public string[] Exclude { get; set; }= [];
}