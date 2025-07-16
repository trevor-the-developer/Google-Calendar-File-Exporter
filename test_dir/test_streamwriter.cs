using System;
using System.IO;

try
{
    var path = Path.Combine("invalid", "path", "that", "does", "not", "exist", "file.csv");
    Console.WriteLine($"Attempting to create file at: {path}");
    
    using var writer = new StreamWriter(path);
    writer.WriteLine("Test content");
    
    Console.WriteLine("StreamWriter succeeded!");
}
catch (Exception ex)
{
    Console.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
}
