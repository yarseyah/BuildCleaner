using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CleanupBinObj.Commands;
using CleanupBinObj.Rules.Exclude;
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

serviceCollection.AddTransient<ExclusionRules>();

using var registrar = new DependencyInjectionRegistrar(serviceCollection);
var app = new CommandApp(registrar);

app.Configure(
    config =>
    {
        config.ValidateExamples();

        config.AddCommand<ConsoleCommand>("console")
            .WithDescription("Example console command.")
            .WithExample(new[]
            {
                "console"
            });

        config.AddCommand<WhatIfCommand>("whatif")
            .WithDescription("Show the folders to be deleted")
            .WithExample(new[]
            {
                "whatif",
                "."
            });
    });

return await app.RunAsync(args);