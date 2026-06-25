using System;
using System.Collections.ObjectModel;
using System.Linq;
using LiveWallpaperApp.Models;

namespace LiveWallpaperApp.Services;

public class PerformanceProfile
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public FpsLimitMode FpsLimit { get; set; }
    public HardwareAccelerationMode HardwareAcceleration { get; set; }
    public bool PauseFullscreenGame { get; set; }
    public bool PauseOnBattery { get; set; }
    public int RenderScalePercent { get; set; }
}

public class ProfileService
{
    private readonly SettingsService _settingsService;
    private readonly PerformanceSettings _settings;

    public ObservableCollection<PerformanceProfile> BuiltInProfiles { get; } = new();

    public ProfileService(SettingsService settingsService, PerformanceSettings settings)
    {
        _settingsService = settingsService;
        _settings = settings;

        BuiltInProfiles.Add(new PerformanceProfile
        {
            Name = "Battery Saver",
            Description = "Maximizes battery life by pausing animations frequently.",
            Icon = "\uE856", // Battery icon
            FpsLimit = FpsLimitMode.Fps15,
            HardwareAcceleration = HardwareAccelerationMode.Auto,
            PauseFullscreenGame = true,
            PauseOnBattery = true,
            RenderScalePercent = 50
        });

        BuiltInProfiles.Add(new PerformanceProfile
        {
            Name = "Balanced",
            Description = "Good balance of performance and visual quality.",
            Icon = "\uE716", // Balance icon
            FpsLimit = FpsLimitMode.Fps30,
            HardwareAcceleration = HardwareAccelerationMode.D3D11VA,
            PauseFullscreenGame = true,
            PauseOnBattery = true,
            RenderScalePercent = 100
        });

        BuiltInProfiles.Add(new PerformanceProfile
        {
            Name = "Gaming",
            Description = "Pauses all rendering when a game is detected.",
            Icon = "\uE909", // Game icon
            FpsLimit = FpsLimitMode.Fps60,
            HardwareAcceleration = HardwareAccelerationMode.D3D11VA,
            PauseFullscreenGame = true,
            PauseOnBattery = true,
            RenderScalePercent = 100
        });

        BuiltInProfiles.Add(new PerformanceProfile
        {
            Name = "Ultra",
            Description = "Maximum quality and FPS. High resource usage.",
            Icon = "\uE9A1", // Rocket icon
            FpsLimit = FpsLimitMode.Fps120,
            HardwareAcceleration = HardwareAccelerationMode.D3D11VA,
            PauseFullscreenGame = false,
            PauseOnBattery = false,
            RenderScalePercent = 100
        });
    }

    public void ApplyProfile(PerformanceProfile profile)
    {
        _settings.FpsLimit = profile.FpsLimit;
        _settings.HardwareAcceleration = profile.HardwareAcceleration;
        _settings.PauseFullscreenGame = profile.PauseFullscreenGame;
        _settings.PauseOnBattery = profile.PauseOnBattery;
        _settings.RenderScale = profile.RenderScalePercent / 100.0;
        _settingsService.SaveSettings(_settings);
    }
}
