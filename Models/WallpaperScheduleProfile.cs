namespace LiveWallpaperApp.Models;

public sealed class WallpaperScheduleProfile
{
    public string Name { get; set; } = "Daily Profile";
    public List<WallpaperScheduleRule> Rules { get; set; } = new();
}

public sealed class WallpaperScheduleRule
{
    public string Name { get; set; } = "Night Mode";
    public TimeOnly Start { get; set; } = new(19, 0);
    public TimeOnly End { get; set; } = new(7, 0);
    public string WallpaperId { get; set; } = string.Empty;
    public DayOfWeek[] Days { get; set; } =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    ];
}
