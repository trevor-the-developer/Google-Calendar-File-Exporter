using System.Text.RegularExpressions;
using GoogleCalendarFileExporter.Interfaces;

namespace GoogleCalendarFileExporter.Tests.TestUtilities;

public class MockLoggingService : ILoggingService
{
    private readonly List<LogEntry> _logs = new();

    public IReadOnlyList<LogEntry> Logs => _logs;

    public void LogInformation(string message)
    {
        _logs.Add(new LogEntry("Information", message, null));
    }

    public void LogInformation(string message, params object[] args)
    {
        var formattedMessage = FormatStructuredMessage(message, args);
        _logs.Add(new LogEntry("Information", formattedMessage, null));
    }

    public void LogWarning(string message)
    {
        _logs.Add(new LogEntry("Warning", message, null));
    }

    public void LogWarning(string message, params object[] args)
    {
        var formattedMessage = FormatStructuredMessage(message, args);
        _logs.Add(new LogEntry("Warning", formattedMessage, null));
    }

    public void LogError(string message)
    {
        _logs.Add(new LogEntry("Error", message, null));
    }

    public void LogError(string message, params object[] args)
    {
        var formattedMessage = FormatStructuredMessage(message, args);
        _logs.Add(new LogEntry("Error", formattedMessage, null));
    }

    public void LogError(Exception exception, string message)
    {
        _logs.Add(new LogEntry("Error", message, exception));
    }

    public void LogError(Exception exception, string message, params object[] args)
    {
        var formattedMessage = FormatStructuredMessage(message, args);
        _logs.Add(new LogEntry("Error", formattedMessage, exception));
    }

    public void LogDebug(string message)
    {
        _logs.Add(new LogEntry("Debug", message, null));
    }

    public void LogDebug(string message, params object[] args)
    {
        var formattedMessage = FormatStructuredMessage(message, args);
        _logs.Add(new LogEntry("Debug", formattedMessage, null));
    }

    public void LogTrace(string message)
    {
        _logs.Add(new LogEntry("Trace", message, null));
    }

    public void LogTrace(string message, params object[] args)
    {
        var formattedMessage = FormatStructuredMessage(message, args);
        _logs.Add(new LogEntry("Trace", formattedMessage, null));
    }

    private static string FormatStructuredMessage(string message, params object[] args)
    {
        if (args == null || args.Length == 0) return message;

        try
        {
            // Convert structured logging format {PropertyName} to string.Format style {0}, {1}, etc.
            var regex = new Regex(@"\{([^}]+)\}");
            var matches = regex.Matches(message);
            var formatString = message;

            for (var i = 0; i < matches.Count && i < args.Length; i++)
                formatString = formatString.Replace(matches[i].Value, $"{{{i}}}");

            return string.Format(formatString, args);
        }
        catch (Exception)
        {
            // If formatting fails, return the original message with args appended
            return $"{message} [Args: {string.Join(", ", args)}]";
        }
    }

    public void Clear()
    {
        _logs.Clear();
    }

    public bool ContainsMessage(string message)
    {
        return _logs.Any(l => l.Message.Contains(message));
    }

    public bool ContainsLogLevel(string level)
    {
        return _logs.Any(l => l.Level == level);
    }
}

public record LogEntry(string Level, string Message, Exception? Exception);