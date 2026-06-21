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
    private readonly List<WinFormsWallpaperForm> _wallpaperWindows = new();
    private readonly string _stateFilePath;
    private readonly string _logPath;
    private LibVLC? _sharedWallpaperLibVlc;
    private bool _disposed;
    private bool _isManualPaused;
    private bool _isAutoPaused;

    public WallpaperService(MonitorService monitorService, GPUOptimizationService gpuOptimizationService)
    {
        _monitorService = monitorService;
        _gpuOptimizationService = gpuOptimizationService;

        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LiveWallpaperApp");

        _stateFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LiveWallpaperApp",
            "state.json");

        _logPath = Path.Combine(appDataDir, "debug.log");

        // Pre-initialize LibVLC on background thread so it's ready when Apply is called
        Task.Run(() =>
        {
            try
            {
                var args = _gpuOptimizationService.BuildWallpaperVlcArguments(new PerformanceSettings());
                _sharedWallpaperLibVlc = new LibVLC(args.ToArray());
                Log("Pre-initialized LibVLC OK");
            }
            catch (Exception ex)
            {
                Log($"Pre-init LibVLC failed: {ex.Message}");
            }
        });
    }

    public event EventHandler<string>? StatusChanged;
    public event EventHandler? ActiveWallpapersChanged;

    public bool IsRunning => _wallpaperWindows.Count > 0;
    public bool IsPaused => _isManualPaused || _isAutoPaused;
    public string? CurrentWallpaperPath { get; private set; }

    private void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
            File.AppendAllText(_logPath, $"[{DateTime.Now:HH:mm:ss.fff}] WallpaperService: {message}\n");
        }
        catch { }
    }

    public void ApplyWallpaper(string videoPath, string? monitorDeviceName = null)
    {
        ApplyWallpaper(videoPath, monitorDeviceName, new PerformanceSettings());
    }

    public void ApplyWallpaper(string videoPath, string? monitorDeviceName, PerformanceSettings settings)
    {
        ThrowIfDisposed();

        Log($"ApplyWallpaper called: path={videoPath}, monitor={monitorDeviceName}");

        if (string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath))
        {
            Log($"ApplyWallpaper: file not found: {videoPath}");
            throw new FileNotFoundException("Select a valid MP4 video file before applying a wallpaper.", videoPath);
        }

        if (!Application.Current.Dispatcher.CheckAccess())
        {
            Log("ApplyWallpaper: marshalling to UI thread");
            Application.Current.Dispatcher.Invoke(() => ApplyWallpaper(videoPath, monitorDeviceName, settings));
            return;
        }

        // Step 1: Find WorkerW
        Log("ApplyWallpaper: calling EnsureWorkerW");
        var (desktopHost, shellView) = Win32.EnsureWorkerW();
        Log($"ApplyWallpaper: desktopHost=0x{desktopHost:X}, shellView=0x{shellView:X}");

        // Step 2: Get target monitors
        var monitors = _monitorService.GetMonitors();
        var targetMonitors = monitors.ToList();

        if (!string.IsNullOrWhiteSpace(monitorDeviceName) && monitorDeviceName != "*")
        {
            targetMonitors = targetMonitors
                .Where(m => string.Equals(m.DeviceName, monitorDeviceName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        Log($"ApplyWallpaper: targeting {targetMonitors.Count} monitor(s)");

        // Step 3: Remove old windows (non-blocking)
        foreach (var monitor in targetMonitors)
        {
            var existingWindow = _wallpaperWindows.FirstOrDefault(w =>
                string.Equals(w.Monitor.DeviceName, monitor.DeviceName, StringComparison.OrdinalIgnoreCase));
            if (existingWindow != null)
            {
                _wallpaperWindows.Remove(existingWindow);
                try { existingWindow.Dispose(); } catch (Exception ex) { Log($"ApplyWallpaper: old.Dispose error: {ex}"); }
            }
        }

        // Step 4: Create shared LibVLC (once)
        if (_sharedWallpaperLibVlc == null)
        {
            var vlcArguments = _gpuOptimizationService.BuildWallpaperVlcArguments(settings);
            Log($"ApplyWallpaper: creating LibVLC with args: {string.Join(" ", vlcArguments)}");
            _sharedWallpaperLibVlc = new LibVLC(vlcArguments.ToArray());
            Log("ApplyWallpaper: LibVLC created OK");
        }

        // Step 5: Create and attach wallpaper windows
        foreach (var monitor in targetMonitors)
        {
            Log($"ApplyWallpaper: creating window for {monitor.DeviceName} bounds=({monitor.Bounds.Left},{monitor.Bounds.Top},{monitor.Bounds.Width},{monitor.Bounds.Height})");

            var wallpaperWindow = new WinFormsWallpaperForm(monitor, _sharedWallpaperLibVlc);
            wallpaperWindow.Show();

            var handle = wallpaperWindow.Handle;
            Log($"ApplyWallpaper: window HWND = 0x{handle:X}");

            Win32.ConfigureWallpaperChild(
                handle,
                desktopHost,
                shellView,
                monitor.Bounds.Left,
                monitor.Bounds.Top,
                monitor.Bounds.Width,
                monitor.Bounds.Height);

            Log("ApplyWallpaper: calling Play");
            wallpaperWindow.Play(videoPath);
            
            // Make the WinForms Form AND the LibVLC VideoView child windows click-through
            // so the user can interact with their desktop.
            Win32.MakeWindowAndChildrenTransparent(handle);
            
            _wallpaperWindows.Add(wallpaperWindow);
            Log("ApplyWallpaper: Play called OK");
        }

        CurrentWallpaperPath = videoPath;
        _isManualPaused = false;
        _isAutoPaused = false;
        SaveState();
        Log("ApplyWallpaper: done, state saved");
        StatusChanged?.Invoke(this, $"Wallpaper applied to {_wallpaperWindows.Count} display(s).");
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

    public void Stop(bool saveState = true)
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.Invoke(() => Stop(saveState));
            return;
        }

        var windowsToDispose = _wallpaperWindows.ToList();
        _wallpaperWindows.Clear();
        _isManualPaused = false;
        _isAutoPaused = false;
        CurrentWallpaperPath = null;

        var libVlc = _sharedWallpaperLibVlc;
        _sharedWallpaperLibVlc = null;

        Task.Run(() =>
        {
            foreach (var window in windowsToDispose)
            {
                try { window.Dispose(); } catch { }
            }
            try { libVlc?.Dispose(); } catch { }
        });

        ActiveWallpapersChanged?.Invoke(this, EventArgs.Empty);

        if (saveState)
        {
            SaveState();
        }
        
        StatusChanged?.Invoke(this, "Wallpaper stopped.");
    }

    public void ClearMonitorWallpaper(string monitorDeviceName)
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.Invoke(() => ClearMonitorWallpaper(monitorDeviceName));
            return;
        }

        var window = _wallpaperWindows.FirstOrDefault(w => string.Equals(w.Monitor.DeviceName, monitorDeviceName, StringComparison.OrdinalIgnoreCase));
        if (window != null)
        {
            _wallpaperWindows.Remove(window);
            window.Stop();
            try { window.Dispose(); } catch { }
            
            ActiveWallpapersChanged?.Invoke(this, EventArgs.Empty);
            SaveState();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop(saveState: false);
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
        catch (Exception ex)
        {
            Log($"RestoreState failed: {ex.Message}");
        }
    }

    public IReadOnlyDictionary<string, string> GetActiveWallpapers()
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var window in _wallpaperWindows)
        {
            if (!string.IsNullOrWhiteSpace(window.CurrentPath))
            {
                dict[window.Monitor.DeviceName] = window.CurrentPath;
            }
        }
        return dict;
    }
}
