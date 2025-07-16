namespace GoogleCalendarFileExporter.Models;

public class TimezoneConfiguration
{
    public string DefaultTimezone { get; set; } = "Local";
    public bool ConvertToLocalTime { get; set; } = true;
    public Dictionary<string, string> TimezoneMapping { get; set; } = new();
}