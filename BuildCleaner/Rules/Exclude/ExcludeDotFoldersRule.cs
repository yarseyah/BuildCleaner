namespace BuildCleaner.Rules.Exclude;

public class ExcludeDotFoldersRule : IExclusionRule
{
    public Exclusion ShouldExclude(string path)
    {
        var finalFolder = Path.GetFileName(path);
        return finalFolder.Length > 1 && finalFolder.StartsWith(".")
            ? Exclusion.ExcludeSelfAndChildren
            : Exclusion.None;
    }
}