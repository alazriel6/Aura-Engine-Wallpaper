using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using LibVLCSharp.Shared;
using LiveWallpaperApp.Models;
using LiveWallpaperApp.Native;
using LiveWallpaperApp.Views;

namespace LiveWallpaperApp.Services;

public sealed class WallpaperService : IDisposable
{
    private readonly MonitorService _monitorService;
    private readonly GPUOptimizationService _gpuOptimizationService;
    private readonly List<WallpaperWindow> _wallpaperWindows = new();
    private readonly string _stateFilePath;
    private LibVLC? _sharedWallpaperLibVlc;
    private bool _disposed;
    private bool _isManualPaused;
    private bool _isAutoPaused;

    public WallpaperService(MonitorService monitorService, GPUOptimizationService gpuOptimizationService)
    {
        _monitorService = monitorService;
        _gpuOptimizationService = gpuOptimizationService;
        _stateFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LiveWallpaperApp",
            "state.json");
    }

    public event EventHandler<string>? StatusChanged;

    public bool IsRunning => _wallpaperWindows.Count > 0;
    public bool IsPaused => _isManualPaused || _isAutoPaused;
    public string? CurrentWallpaperPath { get; private set; }

    public static IReadOnlyList<string> VlcArguments { get; } =
        new GPUOptimizationService().BuildWallpaperVlcArguments(new PerformanceSettings());

    public void ApplyWallpaper(string videoPath, string? monitorDeviceName = null)
    {
        ApplyWallpaper(videoPath, monitorDeviceName, new PerformanceSettings());
    }

    public void ApplyWallpaper(string videoPath, string? monitorDeviceName, PerformanceSettings settings)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath))
        {
            throw new FileNotFoundException("Select a valid MP4 video file before applying a wallpaper.", videoPath);
        }

        if (!Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.Invoke(() => ApplyWallpaper(videoPath, monitorDeviceName, settings));
            return;
        }

        var workerW = Win32.EnsureWorkerW();
        var monitors = _monitorService.GetMonitors();
        var targetMonitors = monitors.ToList();

        if (!string.IsNullOrWhiteSpace(monitorDeviceName) && monitorDeviceName != "*")
        {
            targetMonitors = targetMonitors
                .Where(m => string.Equals(m.DeviceName, monitorDeviceName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        foreach (var monitor in targetMonitors)
        {
            var existingWindow = _wallpaperWindows.FirstOrDefault(w => string.Equals(w.Monitor.DeviceName, monitor.DeviceName, StringComparison.OrdinalIgnoreCase));
            if (existingWindow != null)
            {
                try
                {
                    existingWindow.Stop();
                    existingWindow.Close();
                    existingWindow.Dispose();
                }
                catch { }
                _wallpaperWindows.Remove(existingWindow);
            }
        }

        if (_sharedWallpaperLibVlc == null)
        {
            var vlcArguments = _gpuOptimizationService.BuildWallpaperVlcArguments(settings);
            _sharedWallpaperLibVlc = new LibVLC(vlcArguments.ToArray());
        }

        foreach (var monitor in targetMonitors)
        {
            var wallpaperWindow = new WallpaperWindow(monitor, _sharedWallpaperLibVlc);
            wallpaperWindow.Show();

            var handle = new WindowInteropHelper(wallpaperWindow).Handle;
            Win32.ConfigureWallpaperChild(
                handle,
                workerW,
                monitor.Bounds.Left,
                monitor.Bounds.Top,
                monitor.Bounds.Width,
                monitor.Bounds.Height);

            wallpaperWindow.Play(videoPath);
            _wallpaperWindows.Add(wallpaperWindow);
        }

        CurrentWallpaperPath = videoPath;
        _isManualPaused = false;
        _isAutoPaused = false;
        SaveState();
        StatusChanged?.Invoke(this, $"Wallpaper applied to {_wallpaperWindows.Count} display(s) using {settings.HardwareAcceleration} / {settings.PowerProfile}.");
    }

    public void Pause()
    {
        _isManualPaused = true;
        ApplyPauseState();
        StatusChanged?.Invoke(this, "Wallpaper paused.");
    }

    public void Resume()
    {
        _isManualPaused = false;
        ApplyPauseState();
        StatusChanged?.Invoke(this, _isAutoPaused ? "Wallpaper is still auto-paused." : "Wallpaper resumed.");
    }

    public void SetAutoPaused(bool paused, string reason)
    {
        if (_isAutoPaused == paused)
        {
            return;
        }

        _isAutoPaused = paused;
        ApplyPauseState();
        StatusChanged?.Invoke(this, paused ? $"Auto-paused: {reason}" : "Auto-pause released.");
    }

    public void TogglePause()
    {
        if (!IsRunning)
        {
            return;
        }

        if (_isManualPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    private void ApplyPauseState()
    {
        foreach (var window in _wallpaperWindows)
        {
            if (IsPaused)
            {
                window.Pause();
            }
            else
            {
                window.Resume();
            }
        }
    }

    public void Stop()
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.Invoke(Stop);
            return;
        }

        foreach (var window in _wallpaperWindows.ToList())
        {
            try
            {
                window.Stop();
                window.Close();
                window.Dispose();
            }
            catch
            {
                // Shutdown cleanup must keep going even if a renderer is already closed.
            }
        }

        _wallpaperWindows.Clear();
        _sharedWallpaperLibVlc?.Dispose();
        _sharedWallpaperLibVlc = null;
        _isManualPaused = false;
        _isAutoPaused = false;
        SaveState();
        StatusChanged?.Invoke(this, "Wallpaper stopped.");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void SaveState()
    {
        try
        {
            var state = new ActiveWallpaperState();
            foreach (var window in _wallpaperWindows)
            {
                if (!string.IsNullOrWhiteSpace(window.CurrentPath))
                {
                    state.ActiveMonitors[window.Monitor.DeviceName] = window.CurrentPath;
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_stateFilePath)!);
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_stateFilePath, json);
        }
        catch { }
    }

    public void RestoreState(PerformanceSettings settings)
    {
        try
        {
            if (!File.Exists(_stateFilePath)) return;

            var json = File.ReadAllText(_stateFilePath);
            var state = JsonSerializer.Deserialize<ActiveWallpaperState>(json);
            if (state == null) return;

            foreach (var kvp in state.ActiveMonitors)
            {
                if (File.Exists(kvp.Value))
                {
                    ApplyWallpaper(kvp.Value, kvp.Key, settings);
                }
            }
        }
        catch { }
    }
}
