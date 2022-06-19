// Set up configuration with support for Json configuration and environment variables which
// need to be prefixed with "BUILDCLEANER_"

using BuildCleaner.Rules.Selectors;

var configurationBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables("BUILDCLEANER_");

var config = configurationBuilder.Build();

// Build up the DI container
var serviceCollection = new ServiceCollection();
    
// Add logging support to DI container
serviceCollection.AddLogging(logging =>
{
    logging.AddConfiguration(config.GetSection("Logging"));
    logging.AddConsole();
    logging.AddDebug();
});

// Configure the exclusion rules
serviceCollection.Configure<ExclusionsConfiguration>(
    options => config
        .GetSection("ExclusionRules")
        .Bind(options));
serviceCollection.AddTransient<ExclusionRules>();

serviceCollection.AddTransient<CSharpBuildFolderSelector>();
serviceCollection.AddTransient<FolderSizeCalculator>();
serviceCollection.AddTransient<RecursiveFolderLocator>();

using var registrar = new DependencyInjectionRegistrar(serviceCollection);
var app = new CommandApp(registrar);

app.Configure(
    configurator =>
    {
        configurator.ValidateExamples();
        configurator.AddCommand<WhatIfCommand>("whatif")
            .WithDescription("Show the folders to be deleted")
            .WithExample(new[]
            {
                "whatif",
                "."
            });
    });

return await app.RunAsync(args);