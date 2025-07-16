namespace GoogleCalendarFileExporter.Models;

public class LoggingConfiguration
{
    public string LogLevel { get; set; } = "Information";
    public bool EnableConsoleLogging { get; set; } = true;
    public bool EnableFileLogging { get; set; } = false;
    public string? LogFilePath { get; set; }
    public bool EnableStructuredLogging { get; set; } = true;
}