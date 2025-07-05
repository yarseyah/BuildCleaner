namespace BuildCleaner.Rules.Exclude;

public class ExclusionRules
{
    private readonly List<IExclusionRule> rules = [];

    public ExclusionRules(
        IOptions<ExclusionsConfiguration> configuration,
        IOptions<FolderRulesConfiguration> folderRulesConfiguration,
        ILogger<ExclusionRules> logger)
    {
        Logger = logger;
        var settings = configuration.Value;

        if (settings.AncestorPath)
        {
            Logger.LogTrace("Adding ancestor path exclusion rule");
            rules.Add(new ExcludeAncestorPathRule());
        }

        if (settings.SymbolicLinks)
        {
            Logger.LogTrace("Adding symbolic link exclusion rule");
            rules.Add(new ExcludeSymbolicLinksRule());
        }

        if (settings.HiddenFolders)
        {
            Logger.LogTrace("Adding hidden folder exclusion rule");
            rules.Add(new ExcludeHiddenFoldersRule());
        }

        foreach (var patternToExclude in folderRulesConfiguration.Value.Exclude)
        {
            rules.Add(new ExcludeFoldersRule(patternToExclude));
        }
    }

    private ILogger<ExclusionRules> Logger { get; }

    public Exclusion Enforce(string folder) => rules.Aggregate(Exclusion.None,
        (incoming, rule) => incoming | rule.ShouldExclude(folder));
    
    public record ExceptionDetails(string Folder, Type Type, Exception Exception);
}