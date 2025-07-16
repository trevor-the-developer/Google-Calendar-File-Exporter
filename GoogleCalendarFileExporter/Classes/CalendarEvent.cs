namespace GoogleCalendarFileExporter.Classes;

public class CalendarEvent
{
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? LastModified { get; set; }
    public string? Uid { get; set; }
    public string? Status { get; set; }
    public string? Organizer { get; set; }
    public List<string> Attendees { get; set; } = new();
    public string? RecurrenceRule { get; set; }
    public string? CalendarName { get; set; }
}