using System.Drawing;
using LiveWallpaperApp.Helpers;

namespace LiveWallpaperApp.Models;

public sealed class MonitorInfoModel : ObservableObject
{
    public MonitorInfo BaseInfo { get; }

    public MonitorInfoModel(MonitorInfo baseInfo)
    {
        BaseInfo = baseInfo;
        Performance = new MonitorPerformanceModel();
    }

    public string DeviceName => BaseInfo.DeviceName;
    public string FriendlyName => BaseInfo.FriendlyName;
    public string DisplayName => BaseInfo.DisplayName;
    public bool IsPrimary => BaseInfo.IsPrimary;
    public Rectangle Bounds => BaseInfo.Bounds;
    public Rectangle WorkingArea => BaseInfo.WorkingArea;

    private int _refreshRate = 60;
    public int RefreshRate
    {
        get => _refreshRate;
        set => SetProperty(ref _refreshRate, value);
    }

    private int _colorDepth = 32;
    public int ColorDepth
    {
        get => _colorDepth;
        set => SetProperty(ref _colorDepth, value);
    }

    private bool _isHdrEnabled;
    public bool IsHdrEnabled
    {
        get => _isHdrEnabled;
        set => SetProperty(ref _isHdrEnabled, value);
    }

    private int _scalingPercentage = 100;
    public int ScalingPercentage
    {
        get => _scalingPercentage;
        set => SetProperty(ref _scalingPercentage, value);
    }

    private string _orientation = "Landscape";
    public string Orientation
    {
        get => _orientation;
        set => SetProperty(ref _orientation, value);
    }

    private string _connectionType = "Unknown";
    public string ConnectionType
    {
        get => _connectionType;
        set => SetProperty(ref _connectionType, value);
    }

    private string _healthStatus = "Healthy";
    public string HealthStatus
    {
        get => _healthStatus;
        set => SetProperty(ref _healthStatus, value);
    }

    private bool _isWallpaperActive;
    public bool IsWallpaperActive
    {
        get => _isWallpaperActive;
        set => SetProperty(ref _isWallpaperActive, value);
    }

    private string _currentWallpaperName = "None";
    public string CurrentWallpaperName
    {
        get => _currentWallpaperName;
        set => SetProperty(ref _currentWallpaperName, value);
    }

    private double _canvasLeft;
    public double CanvasLeft
    {
        get => _canvasLeft;
        set 
        {
            SetProperty(ref _canvasLeft, value);
            OnPropertyChanged(nameof(CanvasMargin));
        }
    }

    private double _canvasTop;
    public double CanvasTop
    {
        get => _canvasTop;
        set 
        {
            SetProperty(ref _canvasTop, value);
            OnPropertyChanged(nameof(CanvasMargin));
        }
    }

    public System.Windows.Thickness CanvasMargin => new System.Windows.Thickness(_canvasLeft, _canvasTop, 0, 0);

    private double _canvasWidth = 100;
    public double CanvasWidth
    {
        get => _canvasWidth;
        set => SetProperty(ref _canvasWidth, value);
    }

    private double _canvasHeight = 100;
    public double CanvasHeight
    {
        get => _canvasHeight;
        set => SetProperty(ref _canvasHeight, value);
    }

    public MonitorPerformanceModel Performance { get; }

    public string ResolutionString => $"{Bounds.Width}x{Bounds.Height}";
}
