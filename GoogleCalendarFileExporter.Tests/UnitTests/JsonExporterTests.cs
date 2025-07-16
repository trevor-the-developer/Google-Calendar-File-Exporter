using System.Text.Json;
using FluentAssertions;
using GoogleCalendarFileExporter.Classes;
using GoogleCalendarFileExporter.Exporters;
using GoogleCalendarFileExporter.Services;
using GoogleCalendarFileExporter.Tests.TestUtilities;
using Xunit;

namespace GoogleCalendarFileExporter.Tests.UnitTests;

public class JsonExporterTests : IDisposable
{
    private readonly JsonExporter _jsonExporter;
    private readonly MockLoggingService _mockLogger;
    private readonly List<string> _tempFiles = new();
    private readonly TimezoneService _timezoneService;

    public JsonExporterTests()
    {
        _mockLogger = new MockLoggingService();
        _timezoneService = new TimezoneService(_mockLogger);
        _jsonExporter = new JsonExporter();
    }

    public void Dispose()
    {
        foreach (var tempFile in _tempFiles) TestFileHelper.CleanupTempFile(tempFile);
    }

    [Fact]
    public void Export_WithValidEvents_ShouldCreateJsonFile()
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

        var outputPath = GetTempFilePath(".json");

        // Act
        _jsonExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var jsonContent = File.ReadAllText(outputPath);
        jsonContent.Should().NotBeEmpty();

        var deserializedEvents = JsonSerializer.Deserialize<List<CalendarEvent>>(jsonContent);
        deserializedEvents.Should().HaveCount(1);
        var evt = deserializedEvents!.First();
        evt.Summary.Should().Be("Test Event");
        evt.Description.Should().Be("Test Description");
        evt.Location.Should().Be("Test Location");
        evt.Organizer.Should().Be("organizer@example.com");
        evt.Attendees.Should().HaveCount(2);
        evt.Status.Should().Be("CONFIRMED");
        evt.Uid.Should().Be("test-uid");
        evt.CalendarName.Should().Be("Test Calendar");
    }

    [Fact]
    public async Task ExportAsync_WithValidEvents_ShouldCreateJsonFile()
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

        var outputPath = GetTempFilePath(".json");

        // Act
        await _jsonExporter.ExportAsync(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var jsonContent = File.ReadAllText(outputPath);
        jsonContent.Should().NotBeEmpty();

        var deserializedEvents = JsonSerializer.Deserialize<List<CalendarEvent>>(jsonContent);
        deserializedEvents.Should().HaveCount(1);
        var evt = deserializedEvents!.First();
        evt.Summary.Should().Be("Async Test Event");
        evt.Description.Should().Be("Async Test Description");
        evt.Location.Should().Be("Async Test Location");
    }

    [Fact]
    public void Export_WithEmptyEventsList_ShouldCreateJsonFileWithEmptyArray()
    {
        // Arrange
        var events = new List<CalendarEvent>();
        var outputPath = GetTempFilePath(".json");

        // Act
        _jsonExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var jsonContent = File.ReadAllText(outputPath);
        jsonContent.Should().NotBeEmpty();

        var deserializedEvents = JsonSerializer.Deserialize<List<CalendarEvent>>(jsonContent);
        deserializedEvents.Should().NotBeNull();
        deserializedEvents.Should().BeEmpty();
    }

    [Fact]
    public void Export_WithSpecialCharactersInEventData_ShouldEscapeProperlyInJson()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create()
                .WithSummary("Event with \"quotes\" and \\backslashes")
                .WithDescription("Description with\nnewlines and \"quotes\"")
                .WithLocation("Location with unicode: 🎉 and special chars: @#$%")
                .Build()
        };

        var outputPath = GetTempFilePath(".json");

        // Act
        _jsonExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var jsonContent = File.ReadAllText(outputPath);
        jsonContent.Should().NotBeEmpty();

        var deserializedEvents = JsonSerializer.Deserialize<List<CalendarEvent>>(jsonContent);
        deserializedEvents.Should().HaveCount(1);
        var evt = deserializedEvents!.First();
        evt.Summary.Should().Be("Event with \"quotes\" and \\backslashes");
        evt.Description.Should().Be("Description with\nnewlines and \"quotes\"");
        evt.Location.Should().Be("Location with unicode: 🎉 and special chars: @#$%");
    }

    [Fact]
    public void Export_WithAllDayEvent_ShouldSerializeCorrectly()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create()
                .WithSummary("All Day Event")
                .WithTimeRange(new DateTime(2024, 12, 15, 0, 0, 0), new DateTime(2024, 12, 16, 0, 0, 0))
                .Build()
        };

        var outputPath = GetTempFilePath(".json");

        // Act
        _jsonExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var jsonContent = File.ReadAllText(outputPath);
        jsonContent.Should().NotBeEmpty();

        var deserializedEvents = JsonSerializer.Deserialize<List<CalendarEvent>>(jsonContent);
        deserializedEvents.Should().HaveCount(1);
        var evt = deserializedEvents!.First();
        evt.Summary.Should().Be("All Day Event");
        evt.StartDateTime.Should().Be(new DateTime(2024, 12, 15, 0, 0, 0));
        evt.EndDateTime.Should().Be(new DateTime(2024, 12, 16, 0, 0, 0));
    }

    [Fact]
    public void Export_WithMultipleEvents_ShouldSerializeAllEvents()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create().WithSummary("Event 1").WithUid("uid-1").Build(),
            TestEventBuilder.Create().WithSummary("Event 2").WithUid("uid-2").Build(),
            TestEventBuilder.Create().WithSummary("Event 3").WithUid("uid-3").Build()
        };

        var outputPath = GetTempFilePath(".json");

        // Act
        _jsonExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var jsonContent = File.ReadAllText(outputPath);
        jsonContent.Should().NotBeEmpty();

        var deserializedEvents = JsonSerializer.Deserialize<List<CalendarEvent>>(jsonContent);
        deserializedEvents.Should().HaveCount(3);
        deserializedEvents.Should().Contain(e => e.Summary == "Event 1" && e.Uid == "uid-1");
        deserializedEvents.Should().Contain(e => e.Summary == "Event 2" && e.Uid == "uid-2");
        deserializedEvents.Should().Contain(e => e.Summary == "Event 3" && e.Uid == "uid-3");
    }

    [Fact]
    public void Export_WithNullEventProperties_ShouldSerializeWithNullValues()
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

        var outputPath = GetTempFilePath(".json");

        // Act
        _jsonExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var jsonContent = File.ReadAllText(outputPath);
        jsonContent.Should().NotBeEmpty();

        var deserializedEvents = JsonSerializer.Deserialize<List<CalendarEvent>>(jsonContent);
        deserializedEvents.Should().HaveCount(1);
        var evt = deserializedEvents!.First();
        evt.Summary.Should().Be("Event with nulls");
        evt.Description.Should().BeNull();
        evt.Location.Should().BeNull();
        evt.StartDateTime.Should().BeNull();
        evt.EndDateTime.Should().BeNull();
        evt.Organizer.Should().BeNull();
        evt.Attendees.Should().BeEmpty();
        evt.Status.Should().BeNull();
        evt.RecurrenceRule.Should().BeNull();
        evt.Uid.Should().BeNull();
        evt.CalendarName.Should().BeNull();
        evt.Created.Should().BeNull();
        evt.LastModified.Should().BeNull();
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

        var outputPath = GetTempFilePath(".json");

        // Act
        _jsonExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var jsonContent = File.ReadAllText(outputPath);
        jsonContent.Should().NotBeEmpty();

        var deserializedEvents = JsonSerializer.Deserialize<List<CalendarEvent>>(jsonContent);
        deserializedEvents.Should().HaveCount(1);
        var evt = deserializedEvents!.First();
        evt.Summary.Should().Be("Recurring Event");
        evt.RecurrenceRule.Should().Be("FREQ=WEEKLY;BYDAY=MO,WE,FR");
    }

    [Fact]
    public void Export_WithInvalidOutputPath_ShouldThrowException()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create().Build()
        };

        var invalidPath = Path.Combine("invalid", "path", "that", "does", "not", "exist", "file.json");

        // Act & Assert
        var exception = Assert.Throws<DirectoryNotFoundException>(() => _jsonExporter.Export(events, invalidPath));
        exception.Should().NotBeNull();
    }

    [Fact]
    public void Export_WithLargeNumberOfEvents_ShouldProcessAllEvents()
    {
        // Arrange
        var events = new List<CalendarEvent>();
        for (var i = 0; i < 1000; i++)
            events.Add(TestEventBuilder.Create().WithSummary($"Event {i}").WithUid($"uid-{i}").Build());

        var outputPath = GetTempFilePath(".json");

        // Act
        _jsonExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var jsonContent = File.ReadAllText(outputPath);
        jsonContent.Should().NotBeEmpty();

        var deserializedEvents = JsonSerializer.Deserialize<List<CalendarEvent>>(jsonContent);
        deserializedEvents.Should().HaveCount(1000);
        deserializedEvents.Should().Contain(e => e.Summary == "Event 0" && e.Uid == "uid-0");
        deserializedEvents.Should().Contain(e => e.Summary == "Event 999" && e.Uid == "uid-999");
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
        var exception = Assert.Throws<ArgumentException>(() => _jsonExporter.Export(events, outputPath!));
        exception.Should().NotBeNull();
    }

    [Fact]
    public void Export_WithComplexEventData_ShouldPreserveAllData()
    {
        // Arrange
        var complexEvent = TestEventBuilder.Create()
            .WithSummary("Complex Event")
            .WithDescription("Multi-line\ndescription with\nspecial chars: <>&\"'")
            .WithLocation("Complex Location with 📍 emoji")
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
        var outputPath = GetTempFilePath(".json");

        // Act
        _jsonExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var jsonContent = File.ReadAllText(outputPath);
        jsonContent.Should().NotBeEmpty();

        var deserializedEvents = JsonSerializer.Deserialize<List<CalendarEvent>>(jsonContent);
        deserializedEvents.Should().HaveCount(1);
        var evt = deserializedEvents!.First();

        evt.Summary.Should().Be("Complex Event");
        evt.Description.Should().Be("Multi-line\ndescription with\nspecial chars: <>&\"'");
        evt.Location.Should().Be("Complex Location with 📍 emoji");
        evt.StartDateTime.Should().Be(new DateTime(2024, 12, 15, 10, 30, 45));
        evt.EndDateTime.Should().Be(new DateTime(2024, 12, 15, 12, 45, 30));
        evt.Organizer.Should().Be("complex.organizer@example.com");
        evt.Attendees.Should().HaveCount(3);
        evt.Status.Should().Be("TENTATIVE");
        evt.RecurrenceRule.Should().Be("FREQ=MONTHLY;BYMONTHDAY=15");
        evt.Uid.Should().Be("complex-uid-123");
        evt.CalendarName.Should().Be("Complex Calendar");
        evt.Created.Should().Be(new DateTime(2024, 12, 1, 8, 0, 0));
        evt.LastModified.Should().Be(new DateTime(2024, 12, 10, 14, 30, 0));
    }

    [Fact]
    public void Export_ShouldCreateFormattedJsonFile()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create()
                .WithSummary("Formatted Event")
                .WithDescription("This should be formatted")
                .Build()
        };

        var outputPath = GetTempFilePath(".json");

        // Act
        _jsonExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var jsonContent = File.ReadAllText(outputPath);
        jsonContent.Should().NotBeEmpty();

        // Check that the JSON is formatted (indented)
        jsonContent.Should().Contain("  "); // Should contain indentation
        jsonContent.Should().Contain("\n"); // Should contain newlines
    }

    private string GetTempFilePath(string extension)
    {
        var tempFile = Path.GetTempFileName();
        var fileWithExtension = Path.ChangeExtension(tempFile, extension);
        _tempFiles.Add(fileWithExtension);
        return fileWithExtension;
    }
}