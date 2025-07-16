using System.IO.Compression;
using FluentAssertions;
using GoogleCalendarFileExporter.Classes;
using GoogleCalendarFileExporter.Services;
using GoogleCalendarFileExporter.Tests.Fixtures;
using GoogleCalendarFileExporter.Tests.TestUtilities;
using Xunit;

namespace GoogleCalendarFileExporter.Tests.UnitTests;

public class FileProcessorTests : IDisposable
{
    private readonly FileProcessor _fileProcessor;
    private readonly MockLoggingService _mockLogger;
    private readonly List<string> _tempFiles = new();
    private readonly TimezoneService _timezoneService;

    public FileProcessorTests()
    {
        _mockLogger = new MockLoggingService();
        _timezoneService = new TimezoneService(_mockLogger);
        var icsParser = new IcsParser(_timezoneService, _mockLogger);
        _fileProcessor = new FileProcessor(icsParser, _mockLogger);
    }

    public void Dispose()
    {
        foreach (var tempFile in _tempFiles) TestFileHelper.CleanupTempFile(tempFile);
    }

    [Fact]
    public void ProcessIcsFile_WithValidFile_ShouldParseEventsCorrectly()
    {
        // Arrange
        var tempFile = CreateTempIcsFile(TestData.SimpleEventIcs);

        // Act
        var events = _fileProcessor.ProcessIcsFile(tempFile);

        // Assert
        events.Should().HaveCount(1);
        var evt = events.First();
        evt.Summary.Should().Be("Team Meeting");
        evt.Description.Should().Be("Weekly team sync");
        evt.Location.Should().Be("Conference Room A");
        evt.Organizer.Should().Be("john@example.com");
        evt.Attendees.Should().HaveCount(2);
    }

    [Fact]
    public async Task ProcessIcsFileAsync_WithValidFile_ShouldParseEventsCorrectly()
    {
        // Arrange
        var tempFile = CreateTempIcsFile(TestData.SimpleEventIcs);

        // Act
        var events = await _fileProcessor.ProcessIcsFileAsync(tempFile);

        // Assert
        events.Should().HaveCount(1);
        var evt = events.First();
        evt.Summary.Should().Be("Team Meeting");
        evt.Description.Should().Be("Weekly team sync");
        evt.Location.Should().Be("Conference Room A");
        evt.Organizer.Should().Be("john@example.com");
        evt.Attendees.Should().HaveCount(2);
    }

    [Fact]
    public void ProcessIcsFile_WithNonExistentFile_ShouldReturnEmptyList()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), "non-existent.ics");

        // Act
        var events = _fileProcessor.ProcessIcsFile(nonExistentFile);

        // Assert
        events.Should().BeEmpty();
        _mockLogger.ContainsLogLevel("Error").Should().BeTrue();
    }

    [Fact]
    public async Task ProcessIcsFileAsync_WithNonExistentFile_ShouldReturnEmptyList()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), "non-existent.ics");

        // Act
        var events = await _fileProcessor.ProcessIcsFileAsync(nonExistentFile);

        // Assert
        events.Should().BeEmpty();
        _mockLogger.ContainsLogLevel("Error").Should().BeTrue();
    }

    [Fact]
    public void ProcessIcsFile_WithInvalidContent_ShouldReturnEmptyList()
    {
        // Arrange
        var tempFile = CreateTempIcsFile("Invalid ICS content");

        // Act
        var events = _fileProcessor.ProcessIcsFile(tempFile);

        // Assert
        events.Should().BeEmpty();
    }

    [Fact]
    public void ProcessZipFile_WithValidZipContainingIcsFiles_ShouldParseAllEvents()
    {
        // Arrange
        var tempZipFile = CreateTempZipFile();

        // Act
        var events = _fileProcessor.ProcessZipFile(tempZipFile);

        // Assert
        events.Should().HaveCount(2);
        events.Should().Contain(e => e.Summary == "Team Meeting");
        events.Should().Contain(e => e.Summary == "All Day Event");
    }

    [Fact]
    public async Task ProcessZipFileAsync_WithValidZipContainingIcsFiles_ShouldParseAllEvents()
    {
        // Arrange
        var tempZipFile = CreateTempZipFile();

        // Act
        var events = await _fileProcessor.ProcessZipFileAsync(tempZipFile);

        // Assert
        events.Should().HaveCount(2);
        events.Should().Contain(e => e.Summary == "Team Meeting");
        events.Should().Contain(e => e.Summary == "All Day Event");
    }

    [Fact]
    public void ProcessZipFile_WithNonExistentFile_ShouldReturnEmptyList()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), "non-existent.zip");

        // Act
        var events = _fileProcessor.ProcessZipFile(nonExistentFile);

        // Assert
        events.Should().BeEmpty();
        _mockLogger.ContainsLogLevel("Error").Should().BeTrue();
    }

    [Fact]
    public async Task ProcessZipFileAsync_WithNonExistentFile_ShouldReturnEmptyList()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), "non-existent.zip");

        // Act
        var events = await _fileProcessor.ProcessZipFileAsync(nonExistentFile);

        // Assert
        events.Should().BeEmpty();
        _mockLogger.ContainsLogLevel("Error").Should().BeTrue();
    }

    [Fact]
    public void ProcessZipFile_WithEmptyZip_ShouldReturnEmptyList()
    {
        // Arrange
        var tempZipFile = CreateEmptyZipFile();

        // Act
        var events = _fileProcessor.ProcessZipFile(tempZipFile);

        // Assert
        events.Should().BeEmpty();
    }

    [Fact]
    public void ProcessZipFile_WithZipContainingNonIcsFiles_ShouldReturnEmptyList()
    {
        // Arrange
        var tempZipFile = CreateZipWithNonIcsFiles();

        // Act
        var events = _fileProcessor.ProcessZipFile(tempZipFile);

        // Assert
        events.Should().BeEmpty();
    }

    [Fact]
    public void ProcessZipFile_WithCorruptedZip_ShouldReturnEmptyList()
    {
        // Arrange
        var tempFile = GetTempFilePath(".zip");
        File.WriteAllText(tempFile, "This is not a valid zip file");

        // Act
        var events = _fileProcessor.ProcessZipFile(tempFile);

        // Assert
        events.Should().BeEmpty();
        _mockLogger.ContainsLogLevel("Error").Should().BeTrue();
    }

    [Fact]
    public void ProcessZipFile_WithLargeNumberOfFiles_ShouldProcessAllFiles()
    {
        // Arrange
        var tempZipFile = CreateZipWithMultipleIcsFiles(10);

        // Act
        var events = _fileProcessor.ProcessZipFile(tempZipFile);

        // Assert
        events.Should().HaveCount(10);
        events.Should().OnlyContain(e => e.Summary == "Team Meeting");
    }

    private string CreateTempIcsFile(string content)
    {
        var tempFile = GetTempFilePath(".ics");
        File.WriteAllText(tempFile, content);
        return tempFile;
    }

    private string CreateTempZipFile()
    {
        var tempZipFile = GetTempFilePath(".zip");

        using (var archive = ZipFile.Open(tempZipFile, ZipArchiveMode.Create))
        {
            var entry1 = archive.CreateEntry("event1.ics");
            using (var stream = entry1.Open())
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(TestData.SimpleEventIcs);
            }

            var entry2 = archive.CreateEntry("event2.ics");
            using (var stream = entry2.Open())
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(TestData.AllDayEventIcs);
            }
        }

        return tempZipFile;
    }

    private string CreateEmptyZipFile()
    {
        var tempZipFile = GetTempFilePath(".zip");
        using (var archive = ZipFile.Open(tempZipFile, ZipArchiveMode.Create))
        {
            // Create empty zip file
        }

        return tempZipFile;
    }

    private string CreateZipWithNonIcsFiles()
    {
        var tempZipFile = GetTempFilePath(".zip");

        using var archive = ZipFile.Open(tempZipFile, ZipArchiveMode.Create);
        var entry1 = archive.CreateEntry("document.txt");
        using (var stream = entry1.Open())
        using (var writer = new StreamWriter(stream))
        {
            writer.Write("This is a text file");
        }

        var entry2 = archive.CreateEntry("image.png");
        using (var stream = entry2.Open())
        using (var writer = new StreamWriter(stream))
        {
            writer.Write("Fake image content");
        }

        return tempZipFile;
    }

    private string CreateZipWithMultipleIcsFiles(int count)
    {
        var tempZipFile = GetTempFilePath(".zip");

        using var archive = ZipFile.Open(tempZipFile, ZipArchiveMode.Create);
        for (var i = 0; i < count; i++)
        {
            var entry = archive.CreateEntry($"event{i}.ics");
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream);
            writer.Write(TestData.SimpleEventIcs);
        }

        return tempZipFile;
    }

    private string GetTempFilePath(string extension)
    {
        var tempFile = Path.GetTempFileName();
        var fileWithExtension = Path.ChangeExtension(tempFile, extension);
        _tempFiles.Add(fileWithExtension);
        return fileWithExtension;
    }
}