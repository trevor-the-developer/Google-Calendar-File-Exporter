namespace GoogleCalendarFileExporter.Models;

public class ProcessingConfiguration
{
    public int MaxConcurrentFiles { get; set; } = 4;
    public bool EnableAsyncProcessing { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 300;
    public bool ValidateIcsContent { get; set; } = true;
}