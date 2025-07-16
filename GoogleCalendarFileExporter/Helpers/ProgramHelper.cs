using GoogleCalendarFileExporter.Classes;
using GoogleCalendarFileExporter.Exporters;
using GoogleCalendarFileExporter.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GoogleCalendarFileExporter.Helpers;

public class ProgramHelper
{
    private readonly ILoggingService _logger;
    private readonly IServiceProvider _serviceProvider;

    public ProgramHelper(ILoggingService logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    
    public void ParseArguments(string[] args)
    {
        var inputPath = args[0];
        string? outputPath = null;
        string? format = null;

        // Parse additional arguments
        for (var i = 1; i < args.Length; i++)
            if (args[i] == "--format" && i + 1 < args.Length)
            {
                format = args[i + 1];
                i++; // Skip the format value
            }
            else if (!args[i].StartsWith("--") && outputPath == null)
            {
                outputPath = args[i];
            }

        ProcessFile(inputPath, outputPath, format);
    }

    public void ProcessFile(string inputPath, string? outputPath, string? format)
    {
        try
        {
            if (!File.Exists(inputPath))
            {
                var errorMsg = $"Error: File '{inputPath}' not found.";
                _logger.LogError(errorMsg);
                Console.WriteLine(errorMsg);
                return;
            }

            var extension = Path.GetExtension(inputPath).ToLowerInvariant();
            List<CalendarEvent> events;

            // Get services from DI container
            var fileProcessor = _serviceProvider.GetRequiredService<IFileProcessor>();

            _logger.LogInformation("Processing file: {FilePath}", inputPath);

            switch (extension)
            {
                case ".zip":
                    events = fileProcessor.ProcessZipFile(inputPath);
                    break;
                case ".ics":
                    events = fileProcessor.ProcessIcsFile(inputPath);
                    break;
                default:
                {
                    var errorMsg = $"Error: Unsupported file type '{extension}'. Please use .ics or .zip files.";
                    _logger.LogError(errorMsg);
                    Console.WriteLine(errorMsg);
                    return;
                }
            }

            if (events.Count == 0)
            {
                const string msg = "No calendar events found in the file.";
                _logger.LogWarning(msg);
                Console.WriteLine(msg);
                return;
            }

            _logger.LogInformation("Found {EventCount} events", events.Count);

            // Determine output format and path
            IExporter exporter;
            if (!string.IsNullOrEmpty(format))
                try
                {
                    exporter = ExporterFactory.CreateExporter(format);
                }
                catch (ArgumentException ex)
                {
                    _logger.LogError(ex, "Invalid format specified: {Format}", format);
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine($"Supported formats: {ExporterFactory.GetSupportedFormatsString()}");
                    return;
                }
            else if (!string.IsNullOrEmpty(outputPath))
                try
                {
                    exporter = ExporterFactory.CreateExporterFromFilePath(outputPath);
                }
                catch (ArgumentException ex)
                {
                    _logger.LogError(ex, "Invalid output path format: {OutputPath}", outputPath);
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine($"Supported formats: {ExporterFactory.GetSupportedFormatsString()}");
                    return;
                }
            else
                // Default to CSV
                exporter = new CsvExporter();

            // Generate output path if not provided
            if (string.IsNullOrEmpty(outputPath))
            {
                var baseFileName = Path.GetFileNameWithoutExtension(inputPath);
                if (baseFileName.EndsWith(".ical", StringComparison.OrdinalIgnoreCase))
                    baseFileName = baseFileName.Substring(0, baseFileName.Length - 5);
                outputPath = $"calendar_export_{baseFileName}{exporter.GetFileExtension()}";
            }

            _logger.LogInformation("Exporting {EventCount} events to {Format} format at {OutputPath}",
                events.Count, exporter.GetFormatName(), outputPath);

            Console.WriteLine($"Found {events.Count} events. Exporting to {exporter.GetFormatName()}...");
            exporter.Export(events, outputPath);

            _logger.LogInformation("Export completed successfully: {OutputPath}", outputPath);
            Console.WriteLine($"Export completed! Data saved to {outputPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file: {InputPath}", inputPath);
            Console.WriteLine($"Error processing file: {ex.Message}");
        }
    }
}