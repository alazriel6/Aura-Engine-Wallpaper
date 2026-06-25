using System.Diagnostics;
using System.Drawing;
using System.Windows.Threading;
using LiveWallpaperApp.Models;
using LiveWallpaperApp.Native;
using Microsoft.Win32;

namespace LiveWallpaperApp.Services;

public sealed class AutoPauseService : IDisposable
{
    private readonly WallpaperService _wallpaperService;
    private readonly PerformanceService _performanceService;
    private readonly PerformanceSettings _settings;
    private readonly DispatcherTimer _timer;
    private bool _disposed;
    
    // System States
    private bool _isScreenLocked;
    private bool _isMonitorSleeping;

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
        
        SystemEvents.SessionSwitch += OnSessionSwitch;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        _isScreenLocked = e.Reason == SessionSwitchReason.SessionLock;
        if (e.Reason == SessionSwitchReason.SessionUnlock) _isScreenLocked = false;
        Evaluate();
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend) _isMonitorSleeping = true;
        if (e.Mode == PowerModes.Resume) _isMonitorSleeping = false;
        Evaluate();
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

    public event EventHandler<string>? LimitWarningTriggered;

    public void Evaluate()
    {
        if (_disposed || !_wallpaperService.IsRunning)
        {
            return;
        }

        var foreground = GetForegroundProcessName();
        var reasons = new List<string>();
        var snapshot = _performanceService.Current;
        var isThrottled = false;
        var throttleReasons = new List<string>();

        // 1. SMART PAUSE ENGINE
        if (_isScreenLocked && _settings.PauseScreenLocked) reasons.Add("Screen is locked");
        if (_isMonitorSleeping && _settings.PauseMonitorSleeping) reasons.Add("Monitor is sleeping");
        if (_settings.PauseRemoteDesktop && System.Windows.Forms.SystemInformation.TerminalServerSession) reasons.Add("Remote Desktop Session");

        if (_settings.PauseFullscreenGame && IsForegroundFullscreen(out var fullscreenProcess))
        {
            foreground = fullscreenProcess;
            if (!IsWhitelisted(foreground)) reasons.Add($"Fullscreen app: {foreground}");
        }

        if (_settings.PauseMaximizedApplication && IsForegroundMaximized(out var maximizedProcess))
        {
            foreground = maximizedProcess;
            if (!IsWhitelisted(foreground)) reasons.Add($"Maximized app: {foreground}");
        }

        if (_settings.PauseMinimized && foreground == string.Empty)
        {
            // Simple heuristic: if no foreground window, or only explorer desktop, it might be minimized, 
            // but we usually want to run when desktop is visible. Wait, pause minimized means pause if wallpaper is obscured.
            // We'll just pause if another app is full screen or maximized, which we already cover.
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

        if (_settings.PauseBatterySaver && power.BatteryChargeStatus.HasFlag(System.Windows.Forms.BatteryChargeStatus.Low))
        {
            reasons.Add("Battery saver active");
        }

        if (_settings.PauseHighGpuUsage && snapshot.GpuUsagePercent >= _settings.AutoPauseGpuThreshold)
        {
            reasons.Add($"GPU load {snapshot.GpuUsagePercent:0}%");
        }

        if (snapshot.CpuUsagePercent >= _settings.AutoPauseCpuThreshold)
        {
            reasons.Add($"CPU load {snapshot.CpuUsagePercent:0}%");
        }

        if (_settings.PauseHighCpuTemperature && snapshot.CpuTemperatureCelsius >= _settings.AutoPauseCpuTemperatureThreshold)
        {
            reasons.Add($"CPU temp {snapshot.CpuTemperatureCelsius:0} C");
        }

        if (_settings.PauseUserInactive && Win32.GetIdleTime() >= TimeSpan.FromMinutes(_settings.IdlePauseMinutes))
        {
            reasons.Add("User inactive");
        }

        if (_settings.PauseStreamingSoftware && IsStreamingSoftwareRunning())
        {
            reasons.Add("Streaming/Recording software active");
        }

        // 2. RESOURCE LIMITS ENFORCEMENT
        string limitWarning = string.Empty;

        if (snapshot.CpuUsagePercent >= _settings.MaxCpuUsageLimit) limitWarning = $"System CPU Limit Exceeded ({snapshot.CpuUsagePercent:0}% > {_settings.MaxCpuUsageLimit}%)";
        else if (snapshot.GpuUsagePercent >= _settings.MaxGpuUsageLimit) limitWarning = $"System GPU Limit Exceeded ({snapshot.GpuUsagePercent:0}% > {_settings.MaxGpuUsageLimit}%)";
        else if (snapshot.AppRamMb >= _settings.MaxRamUsageLimitMb) limitWarning = $"App RAM Limit Exceeded ({snapshot.AppRamMb:0} MB > {_settings.MaxRamUsageLimitMb} MB)";
        else if (snapshot.VramUsageMb > 0 && snapshot.VramUsageMb >= _settings.MaxVramUsageLimitMb) limitWarning = $"System VRAM Limit Exceeded ({snapshot.VramUsageMb:0} MB > {_settings.MaxVramUsageLimitMb} MB)";

        if (!string.IsNullOrEmpty(limitWarning))
        {
            if (_settings.ResourceLimitExceededAction == ResourceExceedAction.PauseWallpaper) reasons.Add(limitWarning);
            else if (_settings.ResourceLimitExceededAction == ResourceExceedAction.WarnUser) LimitWarningTriggered?.Invoke(this, limitWarning);
            else if (_settings.ResourceLimitExceededAction == ResourceExceedAction.ReduceQuality)
            {
                isThrottled = true;
                throttleReasons.Add(limitWarning);
            }
        }

        // 3. ADAPTIVE PERFORMANCE (THROTTLING)
        if (_settings.ReduceFpsHighCpu && snapshot.CpuUsagePercent > 80)
        {
            isThrottled = true;
            throttleReasons.Add("High CPU Load");
        }
        if (_settings.ReduceFpsHighGpu && snapshot.GpuUsagePercent > 80)
        {
            isThrottled = true;
            throttleReasons.Add("High GPU Load");
        }
        if (_settings.ReduceFpsUnfocused && !string.IsNullOrEmpty(foreground) && !IsWhitelisted(foreground))
        {
            isThrottled = true;
            throttleReasons.Add("Wallpaper Unfocused");
        }
        if (_settings.DynamicFpsEnabled && Win32.GetIdleTime().TotalSeconds > 10)
        {
            isThrottled = true;
            throttleReasons.Add("Dynamic FPS (Inactive)");
        }

        // Apply Actions
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

        // Apply Throttling (Reduce playback rate by half)
        float targetRate = isThrottled ? (float)(_settings.AnimationSpeed * 0.3) : (float)_settings.AnimationSpeed;
        _wallpaperService.SetPlaybackRate(targetRate);

        // Audio Management
        bool shouldMute = _settings.MuteWallpaperAudio;
        if (!shouldMute && _settings.MuteWhenFullscreen && IsForegroundFullscreen(out _)) shouldMute = true;
        if (!shouldMute && _settings.MuteWhenUnfocused && !IsWhitelisted(foreground)) shouldMute = true;
        
        _wallpaperService.SetVolume(_settings.MasterVolume, shouldMute);

        StateChanged?.Invoke(this, Current);
    }

    private bool IsStreamingSoftwareRunning()
    {
        string[] streamingApps = { "obs64", "obs32", "discord", "zoom", "teams", "xsplit" };
        foreach (var app in streamingApps)
        {
            if (Process.GetProcessesByName(app).Length > 0) return true;
        }
        return false;
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
        
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
    }
}
