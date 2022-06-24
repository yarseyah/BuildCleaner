namespace BuildCleaner.Rules.Exclude;

public class ExcludeHiddenFoldersRule : IExclusionRule
{ 
    public Exclusion ShouldExclude(string path)
    {
        var info = new DirectoryInfo(path);
        var isHidden = (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
        return isHidden ? Exclusion.ExcludeSelfAndChildren : Exclusion.None;
    }
}