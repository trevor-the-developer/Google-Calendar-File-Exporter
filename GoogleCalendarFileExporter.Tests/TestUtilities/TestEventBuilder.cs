using GoogleCalendarFileExporter.Classes;

namespace GoogleCalendarFileExporter.Tests.TestUtilities;

public class TestEventBuilder
{
    private readonly CalendarEvent _event;

    private TestEventBuilder()
    {
        _event = new CalendarEvent
        {
            Summary = "Default Event",
            StartDateTime = DateTime.Now,
            EndDateTime = DateTime.Now.AddHours(1),
            Uid = Guid.NewGuid().ToString(),
            Status = "CONFIRMED",
            CalendarName = "Test Calendar",
            Attendees = new List<string>()
        };
    }

    public TestEventBuilder WithSummary(string summary)
    {
        _event.Summary = summary;
        return this;
    }

    public TestEventBuilder WithDescription(string description)
    {
        _event.Description = description;
        return this;
    }

    public TestEventBuilder WithLocation(string location)
    {
        _event.Location = location;
        return this;
    }

    public TestEventBuilder WithStartDateTime(DateTime startDateTime)
    {
        _event.StartDateTime = startDateTime;
        return this;
    }

    public TestEventBuilder WithEndDateTime(DateTime endDateTime)
    {
        _event.EndDateTime = endDateTime;
        return this;
    }

    public TestEventBuilder WithTimeRange(DateTime start, DateTime end)
    {
        _event.StartDateTime = start;
        _event.EndDateTime = end;
        return this;
    }

    public TestEventBuilder WithAllDayEvent(DateTime date)
    {
        _event.StartDateTime = date.Date;
        _event.EndDateTime = date.Date.AddDays(1);
        return this;
    }

    public TestEventBuilder WithCreated(DateTime created)
    {
        _event.Created = created;
        return this;
    }

    public TestEventBuilder WithLastModified(DateTime lastModified)
    {
        _event.LastModified = lastModified;
        return this;
    }

    public TestEventBuilder WithUid(string uid)
    {
        _event.Uid = uid;
        return this;
    }

    public TestEventBuilder WithStatus(string status)
    {
        _event.Status = status;
        return this;
    }

    public TestEventBuilder WithOrganizer(string organizer)
    {
        _event.Organizer = organizer;
        return this;
    }

    public TestEventBuilder WithAttendees(params string[] attendees)
    {
        _event.Attendees.Clear();
        _event.Attendees.AddRange(attendees);
        return this;
    }

    public TestEventBuilder WithRecurrenceRule(string recurrenceRule)
    {
        _event.RecurrenceRule = recurrenceRule;
        return this;
    }

    public TestEventBuilder WithCalendarName(string calendarName)
    {
        _event.CalendarName = calendarName;
        return this;
    }

    public TestEventBuilder WithMultiLineDescription(string description)
    {
        _event.Description = description.Replace("\\n", "\n");
        return this;
    }

    public TestEventBuilder WithSpecialCharacters()
    {
        _event.Summary = "Event with \"quotes\" and ,commas, and ;semicolons;";
        _event.Description = "Description with\nnewlines and special chars: ,;\"\\";
        _event.Location = "Location with, special; chars\" and\\backslashes";
        return this;
    }

    public CalendarEvent Build()
    {
        return _event;
    }

    public static TestEventBuilder Create()
    {
        return new TestEventBuilder();
    }
}