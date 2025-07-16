using GoogleCalendarFileExporter.Classes;

namespace GoogleCalendarFileExporter.Interfaces;

public interface IExporterAsync : IExporter
{
    Task ExportAsync(List<CalendarEvent> events, string outputPath);
}