return await ApplicationConfigurationExtensions.CreateConfiguration()
    .Bind(config => config.ConfigureServices())
    .Bind(services => services.CreateCommandApp())
    .MatchAsync(
        onSuccess: app => app.RunAsync(args),
        onFailure: error => {
            Console.WriteLine($"Failed to start BuildCleaner: {error}");
            Console.WriteLine("Please check your configuration and try again.");
            return Task.FromResult(1);
        });
