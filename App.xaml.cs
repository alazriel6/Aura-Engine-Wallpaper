using System.Windows;
using LibVLCSharp.Shared;

namespace LiveWallpaperApp;

/// <summary>
/// Application bootstrap. The dashboard and wallpaper renderer are intentionally separate windows:
/// MainWindow is the human-facing control panel, while WallpaperWindow is a pure render surface.
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // LibVLCSharp must discover the VideoLAN native runtime before any VideoView is created.
            Core.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"LibVLC failed to initialize. Restore NuGet packages and ensure VideoLAN.LibVLC.Windows is present.\n\n{ex.Message}",
                "Live Wallpaper Startup",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
