namespace LiveWallpaperApp.Models;

public sealed class ActiveWallpaperState
{
    // Dictionary mapping Monitor Device Name to Wallpaper Video Path
    public Dictionary<string, string> ActiveMonitors { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
