using System.Windows.Input;
using LiveWallpaperApp.Helpers;

namespace LiveWallpaperApp.ViewModels;

public class MonitorCardViewModel : ObservableObject
{
    private readonly string _deviceName;
    private readonly Action<string> _clearWallpaperAction;
    private string _displayName;
    private string _activeWallpaperName;
    private string _resolutionText;

    public MonitorCardViewModel(string deviceName, string displayName, string resolutionText, string activeWallpaperName, Action<string> clearWallpaperAction)
    {
        _deviceName = deviceName;
        _displayName = displayName;
        _resolutionText = resolutionText;
        _activeWallpaperName = activeWallpaperName;
        _clearWallpaperAction = clearWallpaperAction;

        ClearWallpaperCommand = new RelayCommand(ClearWallpaper);
    }

    public string DeviceName => _deviceName;

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public string ResolutionText
    {
        get => _resolutionText;
        set => SetProperty(ref _resolutionText, value);
    }

    public string ActiveWallpaperName
    {
        get => _activeWallpaperName;
        set => SetProperty(ref _activeWallpaperName, value);
    }

    public ICommand ClearWallpaperCommand { get; }

    private void ClearWallpaper()
    {
        _clearWallpaperAction(_deviceName);
    }
}
