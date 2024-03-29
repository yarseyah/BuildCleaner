﻿namespace BuildCleaner.Commands;

public class DeleteCommandSettings : CommandSettings
{
    [CommandArgument(0, "[path]")]
    [Description("Starting path perform operation against")]
    [DefaultValue(".")]
    public string RootLocation { get; set; } = ".";

    [CommandOption("-e|--displayAccessErrors")]
    [Description("List any folders that could not be accessed")]
    [DefaultValue(false)]
    public bool DisplayAccessErrors { get; set; }

    [CommandOption("-d|--displayFolder")]
    [Description("Show the base folder")]
    [DefaultValue(false)]
    public bool DisplayBaseFolder { get; set; }

    [CommandOption("-i|--interactive|-p|--prompt")]
    [Description("Interactive mode, prompts for confirmation for each folder")]
    [DefaultValue(false)]
    public bool Interactive { get; set; }
    
    [CommandOption("-s|--showSizes")]
    [Description("Show the size of each folder")]
    [DefaultValue(false)]
    public bool ShowSizes { get; set; }

    public override ValidationResult Validate()
    {
        return RootLocation is { Length: > 0 }
            ? EnsurePathExists()
            : ValidationResult.Error("Unspecified issue with root folder supplied");
    }

    private ValidationResult EnsurePathExists()
    {
        var root = RootLocation.StartsWith('~')
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + RootLocation[1..]
            : RootLocation == "."
                ? Directory.GetCurrentDirectory()
                : RootLocation;

        RootLocation = Path.GetFullPath(root);

        return Directory.Exists(RootLocation)
            ? ValidationResult.Success()
            : ValidationResult.Error($"Unable to find directory '{RootLocation}'");
    }
}