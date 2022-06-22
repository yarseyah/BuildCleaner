namespace BuildCleaner.Rules.Exclude;

public class ExcludeAncestorPathRule : IExclusionRule
{
    private const StringComparison Comparison = StringComparison.CurrentCultureIgnoreCase;

    private static readonly string? EntryAssembly =
        Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? string.Empty;

    /// <summary>
    /// Determines whether the specified path is a direct ancestor of the current assembly.
    /// If it is, then we can not include this path, but it is possible for children to be
    /// on a different path, so allow further recursion by returning only Exclusion.ExcludeSelf.
    /// </summary>
    /// <param name="path">Path to test</param>
    /// <returns>An Exclusion result of None or Self.</returns>
    public Exclusion ShouldExclude(string path) =>
        EntryAssembly != null && EntryAssembly.StartsWith(path, Comparison)
            ? Exclusion.ExcludeSelf
            : Exclusion.None;
}