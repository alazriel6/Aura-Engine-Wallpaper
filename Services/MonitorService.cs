using LiveWallpaperApp.Models;
using Forms = System.Windows.Forms;

namespace LiveWallpaperApp.Services;

public sealed class MonitorService
{
    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        return Forms.Screen.AllScreens
            .Select((screen, index) => new MonitorInfo(
                DeviceName: screen.DeviceName,
                FriendlyName: $"Display {index + 1}",
                Bounds: screen.Bounds,
                WorkingArea: screen.WorkingArea,
                IsPrimary: screen.Primary))
            .ToList();
    }

    public MonitorInfo GetPrimaryMonitor()
    {
        return GetMonitors().FirstOrDefault(m => m.IsPrimary)
            ?? GetMonitors().First();
    }
}
