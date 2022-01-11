namespace CleanupBinObj.Rules.Exclude;

using System.IO;

public class ExcludeDotFolders : IExclusionRule
{
    public Exclusion ShouldExclude(string path)
    {
        var finalFolder = Path.GetFileName(path);
        return finalFolder.Length > 1 && finalFolder.StartsWith(".")
            ? Exclusion.ExcludeSelfAndChildren
            : Exclusion.None;
    }
}