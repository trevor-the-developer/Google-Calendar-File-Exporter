using System.Text.Json;
using GoogleCalendarFileExporter.Interfaces;
using GoogleCalendarFileExporter.Models;

namespace GoogleCalendarFileExporter.Services;

public class ConfigurationService
{
    private readonly string _configFilePath;
    private readonly ILoggingService _logger;

    public ConfigurationService(ILoggingService logger, string? configFilePath = null)
    {
        _logger = logger;
        _configFilePath = configFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GoogleCalendarFileExporter",
            "appsettings.json"
        );

        Configuration = LoadConfiguration();
    }

    public ExporterConfiguration Configuration { get; private set; }

    private ExporterConfiguration LoadConfiguration()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<ExporterConfiguration>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });

                if (config != null)
                {
                    _logger.LogInformation("Configuration loaded from: {ConfigPath}", _configFilePath);
                    return config;
                }
            }
            else
            {
                _logger.LogInformation("Configuration file not found, using defaults: {ConfigPath}", _configFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration from: {ConfigPath}", _configFilePath);
        }

        return new ExporterConfiguration();
    }

    public void SaveConfiguration()
    {
        try
        {
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(Configuration, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            File.WriteAllText(_configFilePath, json);
            _logger.LogInformation("Configuration saved to: {ConfigPath}", _configFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration to: {ConfigPath}", _configFilePath);
        }
    }

    public void UpdateConfiguration(ExporterConfiguration newConfiguration)
    {
        Configuration = newConfiguration ?? throw new ArgumentNullException(nameof(newConfiguration));
        _logger.LogInformation("Configuration updated in memory");
    }

    public void UpdateConfiguration(Action<ExporterConfiguration> updateAction)
    {
        updateAction(Configuration);
        _logger.LogInformation("Configuration updated in memory");
    }

    public void ReloadConfiguration()
    {
        Configuration = LoadConfiguration();
        _logger.LogInformation("Configuration reloaded from: {ConfigPath}", _configFilePath);
    }

    public string GetConfigurationPath()
    {
        return _configFilePath;
    }

    public void CreateDefaultConfigurationFile()
    {
        try
        {
            var defaultConfig = new ExporterConfiguration();
            var directory = Path.GetDirectoryName(_configFilePath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            File.WriteAllText(_configFilePath, json);
            _logger.LogInformation("Default configuration file created at: {ConfigPath}", _configFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating default configuration file at: {ConfigPath}", _configFilePath);
        }
    }
}