using GoogleCalendarFileExporter.Classes;

namespace GoogleCalendarFileExporter.Interfaces;

public interface IExporter
{
    void Export(List<CalendarEvent> events, string outputPath);
    string GetFileExtension();
    string GetFormatName();
}