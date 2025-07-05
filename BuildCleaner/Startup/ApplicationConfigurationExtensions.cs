namespace BuildCleaner.Startup;

public static class ApplicationConfigurationExtensions
{
    public static IResult<IConfiguration> CreateConfiguration()
    {
        try
        {
            var executingLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                                    ?? Environment.CurrentDirectory;

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(executingLocation)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables("BUILDCLEANER_");

            var config = configurationBuilder.Build();
            return Results.Ok<IConfiguration>(config);
        }
        catch (Exception ex)
        {
            return Results.Fail<IConfiguration>($"Failed to load configuration: {ex.Message}");
        }
    }

    public static IResult<ServiceCollection> ConfigureServices(this IConfiguration configuration)
    {
        try
        {
            var serviceCollection = new ServiceCollection();

            // Add logging support
            serviceCollection.AddLogging(logging =>
            {
                logging.AddConfiguration(configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
            });

            // Configure exclusion rules and folder rules
            serviceCollection.Configure<ExclusionsConfiguration>(configuration.GetSection("Exclude"));
            serviceCollection.Configure<FolderRulesConfiguration>(configuration.GetSection("Folders"));

            // Register application services
            serviceCollection.AddTransient<ExclusionRules>();
            serviceCollection.AddTransient<IFolderSelector, CSharpBuildFolderSelector>();
            serviceCollection.AddTransient<FolderSizeCalculator>();
            serviceCollection.AddTransient<RecursiveFolderLocator>();
            serviceCollection.AddTransient<IList<IAccessIssue>>(_ => new List<IAccessIssue>());

            return Results.Ok(serviceCollection);
        }
        catch (Exception ex)
        {
            return Results.Fail<ServiceCollection>($"Failed to configure services: {ex.Message}");
        }
    }

    public static IResult<CommandApp> CreateCommandApp(this ServiceCollection serviceCollection)
    {
        try
        {
            var consoleApp = serviceCollection.UseSpectreCommandLine();
            return Results.Ok(consoleApp);
        }
        catch (Exception ex)
        {
            return Results.Fail<CommandApp>($"Failed to create command app: {ex.Message}");
        }
    }
}
