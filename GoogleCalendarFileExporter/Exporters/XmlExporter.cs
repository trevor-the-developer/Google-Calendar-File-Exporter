using System.Text;
using System.Xml;
using GoogleCalendarFileExporter.Classes;

namespace GoogleCalendarFileExporter.Exporters;

public class XmlExporter : AsyncExporterBase
{
    public override void Export(List<CalendarEvent> events, string outputPath)
    {
        ValidateInputs(events, outputPath);
        var sortedEvents = events.OrderBy(e => e.StartDateTime).ToList();

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = Encoding.UTF8
        };

        using var writer = XmlWriter.Create(outputPath, settings);
        writer.WriteStartDocument();
        writer.WriteStartElement("CalendarEvents");

        foreach (var evt in sortedEvents)
        {
            writer.WriteStartElement("Event");

            WriteElementSafe(writer, "Summary", evt.Summary);
            WriteElementSafe(writer, "Description", evt.Description);
            WriteElementSafe(writer, "Location", evt.Location);
            WriteElementSafe(writer, "Uid", evt.Uid);
            WriteElementSafe(writer, "Status", evt.Status);
            WriteElementSafe(writer, "Organizer", evt.Organizer);
            WriteElementSafe(writer, "RecurrenceRule", evt.RecurrenceRule);
            WriteElementSafe(writer, "CalendarName", evt.CalendarName);

            if (evt.StartDateTime.HasValue)
            {
                writer.WriteElementString("StartDateTime", evt.StartDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
                writer.WriteElementString("StartDate", evt.StartDateTime.Value.ToString("yyyy-MM-dd"));
                writer.WriteElementString("StartTime", evt.StartDateTime.Value.ToString("yyyy-MM-dd HH:mm:ss"));
            }

            if (evt.EndDateTime.HasValue)
            {
                writer.WriteElementString("EndDateTime", evt.EndDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
                writer.WriteElementString("EndDate", evt.EndDateTime.Value.ToString("yyyy-MM-dd"));
                writer.WriteElementString("EndTime", evt.EndDateTime.Value.ToString("yyyy-MM-dd HH:mm:ss"));
            }

            if (evt.Created.HasValue)
                writer.WriteElementString("Created", evt.Created.Value.ToString("yyyy-MM-dd HH:mm:ss"));

            if (evt.LastModified.HasValue)
                writer.WriteElementString("LastModified", evt.LastModified.Value.ToString("yyyy-MM-dd HH:mm:ss"));

            // Check if it's an all-day event
            var isAllDay = evt.StartDateTime.HasValue && evt.EndDateTime.HasValue &&
                           evt.StartDateTime.Value.TimeOfDay == TimeSpan.Zero &&
                           evt.EndDateTime.Value.TimeOfDay == TimeSpan.Zero;
            writer.WriteElementString("AllDayEvent", isAllDay ? "True" : "False");

            // Always create attendees element
            writer.WriteStartElement("Attendees");
            foreach (var attendee in evt.Attendees)
                writer.WriteElementString("Attendee", attendee);

            writer.WriteEndElement();

            writer.WriteEndElement(); // Event
        }

        writer.WriteEndElement(); // CalendarEvents
        writer.WriteEndDocument();
    }

    private void WriteElementIfNotEmpty(XmlWriter writer, string elementName, string? value)
    {
        if (!string.IsNullOrEmpty(value)) writer.WriteElementString(elementName, value);
    }

    private void WriteElementSafe(XmlWriter writer, string elementName, string? value)
    {
        writer.WriteElementString(elementName, value ?? string.Empty);
    }

    public override async Task ExportAsync(List<CalendarEvent> events, string outputPath)
    {
        ValidateInputs(events, outputPath);
        await Task.Run(() => Export(events, outputPath));
    }

    public override string GetFileExtension()
    {
        return ".xml";
    }

    public override string GetFormatName()
    {
        return "XML";
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