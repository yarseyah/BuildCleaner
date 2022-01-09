namespace CleanupBinObj.Rules.Exclude;

internal interface IExclusionRule
{
    Exclusion ShouldExclude(string path);
}