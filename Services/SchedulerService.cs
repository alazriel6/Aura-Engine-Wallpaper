using LiveWallpaperApp.Models;

namespace LiveWallpaperApp.Services;

public sealed class SchedulerService
{
    public WallpaperScheduleRule? GetActiveRule(WallpaperScheduleProfile profile, DateTimeOffset now)
    {
        var currentDay = now.DayOfWeek;
        var currentTime = TimeOnly.FromDateTime(now.LocalDateTime);

        return profile.Rules.FirstOrDefault(rule =>
            rule.Days.Contains(currentDay) && IsInsideWindow(currentTime, rule.Start, rule.End));
    }

    private static bool IsInsideWindow(TimeOnly value, TimeOnly start, TimeOnly end)
    {
        if (start <= end)
        {
            return value >= start && value <= end;
        }

        return value >= start || value <= end;
    }
}
