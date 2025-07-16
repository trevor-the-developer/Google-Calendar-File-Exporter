using GoogleCalendarFileExporter.Classes;

namespace GoogleCalendarFileExporter.Tests.Fixtures;

public static class TestData
{
    public static readonly string SimpleEventIcs = @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Google Inc//Google Calendar 70.9054//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
BEGIN:VEVENT
DTSTART:20241215T100000Z
DTEND:20241215T110000Z
SUMMARY:Team Meeting
DESCRIPTION:Weekly team sync
LOCATION:Conference Room A
ORGANIZER;CN=John Doe:mailto:john@example.com
ATTENDEE;CN=Jane Smith:mailto:jane@example.com
ATTENDEE;CN=Bob Johnson:mailto:bob@example.com
STATUS:CONFIRMED
UID:test-event-1@example.com
CREATED:20241201T120000Z
LAST-MODIFIED:20241201T120000Z
END:VEVENT
END:VCALENDAR";

    public static readonly string AllDayEventIcs = @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Google Inc//Google Calendar 70.9054//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
BEGIN:VEVENT
DTSTART;VALUE=DATE:20241216
DTEND;VALUE=DATE:20241217
SUMMARY:All Day Event
DESCRIPTION:Company holiday
STATUS:CONFIRMED
UID:test-event-2@example.com
CREATED:20241201T120000Z
LAST-MODIFIED:20241201T120000Z
END:VEVENT
END:VCALENDAR";

    public static readonly string MultiLineDescriptionIcs = @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Google Inc//Google Calendar 70.9054//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
BEGIN:VEVENT
DTSTART:20241217T140000Z
DTEND:20241217T150000Z
SUMMARY:Event with Multi-line Description
DESCRIPTION:This is a long description that spans multiple lines.\n\nIt inc
 ludes line breaks and special characters like commas\, semicolons\; and ba
 ckslashes\\.
LOCATION:Room B
STATUS:CONFIRMED
UID:test-event-3@example.com
CREATED:20241201T120000Z
LAST-MODIFIED:20241201T120000Z
END:VEVENT
END:VCALENDAR";

    public static readonly string RecurringEventIcs = @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Google Inc//Google Calendar 70.9054//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
BEGIN:VEVENT
DTSTART:20241218T090000Z
DTEND:20241218T100000Z
SUMMARY:Daily Standup
DESCRIPTION:Daily team standup meeting
LOCATION:Online
ORGANIZER;CN=Scrum Master:mailto:scrum@example.com
RRULE:FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR
STATUS:CONFIRMED
UID:test-event-4@example.com
CREATED:20241201T120000Z
LAST-MODIFIED:20241201T120000Z
END:VEVENT
END:VCALENDAR";

    public static readonly string TimezoneEventIcs = @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Google Inc//Google Calendar 70.9054//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
BEGIN:VEVENT
DTSTART;TZID=America/New_York:20241219T100000
DTEND;TZID=America/New_York:20241219T110000
SUMMARY:Timezone Event
DESCRIPTION:Event with timezone information
LOCATION:New York Office
STATUS:CONFIRMED
UID:test-event-5@example.com
CREATED:20241201T120000Z
LAST-MODIFIED:20241201T120000Z
END:VEVENT
END:VCALENDAR";

    public static readonly string EmptyEventIcs = @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Google Inc//Google Calendar 70.9054//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
BEGIN:VEVENT
DTSTART:20241220T100000Z
DTEND:20241220T110000Z
UID:test-event-6@example.com
CREATED:20241201T120000Z
LAST-MODIFIED:20241201T120000Z
END:VEVENT
END:VCALENDAR";

    public static readonly string InvalidIcsContent = @"BEGIN:VCALENDAR
VERSION:2.0
INVALID_CONTENT_HERE
END:VCALENDAR";

    public static readonly string MalformedDateIcs = @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Google Inc//Google Calendar 70.9054//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
BEGIN:VEVENT
DTSTART:INVALID_DATE
DTEND:ALSO_INVALID
SUMMARY:Event with Invalid Dates
UID:test-event-7@example.com
END:VEVENT
END:VCALENDAR";

    private static CalendarEvent CreateTestEvent()
    {
        return new CalendarEvent
        {
            Summary = "Test Event",
            Description = "Test Description",
            Location = "Test Location",
            StartDateTime = new DateTime(2024, 12, 15, 10, 0, 0),
            EndDateTime = new DateTime(2024, 12, 15, 11, 0, 0),
            Created = new DateTime(2024, 12, 1, 12, 0, 0),
            LastModified = new DateTime(2024, 12, 1, 12, 0, 0),
            Uid = "test-event@example.com",
            Status = "CONFIRMED",
            Organizer = "john@example.com",
            Attendees = new List<string> { "jane@example.com", "bob@example.com" },
            RecurrenceRule = "FREQ=WEEKLY",
            CalendarName = "Test Calendar"
        };
    }

    public static List<CalendarEvent> CreateTestEventList()
    {
        return new List<CalendarEvent>
        {
            CreateTestEvent(),
            new()
            {
                Summary = "All Day Event",
                StartDateTime = new DateTime(2024, 12, 16, 0, 0, 0),
                EndDateTime = new DateTime(2024, 12, 17, 0, 0, 0),
                Uid = "all-day-event@example.com",
                Status = "CONFIRMED",
                CalendarName = "Test Calendar"
            },
            new()
            {
                Summary = "Future Event",
                StartDateTime = new DateTime(2024, 12, 20, 14, 0, 0),
                EndDateTime = new DateTime(2024, 12, 20, 15, 0, 0),
                Uid = "future-event@example.com",
                Status = "TENTATIVE",
                CalendarName = "Test Calendar"
            }
        };
    }
}