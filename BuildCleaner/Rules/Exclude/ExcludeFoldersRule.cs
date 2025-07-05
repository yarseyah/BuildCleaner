namespace BuildCleaner.Rules.Exclude;

public class ExcludeFoldersRule(string globPattern) : IExclusionRule
{
    private Glob Matcher { get; } = Glob.Parse(globPattern);

    public Exclusion ShouldExclude(string path)
    {
        return Matcher.IsMatch(path)
            ? Exclusion.ExcludeSelfAndChildren
            : Exclusion.None;
    }
}