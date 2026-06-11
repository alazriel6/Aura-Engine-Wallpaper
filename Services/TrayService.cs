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
            Icon = System.Drawing.SystemIcons.Application,
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
        
        var panelBrush = Application.Current.TryFindResource("PanelBrush") as System.Windows.Media.Brush;
        var textBrush = Application.Current.TryFindResource("TextBrush") as System.Windows.Media.Brush;
        if (panelBrush != null) menu.Background = panelBrush;
        if (textBrush != null) menu.Foreground = textBrush;

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
