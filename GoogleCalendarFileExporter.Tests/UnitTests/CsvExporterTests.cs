using FluentAssertions;
using GoogleCalendarFileExporter.Classes;
using GoogleCalendarFileExporter.Exporters;
using GoogleCalendarFileExporter.Services;
using GoogleCalendarFileExporter.Tests.TestUtilities;
using Xunit;

namespace GoogleCalendarFileExporter.Tests.UnitTests;

public class CsvExporterTests : IDisposable
{
    private readonly CsvExporter _csvExporter;
    private readonly MockLoggingService _mockLogger;
    private readonly List<string> _tempFiles = new();
    private readonly TimezoneService _timezoneService;

    public CsvExporterTests()
    {
        _mockLogger = new MockLoggingService();
        _timezoneService = new TimezoneService(_mockLogger);
        _csvExporter = new CsvExporter();
    }

    public void Dispose()
    {
        foreach (var tempFile in _tempFiles) TestFileHelper.CleanupTempFile(tempFile);
    }

    [Fact]
    public void Export_WithValidEvents_ShouldCreateCsvFile()
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
                .Build()
        };

        var outputPath = GetTempFilePath(".csv");

        // Act
        _csvExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var csvContent = File.ReadAllText(outputPath);
        csvContent.Should()
            .Contain(
                "Subject,Start Date,Start Time,End Date,End Time,All Day Event,Description,Location,Attendees,Creator,Status,Recurrence,Event ID,Calendar ID,Created,Modified");
        csvContent.Should().Contain("Test Event");
        csvContent.Should().Contain("Test Description");
        csvContent.Should().Contain("Test Location");
        csvContent.Should().Contain("organizer@example.com");
        csvContent.Should().Contain("attendee1@example.com; attendee2@example.com");
        csvContent.Should().Contain("CONFIRMED");
    }

    [Fact]
    public async Task ExportAsync_WithValidEvents_ShouldCreateCsvFile()
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

        var outputPath = GetTempFilePath(".csv");

        // Act
        await _csvExporter.ExportAsync(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var csvContent = File.ReadAllText(outputPath);
        csvContent.Should().Contain("Async Test Event");
        csvContent.Should().Contain("Async Test Description");
        csvContent.Should().Contain("Async Test Location");
    }

    [Fact]
    public void Export_WithEmptyEventsList_ShouldCreateCsvFileWithHeaderOnly()
    {
        // Arrange
        var events = new List<CalendarEvent>();
        var outputPath = GetTempFilePath(".csv");

        // Act
        _csvExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var csvContent = File.ReadAllText(outputPath);
        csvContent.Should()
            .Contain(
                "Subject,Start Date,Start Time,End Date,End Time,All Day Event,Description,Location,Attendees,Creator,Status,Recurrence,Event ID,Calendar ID,Created,Modified");
        csvContent.Split('\n').Should().HaveCount(2); // Header + empty line
    }

    [Fact]
    public void Export_WithSpecialCharactersInEventData_ShouldEscapeProperlyInCsv()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create()
                .WithSummary("Event with \"quotes\" and commas,")
                .WithDescription("Description with\nnewlines and \"quotes\"")
                .WithLocation("Location with, special; characters")
                .Build()
        };

        var outputPath = GetTempFilePath(".csv");

        // Act
        _csvExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var csvContent = File.ReadAllText(outputPath);
        csvContent.Should().Contain("\"Event with \"\"quotes\"\" and commas,\"");
        csvContent.Should().Contain("\"Description with\nnewlines and \"\"quotes\"\"\"");
        csvContent.Should().Contain("\"Location with, special; characters\"");
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

        var outputPath = GetTempFilePath(".csv");

        // Act
        _csvExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var csvContent = File.ReadAllText(outputPath);
        csvContent.Should().Contain("True"); // All Day Event column
        csvContent.Should().Contain("2024-12-15"); // Start Date
        csvContent.Should().Contain("2024-12-16"); // End Date
    }

    [Fact]
    public void Export_WithMultipleEvents_ShouldCreateAllRowsInCsv()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create().WithSummary("Event 1").Build(),
            TestEventBuilder.Create().WithSummary("Event 2").Build(),
            TestEventBuilder.Create().WithSummary("Event 3").Build()
        };

        var outputPath = GetTempFilePath(".csv");

        // Act
        _csvExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var csvContent = File.ReadAllText(outputPath);
        csvContent.Should().Contain("Event 1");
        csvContent.Should().Contain("Event 2");
        csvContent.Should().Contain("Event 3");
        csvContent.Split('\n').Should().HaveCount(5); // Header + 3 events + empty line
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

        var outputPath = GetTempFilePath(".csv");

        // Act
        _csvExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var csvContent = File.ReadAllText(outputPath);
        csvContent.Should().Contain("Event with nulls");
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

        var outputPath = GetTempFilePath(".csv");

        // Act
        _csvExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var csvContent = File.ReadAllText(outputPath);
        csvContent.Should().Contain("Recurring Event");
        csvContent.Should().Contain("FREQ=WEEKLY;BYDAY=MO,WE,FR");
    }

    [Fact]
    public void Export_WithInvalidOutputPath_ShouldThrowException()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create().Build()
        };

        var invalidPath = Path.Combine("invalid", "path", "that", "does", "not", "exist", "file.csv");

        // Act & Assert
        var exception = Assert.Throws<DirectoryNotFoundException>(() => _csvExporter.Export(events, invalidPath));
        exception.Should().NotBeNull();
    }

    [Fact]
    public void Export_WithLargeNumberOfEvents_ShouldProcessAllEvents()
    {
        // Arrange
        var events = new List<CalendarEvent>();
        for (var i = 0; i < 1000; i++) events.Add(TestEventBuilder.Create().WithSummary($"Event {i}").Build());

        var outputPath = GetTempFilePath(".csv");

        // Act
        _csvExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var csvContent = File.ReadAllText(outputPath);
        csvContent.Should().Contain("Event 0");
        csvContent.Should().Contain("Event 999");
        csvContent.Split('\n').Should().HaveCount(1002); // Header + 1000 events + empty line
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
        var exception = Assert.Throws<ArgumentException>(() => _csvExporter.Export(events, outputPath!));
        exception.Should().NotBeNull();
    }

    private string GetTempFilePath(string extension)
    {
        var tempFile = Path.GetTempFileName();
        var fileWithExtension = Path.ChangeExtension(tempFile, extension);
        _tempFiles.Add(fileWithExtension);
        return fileWithExtension;
    }
}