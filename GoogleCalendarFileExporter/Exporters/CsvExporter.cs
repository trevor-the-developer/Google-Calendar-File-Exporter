using System.Text;
using GoogleCalendarFileExporter.Classes;

namespace GoogleCalendarFileExporter.Exporters;

public class CsvExporter : AsyncExporterBase
{
    public override void Export(List<CalendarEvent> events, string outputPath)
    {
        ValidateInputs(events, outputPath);
        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
        WriteContent(writer, events);
    }

    public override async Task ExportAsync(List<CalendarEvent> events, string outputPath)
    {
        ValidateInputs(events, outputPath);
        await using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
        await WriteContentAsync(writer, events);
    }

    private void WriteContent(StreamWriter writer, List<CalendarEvent> events)
    {
        // Write header
        writer.WriteLine(
            "Subject,Start Date,Start Time,End Date,End Time,All Day Event,Description,Location,Attendees,Creator,Status,Recurrence,Event ID,Calendar ID,Created,Modified");

        foreach (var evt in events.OrderBy(e => e.StartDateTime))
        {
            var row = BuildCsvRow(evt);
            writer.WriteLine(string.Join(",", row));
        }
    }

    private async Task WriteContentAsync(StreamWriter writer, List<CalendarEvent> events)
    {
        // Write header
        await writer.WriteLineAsync(
            "Subject,Start Date,Start Time,End Date,End Time,All Day Event,Description,Location,Attendees,Creator,Status,Recurrence,Event ID,Calendar ID,Created,Modified");

        foreach (var evt in events.OrderBy(e => e.StartDateTime))
        {
            var row = BuildCsvRow(evt);
            await writer.WriteLineAsync(string.Join(",", row));
        }
    }

    private string[] BuildCsvRow(CalendarEvent evt)
    {
        var startDate = "";
        var startTime = "";
        var endDate = "";
        var endTime = "";
        var allDay = "False";

        if (evt.StartDateTime.HasValue)
        {
            var start = evt.StartDateTime.Value;
            startDate = start.ToString("yyyy-MM-dd");

            if (start.TimeOfDay == TimeSpan.Zero && evt.EndDateTime.HasValue &&
                evt.EndDateTime.Value.TimeOfDay == TimeSpan.Zero)
                allDay = "True";
            else
                startTime = start.ToString("HH:mm:ss");
        }

        if (evt.EndDateTime.HasValue)
        {
            var end = evt.EndDateTime.Value;
            endDate = end.ToString("yyyy-MM-dd");

            if (allDay == "False") endTime = end.ToString("HH:mm:ss");
        }

        var attendees = string.Join("; ", evt.Attendees);

        return new[]
        {
            EscapeCsvField(evt.Summary ?? ""),
            startDate,
            startTime,
            endDate,
            endTime,
            allDay,
            EscapeCsvField(evt.Description ?? ""),
            EscapeCsvField(evt.Location ?? ""),
            EscapeCsvField(attendees),
            EscapeCsvField(evt.Organizer ?? ""),
            evt.Status ?? "",
            EscapeCsvField(evt.RecurrenceRule ?? ""),
            evt.Uid ?? "",
            EscapeCsvField(evt.CalendarName ?? ""),
            evt.Created?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
            evt.LastModified?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""
        };
    }

    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        // Escape quotes by doubling them
        field = field.Replace("\"", "\"\"");

        // If field contains comma, quote, or newline, wrap in quotes
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            return $"\"{field}\"";

        return field;
    }

    public override string GetFileExtension()
    {
        return ".csv";
    }

    public override string GetFormatName()
    {
        return "CSV";
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