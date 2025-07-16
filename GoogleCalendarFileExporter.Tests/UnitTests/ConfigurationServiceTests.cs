using System.Text.Json;
using FluentAssertions;
using GoogleCalendarFileExporter.Models;
using GoogleCalendarFileExporter.Services;
using GoogleCalendarFileExporter.Tests.TestUtilities;
using Xunit;

namespace GoogleCalendarFileExporter.Tests.UnitTests;

public class ConfigurationServiceTests : IDisposable
{
    private readonly MockLoggingService _mockLogger;
    private readonly List<string> _tempFiles = new();

    public ConfigurationServiceTests()
    {
        _mockLogger = new MockLoggingService();
    }

    public void Dispose()
    {
        foreach (var tempFile in _tempFiles) TestFileHelper.CleanupTempFile(tempFile);
    }

    [Fact]
    public void ConfigurationService_WithDefaultPath_ShouldLoadDefaultConfiguration()
    {
        // Act
        var configService = new ConfigurationService(_mockLogger);

        // Assert
        configService.Configuration.Should().NotBeNull();
        configService.Configuration.Logging.LogLevel.Should().Be("Information");
        configService.Configuration.Export.DefaultFormat.Should().Be("csv");
        configService.Configuration.Processing.MaxConcurrentFiles.Should().Be(4);
    }

    [Fact]
    public void ConfigurationService_WithCustomPath_ShouldLoadFromCustomPath()
    {
        // Arrange
        var customConfig = new ExporterConfiguration
        {
            Export = new ExportConfiguration { DefaultFormat = "json" },
            Logging = new LoggingConfiguration { LogLevel = "Debug" }
        };

        var tempFile = CreateTempConfigFile(customConfig);

        // Act
        var configService = new ConfigurationService(_mockLogger, tempFile);

        // Assert
        configService.Configuration.Export.DefaultFormat.Should().Be("json");
        configService.Configuration.Logging.LogLevel.Should().Be("Debug");
    }

    [Fact]
    public void SaveConfiguration_ShouldPersistConfigurationToFile()
    {
        // Arrange
        var tempFile = GetTempFilePath();
        var configService = new ConfigurationService(_mockLogger, tempFile);

        // Act
        configService.UpdateConfiguration(config =>
        {
            config.Export.DefaultFormat = "xlsx";
            config.Logging.LogLevel = "Warning";
        });
        configService.SaveConfiguration();

        // Assert
        File.Exists(tempFile).Should().BeTrue();

        var savedJson = File.ReadAllText(tempFile);
        var savedConfig = JsonSerializer.Deserialize<ExporterConfiguration>(savedJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        savedConfig.Should().NotBeNull();
        savedConfig!.Export.DefaultFormat.Should().Be("xlsx");
        savedConfig.Logging.LogLevel.Should().Be("Warning");
    }

    [Fact]
    public void UpdateConfiguration_WithNewConfiguration_ShouldUpdateInMemory()
    {
        // Arrange
        var configService = new ConfigurationService(_mockLogger);
        var newConfig = new ExporterConfiguration
        {
            Export = new ExportConfiguration { DefaultFormat = "xml" }
        };

        // Act
        configService.UpdateConfiguration(newConfig);

        // Assert
        configService.Configuration.Export.DefaultFormat.Should().Be("xml");
    }

    [Fact]
    public void UpdateConfiguration_WithAction_ShouldUpdateInMemory()
    {
        // Arrange
        var configService = new ConfigurationService(_mockLogger);

        // Act
        configService.UpdateConfiguration(config =>
        {
            config.Export.DefaultFormat = "json";
            config.Processing.MaxConcurrentFiles = 8;
        });

        // Assert
        configService.Configuration.Export.DefaultFormat.Should().Be("json");
        configService.Configuration.Processing.MaxConcurrentFiles.Should().Be(8);
    }

    [Fact]
    public void ReloadConfiguration_ShouldRefreshFromFile()
    {
        // Arrange
        var customConfig = new ExporterConfiguration
        {
            Export = new ExportConfiguration { DefaultFormat = "xlsx" }
        };

        var tempFile = CreateTempConfigFile(customConfig);
        var configService = new ConfigurationService(_mockLogger, tempFile);

        // Modify configuration in memory
        configService.UpdateConfiguration(config => config.Export.DefaultFormat = "json");

        // Act
        configService.ReloadConfiguration();

        // Assert
        configService.Configuration.Export.DefaultFormat.Should().Be("xlsx");
    }

    [Fact]
    public void CreateDefaultConfigurationFile_ShouldCreateValidFile()
    {
        // Arrange
        var tempFile = GetTempFilePath();
        var configService = new ConfigurationService(_mockLogger, tempFile);

        // Act
        configService.CreateDefaultConfigurationFile();

        // Assert
        File.Exists(tempFile).Should().BeTrue();

        var json = File.ReadAllText(tempFile);
        var config = JsonSerializer.Deserialize<ExporterConfiguration>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        config.Should().NotBeNull();
        config!.Export.DefaultFormat.Should().Be("csv");
        config.Logging.LogLevel.Should().Be("Information");
    }

    [Fact]
    public void GetConfigurationPath_ShouldReturnCorrectPath()
    {
        // Arrange
        var customPath = GetTempFilePath();
        var configService = new ConfigurationService(_mockLogger, customPath);

        // Act
        var path = configService.GetConfigurationPath();

        // Assert
        path.Should().Be(customPath);
    }

    [Fact]
    public void ConfigurationService_WithInvalidJsonFile_ShouldUseDefaults()
    {
        // Arrange
        var tempFile = GetTempFilePath();
        File.WriteAllText(tempFile, "{ invalid json }");

        // Act
        var configService = new ConfigurationService(_mockLogger, tempFile);

        // Assert
        configService.Configuration.Should().NotBeNull();
        configService.Configuration.Export.DefaultFormat.Should().Be("csv");
        _mockLogger.ContainsLogLevel("Error").Should().BeTrue();
    }

    [Fact]
    public void ConfigurationService_WithNonExistentFile_ShouldUseDefaults()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), "non-existent-config.json");

        // Act
        var configService = new ConfigurationService(_mockLogger, nonExistentFile);

        // Assert
        configService.Configuration.Should().NotBeNull();
        configService.Configuration.Export.DefaultFormat.Should().Be("csv");
        _mockLogger.ContainsMessage("Configuration file not found").Should().BeTrue();
    }

    private string CreateTempConfigFile(ExporterConfiguration config)
    {
        var tempFile = GetTempFilePath();
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(tempFile, json);
        return tempFile;
    }

    private string GetTempFilePath()
    {
        var tempFile = Path.GetTempFileName();
        var configFile = Path.ChangeExtension(tempFile, ".json");
        _tempFiles.Add(configFile);
        return configFile;
    }
}