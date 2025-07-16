namespace GoogleCalendarFileExporter.Models;

public class ExportConfiguration
{
    public string DefaultFormat { get; set; } = "csv";
    public bool SortEventsByDate { get; set; } = true;
    public bool IncludeEmptyFields { get; set; } = false;
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public string TimeFormat { get; set; } = "HH:mm:ss";
    public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    public CsvConfiguration Csv { get; set; } = new();
    public JsonConfiguration Json { get; set; } = new();
    public ExcelConfiguration Excel { get; set; } = new();
    public XmlConfiguration Xml { get; set; } = new();
}