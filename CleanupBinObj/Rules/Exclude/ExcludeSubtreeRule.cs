namespace CleanupBinObj.Rules.Exclude;

using System;
using System.IO;

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