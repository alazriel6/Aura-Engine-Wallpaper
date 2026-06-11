using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using LiveWallpaperApp.Helpers;

namespace LiveWallpaperApp.Services;

public sealed class TrayService : IDisposable
{
    private TaskbarIcon? _trayIcon;

    public void Initialize(Action restoreDashboard, Action pauseResumeWallpaper, Action stopWallpaper, Action exitApplication)
    {
        _trayIcon?.Dispose();

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Live Wallpaper App",
            IconSource = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assets/icon.png")),
            ContextMenu = BuildContextMenu(restoreDashboard, pauseResumeWallpaper, stopWallpaper, exitApplication)
        };

        _trayIcon.TrayMouseDoubleClick += (_, _) => restoreDashboard();
    }

    public void ShowInfo(string title, string message)
    {
        _trayIcon?.ShowBalloonTip(title, message, BalloonIcon.Info);
    }

    private static ContextMenu BuildContextMenu(
        Action restoreDashboard,
        Action pauseResumeWallpaper,
        Action stopWallpaper,
        Action exitApplication)
    {
        var menu = new ContextMenu();
        menu.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(10, 13, 18));
        menu.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 248, 251));
        menu.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(56, 72, 89));
        menu.BorderThickness = new Thickness(1);

        menu.Items.Add(new MenuItem
        {
            Header = "Open dashboard",
            Command = new RelayCommand(restoreDashboard)
        });

        menu.Items.Add(new MenuItem
        {
            Header = "Pause / Resume wallpaper",
            Command = new RelayCommand(pauseResumeWallpaper)
        });

        menu.Items.Add(new MenuItem
        {
            Header = "Stop wallpaper",
            Command = new RelayCommand(stopWallpaper)
        });

        menu.Items.Add(new Separator());

        menu.Items.Add(new MenuItem
        {
            Header = "Exit",
            Command = new RelayCommand(exitApplication)
        });

        return menu;
    }

    public void Dispose()
    {
        _trayIcon?.Dispose();
        _trayIcon = null;
    }
}
