namespace BuildCleaner;

public static class ConsoleCommandLineExtensions
{
    public static CommandApp UseSpectreCommandLine(this ServiceCollection serviceCollection)
    {
        using var registrar = new DependencyInjectionRegistrar(serviceCollection);
        var commandApp = new CommandApp(registrar);

        commandApp.Configure(
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
                configurator.SetExceptionHandler(exception => AnsiConsole.WriteException(exception));
            });
        return commandApp;
    }
}