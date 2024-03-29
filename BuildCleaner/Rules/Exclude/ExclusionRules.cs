﻿namespace BuildCleaner.Rules.Exclude;

using BuildCleaner.Setup;

public class ExclusionRules
{
    private readonly List<IExclusionRule> Rules = new();

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
            Rules.Add(new ExcludeAncestorPathRule());
        }

        if (settings.SymbolicLinks)
        {
            Logger.LogTrace("Adding symbolic link exclusion rule");
            Rules.Add(new ExcludeSymbolicLinksRule());
        }

        if (settings.HiddenFolders)
        {
            Logger.LogTrace("Adding hidden folder exclusion rule");
            Rules.Add(new ExcludeHiddenFoldersRule());
        }

        foreach (var patternToExclude in folderRulesConfiguration.Value.Exclude)
        {
            Rules.Add(new ExcludeFoldersRule(patternToExclude));
        }
    }

    private ILogger<ExclusionRules> Logger { get; }

    public Exclusion Enforce(string folder) => Rules.Aggregate(Exclusion.None,
        (incoming, rule) => incoming | rule.ShouldExclude(folder));
    
    public record ExceptionDetails(string Folder, Type Type, Exception Exception);
}