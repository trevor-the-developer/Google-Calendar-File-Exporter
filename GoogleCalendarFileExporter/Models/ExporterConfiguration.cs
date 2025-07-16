namespace GoogleCalendarFileExporter.Models;

public class ExporterConfiguration
{
    public LoggingConfiguration Logging { get; init; } = new();
    public TimezoneConfiguration Timezone { get; init; } = new();
    public ExportConfiguration Export { get; init; } = new();
    public ProcessingConfiguration Processing { get; init; } = new();
}