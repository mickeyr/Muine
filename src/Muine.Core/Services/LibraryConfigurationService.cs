using System.Text.Json;
using Muine.Core.Models;

namespace Muine.Core.Services;

/// <summary>
/// Service for managing library configuration
/// </summary>
public class LibraryConfigurationService
{
    private readonly string _configPath;
    private LibraryConfiguration? _config;

    public LibraryConfigurationService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Muine"
        );
        
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        _configPath = Path.Combine(appDataPath, "library-config.json");
    }

    /// <summary>
    /// Load the library configuration, or create default if it doesn't exist
    /// </summary>
    public LibraryConfiguration LoadConfiguration()
    {
        if (_config != null)
        {
            return _config;
        }

        if (File.Exists(_configPath))
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                _config = JsonSerializer.Deserialize<LibraryConfiguration>(json);
                
                if (_config != null)
                {
                    return _config;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Warning($"Failed to load library configuration: {ex.Message}", "LibraryConfigurationService");
            }
        }

        // Create default configuration
        _config = new LibraryConfiguration();
        SaveConfiguration(_config);
        return _config;
    }

    /// <summary>
    /// Save the library configuration
    /// </summary>
    public void SaveConfiguration(LibraryConfiguration config)
    {
        _config = config;
        
        try
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to save library configuration", ex, "LibraryConfigurationService");
        }
    }

    /// <summary>
    /// Get the current configuration
    /// </summary>
    public LibraryConfiguration GetConfiguration()
    {
        return _config ?? LoadConfiguration();
    }
}
