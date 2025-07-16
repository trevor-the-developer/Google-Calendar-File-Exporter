namespace GoogleCalendarFileExporter.Models;

public class JsonConfiguration
{
    public bool IndentOutput { get; set; } = true;
    public bool IgnoreNullValues { get; set; } = true;
    public string DateFormat { get; set; } = "yyyy-MM-ddTHH:mm:ss";
}