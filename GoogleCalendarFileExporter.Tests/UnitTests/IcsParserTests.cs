using FluentAssertions;
using GoogleCalendarFileExporter.Classes;
using GoogleCalendarFileExporter.Services;
using GoogleCalendarFileExporter.Tests.Fixtures;
using GoogleCalendarFileExporter.Tests.TestUtilities;
using Xunit;

namespace GoogleCalendarFileExporter.Tests.UnitTests;

public class IcsParserTests
{
    private const string TestCalendarName = "Test Calendar";
    private readonly MockLoggingService _mockLogger;
    private readonly IcsParser _parser;
    private readonly TimezoneService _timezoneService;

    public IcsParserTests()
    {
        _mockLogger = new MockLoggingService();
        _timezoneService = new TimezoneService(_mockLogger);
        _parser = new IcsParser(_timezoneService, _mockLogger);
    }

    [Fact]
    public void ParseIcsContent_WithSimpleEvent_ShouldParseCorrectly()
    {
        // Act
        var events = _parser.ParseIcsContent(TestData.SimpleEventIcs, TestCalendarName);

        // Assert
        events.Should().HaveCount(1);
        var evt = events.First();
        evt.Summary.Should().Be("Team Meeting");
        evt.Description.Should().Be("Weekly team sync");
        evt.Location.Should().Be("Conference Room A");
        evt.StartDateTime.Should().NotBeNull();
        evt.EndDateTime.Should().NotBeNull();
        evt.Organizer.Should().Be("john@example.com");
        evt.Attendees.Should().HaveCount(2);
        evt.Attendees.Should().Contain("jane@example.com");
        evt.Attendees.Should().Contain("bob@example.com");
        evt.Status.Should().Be("CONFIRMED");
        evt.Uid.Should().Be("test-event-1@example.com");
        evt.CalendarName.Should().Be(TestCalendarName);
    }

    [Fact]
    public void ParseIcsContent_WithAllDayEvent_ShouldParseCorrectly()
    {
        // Act
        var events = _parser.ParseIcsContent(TestData.AllDayEventIcs, TestCalendarName);

        // Assert
        events.Should().HaveCount(1);
        var evt = events.First();
        evt.Summary.Should().Be("All Day Event");
        evt.Description.Should().Be("Company holiday");
        evt.StartDateTime.Should().NotBeNull();
        evt.EndDateTime.Should().NotBeNull();
        evt.Status.Should().Be("CONFIRMED");
        evt.Uid.Should().Be("test-event-2@example.com");
    }

    [Fact]
    public void ParseIcsContent_WithMultiLineDescription_ShouldParseCorrectly()
    {
        // Act
        var events = _parser.ParseIcsContent(TestData.MultiLineDescriptionIcs, TestCalendarName);

        // Assert
        events.Should().HaveCount(1);
        var evt = events.First();
        evt.Summary.Should().Be("Event with Multi-line Description");
        evt.Description.Should().Contain("This is a long description that spans multiple lines.");
        evt.Description.Should()
            .Contain("It includes line breaks and special characters like commas, semicolons; and backslashes\\.");
        evt.Location.Should().Be("Room B");
    }

    [Fact]
    public void ParseIcsContent_WithRecurringEvent_ShouldParseCorrectly()
    {
        // Act
        var events = _parser.ParseIcsContent(TestData.RecurringEventIcs, TestCalendarName);

        // Assert
        events.Should().HaveCount(1);
        var evt = events.First();
        evt.Summary.Should().Be("Daily Standup");
        evt.Description.Should().Be("Daily team standup meeting");
        evt.Location.Should().Be("Online");
        evt.RecurrenceRule.Should().Be("FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR");
        evt.Organizer.Should().Be("scrum@example.com");
    }

    [Fact]
    public void ParseIcsContent_WithTimezoneEvent_ShouldParseCorrectly()
    {
        // Act
        var events = _parser.ParseIcsContent(TestData.TimezoneEventIcs, TestCalendarName);

        // Assert
        events.Should().HaveCount(1);
        var evt = events.First();
        evt.Summary.Should().Be("Timezone Event");
        evt.Description.Should().Be("Event with timezone information");
        evt.Location.Should().Be("New York Office");
        evt.StartDateTime.Should().NotBeNull();
        evt.EndDateTime.Should().NotBeNull();
    }

    [Fact]
    public void ParseIcsContent_WithEmptyEvent_ShouldReturnEmpty()
    {
        // Act
        var events = _parser.ParseIcsContent(TestData.EmptyEventIcs, TestCalendarName);

        // Assert
        events.Should().BeEmpty();
    }

    [Fact]
    public void ParseIcsContent_WithInvalidContent_ShouldReturnEmptyList()
    {
        // Act
        var events = _parser.ParseIcsContent(TestData.InvalidIcsContent, TestCalendarName);

        // Assert
        events.Should().BeEmpty();
    }

    [Fact]
    public void ParseIcsContent_WithMalformedDates_ShouldHandleGracefully()
    {
        // Act
        var events = _parser.ParseIcsContent(TestData.MalformedDateIcs, TestCalendarName);

        // Assert
        events.Should().HaveCount(1);
        var evt = events.First();
        evt.Summary.Should().Be("Event with Invalid Dates");
        evt.StartDateTime.Should().BeNull();
        evt.EndDateTime.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseIcsContent_WithNullOrEmptyContent_ShouldReturnEmptyList(string? content)
    {
        // Act
        var events = _parser.ParseIcsContent(content!, TestCalendarName);

        // Assert
        events.Should().BeEmpty();
    }

    [Fact]
    public void ParseIcsContent_WithMultipleEvents_ShouldParseAll()
    {
        // Arrange
        var multipleEventsIcs = TestData.SimpleEventIcs + "\n" + TestData.AllDayEventIcs;

        // Act
        var events = _parser.ParseIcsContent(multipleEventsIcs, TestCalendarName);

        // Assert
        events.Should().HaveCount(2);
        events.Should().Contain(e => e.Summary == "Team Meeting");
        events.Should().Contain(e => e.Summary == "All Day Event");
    }

    [Theory]
    [InlineData("\\n", "\n")]
    [InlineData("\\r", "\r")]
    [InlineData("\\,", ",")]
    [InlineData("\\;", ";")]
    [InlineData("\\\\", "\\")]
    [InlineData("Test\\nLine\\,Break", "Test\nLine,Break")]
    public void DecodeIcsText_WithEscapedCharacters_ShouldDecodeCorrectly(string input, string expected)
    {
        // Arrange
        var icsContent = $@"BEGIN:VCALENDAR
VERSION:2.0
BEGIN:VEVENT
DTSTART:20241215T100000Z
SUMMARY:{input}
UID:test@example.com
END:VEVENT
END:VCALENDAR";

        // Act
        var events = _parser.ParseIcsContent(icsContent, TestCalendarName);

        // Assert
        events.Should().HaveCount(1);
        var evt = events.First();
        evt.Summary.Should().Be(expected);
    }

    [Theory]
    [InlineData("CN=John Doe:mailto:john@example.com", "john@example.com")]
    [InlineData("mailto:jane@example.com", "jane@example.com")]
    [InlineData("CN=Test User;ROLE=CHAIR:mailto:test@example.com", "test@example.com")]
    [InlineData("invalid-format", "invalid-format")]
    public void ExtractEmailFromOrganizer_WithVariousFormats_ShouldExtractCorrectly(string organizerValue,
        string expectedEmail)
    {
        // Arrange
        var icsContent = $@"BEGIN:VCALENDAR
VERSION:2.0
BEGIN:VEVENT
DTSTART:20241215T100000Z
SUMMARY:Test Event
ORGANIZER:{organizerValue}
UID:test@example.com
END:VEVENT
END:VCALENDAR";

        // Act
        var events = _parser.ParseIcsContent(icsContent, TestCalendarName);

        // Assert
        events.Should().HaveCount(1);
        var evt = events.First();
        evt.Organizer.Should().Be(expectedEmail);
    }

    [Theory]
    [InlineData("CN=Jane Doe;ROLE=REQ-PARTICIPANT;PARTSTAT=ACCEPTED;RSVP=TRUE:mailto:jane@example.com",
        "jane@example.com")]
    [InlineData("mailto:bob@example.com", "bob@example.com")]
    [InlineData("CN=Test Attendee:mailto:test@example.com", "test@example.com")]
    [InlineData("invalid-format", "invalid-format")]
    public void ExtractEmailFromAttendee_WithVariousFormats_ShouldExtractCorrectly(string attendeeValue,
        string expectedEmail)
    {
        // Arrange
        var icsContent = $@"BEGIN:VCALENDAR
VERSION:2.0
BEGIN:VEVENT
DTSTART:20241215T100000Z
SUMMARY:Test Event
ATTENDEE:{attendeeValue}
UID:test@example.com
END:VEVENT
END:VCALENDAR";

        // Act
        var events = _parser.ParseIcsContent(icsContent, TestCalendarName);

        // Assert
        events.Should().HaveCount(1);
        var evt = events.First();
        evt.Attendees.Should().HaveCount(1);
        evt.Attendees.First().Should().Be(expectedEmail);
    }

    [Fact]
    public void ParseIcsContent_WithMultipleAttendees_ShouldParseAll()
    {
        // Arrange
        var icsContent = @"BEGIN:VCALENDAR
VERSION:2.0
BEGIN:VEVENT
DTSTART:20241215T100000Z
SUMMARY:Test Event
ATTENDEE:mailto:attendee1@example.com
ATTENDEE:mailto:attendee2@example.com
ATTENDEE:mailto:attendee3@example.com
UID:test@example.com
END:VEVENT
END:VCALENDAR";

        // Act
        var events = _parser.ParseIcsContent(icsContent, TestCalendarName);

        // Assert
        events.Should().HaveCount(1);
        var evt = events.First();
        evt.Attendees.Should().HaveCount(3);
        evt.Attendees.Should().Contain("attendee1@example.com");
        evt.Attendees.Should().Contain("attendee2@example.com");
        evt.Attendees.Should().Contain("attendee3@example.com");
    }

    [Fact]
    public void ParseIcsContent_WithMissingRequiredFields_ShouldReturnEmptyList()
    {
        // Arrange
        var icsContent = @"BEGIN:VCALENDAR
VERSION:2.0
BEGIN:VEVENT
DTSTART:20241215T100000Z
DTEND:20241215T110000Z
UID:test@example.com
END:VEVENT
END:VCALENDAR";

        // Act
        var events = _parser.ParseIcsContent(icsContent, TestCalendarName);

        // Assert
        events.Should().BeEmpty();
    }
}