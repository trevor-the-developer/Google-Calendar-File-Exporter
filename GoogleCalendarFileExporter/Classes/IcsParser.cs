using System.Text.RegularExpressions;
using GoogleCalendarFileExporter.Interfaces;
using GoogleCalendarFileExporter.Services;

namespace GoogleCalendarFileExporter.Classes;

public class IcsParser
{
    private readonly ILoggingService _logger;
    private readonly TimezoneService _timezoneService;

    public IcsParser(TimezoneService timezoneService, ILoggingService logger)
    {
        _timezoneService = timezoneService;
        _logger = logger;
    }

    public List<CalendarEvent> ParseIcsContent(string content, string calendarName)
    {
        var events = new List<CalendarEvent>();

        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("ICS content is null or empty for calendar: {CalendarName}", calendarName ?? "Unknown");
            return events;
        }

        try
        {
            // Check for basic malformed content - missing END:VEVENT
            if (!IsValidIcsContent(content))
            {
                _logger.LogError(
                    "Malformed ICS content detected for calendar: {CalendarName}. Missing END:VEVENT or improperly formatted event blocks.",
                    calendarName ?? "Unknown");
                return new List<CalendarEvent>();
            }

            // Split into individual events
            var eventBlocks = Regex.Split(content, @"BEGIN:VEVENT")
                .Where(block => block.Contains("END:VEVENT"))
                .ToList();

            foreach (var eventBlock in eventBlocks)
            {
                var eventContent = "BEGIN:VEVENT" + eventBlock;

                // Additional validation for properly formed event blocks
                if (!IsValidEventBlock(eventContent))
                {
                    _logger.LogError("Malformed event block detected in calendar: {CalendarName}",
                        calendarName ?? "Unknown");
                    continue;
                }

                if (calendarName == null) continue;
                var calendarEvent = ParseEvent(eventContent, calendarName);
                if (calendarEvent != null) events.Add(calendarEvent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing ICS content for calendar: {CalendarName}", calendarName ?? "Unknown");
            return new List<CalendarEvent>();
        }

        return events;
    }

    private CalendarEvent? ParseEvent(string eventContent, string calendarName)
    {
        var calendarEvent = new CalendarEvent { CalendarName = calendarName };

        var lines = eventContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd()) // Only trim end, preserve leading spaces for folding
            .Where(line => !string.IsNullOrEmpty(line))
            .ToList();

        // Handle multi-line properties (lines starting with space or tab)
        var processedLines = new List<string>();
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (i > 0 && (line.StartsWith(" ") || line.StartsWith("\t")))
            {
                // Continuation of previous line - remove the leading space/tab and append
                if (processedLines.Count > 0) processedLines[processedLines.Count - 1] += line.Substring(1);
            }
            else
            {
                processedLines.Add(line);
            }
        }

        foreach (var line in processedLines)
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex <= 0) continue;

            var property = line.Substring(0, colonIndex);
            var value = line.Substring(colonIndex + 1);

            // Handle parameters in property names (e.g., "DTSTART;TZID=America/New_York")
            var propertyName = property.Split(';')[0];
            var parameters = property.Contains(';') ? property.Substring(property.IndexOf(';') + 1) : "";

            switch (propertyName.ToUpperInvariant())
            {
                case "SUMMARY":
                    calendarEvent.Summary = DecodeIcsText(value);
                    break;
                case "DESCRIPTION":
                    calendarEvent.Description = DecodeIcsText(value);
                    break;
                case "LOCATION":
                    calendarEvent.Location = DecodeIcsText(value);
                    break;
                case "DTSTART":
                    calendarEvent.StartDateTime = _timezoneService.ParseIcsDateTime(value, parameters);
                    break;
                case "DTEND":
                    calendarEvent.EndDateTime = _timezoneService.ParseIcsDateTime(value, parameters);
                    break;
                case "CREATED":
                    calendarEvent.Created = _timezoneService.ParseIcsDateTime(value, parameters);
                    break;
                case "LAST-MODIFIED":
                    calendarEvent.LastModified = _timezoneService.ParseIcsDateTime(value, parameters);
                    break;
                case "UID":
                    calendarEvent.Uid = value;
                    break;
                case "STATUS":
                    calendarEvent.Status = value;
                    break;
                case "ORGANIZER":
                    calendarEvent.Organizer = ExtractEmailFromOrganizer(value);
                    break;
                case "ATTENDEE":
                    calendarEvent.Attendees.Add(ExtractEmailFromAttendee(value));
                    break;
                case "RRULE":
                    calendarEvent.RecurrenceRule = value;
                    break;
            }
        }

        return string.IsNullOrEmpty(calendarEvent.Summary) ? null : calendarEvent;
    }


    private string DecodeIcsText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        return text
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\,", ",")
            .Replace("\\;", ";")
            .Replace("\\\\", "\\");
    }

    private string ExtractEmailFromOrganizer(string organizer)
    {
        // Example: "CN=John Doe:mailto:john@example.com"
        var match = Regex.Match(organizer, @"mailto:([^;]+)");
        return match.Success ? match.Groups[1].Value : organizer;
    }

    private string ExtractEmailFromAttendee(string attendee)
    {
        // Example: "CN=Jane Doe;ROLE=REQ-PARTICIPANT;PARTSTAT=ACCEPTED;RSVP=TRUE:mailto:jane@example.com"
        var match = Regex.Match(attendee, @"mailto:([^;]+)");
        return match.Success ? match.Groups[1].Value : attendee;
    }

    private bool IsValidIcsContent(string content)
    {
        // Check for basic structural validity
        if (!content.Contains("BEGIN:VCALENDAR") || !content.Contains("END:VCALENDAR")) return false;

        // Count BEGIN:VEVENT and END:VEVENT occurrences at line boundaries
        // Use regex that handles different line endings (\r\n, \n, \r)
        var beginEventCount = Regex
            .Matches(content, @"(?:^|\r\n|\n|\r)BEGIN:VEVENT(?:\r\n|\n|\r|$)", RegexOptions.Multiline).Count;
        var endEventCount =
            Regex.Matches(content, @"(?:^|\r\n|\n|\r)END:VEVENT(?:\r\n|\n|\r|$)", RegexOptions.Multiline).Count;

        // Each BEGIN:VEVENT must have a corresponding END:VEVENT
        return beginEventCount == endEventCount && beginEventCount > 0;
    }

    private bool IsValidEventBlock(string eventBlock)
    {
        // Ensure the event block has both BEGIN and END markers
        return eventBlock.Contains("BEGIN:VEVENT") && eventBlock.Contains("END:VEVENT");
    }
}