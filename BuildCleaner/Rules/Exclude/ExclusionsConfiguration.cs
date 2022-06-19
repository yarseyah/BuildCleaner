namespace BuildCleaner.Rules.Exclude;

public class ExclusionsConfiguration
{
    public bool ExcludeAncestorPath { get; set; }

    public bool ExcludeSymbolicLinks { get; set; }
    
    public bool ExcludeDotFolders { get; set; }
    
    public string[] ExcludeSubtrees { get; set; } = Array.Empty<string>();
}