using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LiveWallpaperApp.Helpers;
using LiveWallpaperApp.Models;
using LiveWallpaperApp.Services;

namespace LiveWallpaperApp.ViewModels;

public sealed class MonitorViewModel : ObservableObject, IDisposable
{
    private readonly MonitorService _monitorService;
    private readonly PerformanceService _performanceService;

    public MonitorViewModel(MonitorService monitorService, PerformanceService performanceService)
    {
        _monitorService = monitorService;
        _performanceService = performanceService;
        Monitors = new ObservableCollection<MonitorInfoModel>();
        
        WallpaperModes = new ObservableCollection<string>
        {
            "Individual Mode",
            "Clone Mode",
            "Span Mode",
            "Playlist Sync Mode"
        };
        
        SelectedWallpaperMode = WallpaperModes[0];
        
        RefreshMonitorsCommand = new RelayCommand(RefreshMonitors);
        ChangeWallpaperCommand = new RelayCommand(ChangeWallpaper);
        SyncWallpapersCommand = new RelayCommand(SyncWallpapers);
        DetectDisplaysCommand = new RelayCommand(DetectDisplays);
        ApplyToAllCommand = new RelayCommand(ApplyToAll);
        
        RefreshMonitors();

        _performanceService.SnapshotUpdated += OnPerformanceSnapshotUpdated;
    }

    private void OnPerformanceSnapshotUpdated(object? sender, SystemPerformanceSnapshot e)
    {
        // System-wide GPU usage broadcasted to all monitors
        foreach (var m in Monitors)
        {
            m.Performance.GpuUsage = e.GpuUsagePercent / 100.0; // Assuming UI expects 0.0-1.0
            m.Performance.VramUsageMb = e.VramUsageMb;
            // Simulated per-monitor Render FPS based on GPU load
            m.Performance.Fps = e.GpuUsagePercent > 0 ? (int)(60 + (e.GpuUsagePercent / 2.0)) : 0;
            
            // Re-evaluate health status based on GPU per monitor if needed
            if (e.GpuUsagePercent > 80) m.HealthStatus = "Critical";
            else if (e.GpuUsagePercent > 50) m.HealthStatus = "Heavy Load";
            else m.HealthStatus = "Healthy";
        }
    }

    public ObservableCollection<MonitorInfoModel> Monitors { get; }

    private MonitorInfoModel? _selectedMonitor;
    public MonitorInfoModel? SelectedMonitor
    {
        get => _selectedMonitor;
        set => SetProperty(ref _selectedMonitor, value);
    }

    public ObservableCollection<string> WallpaperModes { get; }

    private string _selectedWallpaperMode = string.Empty;
    public string SelectedWallpaperMode
    {
        get => _selectedWallpaperMode;
        set => SetProperty(ref _selectedWallpaperMode, value);
    }

    public ICommand RefreshMonitorsCommand { get; }
    public ICommand ChangeWallpaperCommand { get; }
    public ICommand SyncWallpapersCommand { get; }
    public ICommand DetectDisplaysCommand { get; }
    public ICommand ApplyToAllCommand { get; }

    private void RefreshMonitors()
    {
        Monitors.Clear();
        var sysMonitors = _monitorService.GetExtendedMonitors();
        
        foreach (var m in sysMonitors)
        {
            Monitors.Add(m);
        }

        if (Monitors.Any())
        {
            SelectedMonitor = Monitors.FirstOrDefault(m => m.IsPrimary) ?? Monitors.First();
            CalculateCanvasLayout();
        }
    }

    private void CalculateCanvasLayout()
    {
        if (!Monitors.Any()) return;

        double minX = Monitors.Min(m => m.Bounds.X);
        double minY = Monitors.Min(m => m.Bounds.Y);
        double maxX = Monitors.Max(m => m.Bounds.Right);
        double maxY = Monitors.Max(m => m.Bounds.Bottom);

        double totalWidth = maxX - minX;
        double totalHeight = maxY - minY;

        // Container is roughly 800x300, leave some padding
        double maxTargetWidth = 700;
        double maxTargetHeight = 240;

        double scaleX = maxTargetWidth / totalWidth;
        double scaleY = maxTargetHeight / totalHeight;
        double scale = System.Math.Min(scaleX, scaleY);

        // Center offset
        double scaledTotalWidth = totalWidth * scale;
        double scaledTotalHeight = totalHeight * scale;
        double offsetX = (maxTargetWidth - scaledTotalWidth) / 2 + 50; // Add 50 for absolute container padding
        double offsetY = (maxTargetHeight - scaledTotalHeight) / 2 + 30; // Add 30 for absolute container padding

        foreach (var m in Monitors)
        {
            m.CanvasWidth = m.Bounds.Width * scale;
            m.CanvasHeight = m.Bounds.Height * scale;
            m.CanvasLeft = (m.Bounds.X - minX) * scale + offsetX;
            m.CanvasTop = (m.Bounds.Y - minY) * scale + offsetY;
        }
    }

    private void ChangeWallpaper()
    {
        // TODO: Open wallpaper selection dialog or navigate to Home/Marketplace with target monitor
    }

    private void SyncWallpapers()
    {
        // TODO: Implement sync logic
    }

    private void DetectDisplays()
    {
        RefreshMonitors();
    }

    private void ApplyToAll()
    {
        // TODO: Apply current wallpaper to all
    }

    public void Dispose()
    {
        _performanceService.SnapshotUpdated -= OnPerformanceSnapshotUpdated;
    }
}
