namespace BuildCleaner.Rules.Exclude;

public class ExclusionRules
{
    private readonly List<IExclusionRule> Rules = new();

    public ExclusionRules(
        IOptions<ExclusionsConfiguration> configuration,
        ILogger<ExclusionRules> logger)
    {
        Logger = logger;
        var settings = configuration.Value;

        // if (settings.ExcludeAncestorPath)
        // {
        //     Logger.LogTrace("Adding ancestor path exclusion rule");
        //     Rules.Add(new ExcludeAncestorPathRule());
        // }

        if (settings.ExcludeSymbolicLinks)
        {
            Logger.LogTrace("Adding symbolic link exclusion rule");
            Rules.Add(new ExcludeSymbolicLinksRule());
        }

        if (settings.ExcludeDotFolders)
        {
            Logger.LogTrace("Adding dot folder exclusion rule");
            Rules.Add(new ExcludeDotFoldersRule());
        }

        foreach (var subtree in settings.ExcludeSubtrees)
        {
            Logger.LogTrace("Adding subtree exclusion rule: {Name}", subtree);
            Rules.Add(new ExcludeSubtreeRule(subtree));
        }
    }

    private ILogger<ExclusionRules> Logger { get; }

    public Exclusion Enforce(string folder, Exclusion onException)
    {
        return Rules.Aggregate(Exclusion.None, (incoming, rule) =>
        {
            try
            {
                return incoming | rule.ShouldExclude(folder);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error processing rule {Rule}", rule.GetType().Name);
                return onException;
            }
        });
    }
}