using GoogleCalendarFileExporter.Classes;

namespace GoogleCalendarFileExporter.Interfaces;

public interface IFileProcessor
{
    List<CalendarEvent> ProcessZipFile(string zipPath);
    List<CalendarEvent> ProcessIcsFile(string icsPath);
    Task<List<CalendarEvent>> ProcessZipFileAsync(string zipPath);
    Task<List<CalendarEvent>> ProcessIcsFileAsync(string icsPath);
}