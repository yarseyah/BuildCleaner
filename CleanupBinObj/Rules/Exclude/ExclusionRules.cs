namespace CleanupBinObj.Rules.Exclude;

using System.Collections.Generic;
using System.Linq;

public class ExclusionRules
{
    private List<IExclusionRule> Rules = new();

    public ExclusionRules()
    {
        Rules.Add(new ExcludeAncestorPathRule());
        Rules.Add(new ExcludeSubtreeRule(".git"));
        Rules.Add(new ExcludeSubtreeRule("node_modules"));
    }

    public Exclusion Enforce(string folder)
    {
        return Rules.Aggregate(
            Exclusion.None,
            (current, rule) => current | rule.ShouldExclude(folder));
    }
}