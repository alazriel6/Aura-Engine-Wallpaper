using LiveWallpaperApp.Helpers;

namespace LiveWallpaperApp.Models;

public sealed class MonitorPerformanceModel : ObservableObject
{
    private double _gpuUsage;
    public double GpuUsage
    {
        get => _gpuUsage;
        set => SetProperty(ref _gpuUsage, value);
    }

    private double _vramUsageMb;
    public double VramUsageMb
    {
        get => _vramUsageMb;
        set => SetProperty(ref _vramUsageMb, value);
    }

    private int _fps;
    public int Fps
    {
        get => _fps;
        set => SetProperty(ref _fps, value);
    }

    private int _frameDrops;
    public int FrameDrops
    {
        get => _frameDrops;
        set => SetProperty(ref _frameDrops, value);
    }
}
