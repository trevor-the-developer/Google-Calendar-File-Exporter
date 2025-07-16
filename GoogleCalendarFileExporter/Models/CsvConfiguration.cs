namespace GoogleCalendarFileExporter.Models;

public class CsvConfiguration
{
    public string Delimiter { get; set; } = ",";
    public bool IncludeHeaders { get; set; } = true;
    public string QuoteCharacter { get; set; } = "\"";
    public string EscapeCharacter { get; set; } = "\"";
}