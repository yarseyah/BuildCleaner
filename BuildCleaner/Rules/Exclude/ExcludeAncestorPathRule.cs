namespace BuildCleaner.Rules.Exclude;

using System.IO;
using System.Reflection;

public class ExcludeAncestorPathRule : IExclusionRule
{
    private static readonly string? EntryAssembly =
        Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? string.Empty;

    public Exclusion ShouldExclude(string path) =>
        EntryAssembly != null && EntryAssembly.StartsWith(path)
            ? Exclusion.ExcludeSelf | Exclusion.ExcludeChildren
            : Exclusion.None;
}