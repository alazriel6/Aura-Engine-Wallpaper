using System;
using LiveWallpaperApp.Models;
using LiveWallpaperApp.Helpers;

namespace LiveWallpaperApp.Services;

public class SystemHealthService : ObservableObject
{
    private readonly PerformanceService _performanceService;
    private readonly SettingsService _settingsService;

    private string _healthScore = "A+";
    public string HealthScore
    {
        get => _healthScore;
        private set => SetProperty(ref _healthScore, value);
    }

    private string _recommendation = "Your system is running optimally.";
    public string Recommendation
    {
        get => _recommendation;
        private set => SetProperty(ref _recommendation, value);
    }

    private string _systemStatus = "Excellent";
    public string SystemStatus
    {
        get => _systemStatus;
        private set => SetProperty(ref _systemStatus, value);
    }

    public SystemHealthService(PerformanceService performanceService, SettingsService settingsService)
    {
        _performanceService = performanceService;
        _settingsService = settingsService;
    }

    public void UpdateHealth()
    {
        var snapshot = _performanceService.Current;
        if (snapshot == null) return;

        double cpu = snapshot.CpuUsagePercent;
        double gpu = snapshot.GpuUsagePercent;
        double ramMb = snapshot.AppRamMb;

        if (cpu > 80 || gpu > 85)
        {
            HealthScore = "D";
            SystemStatus = "Critical Load";
            Recommendation = "Hardware decoding or a lower FPS limit is highly recommended to reduce CPU/GPU usage.";
        }
        else if (cpu > 60 || gpu > 65 || ramMb > 2000)
        {
            HealthScore = "C";
            SystemStatus = "Heavy Load";
            Recommendation = "Consider switching to Battery Saver mode if performance drops.";
        }
        else if (cpu > 35 || gpu > 40 || ramMb > 1000)
        {
            HealthScore = "B";
            SystemStatus = "Moderate Load";
            Recommendation = "Running smoothly. Enabling 'Pause on fullscreen' can save resources.";
        }
        else if (cpu > 15 || gpu > 20)
        {
            HealthScore = "A";
            SystemStatus = "Good";
            Recommendation = "Resource usage is well within optimal limits.";
        }
        else
        {
            HealthScore = "A+";
            SystemStatus = "Excellent";
            Recommendation = "Minimal impact. System is running flawlessly.";
        }
    }
}
