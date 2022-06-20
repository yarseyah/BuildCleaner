namespace BuildCleaner.Rules.Exclude;

public class ExcludeSymbolicLinksRule : IExclusionRule
{
    public ExcludeSymbolicLinksRule()
    {
        // TODO: need to use DI to create these
        // Logger = logger;
    }

    public Exclusion ShouldExclude(string path)
    {
        try
        {
            var info = new DirectoryInfo(path);
            var isLink = (info.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
            return isLink ? Exclusion.ExcludeSelfAndChildren : Exclusion.None;
        }
        catch (Exception)
        {
            // Logger.LogDebug("Found a {ExceptionType} whilst examining {Path}", e.GetType().Name, path);
            return Exclusion.ExcludeSelfAndChildren;
        }
    }
}