using GoogleCalendarFileExporter.Classes;
using GoogleCalendarFileExporter.Exporters;
using GoogleCalendarFileExporter.Helpers;
using GoogleCalendarFileExporter.Interfaces;
using GoogleCalendarFileExporter.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoogleCalendarFileExporter.Services;

public static class ServiceContainer
{
    public static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Register services
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<ConfigurationService>();
        services.AddSingleton<ProgramHelper>();
        services.AddSingleton<TimezoneService>();
        services.AddSingleton<IcsParser>();
        services.AddTransient<IFileProcessor, FileProcessor>();
        services.AddTransient<IExporter, CsvExporter>();
        services.AddTransient<IExporter, JsonExporter>();
        services.AddTransient<IExporter, ExcelExporter>();
        services.AddTransient<IExporter, XmlExporter>();

        return services.BuildServiceProvider();
    }

    public static IServiceProvider ConfigureServicesWithConfiguration(ExporterConfiguration configuration)
    {
        var services = new ServiceCollection();

        // Configure logging based on configuration
        services.AddLogging(builder =>
        {
            if (configuration.Logging.EnableConsoleLogging) builder.AddConsole();

            var logLevel = Enum.TryParse<LogLevel>(configuration.Logging.LogLevel, out var level)
                ? level
                : LogLevel.Information;
            builder.SetMinimumLevel(logLevel);
        });

        // Register configuration
        services.AddSingleton(configuration);

        // Register services
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<ConfigurationService>(provider =>
            new ConfigurationService(provider.GetRequiredService<ILoggingService>()));
        services.AddSingleton<TimezoneService>();
        services.AddSingleton<IcsParser>();
        services.AddTransient<IFileProcessor, FileProcessor>();
        services.AddTransient<IExporter, CsvExporter>();
        services.AddTransient<IExporter, JsonExporter>();
        services.AddTransient<IExporter, ExcelExporter>();
        services.AddTransient<IExporter, XmlExporter>();

        return services.BuildServiceProvider();
    }
}