namespace GoogleCalendarFileExporter.Models;

public class ExcelConfiguration
{
    public string WorksheetName { get; set; } = "Calendar Events";
    public bool AutoFitColumns { get; set; } = true;
    public bool FreezeHeaderRow { get; set; } = true;
    public bool ApplyFormatting { get; set; } = true;
}