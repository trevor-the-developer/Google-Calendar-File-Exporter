using FluentAssertions;
using GoogleCalendarFileExporter.Classes;
using GoogleCalendarFileExporter.Exporters;
using GoogleCalendarFileExporter.Services;
using GoogleCalendarFileExporter.Tests.TestUtilities;
using OfficeOpenXml;
using Xunit;

namespace GoogleCalendarFileExporter.Tests.UnitTests;

public class ExcelExporterTests : IDisposable
{
    private readonly ExcelExporter _excelExporter;
    private readonly MockLoggingService _mockLogger;
    private readonly List<string> _tempFiles = new();
    private readonly TimezoneService _timezoneService;

    public ExcelExporterTests()
    {
        _mockLogger = new MockLoggingService();
        _timezoneService = new TimezoneService(_mockLogger);
        _excelExporter = new ExcelExporter();

        // Set EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public void Dispose()
    {
        foreach (var tempFile in _tempFiles) TestFileHelper.CleanupTempFile(tempFile);
    }

    [Fact]
    public void Export_WithValidEvents_ShouldCreateExcelFile()
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

        var outputPath = GetTempFilePath(".xlsx");

        // Act
        _excelExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets.First();
        worksheet.Should().NotBeNull();

        // Check headers
        worksheet.Cells[1, 1].Value.Should().Be("Subject");
        worksheet.Cells[1, 2].Value.Should().Be("Start Date");
        worksheet.Cells[1, 3].Value.Should().Be("Start Time");
        worksheet.Cells[1, 4].Value.Should().Be("End Date");
        worksheet.Cells[1, 5].Value.Should().Be("End Time");
        worksheet.Cells[1, 6].Value.Should().Be("All Day Event");
        worksheet.Cells[1, 7].Value.Should().Be("Description");
        worksheet.Cells[1, 8].Value.Should().Be("Location");
        worksheet.Cells[1, 9].Value.Should().Be("Attendees");
        worksheet.Cells[1, 10].Value.Should().Be("Creator");
        worksheet.Cells[1, 11].Value.Should().Be("Status");
        worksheet.Cells[1, 12].Value.Should().Be("Recurrence");
        worksheet.Cells[1, 13].Value.Should().Be("Event ID");
        worksheet.Cells[1, 14].Value.Should().Be("Calendar ID");
        worksheet.Cells[1, 15].Value.Should().Be("Created");
        worksheet.Cells[1, 16].Value.Should().Be("Modified");

        // Check data
        worksheet.Cells[2, 1].Value.Should().Be("Test Event");
        worksheet.Cells[2, 7].Value.Should().Be("Test Description");
        worksheet.Cells[2, 8].Value.Should().Be("Test Location");
        worksheet.Cells[2, 9].Value.Should().Be("attendee1@example.com; attendee2@example.com");
        worksheet.Cells[2, 10].Value.Should().Be("organizer@example.com");
        worksheet.Cells[2, 11].Value.Should().Be("CONFIRMED");
        worksheet.Cells[2, 13].Value.Should().Be("test-uid");
        worksheet.Cells[2, 14].Value.Should().Be("Test Calendar");
    }

    [Fact]
    public async Task ExportAsync_WithValidEvents_ShouldCreateExcelFile()
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

        var outputPath = GetTempFilePath(".xlsx");

        // Act
        await _excelExporter.ExportAsync(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets.First();
        worksheet.Cells[2, 1].Value.Should().Be("Async Test Event");
        worksheet.Cells[2, 7].Value.Should().Be("Async Test Description");
        worksheet.Cells[2, 8].Value.Should().Be("Async Test Location");
    }

    [Fact]
    public void Export_WithEmptyEventsList_ShouldCreateExcelFileWithHeaderOnly()
    {
        // Arrange
        var events = new List<CalendarEvent>();
        var outputPath = GetTempFilePath(".xlsx");

        // Act
        _excelExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets.First();
        worksheet.Should().NotBeNull();

        // Check headers exist
        worksheet.Cells[1, 1].Value.Should().Be("Subject");

        // Check no data rows
        worksheet.Cells[2, 1].Value.Should().BeNull();
    }

    [Fact]
    public void Export_WithSpecialCharactersInEventData_ShouldHandleCorrectly()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create()
                .WithSummary("Event with \"quotes\" and special chars: <>&#")
                .WithDescription("Description with\nnewlines and \"quotes\"")
                .WithLocation("Location with unicode: 🎉 and special chars: @#$%")
                .Build()
        };

        var outputPath = GetTempFilePath(".xlsx");

        // Act
        _excelExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets.First();
        worksheet.Cells[2, 1].Value.Should().Be("Event with \"quotes\" and special chars: <>&#");
        worksheet.Cells[2, 7].Value.Should().Be("Description with\nnewlines and \"quotes\"");
        worksheet.Cells[2, 8].Value.Should().Be("Location with unicode: 🎉 and special chars: @#$%");
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

        var outputPath = GetTempFilePath(".xlsx");

        // Act
        _excelExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets.First();
        worksheet.Cells[2, 1].Value.Should().Be("All Day Event");
        worksheet.Cells[2, 6].Value.Should().Be("True"); // All Day Event column
        worksheet.Cells[2, 2].Value.Should().Be("2024-12-15"); // Start Date
        worksheet.Cells[2, 4].Value.Should().Be("2024-12-16"); // End Date
    }

    [Fact]
    public void Export_WithMultipleEvents_ShouldCreateAllRowsInExcel()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create().WithSummary("Event 1").WithUid("uid-1").Build(),
            TestEventBuilder.Create().WithSummary("Event 2").WithUid("uid-2").Build(),
            TestEventBuilder.Create().WithSummary("Event 3").WithUid("uid-3").Build()
        };

        var outputPath = GetTempFilePath(".xlsx");

        // Act
        _excelExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets.First();
        worksheet.Cells[2, 1].Value.Should().Be("Event 1");
        worksheet.Cells[3, 1].Value.Should().Be("Event 2");
        worksheet.Cells[4, 1].Value.Should().Be("Event 3");
        worksheet.Cells[2, 13].Value.Should().Be("uid-1");
        worksheet.Cells[3, 13].Value.Should().Be("uid-2");
        worksheet.Cells[4, 13].Value.Should().Be("uid-3");
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

        var outputPath = GetTempFilePath(".xlsx");

        // Act
        _excelExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets.First();
        worksheet.Cells[2, 1].Value.Should().Be("Event with nulls");
        worksheet.Cells[2, 7].Value.Should().BeNull(); // Description
        worksheet.Cells[2, 8].Value.Should().BeNull(); // Location
        worksheet.Cells[2, 10].Value.Should().BeNull(); // Organizer
        worksheet.Cells[2, 11].Value.Should().BeNull(); // Status
        worksheet.Cells[2, 12].Value.Should().BeNull(); // RecurrenceRule
        worksheet.Cells[2, 13].Value.Should().BeNull(); // Uid
        worksheet.Cells[2, 14].Value.Should().BeNull(); // CalendarName
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

        var outputPath = GetTempFilePath(".xlsx");

        // Act
        _excelExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets.First();
        worksheet.Cells[2, 1].Value.Should().Be("Recurring Event");
        worksheet.Cells[2, 12].Value.Should().Be("FREQ=WEEKLY;BYDAY=MO,WE,FR");
    }

    [Fact]
    public void Export_WithInvalidOutputPath_ShouldThrowException()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create().Build()
        };

        var invalidPath = Path.Combine("invalid", "path", "that", "does", "not", "exist", "file.xlsx");

        // Act & Assert
        var exception = Assert.Throws<DirectoryNotFoundException>(() => _excelExporter.Export(events, invalidPath));
        exception.Should().NotBeNull();
    }

    [Fact]
    public void Export_WithLargeNumberOfEvents_ShouldProcessAllEvents()
    {
        // Arrange
        var events = new List<CalendarEvent>();
        for (var i = 0; i < 1000; i++)
            events.Add(TestEventBuilder.Create().WithSummary($"Event {i}").WithUid($"uid-{i}").Build());

        var outputPath = GetTempFilePath(".xlsx");

        // Act
        _excelExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets.First();
        worksheet.Cells[2, 1].Value.Should().Be("Event 0");
        worksheet.Cells[1001, 1].Value.Should().Be("Event 999");
        worksheet.Cells[2, 13].Value.Should().Be("uid-0");
        worksheet.Cells[1001, 13].Value.Should().Be("uid-999");
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
        var exception = Assert.Throws<ArgumentException>(() => _excelExporter.Export(events, outputPath!));
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

        var outputPath = GetTempFilePath(".xlsx");

        // Act
        _excelExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets.First();

        // Regular event
        worksheet.Cells[2, 1].Value.Should().Be("Regular Event");
        worksheet.Cells[2, 2].Value.Should().Be("2024-12-15"); // Start Date
        worksheet.Cells[2, 3].Value.Should().Be("2024-12-15 10:30:00"); // Start Time
        worksheet.Cells[2, 4].Value.Should().Be("2024-12-15"); // End Date
        worksheet.Cells[2, 5].Value.Should().Be("2024-12-15 11:30:00"); // End Time
        worksheet.Cells[2, 6].Value.Should().Be("False"); // All Day Event

        // All day event
        worksheet.Cells[3, 1].Value.Should().Be("All Day Event");
        worksheet.Cells[3, 2].Value.Should().Be("2024-12-16"); // Start Date
        worksheet.Cells[3, 3].Value.Should().Be("2024-12-16 00:00:00"); // Start Time
        worksheet.Cells[3, 4].Value.Should().Be("2024-12-17"); // End Date
        worksheet.Cells[3, 5].Value.Should().Be("2024-12-17 00:00:00"); // End Time
        worksheet.Cells[3, 6].Value.Should().Be("True"); // All Day Event
    }

    [Fact]
    public void Export_ShouldCreateWorksheetWithCorrectName()
    {
        // Arrange
        var events = new List<CalendarEvent>
        {
            TestEventBuilder.Create().WithSummary("Test Event").Build()
        };

        var outputPath = GetTempFilePath(".xlsx");

        // Act
        _excelExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        using var package = new ExcelPackage(new FileInfo(outputPath));
        package.Workbook.Worksheets.Should().HaveCount(1);
        var worksheet = package.Workbook.Worksheets.First();
        worksheet.Name.Should().Be("Calendar Events");
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
        var outputPath = GetTempFilePath(".xlsx");

        // Act
        _excelExporter.Export(events, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets.First();

        worksheet.Cells[2, 1].Value.Should().Be("Complex Event");
        worksheet.Cells[2, 7].Value.Should().Be("Multi-line\ndescription with\nspecial chars");
        worksheet.Cells[2, 8].Value.Should().Be("Complex Location");
        worksheet.Cells[2, 9].Value.Should()
            .Be("attendee1@example.com; attendee2@example.com; attendee3@example.com");
        worksheet.Cells[2, 10].Value.Should().Be("complex.organizer@example.com");
        worksheet.Cells[2, 11].Value.Should().Be("TENTATIVE");
        worksheet.Cells[2, 12].Value.Should().Be("FREQ=MONTHLY;BYMONTHDAY=15");
        worksheet.Cells[2, 13].Value.Should().Be("complex-uid-123");
        worksheet.Cells[2, 14].Value.Should().Be("Complex Calendar");
        worksheet.Cells[2, 15].Value.Should().Be("2024-12-01 08:00:00");
        worksheet.Cells[2, 16].Value.Should().Be("2024-12-10 14:30:00");
    }

    private string GetTempFilePath(string extension)
    {
        var tempFile = Path.GetTempFileName();
        var fileWithExtension = Path.ChangeExtension(tempFile, extension);
        _tempFiles.Add(fileWithExtension);
        return fileWithExtension;
    }
}