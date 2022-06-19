namespace BuildCleaner.Rules.Exclude;

[Flags]
public enum Exclusion
{
    None = 0,

    ExcludeSelf = 1,

    ExcludeChildren = 2,

    ExcludeSelfAndChildren = 3,
}