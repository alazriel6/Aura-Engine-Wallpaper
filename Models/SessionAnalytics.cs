using System;
using LiveWallpaperApp.Helpers;

namespace LiveWallpaperApp.Models;

public class SessionAnalytics : ObservableObject
{
    private TimeSpan _currentSessionRuntime;
    private double _averageFps;
    private double _peakGpuUsage;
    private double _peakRamUsageMb;
    private long _droppedFrames;
    private long _renderedFrames;
    private int _totalWallpapersApplied;
    private TimeSpan _totalRuntimeToday;

    public TimeSpan CurrentSessionRuntime
    {
        get => _currentSessionRuntime;
        set => SetProperty(ref _currentSessionRuntime, value);
    }

    public double AverageFps
    {
        get => _averageFps;
        set => SetProperty(ref _averageFps, value);
    }

    public double PeakGpuUsage
    {
        get => _peakGpuUsage;
        set => SetProperty(ref _peakGpuUsage, value);
    }

    public double PeakRamUsageMb
    {
        get => _peakRamUsageMb;
        set => SetProperty(ref _peakRamUsageMb, value);
    }

    public long DroppedFrames
    {
        get => _droppedFrames;
        set => SetProperty(ref _droppedFrames, value);
    }

    public long RenderedFrames
    {
        get => _renderedFrames;
        set => SetProperty(ref _renderedFrames, value);
    }

    public int TotalWallpapersApplied
    {
        get => _totalWallpapersApplied;
        set => SetProperty(ref _totalWallpapersApplied, value);
    }

    public TimeSpan TotalRuntimeToday
    {
        get => _totalRuntimeToday;
        set => SetProperty(ref _totalRuntimeToday, value);
    }
}
