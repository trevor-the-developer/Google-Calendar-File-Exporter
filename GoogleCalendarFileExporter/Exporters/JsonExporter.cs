using System.Text;
using GoogleCalendarFileExporter.Classes;
using Newtonsoft.Json;

namespace GoogleCalendarFileExporter.Exporters;

public class JsonExporter : AsyncExporterBase
{
    public override void Export(List<CalendarEvent> events, string outputPath)
    {
        ValidateInputs(events, outputPath);
        var sortedEvents = events.OrderBy(e => e.StartDateTime).ToList();

        var json = JsonConvert.SerializeObject(sortedEvents, Formatting.Indented, new JsonSerializerSettings
        {
            DateFormatString = "yyyy-MM-ddTHH:mm:ss",
            NullValueHandling = NullValueHandling.Ignore
        });

        File.WriteAllText(outputPath, json, Encoding.UTF8);
    }

    public override async Task ExportAsync(List<CalendarEvent> events, string outputPath)
    {
        ValidateInputs(events, outputPath);
        var sortedEvents = events.OrderBy(e => e.StartDateTime).ToList();

        var json = await Task.Run(() => JsonConvert.SerializeObject(sortedEvents, Formatting.Indented,
            new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-ddTHH:mm:ss",
                NullValueHandling = NullValueHandling.Ignore
            }));

        await WriteAllTextAsync(outputPath, json);
    }

    public override string GetFileExtension()
    {
        return ".json";
    }

    public override string GetFormatName()
    {
        return "JSON";
    }

    private void ValidateInputs(List<CalendarEvent> events, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(events);

        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));

        try
        {
            var fullPath = Path.GetFullPath(outputPath);
            var directory = Path.GetDirectoryName(fullPath);

            // Check if the directory exists
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                throw new DirectoryNotFoundException($"Directory does not exist: {directory}");
        }
        catch (ArgumentException ex)
        {
            throw new DirectoryNotFoundException($"Invalid output path: {outputPath}", ex);
        }
        catch (NotSupportedException ex)
        {
            throw new DirectoryNotFoundException($"Invalid output path: {outputPath}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new DirectoryNotFoundException($"Invalid output path: {outputPath}", ex);
        }
    }
}