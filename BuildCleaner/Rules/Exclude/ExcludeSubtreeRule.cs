namespace BuildCleaner.Rules.Exclude;

public class ExcludeSubtreeRule : IExclusionRule
{
    public ExcludeSubtreeRule(string name)
    {
        Name = name;
    }

    private string Name { get; }

    public Exclusion ShouldExclude(string path) =>
        Path.GetFileName(path).Equals(Name, StringComparison.OrdinalIgnoreCase)
            ? Exclusion.ExcludeSelf | Exclusion.ExcludeChildren
            : Exclusion.None;
}