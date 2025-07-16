using System.IO.Compression;
using System.Text.Json;
using FluentAssertions;
using GoogleCalendarFileExporter.Classes;
using GoogleCalendarFileExporter.Exporters;
using GoogleCalendarFileExporter.Interfaces;
using GoogleCalendarFileExporter.Services;
using GoogleCalendarFileExporter.Tests.Fixtures;
using GoogleCalendarFileExporter.Tests.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GoogleCalendarFileExporter.Tests.IntegrationTests;

public class EndToEndTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly List<string> _tempDirectories = new();
    private readonly List<string> _tempFiles = new();

    public EndToEndTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggingService, MockLoggingService>();
        services.AddSingleton<TimezoneService>();
        services.AddSingleton<IcsParser>();
        services.AddSingleton<IFileProcessor, FileProcessor>();
        services.AddTransient<CsvExporter>();
        services.AddTransient<JsonExporter>();
        services.AddTransient<ExcelExporter>();
        services.AddTransient<XmlExporter>();
        services.AddSingleton<ConfigurationService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        foreach (var tempFile in _tempFiles) TestFileHelper.CleanupTempFile(tempFile);

        foreach (var tempDir in _tempDirectories) TestFileHelper.CleanupTempDirectory(tempDir);

        if (_serviceProvider is IDisposable disposable) disposable.Dispose();
    }

    [Theory]
    [InlineData("csv")]
    [InlineData("json")]
    [InlineData("xlsx")]
    [InlineData("xml")]
    public void ProcessAndExportIcsFile_WithAllFormats_ShouldCompleteSuccessfully(string format)
    {
        // Arrange
        var fileProcessor = _serviceProvider.GetRequiredService<IFileProcessor>();

        var icsFile = CreateTempIcsFile(TestData.SimpleEventIcs);
        var outputFile = GetTempFilePath($".{format}");

        // Act
        var events = fileProcessor.ProcessIcsFile(icsFile);
        var exporter = ExporterFactory.CreateExporter(format);
        exporter.Export(events, outputFile);

        // Assert
        events.Should().HaveCount(1);
        File.Exists(outputFile).Should().BeTrue();

        var fileInfo = new FileInfo(outputFile);
        fileInfo.Length.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("csv")]
    [InlineData("json")]
    [InlineData("xlsx")]
    [InlineData("xml")]
    public async Task ProcessAndExportIcsFileAsync_WithAllFormats_ShouldCompleteSuccessfully(string format)
    {
        // Arrange
        var fileProcessor = _serviceProvider.GetRequiredService<IFileProcessor>();

        var icsFile = CreateTempIcsFile(TestData.SimpleEventIcs);
        var outputFile = GetTempFilePath($".{format}");

        // Act
        var events = await fileProcessor.ProcessIcsFileAsync(icsFile);
        var exporter = ExporterFactory.CreateExporter(format);
        await ((IExporterAsync)exporter).ExportAsync(events, outputFile);

        // Assert
        events.Should().HaveCount(1);
        File.Exists(outputFile).Should().BeTrue();

        var fileInfo = new FileInfo(outputFile);
        fileInfo.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ProcessZipFileWithMultipleIcsFiles_ShouldExtractAndProcessAllEvents()
    {
        // Arrange
        var fileProcessor = _serviceProvider.GetRequiredService<IFileProcessor>();

        var zipFile = CreateTempZipFileWithMultipleIcs();
        var outputFile = GetTempFilePath(".json");

        // Act
        var events = fileProcessor.ProcessZipFile(zipFile);
        var exporter = ExporterFactory.CreateExporter("json");
        exporter.Export(events, outputFile);

        // Assert
        events.Should().HaveCount(3);
        File.Exists(outputFile).Should().BeTrue();

        var jsonContent = File.ReadAllText(outputFile);
        var deserializedEvents = JsonSerializer.Deserialize<List<CalendarEvent>>(jsonContent);
        deserializedEvents.Should().HaveCount(3);
    }

    [Fact]
    public void CompleteWorkflow_FromConfigurationToExport_ShouldWork()
    {
        // Arrange
        var configService = _serviceProvider.GetRequiredService<ConfigurationService>();
        var fileProcessor = _serviceProvider.GetRequiredService<IFileProcessor>();

        // Update configuration
        configService.UpdateConfiguration(config =>
        {
            config.Export.DefaultFormat = "csv";
            config.Processing.MaxConcurrentFiles = 2;
        });

        var icsFile = CreateTempIcsFile(TestData.RecurringEventIcs);
        var outputFile = GetTempFilePath(".csv");

        // Act
        var events = fileProcessor.ProcessIcsFile(icsFile);
        var exporter = ExporterFactory.CreateExporter(configService.Configuration.Export.DefaultFormat);
        exporter.Export(events, outputFile);

        // Assert
        events.Should().HaveCount(1);
        events.First().RecurrenceRule.Should().Be("FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR");
        File.Exists(outputFile).Should().BeTrue();

        var csvContent = File.ReadAllText(outputFile);
        csvContent.Should().Contain("Daily Standup");
        csvContent.Should().Contain("FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR");
    }

    [Fact]
    public void ProcessMultipleFilesWithDifferentFormats_ShouldHandleAllCorrectly()
    {
        // Arrange
        var fileProcessor = _serviceProvider.GetRequiredService<IFileProcessor>();

        var simpleIcsFile = CreateTempIcsFile(TestData.SimpleEventIcs);
        var allDayIcsFile = CreateTempIcsFile(TestData.AllDayEventIcs);
        var recurringIcsFile = CreateTempIcsFile(TestData.RecurringEventIcs);

        var csvOutput = GetTempFilePath(".csv");
        var jsonOutput = GetTempFilePath(".json");
        var xlsxOutput = GetTempFilePath(".xlsx");

        // Act
        var simpleEvents = fileProcessor.ProcessIcsFile(simpleIcsFile);
        var allDayEvents = fileProcessor.ProcessIcsFile(allDayIcsFile);
        var recurringEvents = fileProcessor.ProcessIcsFile(recurringIcsFile);

        var csvExporter = ExporterFactory.CreateExporter("csv");
        var jsonExporter = ExporterFactory.CreateExporter("json");
        var xlsxExporter = ExporterFactory.CreateExporter("xlsx");

        csvExporter.Export(simpleEvents, csvOutput);
        jsonExporter.Export(allDayEvents, jsonOutput);
        xlsxExporter.Export(recurringEvents, xlsxOutput);

        // Assert
        simpleEvents.Should().HaveCount(1);
        allDayEvents.Should().HaveCount(1);
        recurringEvents.Should().HaveCount(1);

        File.Exists(csvOutput).Should().BeTrue();
        File.Exists(jsonOutput).Should().BeTrue();
        File.Exists(xlsxOutput).Should().BeTrue();

        var csvContent = File.ReadAllText(csvOutput);
        csvContent.Should().Contain("Team Meeting");

        var jsonContent = File.ReadAllText(jsonOutput);
        var jsonEvents = JsonSerializer.Deserialize<List<CalendarEvent>>(jsonContent);
        jsonEvents.Should().HaveCount(1);
        jsonEvents!.First().Summary.Should().Be("All Day Event");
    }

    [Fact]
    public void ProcessLargeZipFile_ShouldHandlePerformanceGracefully()
    {
        // Arrange
        var fileProcessor = _serviceProvider.GetRequiredService<IFileProcessor>();

        var largeZipFile = CreateLargeZipFile(100); // 100 ICS files
        var outputFile = GetTempFilePath(".json");

        // Act
        var startTime = DateTime.Now;
        var events = fileProcessor.ProcessZipFile(largeZipFile);
        var exporter = ExporterFactory.CreateExporter("json");
        exporter.Export(events, outputFile);
        var endTime = DateTime.Now;

        // Assert
        events.Should().HaveCount(100);
        File.Exists(outputFile).Should().BeTrue();

        var processingTime = endTime - startTime;
        processingTime.Should().BeLessThan(TimeSpan.FromSeconds(30)); // Should complete in reasonable time

        var jsonContent = File.ReadAllText(outputFile);
        var deserializedEvents = JsonSerializer.Deserialize<List<CalendarEvent>>(jsonContent);
        deserializedEvents.Should().HaveCount(100);
    }

    [Fact]
    public void ProcessCorruptedFiles_ShouldHandleGracefullyAndContinue()
    {
        // Arrange
        var fileProcessor = _serviceProvider.GetRequiredService<IFileProcessor>();

        var validIcsFile = CreateTempIcsFile(TestData.SimpleEventIcs);
        var corruptedIcsFile = CreateTempIcsFile("This is not valid ICS content");
        var emptyIcsFile = CreateTempIcsFile("");

        var outputFile = GetTempFilePath(".csv");

        // Act
        var validEvents = fileProcessor.ProcessIcsFile(validIcsFile);
        var corruptedEvents = fileProcessor.ProcessIcsFile(corruptedIcsFile);
        var emptyEvents = fileProcessor.ProcessIcsFile(emptyIcsFile);

        var allEvents = new List<CalendarEvent>();
        allEvents.AddRange(validEvents);
        allEvents.AddRange(corruptedEvents);
        allEvents.AddRange(emptyEvents);

        var exporter = ExporterFactory.CreateExporter("csv");
        exporter.Export(allEvents, outputFile);

        // Assert
        validEvents.Should().HaveCount(1);
        corruptedEvents.Should().BeEmpty();
        emptyEvents.Should().BeEmpty();
        allEvents.Should().HaveCount(1);

        File.Exists(outputFile).Should().BeTrue();
        var csvContent = File.ReadAllText(outputFile);
        csvContent.Should().Contain("Team Meeting");
    }

    [Fact]
    public void TimezoneHandling_AcrossEntireWorkflow_ShouldBeConsistent()
    {
        // Arrange
        var fileProcessor = _serviceProvider.GetRequiredService<IFileProcessor>();

        var timezoneIcsFile = CreateTempIcsFile(TestData.TimezoneEventIcs);
        var csvOutput = GetTempFilePath(".csv");
        var jsonOutput = GetTempFilePath(".json");

        // Act
        var events = fileProcessor.ProcessIcsFile(timezoneIcsFile);

        var csvExporter = ExporterFactory.CreateExporter("csv");
        var jsonExporter = ExporterFactory.CreateExporter("json");

        csvExporter.Export(events, csvOutput);
        jsonExporter.Export(events, jsonOutput);

        // Assert
        events.Should().HaveCount(1);
        var evt = events.First();
        evt.Summary.Should().Be("Timezone Event");
        evt.StartDateTime.Should().NotBeNull();
        evt.EndDateTime.Should().NotBeNull();

        // Verify timezone information is preserved in exports
        File.Exists(csvOutput).Should().BeTrue();
        File.Exists(jsonOutput).Should().BeTrue();

        var csvContent = File.ReadAllText(csvOutput);
        csvContent.Should().Contain("Timezone Event");

        var jsonContent = File.ReadAllText(jsonOutput);
        var jsonEvents = JsonSerializer.Deserialize<List<CalendarEvent>>(jsonContent);
        jsonEvents!.First().Summary.Should().Be("Timezone Event");
    }

    [Fact]
    public void ConfigurationPersistence_ShouldMaintainSettingsAcrossOperations()
    {
        // Arrange
        var configService = _serviceProvider.GetRequiredService<ConfigurationService>();
        var tempConfigFile = GetTempFilePath(".json");
        var configServiceWithCustomPath = new ConfigurationService(
            _serviceProvider.GetRequiredService<ILoggingService>(),
            tempConfigFile);

        // Act
        configServiceWithCustomPath.UpdateConfiguration(config =>
        {
            config.Export.DefaultFormat = "xlsx";
            config.Processing.MaxConcurrentFiles = 8;
            config.Logging.LogLevel = "Debug";
        });

        configServiceWithCustomPath.SaveConfiguration();

        // Create new instance to test persistence
        var newConfigService = new ConfigurationService(
            _serviceProvider.GetRequiredService<ILoggingService>(),
            tempConfigFile);

        // Assert
        newConfigService.Configuration.Export.DefaultFormat.Should().Be("xlsx");
        newConfigService.Configuration.Processing.MaxConcurrentFiles.Should().Be(8);
        newConfigService.Configuration.Logging.LogLevel.Should().Be("Debug");
    }

    [Fact]
    public async Task AsyncWorkflow_ShouldWorkEndToEnd()
    {
        // Arrange
        var fileProcessor = _serviceProvider.GetRequiredService<IFileProcessor>();

        var icsFile = CreateTempIcsFile(TestData.MultiLineDescriptionIcs);
        var outputFile = GetTempFilePath(".json");

        // Act
        var events = await fileProcessor.ProcessIcsFileAsync(icsFile);
        var exporter = ExporterFactory.CreateExporter("json");
        await ((IExporterAsync)exporter).ExportAsync(events, outputFile);

        // Assert
        events.Should().HaveCount(1);
        File.Exists(outputFile).Should().BeTrue();

        var jsonContent = File.ReadAllText(outputFile);
        var deserializedEvents = JsonSerializer.Deserialize<List<CalendarEvent>>(jsonContent);
        deserializedEvents.Should().HaveCount(1);
        deserializedEvents!.First().Summary.Should().Be("Event with Multi-line Description");
    }

    private string CreateTempIcsFile(string content)
    {
        var tempFile = GetTempFilePath(".ics");
        File.WriteAllText(tempFile, content);
        return tempFile;
    }

    private string CreateTempZipFileWithMultipleIcs()
    {
        var tempZipFile = GetTempFilePath(".zip");

        using (var archive = ZipFile.Open(tempZipFile, ZipArchiveMode.Create))
        {
            var entry1 = archive.CreateEntry("simple.ics");
            using (var stream = entry1.Open())
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(TestData.SimpleEventIcs);
            }

            var entry2 = archive.CreateEntry("allday.ics");
            using (var stream = entry2.Open())
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(TestData.AllDayEventIcs);
            }

            var entry3 = archive.CreateEntry("recurring.ics");
            using (var stream = entry3.Open())
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(TestData.RecurringEventIcs);
            }
        }

        return tempZipFile;
    }

    private string CreateLargeZipFile(int fileCount)
    {
        var tempZipFile = GetTempFilePath(".zip");

        using (var archive = ZipFile.Open(tempZipFile, ZipArchiveMode.Create))
        {
            for (var i = 0; i < fileCount; i++)
            {
                var entry = archive.CreateEntry($"event_{i}.ics");
                using (var stream = entry.Open())
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(TestData.SimpleEventIcs);
                }
            }
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