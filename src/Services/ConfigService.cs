using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using ScreenHealthGuardian.Models;

namespace ScreenHealthGuardian.Services;

/// <summary>
/// Thread-safe configuration manager persisting to %APPDATA%/ScreenHealthGuardian/config.json.
/// </summary>
public class ConfigService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ScreenHealthGuardian"
    );

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");
    private readonly object _lock = new();
    private AppConfig _config = new();

    public AppConfig Current
    {
        get
        {
            lock (_lock)
            {
                return _config;
            }
        }
    }

    public ConfigService()
    {
        Load();
    }

    public void Load()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    _config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                    return;
                }

                // Check legacy config from WorkHealthTimer
                string legacyPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "WorkHealthTimer",
                    "config.json"
                );

                if (File.Exists(legacyPath))
                {
                    string legacyJson = File.ReadAllText(legacyPath);
                    _config = JsonSerializer.Deserialize<AppConfig>(legacyJson) ?? new AppConfig();
                    Save();
                    return;
                }

                // Initialize default and check registry language
                _config = new AppConfig();
                DetectRegistryLanguage();
                Save();
            }
            catch
            {
                _config = new AppConfig();
                Save();
            }
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            try
            {
                if (!Directory.Exists(ConfigDir))
                {
                    Directory.CreateDirectory(ConfigDir);
                }

                string json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch
            {
                // Silently handle IO failures
            }
        }
    }

    public void Update(Action<AppConfig> updateAction)
    {
        lock (_lock)
        {
            updateAction(_config);
            Save();
        }
    }

    private void DetectRegistryLanguage()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\ScreenHealthGuardian");
            if (key?.GetValue("InstallLanguage") is string lang)
            {
                _config.Language = lang.Equals("spanish", StringComparison.OrdinalIgnoreCase) ? "es" : "en";
            }
        }
        catch
        {
            // Ignore registry lookup errors
        }
    }
}
