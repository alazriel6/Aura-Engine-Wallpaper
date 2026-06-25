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

    public IReadOnlyList<MonitorInfoModel> GetExtendedMonitors()
    {
        var extendedList = new List<MonitorInfoModel>();
        var screens = Forms.Screen.AllScreens;
        for (int i = 0; i < screens.Length; i++)
        {
            var screen = screens[i];
            var monitorInfo = new MonitorInfo(
                DeviceName: screen.DeviceName,
                FriendlyName: $"Display {i + 1}",
                Bounds: screen.Bounds,
                WorkingArea: screen.WorkingArea,
                IsPrimary: screen.Primary);

            var model = new MonitorInfoModel(monitorInfo);

            // Fetch advanced display settings via P/Invoke
            var devMode = new Native.Win32.DEVMODE();
            devMode.dmSize = (short)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.Win32.DEVMODE));

            if (Native.Win32.EnumDisplaySettings(screen.DeviceName, Native.Win32.ENUM_CURRENT_SETTINGS, ref devMode))
            {
                model.RefreshRate = devMode.dmDisplayFrequency;
                model.ColorDepth = devMode.dmBitsPerPel;
                
                // Estimate Scaling: physical height / scaled working area height
                if (devMode.dmPelsHeight > 0 && screen.Bounds.Height > 0)
                {
                    double scaling = (double)devMode.dmPelsHeight / screen.Bounds.Height * 100;
                    model.ScalingPercentage = (int)Math.Round(scaling / 25.0) * 25; // Snap to 100, 125, 150...
                }

                model.Orientation = devMode.dmDisplayOrientation switch
                {
                    1 => "Portrait",
                    2 => "Landscape (Flipped)",
                    3 => "Portrait (Flipped)",
                    _ => "Landscape"
                };
            }

            // Default mock data for HDR
            model.IsHdrEnabled = devMode.dmBitsPerPel > 32;

            extendedList.Add(model);
        }

        return extendedList;
    }
}
