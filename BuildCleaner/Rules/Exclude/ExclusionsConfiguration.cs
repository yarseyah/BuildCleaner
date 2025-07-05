namespace BuildCleaner.Rules.Exclude;

public class ExclusionsConfiguration
{
    [UsedImplicitly]
    public bool AncestorPath { get; set; }

    [UsedImplicitly]
    public bool SymbolicLinks { get; set; }
    
    [UsedImplicitly]
    public bool HiddenFolders { get; set; }
}