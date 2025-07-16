using System.IO.Compression;
using FluentAssertions;
using GoogleCalendarFileExporter.Classes;
using GoogleCalendarFileExporter.Interfaces;
using GoogleCalendarFileExporter.Services;
using GoogleCalendarFileExporter.Tests.Fixtures;
using GoogleCalendarFileExporter.Tests.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GoogleCalendarFileExporter.Tests.IntegrationTests;

public class FileProcessingTests : IDisposable
{
    private readonly IFileProcessor _fileProcessor;
    private readonly MockLoggingService _mockLogger;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<string> _tempDirectories = new();
    private readonly List<string> _tempFiles = new();
    private readonly TimezoneService _timezoneService;

    public FileProcessingTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggingService, MockLoggingService>();
        services.AddSingleton<TimezoneService>();
        services.AddSingleton<IcsParser>();
        services.AddSingleton<IFileProcessor, FileProcessor>();

        _serviceProvider = services.BuildServiceProvider();
        _fileProcessor = _serviceProvider.GetRequiredService<IFileProcessor>();
        _timezoneService = _serviceProvider.GetRequiredService<TimezoneService>();
        _mockLogger = (MockLoggingService)_serviceProvider.GetRequiredService<ILoggingService>();
    }

    public void Dispose()
    {
        foreach (var tempFile in _tempFiles) TestFileHelper.CleanupTempFile(tempFile);

        foreach (var tempDir in _tempDirectories) TestFileHelper.CleanupTempDirectory(tempDir);

        if (_serviceProvider is IDisposable disposable) disposable.Dispose();
    }

    [Fact]
    public void ProcessIcsFile_WithComplexEventStructure_ShouldParseAllFields()
    {
        // Arrange
        var complexIcsContent = @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Test//Test Calendar//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
BEGIN:VEVENT
DTSTART:20241215T140000Z
DTEND:20241215T150000Z
DTSTAMP:20241201T120000Z
UID:complex-event-123@example.com
CREATED:20241201T100000Z
LAST-MODIFIED:20241210T140000Z
SUMMARY:Complex Meeting with Multiple Attendees
DESCRIPTION:This is a complex meeting with multiple attendees\, special characters like \n newlines\, and various properties.
LOCATION:Conference Room B\, Building 2\, Floor 3
ORGANIZER;CN=John Doe:mailto:john.doe@example.com
ATTENDEE;CN=Jane Smith;ROLE=REQ-PARTICIPANT;PARTSTAT=ACCEPTED;RSVP=TRUE:mailto:jane.smith@example.com
ATTENDEE;CN=Bob Johnson;ROLE=OPT-PARTICIPANT;PARTSTAT=NEEDS-ACTION;RSVP=TRUE:mailto:bob.johnson@example.com
ATTENDEE;CN=Alice Brown;ROLE=REQ-PARTICIPANT;PARTSTAT=DECLINED;RSVP=TRUE:mailto:alice.brown@example.com
STATUS:CONFIRMED
CLASS:PUBLIC
PRIORITY:5
RRULE:FREQ=WEEKLY;BYDAY=MO;UNTIL=20250315T140000Z
END:VEVENT
END:VCALENDAR";

        var tempFile = CreateTempIcsFile(complexIcsContent);

        // Act
        var events = _fileProcessor.ProcessIcsFile(tempFile);

        // Assert
        events.Should().HaveCount(1);
        var evt = events.First();

        evt.Summary.Should().Be("Complex Meeting with Multiple Attendees");
        evt.Description.Should().Contain("This is a complex meeting");
        evt.Description.Should().Contain("special characters");
        evt.Location.Should().Be("Conference Room B, Building 2, Floor 3");
        evt.Organizer.Should().Be("john.doe@example.com");
        evt.Attendees.Should().HaveCount(3);
        evt.Attendees.Should().Contain("jane.smith@example.com");
        evt.Attendees.Should().Contain("bob.johnson@example.com");
        evt.Attendees.Should().Contain("alice.brown@example.com");
        evt.Status.Should().Be("CONFIRMED");
        evt.RecurrenceRule.Should().Be("FREQ=WEEKLY;BYDAY=MO;UNTIL=20250315T140000Z");
        evt.Uid.Should().Be("complex-event-123@example.com");
        evt.StartDateTime.Should().NotBeNull();
        evt.EndDateTime.Should().NotBeNull();
        evt.Created.Should().NotBeNull();
        evt.LastModified.Should().NotBeNull();
    }

    [Fact]
    public void ProcessZipFile_WithMixedFileTypes_ShouldProcessOnlyIcsFiles()
    {
        // Arrange
        var tempZipFile = CreateZipWithMixedFileTypes();

        // Act
        var events = _fileProcessor.ProcessZipFile(tempZipFile);

        // Assert
        events.Should().HaveCount(2); // Only ICS files should be processed
        events.Should().Contain(e => e.Summary == "Team Meeting");
        events.Should().Contain(e => e.Summary == "All Day Event");
    }

    [Fact]
    public async Task ProcessZipFileAsync_WithNestedDirectories_ShouldProcessAllIcsFiles()
    {
        // Arrange
        var tempZipFile = CreateZipWithNestedDirectories();

        // Act
        var events = await _fileProcessor.ProcessZipFileAsync(tempZipFile);

        // Assert
        events.Should().HaveCount(3);
        events.Should().Contain(e => e.Summary == "Team Meeting");
        events.Should().Contain(e => e.Summary == "All Day Event");
        events.Should().Contain(e => e.Summary == "Daily Standup");
    }

    [Fact]
    public void ProcessIcsFile_WithMultipleEvents_ShouldParseAllEvents()
    {
        // Arrange
        var multipleEventsIcs = @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Test//Test Calendar//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
BEGIN:VEVENT
DTSTART:20241215T100000Z
DTEND:20241215T110000Z
SUMMARY:First Event
UID:first-event@example.com
END:VEVENT
BEGIN:VEVENT
DTSTART:20241216T140000Z
DTEND:20241216T150000Z
SUMMARY:Second Event
UID:second-event@example.com
END:VEVENT
BEGIN:VEVENT
DTSTART:20241217T090000Z
DTEND:20241217T100000Z
SUMMARY:Third Event
UID:third-event@example.com
END:VEVENT
END:VCALENDAR";

        var tempFile = CreateTempIcsFile(multipleEventsIcs);

        // Act
        var events = _fileProcessor.ProcessIcsFile(tempFile);

        // Assert
        events.Should().HaveCount(3);
        events.Should().Contain(e => e.Summary == "First Event");
        events.Should().Contain(e => e.Summary == "Second Event");
        events.Should().Contain(e => e.Summary == "Third Event");

        // Verify unique UIDs
        var uids = events.Select(e => e.Uid).ToList();
        uids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ProcessIcsFile_WithTimezoneInformation_ShouldHandleTimezoneCorrectly()
    {
        // Arrange
        var timezoneIcsContent = @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Test//Test Calendar//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
BEGIN:VTIMEZONE
TZID:America/New_York
BEGIN:STANDARD
DTSTART:20231105T060000
RRULE:FREQ=YEARLY;BYMONTH=11;BYDAY=1SU
TZNAME:EST
TZOFFSETFROM:-0400
TZOFFSETTO:-0500
END:STANDARD
BEGIN:DAYLIGHT
DTSTART:20240310T070000
RRULE:FREQ=YEARLY;BYMONTH=3;BYDAY=2SU
TZNAME:EDT
TZOFFSETFROM:-0500
TZOFFSETTO:-0400
END:DAYLIGHT
END:VTIMEZONE
BEGIN:VEVENT
DTSTART;TZID=America/New_York:20241215T100000
DTEND;TZID=America/New_York:20241215T110000
SUMMARY:Timezone Event
DESCRIPTION:Event with timezone information
LOCATION:New York Office
UID:timezone-event@example.com
END:VEVENT
END:VCALENDAR";

        var tempFile = CreateTempIcsFile(timezoneIcsContent);

        // Act
        var events = _fileProcessor.ProcessIcsFile(tempFile);

        // Assert
        events.Should().HaveCount(1);
        var evt = events.First();
        evt.Summary.Should().Be("Timezone Event");
        evt.StartDateTime.Should().NotBeNull();
        evt.EndDateTime.Should().NotBeNull();

        // Verify timezone handling didn't cause null dates
        evt.StartDateTime.Should().Be(new DateTime(2024, 12, 15, 10, 0, 0));
        evt.EndDateTime.Should().Be(new DateTime(2024, 12, 15, 11, 0, 0));
    }

    [Fact]
    public void ProcessIcsFile_WithMalformedContent_ShouldLogErrorAndReturnEmptyList()
    {
        // Arrange
        var malformedIcsContent = @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Test//Test Calendar//EN
BEGIN:VEVENT
DTSTART:20241215T100000Z
SUMMARY:Event without END:VEVENT
UID:malformed-event@example.com
END:VCALENDAR";

        var tempFile = CreateTempIcsFile(malformedIcsContent);

        // Act
        var events = _fileProcessor.ProcessIcsFile(tempFile);

        // Assert
        events.Should().BeEmpty();
        _mockLogger.ContainsLogLevel("Error").Should().BeTrue();
    }

    [Fact]
    public void ProcessIcsFile_WithUnicodeCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var unicodeIcsContent = @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Test//Test Calendar//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
BEGIN:VEVENT
DTSTART:20241215T100000Z
DTEND:20241215T110000Z
SUMMARY:Événement avec caractères spéciaux 🎉
DESCRIPTION:Réunion d'équipe avec café ☕ et gâteau 🍰
LOCATION:Salle de conférence Café
ORGANIZER:mailto:équipe@example.com
ATTENDEE:mailto:andré@example.com
ATTENDEE:mailto:josé@example.com
UID:unicode-event@example.com
END:VEVENT
END:VCALENDAR";

        var tempFile = CreateTempIcsFile(unicodeIcsContent);

        // Act
        var events = _fileProcessor.ProcessIcsFile(tempFile);

        // Assert
        events.Should().HaveCount(1);
        var evt = events.First();
        evt.Summary.Should().Be("Événement avec caractères spéciaux 🎉");
        evt.Description.Should().Be("Réunion d'équipe avec café ☕ et gâteau 🍰");
        evt.Location.Should().Be("Salle de conférence Café");
        evt.Organizer.Should().Be("équipe@example.com");
        evt.Attendees.Should().Contain("andré@example.com");
        evt.Attendees.Should().Contain("josé@example.com");
    }

    [Fact]
    public void ProcessZipFile_WithPasswordProtectedZip_ShouldHandleGracefully()
    {
        // Arrange
        var tempZipFile = CreatePasswordProtectedZip();

        // Act
        var events = _fileProcessor.ProcessZipFile(tempZipFile);

        // Assert
        events.Should().BeEmpty();
        _mockLogger.ContainsLogLevel("Error").Should().BeTrue();
    }

    [Fact]
    public void ProcessIcsFile_WithVeryLargeFile_ShouldProcessWithinReasonableTime()
    {
        // Arrange
        var largeIcsContent = GenerateLargeIcsFile(1000); // 1000 events
        var tempFile = CreateTempIcsFile(largeIcsContent);

        // Act
        var startTime = DateTime.Now;
        var events = _fileProcessor.ProcessIcsFile(tempFile);
        var endTime = DateTime.Now;

        // Assert
        events.Should().HaveCount(1000);
        var processingTime = endTime - startTime;
        processingTime.Should().BeLessThan(TimeSpan.FromSeconds(10)); // Should process within 10 seconds
    }

    [Fact]
    public async Task ProcessIcsFileAsync_WithMultipleConcurrentOperations_ShouldHandleCorrectly()
    {
        // Arrange
        var icsFiles = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            var content = TestData.SimpleEventIcs.Replace("Team Meeting", $"Meeting {i}");
            icsFiles.Add(CreateTempIcsFile(content));
        }

        // Act
        var tasks = icsFiles.Select(file => _fileProcessor.ProcessIcsFileAsync(file)).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        foreach (var result in results) result.Should().HaveCount(1);

        var allEvents = results.SelectMany(r => r).ToList();
        allEvents.Should().HaveCount(10);

        // Verify all events have different summaries
        var summaries = allEvents.Select(e => e.Summary).ToList();
        summaries.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ProcessIcsFile_WithDifferentLineEndings_ShouldHandleAll()
    {
        // Test with different line endings (Windows, Unix, Mac)
        var testCases = new[]
        {
            TestData.SimpleEventIcs.Replace("\n", "\r\n"), // Windows
            TestData.SimpleEventIcs.Replace("\n", "\n"), // Unix
            TestData.SimpleEventIcs.Replace("\n", "\r") // Mac
        };

        foreach (var (content, index) in testCases.Select((content, index) => (content, index)))
        {
            // Arrange
            var tempFile = CreateTempIcsFile(content);

            // Act
            var events = _fileProcessor.ProcessIcsFile(tempFile);

            // Assert
            events.Should().HaveCount(1, $"Test case {index} failed");
            events.First().Summary.Should().Be("Team Meeting");
        }
    }

    [Fact]
    public void ProcessIcsFile_WithFoldedLines_ShouldUnfoldCorrectly()
    {
        // Arrange
        var foldedIcsContent = @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Test//Test Calendar//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
BEGIN:VEVENT
DTSTART:20241215T100000Z
DTEND:20241215T110000Z
SUMMARY:This is a very long summary that should be folded across multiple
  lines according to the RFC 5545 specification for line folding in iCalendar
  files
DESCRIPTION:This is also a very long description that contains multiple
  lines and should be properly unfolded when parsed by the ICS parser
  implementation
LOCATION:Conference Room A
UID:folded-event@example.com
END:VEVENT
END:VCALENDAR";

        var tempFile = CreateTempIcsFile(foldedIcsContent);

        // Act
        var events = _fileProcessor.ProcessIcsFile(tempFile);

        // Assert
        events.Should().HaveCount(1);
        var evt = events.First();
        evt.Summary.Should()
            .Be(
                "This is a very long summary that should be folded across multiple lines according to the RFC 5545 specification for line folding in iCalendar files");
        evt.Description.Should()
            .Be(
                "This is also a very long description that contains multiple lines and should be properly unfolded when parsed by the ICS parser implementation");
    }

    private string CreateTempIcsFile(string content)
    {
        var tempFile = GetTempFilePath(".ics");
        File.WriteAllText(tempFile, content);
        return tempFile;
    }

    private string CreateZipWithMixedFileTypes()
    {
        var tempZipFile = GetTempFilePath(".zip");

        using var archive = ZipFile.Open(tempZipFile, ZipArchiveMode.Create);
        // Add ICS files
        var icsEntry1 = archive.CreateEntry("calendar1.ics");
        using (var stream = icsEntry1.Open())
        using (var writer = new StreamWriter(stream))
        {
            writer.Write(TestData.SimpleEventIcs);
        }

        var icsEntry2 = archive.CreateEntry("calendar2.ics");
        using (var stream = icsEntry2.Open())
        using (var writer = new StreamWriter(stream))
        {
            writer.Write(TestData.AllDayEventIcs);
        }

        // Add non-ICS files
        var txtEntry = archive.CreateEntry("readme.txt");
        using (var stream = txtEntry.Open())
        using (var writer = new StreamWriter(stream))
        {
            writer.Write("This is a readme file");
        }

        var docEntry = archive.CreateEntry("document.docx");
        using (var stream = docEntry.Open())
        using (var writer = new StreamWriter(stream))
        {
            writer.Write("Fake document content");
        }

        return tempZipFile;
    }

    private string CreateZipWithNestedDirectories()
    {
        var tempZipFile = GetTempFilePath(".zip");

        using var archive = ZipFile.Open(tempZipFile, ZipArchiveMode.Create);
        // Root level ICS
        var rootEntry = archive.CreateEntry("root.ics");
        using (var stream = rootEntry.Open())
        using (var writer = new StreamWriter(stream))
        {
            writer.Write(TestData.SimpleEventIcs);
        }

        // Subdirectory ICS
        var subEntry = archive.CreateEntry("subfolder/calendar.ics");
        using (var stream = subEntry.Open())
        using (var writer = new StreamWriter(stream))
        {
            writer.Write(TestData.AllDayEventIcs);
        }

        // Nested subdirectory ICS
        var nestedEntry = archive.CreateEntry("subfolder/nested/deep.ics");
        using (var stream = nestedEntry.Open())
        using (var writer = new StreamWriter(stream))
        {
            writer.Write(TestData.RecurringEventIcs);
        }

        return tempZipFile;
    }

    private string CreatePasswordProtectedZip()
    {
        var tempZipFile = GetTempFilePath(".zip");

        // Create a simple zip file (password protection would require additional libraries)
        // For this test, we'll create a corrupted zip file that will cause an exception
        File.WriteAllBytes(tempZipFile, new byte[] { 0x50, 0x4B, 0x03, 0x04 }); // ZIP header but corrupted

        return tempZipFile;
    }

    private static string GenerateLargeIcsFile(int eventCount)
    {
        var content = @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Test//Test Calendar//EN
CALSCALE:GREGORIAN
METHOD:PUBLISH
";

        for (var i = 0; i < eventCount; i++)
            content += $@"BEGIN:VEVENT
DTSTART:20241215T{i:D2}0000Z
DTEND:20241215T{i:D2}3000Z
SUMMARY:Event {i}
DESCRIPTION:This is event number {i}
LOCATION:Location {i}
UID:event-{i}@example.com
END:VEVENT
";

        content += "END:VCALENDAR";
        return content;
    }

    private string GetTempFilePath(string extension)
    {
        var tempFile = Path.GetTempFileName();
        var fileWithExtension = Path.ChangeExtension(tempFile, extension);
        _tempFiles.Add(fileWithExtension);
        return fileWithExtension;
    }
}