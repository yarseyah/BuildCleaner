namespace CleanupBinObj.Rules.Exclude;

public interface IExclusionRule
{
    Exclusion ShouldExclude(string path);
}