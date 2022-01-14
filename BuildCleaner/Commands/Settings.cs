namespace BuildCleaner.Commands;

using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;

public class Settings : CommandSettings
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
    [Description("Show the base folder (defaults to true, inhibit it with --displayFolder false)")]
    [DefaultValue(true)]
    public bool DisplayBaseFolder { get; set; }

    [CommandOption("-i|--interactive|-p|--prompt")]
    [Description("Interactive mode, prompts for confirmation for each folder")]
    [DefaultValue(false)]
    public bool Interactive { get; set; }

    public override ValidationResult Validate()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        return (root: RootLocation, entryAssembly) switch
        {
            { root: ".", entryAssembly: not null } => ValidateEntryPath(entryAssembly),
            { root.Length: > 0 } => EnsurePathExists(),
            _ => ValidationResult.Error("Unspecified issue with root folder supplied"),
        };
    }

    private ValidationResult EnsurePathExists()
    {
        var root = RootLocation.StartsWith('~')
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + RootLocation[1..]
            : RootLocation;

        RootLocation = Path.GetFullPath(root);

        return Directory.Exists(RootLocation)
            ? ValidationResult.Success()
            : ValidationResult.Error($"Unable to find directory '{RootLocation}'");
    }

    private ValidationResult ValidateEntryPath(Assembly entryAssembly)
    {
        if (!string.IsNullOrWhiteSpace(entryAssembly.Location))
        {
            RootLocation = Path.GetDirectoryName(entryAssembly.Location) ?? RootLocation;
            return ValidationResult.Success();
        }

        return ValidationResult.Error("Seem to be missing entry assembly");
    }
}