using System.Drawing;

namespace LiveWallpaperApp.Models;

public sealed record MonitorInfo(
    string DeviceName,
    string FriendlyName,
    Rectangle Bounds,
    Rectangle WorkingArea,
    bool IsPrimary,
    double DpiScaleX = 1.0,
    double DpiScaleY = 1.0)
{
    public string DisplayName => IsPrimary ? $"{FriendlyName} (Primary)" : FriendlyName;
}
