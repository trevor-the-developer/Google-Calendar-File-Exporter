using System.Xml.Linq;
using FluentAssertions;
using GoogleCalendarFileExporter.Classes;
using GoogleCalendarFileExporter.Exporters;
using GoogleCalendarFileExporter.Services;
using GoogleCalendarFileExporter.Tests.TestUtilities;
using Xunit;

namespace GoogleCalendarFileExporter.Tests.UnitTests;

public class XmlExporterTests : IDisposable
{
    private readonly MockLoggingService _mockLogger;
    private readonly List<string> _tempFiles = new();
    private readonly TimezoneService _timezoneService;
    private readonly XmlExporter _xmlExporter;

    public XmlExporterTests()
    {
        _mockLogger = new MockLoggingService();
        _timezoneService = new TimezoneService(_mockLogger);
        _xmlExporter = new XmlExporter();
    }

    public void Dispose()
    {
        foreach (var tempFile in _tempFiles) TestFileHelper.CleanupTempFile(tempFile);
    }

    [Fact]
    public void Export_WithValidEvents_ShouldCreateXmlFile()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create()
                .WithSummary("Test Event")
                .WithDescription("Test Description")
                .WithLocation("Test Location")
                .WithTimeRange(new DateTime(2024, 12, 15, 10, 0, 0), new DateTime(2024, 12, 15, 11, 0, 0))
                .WithOrganizer("organizer@example.com")
                .WithAttendees("attendee1@example.com", "attendee2@example.com")
                .WithStatus("CONFIRMED")
                .WithUid("test-uid")
                .WithCalendarName("Test Calendar")
                .Build()
        };

        var outputPath = GetTempFilePath(".xml");

        // Act
        _xmlExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        var xmlContent = File.ReadAllText(outputPath);
        var doc = XDocument.Parse(xmlContent);

        doc.Root.Should().NotBeNull();
        doc.Root!.Name.LocalName.Should().Be("CalendarEvents");

        var eventElement = doc.Root.Elements("Event").FirstOrDefault();
        eventElement.Should().NotBeNull();

        eventElement!.Element("Summary")!.Value.Should().Be("Test Event");
        eventElement.Element("Description")!.Value.Should().Be("Test Description");
        eventElement.Element("Location")!.Value.Should().Be("Test Location");
        eventElement.Element("Organizer")!.Value.Should().Be("organizer@example.com");
        eventElement.Element("Status")!.Value.Should().Be("CONFIRMED");
        eventElement.Element("Uid")!.Value.Should().Be("test-uid");
        eventElement.Element("CalendarName")!.Value.Should().Be("Test Calendar");

        var attendeesElement = eventElement.Element("Attendees");
        attendeesElement.Should().NotBeNull();
        attendeesElement!.Elements("Attendee").Should().HaveCount(2);
        attendeesElement.Elements("Attendee").Select(e => e.Value).Should().Contain("attendee1@example.com");
        attendeesElement.Elements("Attendee").Select(e => e.Value).Should().Contain("attendee2@example.com");
    }

    [Fact]
    public async Task ExportAsync_WithValidEvents_ShouldCreateXmlFile()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create()
                .WithSummary("Async Test Event")
                .WithDescription("Async Test Description")
                .WithLocation("Async Test Location")
                .WithTimeRange(new DateTime(2024, 12, 15, 10, 0, 0), new DateTime(2024, 12, 15, 11, 0, 0))
                .Build()
        };

        var outputPath = GetTempFilePath(".xml");

        // Act
        await _xmlExporter.ExportAsync(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        var xmlContent = File.ReadAllText(outputPath);
        var doc = XDocument.Parse(xmlContent);

        var eventElement = doc.Root!.Elements("Event").FirstOrDefault();
        eventElement!.Element("Summary")!.Value.Should().Be("Async Test Event");
        eventElement.Element("Description")!.Value.Should().Be("Async Test Description");
        eventElement.Element("Location")!.Value.Should().Be("Async Test Location");
    }

    [Fact]
    public void Export_WithEmptyEventsList_ShouldCreateXmlFileWithRootOnly()
    {
        // Arrange
        var events = new List<CalendarEvent>();
        var outputPath = GetTempFilePath(".xml");

        // Act
        _xmlExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        var xmlContent = File.ReadAllText(outputPath);
        var doc = XDocument.Parse(xmlContent);

        doc.Root.Should().NotBeNull();
        doc.Root!.Name.LocalName.Should().Be("CalendarEvents");
        doc.Root.Elements("Event").Should().BeEmpty();
    }

    [Fact]
    public void Export_WithSpecialCharactersInEventData_ShouldEscapeProperlyInXml()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create()
                .WithSummary("Event with <tags> & \"quotes\"")
                .WithDescription("Description with\nnewlines and <special> chars & symbols")
                .WithLocation("Location with unicode: 🎉 and special chars: @#$%")
                .Build()
        };

        var outputPath = GetTempFilePath(".xml");

        // Act
        _xmlExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        var xmlContent = File.ReadAllText(outputPath);
        var doc = XDocument.Parse(xmlContent);

        var eventElement = doc.Root!.Elements("Event").FirstOrDefault();
        eventElement!.Element("Summary")!.Value.Should().Be("Event with <tags> & \"quotes\"");
        eventElement.Element("Description")!.Value.Should()
            .Be("Description with\nnewlines and <special> chars & symbols");
        eventElement.Element("Location")!.Value.Should().Be("Location with unicode: 🎉 and special chars: @#$%");
    }

    [Fact]
    public void Export_WithAllDayEvent_ShouldFormatCorrectly()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create()
                .WithSummary("All Day Event")
                .WithTimeRange(new DateTime(2024, 12, 15, 0, 0, 0), new DateTime(2024, 12, 16, 0, 0, 0))
                .Build()
        };

        var outputPath = GetTempFilePath(".xml");

        // Act
        _xmlExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        var xmlContent = File.ReadAllText(outputPath);
        var doc = XDocument.Parse(xmlContent);

        var eventElement = doc.Root!.Elements("Event").FirstOrDefault();
        eventElement!.Element("Summary")!.Value.Should().Be("All Day Event");
        eventElement.Element("AllDayEvent")!.Value.Should().Be("True");
        eventElement.Element("StartDate")!.Value.Should().Be("2024-12-15");
        eventElement.Element("EndDate")!.Value.Should().Be("2024-12-16");
    }

    [Fact]
    public void Export_WithMultipleEvents_ShouldCreateAllEventElementsInXml()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create().WithSummary("Event 1").WithUid("uid-1").Build(),
            TestEventBuilder.Create().WithSummary("Event 2").WithUid("uid-2").Build(),
            TestEventBuilder.Create().WithSummary("Event 3").WithUid("uid-3").Build()
        };

        var outputPath = GetTempFilePath(".xml");

        // Act
        _xmlExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        var xmlContent = File.ReadAllText(outputPath);
        var doc = XDocument.Parse(xmlContent);

        var eventElements = doc.Root!.Elements("Event").ToList();
        eventElements.Should().HaveCount(3);

        eventElements[0].Element("Summary")!.Value.Should().Be("Event 1");
        eventElements[1].Element("Summary")!.Value.Should().Be("Event 2");
        eventElements[2].Element("Summary")!.Value.Should().Be("Event 3");

        eventElements[0].Element("Uid")!.Value.Should().Be("uid-1");
        eventElements[1].Element("Uid")!.Value.Should().Be("uid-2");
        eventElements[2].Element("Uid")!.Value.Should().Be("uid-3");
    }

    [Fact]
    public void Export_WithNullEventProperties_ShouldHandleGracefully()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            new()
            {
                Summary = "Event with nulls",
                Description = null,
                Location = null,
                StartDateTime = null,
                EndDateTime = null,
                Organizer = null,
                Attendees = new List<string>(),
                Status = null,
                RecurrenceRule = null,
                Uid = null,
                CalendarName = null,
                Created = null,
                LastModified = null
            }
        };

        var outputPath = GetTempFilePath(".xml");

        // Act
        _xmlExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        var xmlContent = File.ReadAllText(outputPath);
        var doc = XDocument.Parse(xmlContent);

        var eventElement = doc.Root!.Elements("Event").FirstOrDefault();
        eventElement!.Element("Summary")!.Value.Should().Be("Event with nulls");
        eventElement.Element("Description")!.Value.Should().BeEmpty();
        eventElement.Element("Location")!.Value.Should().BeEmpty();
        eventElement.Element("Organizer")!.Value.Should().BeEmpty();
        eventElement.Element("Status")!.Value.Should().BeEmpty();
        eventElement.Element("RecurrenceRule")!.Value.Should().BeEmpty();
        eventElement.Element("Uid")!.Value.Should().BeEmpty();
        eventElement.Element("CalendarName")!.Value.Should().BeEmpty();
        eventElement.Element("Attendees")!.Elements("Attendee").Should().BeEmpty();
    }

    [Fact]
    public void Export_WithRecurringEvent_ShouldIncludeRecurrenceRule()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create()
                .WithSummary("Recurring Event")
                .WithRecurrenceRule("FREQ=WEEKLY;BYDAY=MO,WE,FR")
                .Build()
        };

        var outputPath = GetTempFilePath(".xml");

        // Act
        _xmlExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        var xmlContent = File.ReadAllText(outputPath);
        var doc = XDocument.Parse(xmlContent);

        var eventElement = doc.Root!.Elements("Event").FirstOrDefault();
        eventElement!.Element("Summary")!.Value.Should().Be("Recurring Event");
        eventElement.Element("RecurrenceRule")!.Value.Should().Be("FREQ=WEEKLY;BYDAY=MO,WE,FR");
    }

    [Fact]
    public void Export_WithInvalidOutputPath_ShouldThrowException()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create().Build()
        };

        var invalidPath = Path.Combine("invalid", "path", "that", "does", "not", "exist", "file.xml");

        // Act & Assert
        var exception = Assert.Throws<DirectoryNotFoundException>(() => _xmlExporter.Export(events, invalidPath));
        exception.Should().NotBeNull();
    }

    [Fact]
    public void Export_WithLargeNumberOfEvents_ShouldProcessAllEvents()
    {
        // Arrange
        var events = new List<CalendarEvent>();
        for (var i = 0; i < 1000; i++)
            events.Add(TestEventBuilder.Create().WithSummary($"Event {i}").WithUid($"uid-{i}").Build());

        var outputPath = GetTempFilePath(".xml");

        // Act
        _xmlExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        var xmlContent = File.ReadAllText(outputPath);
        var doc = XDocument.Parse(xmlContent);

        var eventElements = doc.Root!.Elements("Event").ToList();
        eventElements.Should().HaveCount(1000);

        eventElements[0].Element("Summary")!.Value.Should().Be("Event 0");
        eventElements[999].Element("Summary")!.Value.Should().Be("Event 999");
        eventElements[0].Element("Uid")!.Value.Should().Be("uid-0");
        eventElements[999].Element("Uid")!.Value.Should().Be("uid-999");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Export_WithInvalidOutputPath_ShouldThrowArgumentException(string? outputPath)
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create().Build()
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _xmlExporter.Export(events, outputPath!));
        exception.Should().NotBeNull();
    }

    [Fact]
    public void Export_WithDateTimesAndAllDayDetection_ShouldFormatCorrectly()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create()
                .WithSummary("Regular Event")
                .WithTimeRange(new DateTime(2024, 12, 15, 10, 30, 0), new DateTime(2024, 12, 15, 11, 30, 0))
                .Build(),
            TestEventBuilder.Create()
                .WithSummary("All Day Event")
                .WithTimeRange(new DateTime(2024, 12, 16, 0, 0, 0), new DateTime(2024, 12, 17, 0, 0, 0))
                .Build()
        };

        var outputPath = GetTempFilePath(".xml");

        // Act
        _xmlExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        var xmlContent = File.ReadAllText(outputPath);
        var doc = XDocument.Parse(xmlContent);

        var eventElements = doc.Root!.Elements("Event").ToList();

        // Regular event
        eventElements[0].Element("Summary")!.Value.Should().Be("Regular Event");
        eventElements[0].Element("StartDate")!.Value.Should().Be("2024-12-15");
        eventElements[0].Element("StartTime")!.Value.Should().Be("2024-12-15 10:30:00");
        eventElements[0].Element("EndDate")!.Value.Should().Be("2024-12-15");
        eventElements[0].Element("EndTime")!.Value.Should().Be("2024-12-15 11:30:00");
        eventElements[0].Element("AllDayEvent")!.Value.Should().Be("False");

        // All day event
        eventElements[1].Element("Summary")!.Value.Should().Be("All Day Event");
        eventElements[1].Element("StartDate")!.Value.Should().Be("2024-12-16");
        eventElements[1].Element("StartTime")!.Value.Should().Be("2024-12-16 00:00:00");
        eventElements[1].Element("EndDate")!.Value.Should().Be("2024-12-17");
        eventElements[1].Element("EndTime")!.Value.Should().Be("2024-12-17 00:00:00");
        eventElements[1].Element("AllDayEvent")!.Value.Should().Be("True");
    }

    [Fact]
    public void Export_WithComplexEventData_ShouldPreserveAllData()
    {
        // Arrange
        var complexEvent = TestEventBuilder.Create()
            .WithSummary("Complex Event")
            .WithDescription("Multi-line\ndescription with\nspecial chars")
            .WithLocation("Complex Location")
            .WithTimeRange(
                new DateTime(2024, 12, 15, 10, 30, 45),
                new DateTime(2024, 12, 15, 12, 45, 30))
            .WithOrganizer("complex.organizer@example.com")
            .WithAttendees("attendee1@example.com", "attendee2@example.com", "attendee3@example.com")
            .WithStatus("TENTATIVE")
            .WithRecurrenceRule("FREQ=MONTHLY;BYMONTHDAY=15")
            .WithUid("complex-uid-123")
            .WithCalendarName("Complex Calendar")
            .Build();

        complexEvent.Created = new DateTime(2024, 12, 1, 8, 0, 0);
        complexEvent.LastModified = new DateTime(2024, 12, 10, 14, 30, 0);

        var events = new List<CalendarEvent> { complexEvent };
        var outputPath = GetTempFilePath(".xml");

        // Act
        _xmlExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        var xmlContent = File.ReadAllText(outputPath);
        var doc = XDocument.Parse(xmlContent);

        var eventElement = doc.Root!.Elements("Event").FirstOrDefault();

        eventElement!.Element("Summary")!.Value.Should().Be("Complex Event");
        eventElement.Element("Description")!.Value.Should().Be("Multi-line\ndescription with\nspecial chars");
        eventElement.Element("Location")!.Value.Should().Be("Complex Location");
        eventElement.Element("Organizer")!.Value.Should().Be("complex.organizer@example.com");
        eventElement.Element("Status")!.Value.Should().Be("TENTATIVE");
        eventElement.Element("RecurrenceRule")!.Value.Should().Be("FREQ=MONTHLY;BYMONTHDAY=15");
        eventElement.Element("Uid")!.Value.Should().Be("complex-uid-123");
        eventElement.Element("CalendarName")!.Value.Should().Be("Complex Calendar");
        eventElement.Element("Created")!.Value.Should().Be("2024-12-01 08:00:00");
        eventElement.Element("LastModified")!.Value.Should().Be("2024-12-10 14:30:00");

        var attendeesElement = eventElement.Element("Attendees");
        attendeesElement!.Elements("Attendee").Should().HaveCount(3);
        attendeesElement.Elements("Attendee").Select(e => e.Value).Should().Contain("attendee1@example.com");
        attendeesElement.Elements("Attendee").Select(e => e.Value).Should().Contain("attendee2@example.com");
        attendeesElement.Elements("Attendee").Select(e => e.Value).Should().Contain("attendee3@example.com");
    }

    [Fact]
    public void Export_ShouldCreateWellFormedXmlFile()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create()
                .WithSummary("Well-formed Event")
                .WithDescription("This should be well-formed XML")
                .Build()
        };

        var outputPath = GetTempFilePath(".xml");

        // Act
        _xmlExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        var xmlContent = File.ReadAllText(outputPath);

        // Should be able to parse without exception
        var doc = XDocument.Parse(xmlContent);
        doc.Should().NotBeNull();

        // Should have proper XML declaration
        xmlContent.Should().StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>");

        // Should have root element
        doc.Root!.Name.LocalName.Should().Be("CalendarEvents");
    }

    [Fact]
    public void Export_WithEmptyAttendeesList_ShouldCreateEmptyAttendeesElement()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create()
                .WithSummary("Event with no attendees")
                .Build()
        };

        var outputPath = GetTempFilePath(".xml");

        // Act
        _xmlExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        var xmlContent = File.ReadAllText(outputPath);
        var doc = XDocument.Parse(xmlContent);

        var eventElement = doc.Root!.Elements("Event").FirstOrDefault();
        var attendeesElement = eventElement!.Element("Attendees");
        attendeesElement.Should().NotBeNull();
        attendeesElement!.Elements("Attendee").Should().BeEmpty();
    }

    private string GetTempFilePath(string extension)
    {
        var tempFile = Path.GetTempFileName();
        var fileWithExtension = Path.ChangeExtension(tempFile, extension);
        _tempFiles.Add(fileWithExtension);
        return fileWithExtension;
    }
}