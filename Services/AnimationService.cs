using System.Windows.Media.Animation;
using System.Windows;
using LiveWallpaperApp.Models;

namespace LiveWallpaperApp.Services;

public sealed class AnimationService
{
    public bool ShouldUseRichAnimations(PerformanceSettings settings, SystemPerformanceSnapshot snapshot)
    {
        if (settings.UserPerformanceMode is UserPerformanceMode.PowerSaver or UserPerformanceMode.GamingMode)
        {
            return false;
        }

        if (settings.ReduceBackgroundUsageEnabled && (snapshot.GpuUsagePercent > 80 || snapshot.AppRamMb > 450))
        {
            return false;
        }

        return settings.AnimationSpeed > 0.3;
    }

    public Duration GetFastDuration(PerformanceSettings settings)
    {
        var milliseconds = 140 / Math.Max(0.25, settings.AnimationSpeed);
        return new Duration(TimeSpan.FromMilliseconds(milliseconds));
    }

    public Duration GetNormalDuration(PerformanceSettings settings)
    {
        var milliseconds = 220 / Math.Max(0.25, settings.AnimationSpeed);
        return new Duration(TimeSpan.FromMilliseconds(milliseconds));
    }
}
