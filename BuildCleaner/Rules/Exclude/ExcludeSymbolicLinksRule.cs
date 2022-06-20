namespace BuildCleaner.Rules.Exclude;

public class ExcludeSymbolicLinksRule : IExclusionRule
{ 
    public Exclusion ShouldExclude(string path)
    {
        var info = new DirectoryInfo(path);
        var isLink = (info.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        return isLink ? Exclusion.ExcludeSelfAndChildren : Exclusion.None;
    }
}