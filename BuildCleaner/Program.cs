// Set up configuration with support for Json configuration and environment variables which
// need to be prefixed with "BUILDCLEANER_"
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
serviceCollection.Configure<ExclusionsConfiguration>(config.GetSection("ExclusionRules"));
serviceCollection.AddTransient<ExclusionRules>();

serviceCollection.Configure<FolderRulesConfiguration>(config.GetSection("Folders"));


serviceCollection.AddTransient<IFolderSelector, CSharpBuildFolderSelector>();
serviceCollection.AddTransient<FolderSizeCalculator>();
serviceCollection.AddTransient<RecursiveFolderLocator>();

var consoleApp = serviceCollection.UseSpectreCommandLine();
return await consoleApp.RunAsync(args);