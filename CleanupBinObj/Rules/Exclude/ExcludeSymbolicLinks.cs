namespace CleanupBinObj.Rules.Exclude;

using System.IO;

public class ExcludeSymbolicLinks : IExclusionRule
{
    public Exclusion ShouldExclude(string path)
    {
        var info = new DirectoryInfo(path);

        var isLink = (info.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;

        return isLink ? Exclusion.ExcludeSelfAndChildren : Exclusion.None;
    }
}