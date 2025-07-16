using System.Globalization;
using System.Text.RegularExpressions;
using GoogleCalendarFileExporter.Interfaces;

namespace GoogleCalendarFileExporter.Services;

public class TimezoneService
{
    private readonly ILoggingService _logger;
    private readonly Dictionary<string, TimeZoneInfo> _timezoneCache = new();

    public TimezoneService(ILoggingService logger)
    {
        _logger = logger;
    }

    public DateTime? ParseIcsDateTime(string value, string? parameters = null)
    {
        try
        {
            // Parse timezone from parameters
            var timezoneId = ExtractTimezoneId(parameters);

            // Remove any timezone info from the value itself
            var dateValue = value.Split(';')[0];

            if (dateValue.Length == 8) // YYYYMMDD format (all-day)
            {
                var date = DateTime.ParseExact(dateValue, "yyyyMMdd", null, DateTimeStyles.None);
                return DateTime.SpecifyKind(date, DateTimeKind.Local);
            }

            if (dateValue.EndsWith("Z") && dateValue.Length == 16) // YYYYMMDDTHHMMSSZ format (UTC)
            {
                var utcDate = DateTime.ParseExact(dateValue, "yyyyMMddTHHmmss\\Z", null, DateTimeStyles.None);
                utcDate = DateTime.SpecifyKind(utcDate, DateTimeKind.Utc);
                return TimeZoneInfo.ConvertTimeFromUtc(utcDate, TimeZoneInfo.Local);
            }

            if (dateValue.Length == 15) // YYYYMMDDTHHMMSS format (local or specified timezone)
            {
                var date = DateTime.ParseExact(dateValue, "yyyyMMddTHHmmss", null, DateTimeStyles.None);

                return !string.IsNullOrEmpty(timezoneId) 
                    ? ConvertFromTimezone(date, timezoneId) 
                    : DateTime.SpecifyKind(date, DateTimeKind.Local);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to parse ICS datetime: {Value} with parameters: {Parameters}. Error: {Error}",
                value, parameters ?? "null", ex.Message);
        }

        return null;
    }

    private static string? ExtractTimezoneId(string? parameters)
    {
        if (string.IsNullOrEmpty(parameters))
            return null;

        var match = Regex.Match(parameters, @"TZID=([^;]+)");
        return match.Success ? match.Groups[1].Value : null;
    }

    private DateTime ConvertFromTimezone(DateTime dateTime, string timezoneId)
    {
        try
        {
            var timeZone = GetTimeZoneInfo(timezoneId);
            if (timeZone != null)
                // Just return the datetime as is (no timezone conversion)
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to convert timezone for {TimezoneId}. Error: {Error}", timezoneId, ex.Message);
        }

        // Fallback to treating as local time
        return DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
    }

    private TimeZoneInfo? GetTimeZoneInfo(string timezoneId)
    {
        if (_timezoneCache.TryGetValue(timezoneId, out var cached))
            return cached;

        try
        {
            // Try to find the timezone by ID
            TimeZoneInfo timeZone;

            // Handle common timezone mappings
            var mappedId = MapTimezoneId(timezoneId);

            try
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(mappedId);
            }
            catch (TimeZoneNotFoundException)
            {
                _logger.LogWarning("Timezone not found: {TimezoneId}, using UTC", timezoneId);
                timeZone = TimeZoneInfo.Utc;
            }

            _timezoneCache[timezoneId] = timeZone;
            return timeZone;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting timezone info for {TimezoneId}", timezoneId);
            return null;
        }
    }

    private string MapTimezoneId(string timezoneId)
    {
        // Common timezone ID mappings for cross-platform compatibility
        var mappings = new Dictionary<string, string>
        {
            { "America/New_York", GetWindowsTimezoneId("America/New_York", "Eastern Standard Time") },
            { "America/Chicago", GetWindowsTimezoneId("America/Chicago", "Central Standard Time") },
            { "America/Denver", GetWindowsTimezoneId("America/Denver", "Mountain Standard Time") },
            { "America/Los_Angeles", GetWindowsTimezoneId("America/Los_Angeles", "Pacific Standard Time") },
            { "Europe/London", GetWindowsTimezoneId("Europe/London", "GMT Standard Time") },
            { "Europe/Paris", GetWindowsTimezoneId("Europe/Paris", "Central European Standard Time") },
            { "Europe/Berlin", GetWindowsTimezoneId("Europe/Berlin", "Central European Standard Time") },
            { "Asia/Tokyo", GetWindowsTimezoneId("Asia/Tokyo", "Tokyo Standard Time") },
            { "Australia/Sydney", GetWindowsTimezoneId("Australia/Sydney", "AUS Eastern Standard Time") }
        };

        return mappings.GetValueOrDefault(timezoneId, timezoneId);
    }

    private static string GetWindowsTimezoneId(string ianaId, string windowsId)
    {
        // On Windows, use Windows timezone IDs, otherwise use IANA IDs
        return Environment.OSVersion.Platform == PlatformID.Win32NT ? windowsId : ianaId;
    }

    public static string FormatDateTime(DateTime? dateTime, bool includeTime = true)
    {
        if (!dateTime.HasValue)
            return "";

        var dt = dateTime.Value;

        if (!includeTime || dt.TimeOfDay == TimeSpan.Zero)
            return dt.ToString("yyyy-MM-dd");

        return dt.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public static bool IsAllDayEvent(DateTime? startDateTime, DateTime? endDateTime)
    {
        if (!startDateTime.HasValue || !endDateTime.HasValue)
            return false;

        return startDateTime.Value.TimeOfDay == TimeSpan.Zero &&
               endDateTime.Value.TimeOfDay == TimeSpan.Zero;
    }
}