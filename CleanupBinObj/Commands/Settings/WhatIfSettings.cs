namespace CleanupBinObj.Commands.Settings;

using System.ComponentModel;
using System.IO;
using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;

public class WhatIfSettings : CommandSettings
{
    [CommandArgument(0, "<path>")]
    [Description("Starting path perform operation against")]
    public string RootLocation { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        return (root: RootLocation, entryAssembly) switch
        {
            { root: ".", entryAssembly: not null } => ValidateEntryPath(entryAssembly),
            { root.Length: > 0 } when Directory.Exists(RootLocation) => EnsurePathIsAbsolute(),
            { root.Length: > 0 } =>
                ValidationResult.Error($"Unable to find directory '{RootLocation}'"),
            _ => ValidationResult.Error("Unspecified issue with root folder supplied"),
        };
    }

    private ValidationResult EnsurePathIsAbsolute()
    {
        RootLocation = Path.GetFullPath(RootLocation);
        return ValidationResult.Success();
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