using System.Text;
using GoogleCalendarFileExporter.Classes;
using GoogleCalendarFileExporter.Interfaces;

namespace GoogleCalendarFileExporter.Exporters;

public abstract class AsyncExporterBase : IExporterAsync
{
    public abstract void Export(List<CalendarEvent> events, string outputPath);
    public abstract Task ExportAsync(List<CalendarEvent> events, string outputPath);
    public abstract string GetFileExtension();
    public abstract string GetFormatName();

    protected async Task WriteAllTextAsync(string path, string content)
    {
        await File.WriteAllTextAsync(path, content, Encoding.UTF8);
    }

    protected async Task WriteAllBytesAsync(string path, byte[] bytes)
    {
        await File.WriteAllBytesAsync(path, bytes);
    }
}