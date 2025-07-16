using System.IO.Compression;
using GoogleCalendarFileExporter.Interfaces;

namespace GoogleCalendarFileExporter.Classes;

public class FileProcessor : IFileProcessor
{
    private readonly IcsParser _icsParser;
    private readonly ILoggingService _logger;

    public FileProcessor(IcsParser icsParser, ILoggingService logger)
    {
        _icsParser = icsParser;
        _logger = logger;
    }

    public List<CalendarEvent> ProcessZipFile(string zipPath)
    {
        var events = new List<CalendarEvent>();

        if (!File.Exists(zipPath))
        {
            _logger.LogError("ZIP file not found: {ZipPath}", zipPath);
            return events;
        }

        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
                if (entry.FullName.EndsWith(".ics", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Processing: {FileName}", entry.FullName);

                    using var stream = entry.Open();
                    using var reader = new StreamReader(stream);
                    var content = reader.ReadToEnd();
                    var calendarName = Path.GetFileNameWithoutExtension(entry.FullName);
                    var calendarEvents = _icsParser.ParseIcsContent(content, calendarName);
                    events.AddRange(calendarEvents);
                    _logger.LogDebug("Found {EventCount} events in {FileName}", calendarEvents.Count,
                        entry.FullName);
                }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ZIP file: {ZipPath}", zipPath);
        }

        return events;
    }

    public List<CalendarEvent> ProcessIcsFile(string icsPath)
    {
        _logger.LogInformation("Processing ICS file: {FilePath}", icsPath);

        if (!File.Exists(icsPath))
        {
            _logger.LogError("ICS file not found: {FilePath}", icsPath);
            return new List<CalendarEvent>();
        }

        try
        {
            var content = File.ReadAllText(icsPath);
            var calendarName = Path.GetFileNameWithoutExtension(icsPath);
            var events = _icsParser.ParseIcsContent(content, calendarName);
            _logger.LogDebug("Found {EventCount} events in {FilePath}", events.Count, icsPath);
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ICS file: {FilePath}", icsPath);
            return new List<CalendarEvent>();
        }
    }

    public async Task<List<CalendarEvent>> ProcessZipFileAsync(string zipPath)
    {
        var events = new List<CalendarEvent>();

        if (!File.Exists(zipPath))
        {
            _logger.LogError("ZIP file not found: {ZipPath}", zipPath);
            return events;
        }

        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            var tasks = 
                (from entry in archive.Entries 
                    where entry.FullName.EndsWith(".ics", StringComparison.OrdinalIgnoreCase) 
                    select ProcessZipEntryAsync(entry))
                .ToList();

            var results = await Task.WhenAll(tasks);
            foreach (var result in results) events.AddRange(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ZIP file asynchronously: {ZipPath}", zipPath);
        }

        return events;
    }

    public async Task<List<CalendarEvent>> ProcessIcsFileAsync(string icsPath)
    {
        if (!File.Exists(icsPath))
        {
            _logger.LogError("ICS file not found: {FilePath}", icsPath);
            return new List<CalendarEvent>();
        }

        try
        {
            var content = await File.ReadAllTextAsync(icsPath);
            var calendarName = Path.GetFileNameWithoutExtension(icsPath);
            return await Task.Run(() => _icsParser.ParseIcsContent(content, calendarName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ICS file asynchronously: {FilePath}", icsPath);
            return new List<CalendarEvent>();
        }
    }

    private async Task<List<CalendarEvent>> ProcessZipEntryAsync(ZipArchiveEntry entry)
    {
        _logger.LogInformation("Processing: {FileName}", entry.FullName);

        await using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        var calendarName = Path.GetFileNameWithoutExtension(entry.FullName);
        var events = await Task.Run(() => _icsParser.ParseIcsContent(content, calendarName));
        _logger.LogDebug("Found {EventCount} events in {FileName}", events.Count, entry.FullName);
        return events;
    }
}