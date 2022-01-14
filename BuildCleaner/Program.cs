using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BuildCleaner.Commands;
using BuildCleaner.Rules.Exclude;
using BuildCleaner.Support;
using Spectre.Console.Cli;
using Spectre.Cli.Extensions.DependencyInjection;

var serviceCollection = new ServiceCollection()
    .AddLogging(configure =>
    {
        configure.AddSimpleConsole(opts =>
        {
            opts.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
        });
        configure.SetMinimumLevel(LogLevel.Error);
    });

// TODO: need to construct this in a more DI way
serviceCollection.AddTransient(
    _ => new IExclusionRule[]
        {
            new ExcludeAncestorPathRule(),
            new ExcludeSubtreeRule(".git"),
            new ExcludeSubtreeRule("node_modules"),
            new ExcludeSymbolicLinks(),
            new ExcludeDotFolders(),
        });
serviceCollection.AddTransient<ExclusionRules>();
serviceCollection.AddTransient<RecursiveFolderLocator>();

using var registrar = new DependencyInjectionRegistrar(serviceCollection);
var app = new CommandApp(registrar);

app.Configure(
    config =>
    {
        config.ValidateExamples();
        config.AddCommand<WhatIfCommand>("whatif")
            .WithDescription("Show the folders to be deleted")
            .WithExample(new[]
            {
                "whatif",
                "."
            });
    });

return await app.RunAsync(args);