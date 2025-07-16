using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace GoogleCalendarFileExporter.Tests.TestUtilities;

public static class TestFileHelper
{
    public static string CreateTempFile(string content, string extension = ".ics")
    {
        var tempPath = Path.GetTempFileName();
        var filePath = Path.ChangeExtension(tempPath, extension);

        File.WriteAllText(filePath, content, Encoding.UTF8);
        return filePath;
    }

    public static string CreateTempZipFile(params (string fileName, string content)[] files)
    {
        var tempPath = Path.GetTempFileName();
        var zipPath = Path.ChangeExtension(tempPath, ".zip");

        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var (fileName, content) in files)
        {
            var entry = archive.CreateEntry(fileName);
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.Write(content);
        }

        return zipPath;
    }

    public static void CleanupTempFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    public static void CleanupTempDirectory(string directoryPath)
    {
        try
        {
            if (Directory.Exists(directoryPath)) Directory.Delete(directoryPath, true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    public static string ReadFile(string filePath)
    {
        return File.ReadAllText(filePath, Encoding.UTF8);
    }

    public static bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    public static long GetFileSize(string filePath)
    {
        return new FileInfo(filePath).Length;
    }

    public static string GetTestDataPath(string fileName)
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        return Path.Combine(assemblyDirectory!, "Fixtures", "TestIcsFiles", fileName);
    }
}