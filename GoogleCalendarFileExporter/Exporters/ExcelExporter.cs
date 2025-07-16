using ClosedXML.Excel;
using GoogleCalendarFileExporter.Classes;

namespace GoogleCalendarFileExporter.Exporters;

public class ExcelExporter : AsyncExporterBase
{
    public override void Export(List<CalendarEvent> events, string outputPath)
    {
        ValidateInputs(events, outputPath);
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Calendar Events");

        // Add headers
        var headers = new[]
        {
            "Subject", "Start Date", "Start Time", "End Date", "End Time", "All Day Event",
            "Description", "Location", "Attendees", "Creator", "Status", "Recurrence",
            "Event ID", "Calendar ID", "Created", "Modified"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        var sortedEvents = events.OrderBy(e => e.StartDateTime).ToList();

        for (var row = 0; row < sortedEvents.Count; row++)
        {
            var evt = sortedEvents[row];
            var excelRow = row + 2; // Excel is 1-based, plus header row

            worksheet.Cell(excelRow, 1).Value = evt.Summary ?? "";

            if (evt.StartDateTime.HasValue)
            {
                var start = evt.StartDateTime.Value;
                worksheet.Cell(excelRow, 2).Value = start.ToString("yyyy-MM-dd");

                var isAllDay = start.TimeOfDay == TimeSpan.Zero && evt.EndDateTime.HasValue &&
                               evt.EndDateTime.Value.TimeOfDay == TimeSpan.Zero;

                worksheet.Cell(excelRow, 6).Value = isAllDay ? "True" : "False";

                worksheet.Cell(excelRow, 3).Value = start.ToString("yyyy-MM-dd HH:mm:ss");
            }

            if (evt.EndDateTime.HasValue)
            {
                var end = evt.EndDateTime.Value;
                worksheet.Cell(excelRow, 4).Value = end.ToString("yyyy-MM-dd");

                worksheet.Cell(excelRow, 5).Value = end.ToString("yyyy-MM-dd HH:mm:ss");
            }

            if (evt.Description != null)
                worksheet.Cell(excelRow, 7).Value = evt.Description;
            if (evt.Location != null)
                worksheet.Cell(excelRow, 8).Value = evt.Location;
            worksheet.Cell(excelRow, 9).Value = string.Join("; ", evt.Attendees);
            if (evt.Organizer != null)
                worksheet.Cell(excelRow, 10).Value = evt.Organizer;
            if (evt.Status != null)
                worksheet.Cell(excelRow, 11).Value = evt.Status;
            if (evt.RecurrenceRule != null)
                worksheet.Cell(excelRow, 12).Value = evt.RecurrenceRule;
            if (evt.Uid != null)
                worksheet.Cell(excelRow, 13).Value = evt.Uid;
            if (evt.CalendarName != null)
                worksheet.Cell(excelRow, 14).Value = evt.CalendarName;
            if (evt.Created.HasValue)
                worksheet.Cell(excelRow, 15).Value = evt.Created.Value.ToString("yyyy-MM-dd HH:mm:ss");
            if (evt.LastModified.HasValue)
                worksheet.Cell(excelRow, 16).Value = evt.LastModified.Value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        // Auto-fit columns
        worksheet.ColumnsUsed().AdjustToContents();

        workbook.SaveAs(outputPath);
    }

    public override async Task ExportAsync(List<CalendarEvent> events, string outputPath)
    {
        ValidateInputs(events, outputPath);
        await Task.Run(() => Export(events, outputPath));
    }

    public override string GetFileExtension()
    {
        return ".xlsx";
    }

    public override string GetFormatName()
    {
        return "Excel";
    }

    private void ValidateInputs(List<CalendarEvent> events, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(events);

        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));

        try
        {
            var fullPath = Path.GetFullPath(outputPath);
            var directory = Path.GetDirectoryName(fullPath);

            // Check if the directory exists
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                throw new DirectoryNotFoundException($"Directory does not exist: {directory}");
        }
        catch (ArgumentException ex)
        {
            throw new DirectoryNotFoundException($"Invalid output path: {outputPath}", ex);
        }
        catch (NotSupportedException ex)
        {
            throw new DirectoryNotFoundException($"Invalid output path: {outputPath}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new DirectoryNotFoundException($"Invalid output path: {outputPath}", ex);
        }
    }
}