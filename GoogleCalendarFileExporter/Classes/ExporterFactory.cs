using GoogleCalendarFileExporter.Exporters;
using GoogleCalendarFileExporter.Interfaces;

namespace GoogleCalendarFileExporter.Classes;

public static class ExporterFactory
{
    private static readonly Dictionary<string, Func<IExporter>> Exporters = new()
    {
        { "csv", () => new CsvExporter() },
        { "json", () => new JsonExporter() },
        { "xlsx", () => new ExcelExporter() },
        { "xml", () => new XmlExporter() }
    };

    public static IExporter CreateExporter(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Unsupported export format: " + (format ?? "null"));

        var normalizedFormat = format.ToLowerInvariant().TrimStart('.');

        if (Exporters.TryGetValue(normalizedFormat, out var factory)) return factory();

        throw new ArgumentException($"Unsupported export format: {format}");
    }

    public static IExporter CreateExporterFromFilePath(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant().TrimStart('.');
        return CreateExporter(extension);
    }

    public static string[] GetSupportedFormats()
    {
        return Exporters.Keys.ToArray();
    }

    public static string GetSupportedFormatsString()
    {
        return string.Join(", ", Exporters.Keys);
    }
}