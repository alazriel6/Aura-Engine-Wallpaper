using System.Diagnostics;
using System.Drawing;
using System.Windows.Threading;
using LiveWallpaperApp.Models;
using LiveWallpaperApp.Native;

namespace LiveWallpaperApp.Services;

public sealed class AutoPauseService : IDisposable
{
    private readonly WallpaperService _wallpaperService;
    private readonly PerformanceService _performanceService;
    private readonly PerformanceSettings _settings;
    private readonly DispatcherTimer _timer;
    private bool _disposed;

    public AutoPauseService(
        WallpaperService wallpaperService,
        PerformanceService performanceService,
        PerformanceSettings settings)
    {
        _wallpaperService = wallpaperService;
        _performanceService = performanceService;
        _settings = settings;
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _timer.Tick += (_, _) => Evaluate();
    }

    public event EventHandler<AutoPauseState>? StateChanged;

    public List<string> ProcessWhitelist { get; } = new()
    {
        "explorer",
        "LiveWallpaperApp"
    };

    public List<string> ProcessBlacklist { get; } = new()
    {
        "valorant",
        "cs2",
        "cod",
        "fortniteclient-win64-shipping"
    };

    public AutoPauseState Current { get; private set; } = AutoPauseState.Active;

    public void Start()
    {
        _timer.Start();
        Evaluate();
    }

    public void Stop()
    {
        _timer.Stop();
        _wallpaperService.SetAutoPaused(false, "Auto pause stopped.");
    }

    public void Evaluate()
    {
        if (_disposed || !_wallpaperService.IsRunning)
        {
            return;
        }

        var foreground = GetForegroundProcessName();
        var reasons = new List<string>();
        var snapshot = _performanceService.Current;

        if (_settings.PauseFullscreenGame && IsForegroundFullscreen(out var fullscreenProcess))
        {
            foreground = fullscreenProcess;
            if (!IsWhitelisted(foreground))
            {
                reasons.Add($"Fullscreen app: {foreground}");
            }
        }

        if (_settings.PauseMaximizedApplication && IsForegroundMaximized(out var maximizedProcess))
        {
            foreground = maximizedProcess;
            if (!IsWhitelisted(foreground))
            {
                reasons.Add($"Maximized app: {foreground}");
            }
        }

        if (ProcessBlacklist.Any(name => foreground.Contains(name, StringComparison.OrdinalIgnoreCase)))
        {
            reasons.Add($"Process rule: {foreground}");
        }

        var power = System.Windows.Forms.SystemInformation.PowerStatus;
        if (_settings.PauseOnBattery && power.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Offline)
        {
            reasons.Add("Laptop unplugged");
        }

        if (_settings.PauseBatterySaver
            && power.BatteryChargeStatus.HasFlag(System.Windows.Forms.BatteryChargeStatus.Low))
        {
            reasons.Add("Battery saver");
        }

        if (_settings.PauseHighGpuUsage && snapshot.GpuUsagePercent >= _settings.AutoPauseGpuThreshold)
        {
            reasons.Add($"GPU load {snapshot.GpuUsagePercent:0}%");
        }

        if (snapshot.CpuUsagePercent >= _settings.AutoPauseCpuThreshold)
        {
            reasons.Add($"CPU load {snapshot.CpuUsagePercent:0}%");
        }

        if (_settings.PauseHighCpuTemperature
            && snapshot.CpuTemperatureCelsius >= _settings.AutoPauseCpuTemperatureThreshold)
        {
            reasons.Add($"CPU temp {snapshot.CpuTemperatureCelsius:0} C");
        }

        if (_settings.PauseUserInactive
            && Win32.GetIdleTime() >= TimeSpan.FromMinutes(_settings.IdlePauseMinutes))
        {
            reasons.Add("User inactive");
        }

        Current = reasons.Count > 0
            ? new AutoPauseState
            {
                ShouldPause = true,
                Reason = reasons[0],
                Reasons = reasons,
                ForegroundProcessName = foreground
            }
            : AutoPauseState.Active;

        _wallpaperService.SetAutoPaused(Current.ShouldPause, Current.Reason);
        StateChanged?.Invoke(this, Current);
    }

    private bool IsWhitelisted(string processName)
    {
        return ProcessWhitelist.Any(name => processName.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetForegroundProcessName()
    {
        var hwnd = Win32.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return string.Empty;
        }

        Win32.GetWindowThreadProcessId(hwnd, out var processId);

        try
        {
            return Process.GetProcessById((int)processId).ProcessName;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool IsForegroundFullscreen(out string processName)
    {
        return IsForegroundCoveringMonitor(requireExactBounds: true, out processName);
    }

    private static bool IsForegroundMaximized(out string processName)
    {
        return IsForegroundCoveringMonitor(requireExactBounds: false, out processName);
    }

    private static bool IsForegroundCoveringMonitor(bool requireExactBounds, out string processName)
    {
        processName = GetForegroundProcessName();
        var hwnd = Win32.GetForegroundWindow();
        if (hwnd == IntPtr.Zero || !Win32.GetWindowRect(hwnd, out var rect))
        {
            return false;
        }

        var bounds = new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);
        var screen = System.Windows.Forms.Screen.FromHandle(hwnd);
        var screenBounds = screen.Bounds;

        if (requireExactBounds)
        {
            return Math.Abs(bounds.Left - screenBounds.Left) <= 2
                && Math.Abs(bounds.Top - screenBounds.Top) <= 2
                && Math.Abs(bounds.Width - screenBounds.Width) <= 4
                && Math.Abs(bounds.Height - screenBounds.Height) <= 4;
        }

        return bounds.Width >= screenBounds.Width * 0.92
            && bounds.Height >= screenBounds.Height * 0.88;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
    }
}
