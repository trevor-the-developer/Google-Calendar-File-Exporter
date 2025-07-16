namespace GoogleCalendarFileExporter.Models;

public class XmlConfiguration
{
    public string RootElementName { get; set; } = "CalendarEvents";
    public string EventElementName { get; set; } = "Event";
    public bool IndentOutput { get; set; } = true;
    public string Encoding { get; set; } = "UTF-8";
}