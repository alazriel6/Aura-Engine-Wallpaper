using System;
using System.IO;
using System.Text.Json;
using LiveWallpaperApp.Models;

namespace LiveWallpaperApp.Services;

public sealed class SettingsService
{
    private readonly string _settingsFilePath;

    public SettingsService()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDirectory = Path.Combine(localAppData, "LiveWallpaperApp");
        Directory.CreateDirectory(appDirectory);
        _settingsFilePath = Path.Combine(appDirectory, "Settings.json");
    }

    public PerformanceSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<PerformanceSettings>(json);
                if (settings != null)
                {
                    return settings;
                }
            }
        }
        catch
        {
            // Fallback to default if corrupted
        }

        return new PerformanceSettings();
    }

    public void SaveSettings(PerformanceSettings settings)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
            // Ignored to prevent crashing on exit
        }
    }
}
