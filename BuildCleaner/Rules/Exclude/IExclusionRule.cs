namespace BuildCleaner.Rules.Exclude;

public interface IExclusionRule
{
    Exclusion ShouldExclude(string path);
}