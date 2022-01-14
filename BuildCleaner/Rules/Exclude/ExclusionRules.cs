namespace BuildCleaner.Rules.Exclude;

using System.Collections.Generic;
using System.Linq;

public class ExclusionRules
{
    private List<IExclusionRule> Rules = new();

    public ExclusionRules(IExclusionRule[] rules)
    {
        Rules.AddRange(rules);
    }

    public Exclusion Enforce(string folder)
    {
        return Rules.Aggregate(
            Exclusion.None,
            (current, rule) => current | rule.ShouldExclude(folder));
    }
}