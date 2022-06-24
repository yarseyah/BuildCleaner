namespace BuildCleaner.Rules.Exclude;

using DotNet.Globbing;

public class ExcludeFoldersRule : IExclusionRule
{
    public ExcludeFoldersRule(string globPattern)
    {
        Matcher = Glob.Parse(globPattern);
    }

    private Glob Matcher { get; }

    public Exclusion ShouldExclude(string path)
    {
        return Matcher.IsMatch(path)
            ? Exclusion.ExcludeSelfAndChildren
            : Exclusion.None;
    }
}