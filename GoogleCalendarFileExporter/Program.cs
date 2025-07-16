using GoogleCalendarFileExporter.Classes;
using GoogleCalendarFileExporter.Exporters;
using GoogleCalendarFileExporter.Helpers;
using GoogleCalendarFileExporter.Interfaces;
using GoogleCalendarFileExporter.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GoogleCalendarFileExporter;

internal static class Program
{
    private static IServiceProvider _serviceProvider = null!;
    private static ILoggingService _logger = null!;
    private static ProgramHelper _programHelper = null!;

    private static void Main(string[] args)
    {
        // Initialize services
        _serviceProvider = ServiceContainer.ConfigureServices();
        _logger = _serviceProvider.GetRequiredService<ILoggingService>();
        _programHelper = _serviceProvider.GetRequiredService<ProgramHelper>();

        _logger.LogInformation("Google Calendar File Exporter (.NET) - Starting");
        _logger.LogInformation("Supports: .ics files and .zip archives containing .ics files");
        _logger.LogInformation("Output formats: {Formats}", ExporterFactory.GetSupportedFormatsString());

        Console.WriteLine("Google Calendar File Exporter (.NET)");
        Console.WriteLine("====================================");
        Console.WriteLine("Supports: .ics files and .zip archives containing .ics files");
        Console.WriteLine($"Output formats: {ExporterFactory.GetSupportedFormatsString()}");
        Console.WriteLine();

        try
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: google-calendar-file-exporter <file-path> [output-file] [--format <format>]");
                Console.WriteLine("  file-path: Path to .ics file or .zip archive");
                Console.WriteLine(
                    "  output-file: Optional output file (default: calendar_export with detected format)");
                Console.WriteLine($"  --format: Output format ({ExporterFactory.GetSupportedFormatsString()})");
                Console.WriteLine();
                Console.WriteLine("Examples:");
                Console.WriteLine("  dotnet run calendar.ics");
                Console.WriteLine("  dotnet run calendar.ics events.xlsx");
                Console.WriteLine("  dotnet run calendar.ics events.csv --format json");
                Console.WriteLine();
                Console.Write("Enter file path: ");
                var inputPath = Console.ReadLine();

                if (string.IsNullOrEmpty(inputPath))
                {
                    Console.WriteLine("No file specified. Exiting.");
                    return;
                }

                _programHelper.ProcessFile(inputPath, null, null);
            }
            else
            {
                _programHelper.ParseArguments(args);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in main");
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            if (_serviceProvider is IDisposable disposable) disposable.Dispose();
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}