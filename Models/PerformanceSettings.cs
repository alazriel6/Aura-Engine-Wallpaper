using LiveWallpaperApp.Helpers;

namespace LiveWallpaperApp.Models;

public sealed class PerformanceSettings : ObservableObject
{
    private WallpaperRenderEngine _renderEngine = WallpaperRenderEngine.DirectX;
    private HardwareAccelerationMode _hardwareAcceleration = HardwareAccelerationMode.D3D11VA;
    private FpsLimitMode _fpsLimit = FpsLimitMode.Fps30;
    private PowerProfileMode _powerProfile = PowerProfileMode.Balanced;
    private UserPerformanceMode _userPerformanceMode = UserPerformanceMode.Balanced;
    private TextureFilteringMode _textureFiltering = TextureFilteringMode.Bilinear;
    private bool _adaptiveFpsEnabled = true;
    private bool _batterySaverEnabled = true;
    private bool _dynamicFrameThrottlingEnabled = true;
    private bool _memorySaverEnabled = true;
    private bool _autoPerformanceModeEnabled = true;
    private bool _reduceBackgroundUsageEnabled = true;
    private bool _startMinimized;
    private bool _showTrayIcon = true;
    private bool _minimizeToTray = true;
    private bool _autoRestoreWallpaper = true;
    private bool _loopWallpaper = true;
    private bool _multiMonitorSync = true;
    private bool _pauseFullscreenGame = true;
    private bool _pauseMaximizedApplication = false;
    private bool _pauseOnBattery = true;
    private bool _pauseBatterySaver = true;
    private bool _pauseUserInactive = true;
    private bool _pauseHighGpuUsage = true;
    private bool _pauseHighCpuTemperature = true;
    private bool _vsyncEnabled = true;
    private bool _motionBlurEnabled;
    private bool _bloomEnabled = true;
    private bool _hdrPipelineEnabled;
    private int _decodeThreadCount = 2;
    private double _renderScale = 1.0;
    private double _panelOpacity = 0.72;
    private double _blurStrength = 10;
    private double _glowIntensity = 0.26;
    private double _shadowIntensity = 0.38;
    private double _borderRadius = 18;
    private double _animationSpeed = 1.0;
    private int _thumbnailFps = 8;
    private int _thumbnailMaxConcurrentPlayers = 1;
    private int _thumbnailCacheLimitMb = 256;
    private int _wallpaperVolume;
    private int _previewQuality = 50;
    private int _liveThumbnailQuality = 35;
    private int _autoPauseGpuThreshold = 90;
    private int _autoPauseCpuThreshold = 92;
    private int _autoPauseCpuTemperatureThreshold = 86;
    private int _idlePauseMinutes = 5;
    private string? _lastWallpaperPath;
    private string _selectedTheme = "Minimal Dark";
    private string _accentColorHex = "#33F5FF";

    // --- NEW SETTINGS ---
    private bool _closeToTray = false;
    private bool _checkForUpdatesAutomatically = true;
    private bool _runAsAdministrator = false;
    private double _uiScale = 1.0;
    private int _wallpaperChangeIntervalMinutes = 15;
    private bool _muteWallpaperAudio = false;
    private int _masterVolume = 100;
    private bool _muteWhenUnfocused = false;
    private bool _muteWhenFullscreen = true;
    private bool _audioFadeTransitions = true;
    private bool _pauseRemoteDesktop = true;
    private bool _pauseLaptopUnplugged = true;
    private bool _autoShuffle = true;
    private bool _autoApplyLastWallpaper = true;
    private bool _sendAnonymousDiagnostics = false;

    // --- NEW PERFORMANCE CONTROL CENTER SETTINGS ---
    private bool _frameLimiterEnabled = true;
    private bool _dynamicFpsEnabled = true;
    private bool _pauseMonitorSleeping = true;
    private bool _pauseScreenLocked = true;
    private bool _pauseScreenSharing = false;
    private bool _pauseStreamingSoftware = false;
    private bool _reduceFpsHighCpu = true;
    private bool _reduceFpsHighGpu = true;
    private bool _reduceQualityHighRam = true;
    private bool _disableEffectsOnBattery = true;
    private bool _autoSwitchPerformanceProfile = true;
    private bool _hardwareVideoDecoding = true;
    private bool _multiThreadRendering = true;
    private bool _hardwareScaling = true;
    private bool _gpuScheduling = true;
    private bool _reduceFpsUnfocused = true;
    private bool _pauseMinimized = true;
    private bool _pauseMonitorOff = true;
    private bool _reduceUpdateFrequency = true;
    private bool _suspendBackgroundRendering = true;
    private int _maxCpuUsageLimit = 80;
    private int _maxGpuUsageLimit = 85;
    private int _maxRamUsageLimitMb = 4096;
    private int _maxVramUsageLimitMb = 2048;
    private ResourceExceedAction _resourceLimitExceededAction = ResourceExceedAction.WarnUser;

    public WallpaperRenderEngine RenderEngine
    {
        get => _renderEngine;
        set => SetProperty(ref _renderEngine, value);
    }

    public HardwareAccelerationMode HardwareAcceleration
    {
        get => _hardwareAcceleration;
        set => SetProperty(ref _hardwareAcceleration, value);
    }

    public FpsLimitMode FpsLimit
    {
        get => _fpsLimit;
        set => SetProperty(ref _fpsLimit, value);
    }

    public PowerProfileMode PowerProfile
    {
        get => _powerProfile;
        set
        {
            if (SetProperty(ref _powerProfile, value))
            {
                ApplyPowerProfile(value);
            }
        }
    }

    public UserPerformanceMode UserPerformanceMode
    {
        get => _userPerformanceMode;
        set => SetProperty(ref _userPerformanceMode, value);
    }

    public TextureFilteringMode TextureFiltering
    {
        get => _textureFiltering;
        set => SetProperty(ref _textureFiltering, value);
    }

    public bool AdaptiveFpsEnabled
    {
        get => _adaptiveFpsEnabled;
        set => SetProperty(ref _adaptiveFpsEnabled, value);
    }

    public bool BatterySaverEnabled
    {
        get => _batterySaverEnabled;
        set => SetProperty(ref _batterySaverEnabled, value);
    }

    public bool DynamicFrameThrottlingEnabled
    {
        get => _dynamicFrameThrottlingEnabled;
        set => SetProperty(ref _dynamicFrameThrottlingEnabled, value);
    }

    public bool MemorySaverEnabled
    {
        get => _memorySaverEnabled;
        set => SetProperty(ref _memorySaverEnabled, value);
    }

    public bool AutoPerformanceModeEnabled
    {
        get => _autoPerformanceModeEnabled;
        set => SetProperty(ref _autoPerformanceModeEnabled, value);
    }

    public bool ReduceBackgroundUsageEnabled
    {
        get => _reduceBackgroundUsageEnabled;
        set => SetProperty(ref _reduceBackgroundUsageEnabled, value);
    }

    public bool StartMinimized
    {
        get => _startMinimized;
        set => SetProperty(ref _startMinimized, value);
    }

    public bool ShowTrayIcon
    {
        get => _showTrayIcon;
        set => SetProperty(ref _showTrayIcon, value);
    }

    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set => SetProperty(ref _minimizeToTray, value);
    }

    public bool AutoRestoreWallpaper
    {
        get => _autoRestoreWallpaper;
        set => SetProperty(ref _autoRestoreWallpaper, value);
    }

    public bool LoopWallpaper
    {
        get => _loopWallpaper;
        set => SetProperty(ref _loopWallpaper, value);
    }

    public bool MultiMonitorSync
    {
        get => _multiMonitorSync;
        set => SetProperty(ref _multiMonitorSync, value);
    }

    public bool PauseFullscreenGame
    {
        get => _pauseFullscreenGame;
        set => SetProperty(ref _pauseFullscreenGame, value);
    }

    public bool PauseMaximizedApplication
    {
        get => _pauseMaximizedApplication;
        set => SetProperty(ref _pauseMaximizedApplication, value);
    }

    public bool PauseOnBattery
    {
        get => _pauseOnBattery;
        set => SetProperty(ref _pauseOnBattery, value);
    }

    public bool PauseBatterySaver
    {
        get => _pauseBatterySaver;
        set => SetProperty(ref _pauseBatterySaver, value);
    }

    public bool PauseUserInactive
    {
        get => _pauseUserInactive;
        set => SetProperty(ref _pauseUserInactive, value);
    }

    public bool PauseHighGpuUsage
    {
        get => _pauseHighGpuUsage;
        set => SetProperty(ref _pauseHighGpuUsage, value);
    }

    public bool PauseHighCpuTemperature
    {
        get => _pauseHighCpuTemperature;
        set => SetProperty(ref _pauseHighCpuTemperature, value);
    }

    public bool VSyncEnabled
    {
        get => _vsyncEnabled;
        set => SetProperty(ref _vsyncEnabled, value);
    }

    public bool MotionBlurEnabled
    {
        get => _motionBlurEnabled;
        set => SetProperty(ref _motionBlurEnabled, value);
    }

    public bool BloomEnabled
    {
        get => _bloomEnabled;
        set => SetProperty(ref _bloomEnabled, value);
    }

    public bool HdrPipelineEnabled
    {
        get => _hdrPipelineEnabled;
        set => SetProperty(ref _hdrPipelineEnabled, value);
    }

    public int DecodeThreadCount
    {
        get => _decodeThreadCount;
        set => SetProperty(ref _decodeThreadCount, Math.Clamp(value, 1, 16));
    }

    public double RenderScale
    {
        get => _renderScale;
        set => SetProperty(ref _renderScale, Math.Clamp(value, 0.25, 1.0));
    }

    public double PanelOpacity
    {
        get => _panelOpacity;
        set => SetProperty(ref _panelOpacity, Math.Clamp(value, 0.25, 1.0));
    }

    public double BlurStrength
    {
        get => _blurStrength;
        set => SetProperty(ref _blurStrength, Math.Clamp(value, 0, 40));
    }

    public double GlowIntensity
    {
        get => _glowIntensity;
        set => SetProperty(ref _glowIntensity, Math.Clamp(value, 0, 1));
    }

    public double ShadowIntensity
    {
        get => _shadowIntensity;
        set => SetProperty(ref _shadowIntensity, Math.Clamp(value, 0, 1));
    }

    public double BorderRadius
    {
        get => _borderRadius;
        set => SetProperty(ref _borderRadius, Math.Clamp(value, 6, 28));
    }

    public double AnimationSpeed
    {
        get => _animationSpeed;
        set => SetProperty(ref _animationSpeed, Math.Clamp(value, 0.25, 2.0));
    }

    public int ThumbnailFps
    {
        get => _thumbnailFps;
        set => SetProperty(ref _thumbnailFps, Math.Clamp(value, 5, 30));
    }

    public int ThumbnailMaxConcurrentPlayers
    {
        get => _thumbnailMaxConcurrentPlayers;
        set => SetProperty(ref _thumbnailMaxConcurrentPlayers, Math.Clamp(value, 1, 12));
    }

    public int ThumbnailCacheLimitMb
    {
        get => _thumbnailCacheLimitMb;
        set => SetProperty(ref _thumbnailCacheLimitMb, Math.Clamp(value, 64, 4096));
    }

    public int WallpaperVolume
    {
        get => _wallpaperVolume;
        set => SetProperty(ref _wallpaperVolume, Math.Clamp(value, 0, 100));
    }

    public int PreviewQuality
    {
        get => _previewQuality;
        set => SetProperty(ref _previewQuality, Math.Clamp(value, 10, 100));
    }

    public int LiveThumbnailQuality
    {
        get => _liveThumbnailQuality;
        set => SetProperty(ref _liveThumbnailQuality, Math.Clamp(value, 10, 100));
    }

    public int AutoPauseGpuThreshold
    {
        get => _autoPauseGpuThreshold;
        set => SetProperty(ref _autoPauseGpuThreshold, Math.Clamp(value, 40, 100));
    }

    public int AutoPauseCpuThreshold
    {
        get => _autoPauseCpuThreshold;
        set => SetProperty(ref _autoPauseCpuThreshold, Math.Clamp(value, 40, 100));
    }

    public int AutoPauseCpuTemperatureThreshold
    {
        get => _autoPauseCpuTemperatureThreshold;
        set => SetProperty(ref _autoPauseCpuTemperatureThreshold, Math.Clamp(value, 50, 105));
    }

    public int IdlePauseMinutes
    {
        get => _idlePauseMinutes;
        set => SetProperty(ref _idlePauseMinutes, Math.Clamp(value, 1, 120));
    }

    public string? LastWallpaperPath
    {
        get => _lastWallpaperPath;
        set => SetProperty(ref _lastWallpaperPath, value);
    }

    public string SelectedTheme
    {
        get => _selectedTheme;
        set => SetProperty(ref _selectedTheme, value);
    }

    public string AccentColorHex
    {
        get => _accentColorHex;
        set => SetProperty(ref _accentColorHex, value);
    }

    // --- NEW SETTINGS PROPERTIES ---

    public bool CloseToTray
    {
        get => _closeToTray;
        set => SetProperty(ref _closeToTray, value);
    }

    public bool CheckForUpdatesAutomatically
    {
        get => _checkForUpdatesAutomatically;
        set => SetProperty(ref _checkForUpdatesAutomatically, value);
    }

    public bool RunAsAdministrator
    {
        get => _runAsAdministrator;
        set => SetProperty(ref _runAsAdministrator, value);
    }

    public double UiScale
    {
        get => _uiScale;
        set => SetProperty(ref _uiScale, value);
    }

    public int WallpaperChangeIntervalMinutes
    {
        get => _wallpaperChangeIntervalMinutes;
        set => SetProperty(ref _wallpaperChangeIntervalMinutes, value);
    }

    public bool MuteWallpaperAudio
    {
        get => _muteWallpaperAudio;
        set => SetProperty(ref _muteWallpaperAudio, value);
    }

    public int MasterVolume
    {
        get => _masterVolume;
        set => SetProperty(ref _masterVolume, Math.Clamp(value, 0, 100));
    }

    public bool MuteWhenUnfocused
    {
        get => _muteWhenUnfocused;
        set => SetProperty(ref _muteWhenUnfocused, value);
    }

    public bool MuteWhenFullscreen
    {
        get => _muteWhenFullscreen;
        set => SetProperty(ref _muteWhenFullscreen, value);
    }

    public bool AudioFadeTransitions
    {
        get => _audioFadeTransitions;
        set => SetProperty(ref _audioFadeTransitions, value);
    }

    public bool PauseRemoteDesktop
    {
        get => _pauseRemoteDesktop;
        set => SetProperty(ref _pauseRemoteDesktop, value);
    }

    public bool PauseLaptopUnplugged
    {
        get => _pauseLaptopUnplugged;
        set => SetProperty(ref _pauseLaptopUnplugged, value);
    }

    public bool AutoShuffle
    {
        get => _autoShuffle;
        set => SetProperty(ref _autoShuffle, value);
    }

    public bool AutoApplyLastWallpaper
    {
        get => _autoApplyLastWallpaper;
        set => SetProperty(ref _autoApplyLastWallpaper, value);
    }

    public bool SendAnonymousDiagnostics
    {
        get => _sendAnonymousDiagnostics;
        set => SetProperty(ref _sendAnonymousDiagnostics, value);
    }

    // --- NEW PERFORMANCE CONTROL CENTER SETTINGS ---
    public bool FrameLimiterEnabled { get => _frameLimiterEnabled; set => SetProperty(ref _frameLimiterEnabled, value); }
    public bool DynamicFpsEnabled { get => _dynamicFpsEnabled; set => SetProperty(ref _dynamicFpsEnabled, value); }
    public bool PauseMonitorSleeping { get => _pauseMonitorSleeping; set => SetProperty(ref _pauseMonitorSleeping, value); }
    public bool PauseScreenLocked { get => _pauseScreenLocked; set => SetProperty(ref _pauseScreenLocked, value); }
    public bool PauseScreenSharing { get => _pauseScreenSharing; set => SetProperty(ref _pauseScreenSharing, value); }
    public bool PauseStreamingSoftware { get => _pauseStreamingSoftware; set => SetProperty(ref _pauseStreamingSoftware, value); }
    public bool ReduceFpsHighCpu { get => _reduceFpsHighCpu; set => SetProperty(ref _reduceFpsHighCpu, value); }
    public bool ReduceFpsHighGpu { get => _reduceFpsHighGpu; set => SetProperty(ref _reduceFpsHighGpu, value); }
    public bool ReduceQualityHighRam { get => _reduceQualityHighRam; set => SetProperty(ref _reduceQualityHighRam, value); }
    public bool DisableEffectsOnBattery { get => _disableEffectsOnBattery; set => SetProperty(ref _disableEffectsOnBattery, value); }
    public bool AutoSwitchPerformanceProfile { get => _autoSwitchPerformanceProfile; set => SetProperty(ref _autoSwitchPerformanceProfile, value); }
    public bool HardwareVideoDecoding { get => _hardwareVideoDecoding; set => SetProperty(ref _hardwareVideoDecoding, value); }
    public bool MultiThreadRendering { get => _multiThreadRendering; set => SetProperty(ref _multiThreadRendering, value); }
    public bool HardwareScaling { get => _hardwareScaling; set => SetProperty(ref _hardwareScaling, value); }
    public bool GpuScheduling { get => _gpuScheduling; set => SetProperty(ref _gpuScheduling, value); }
    public bool ReduceFpsUnfocused { get => _reduceFpsUnfocused; set => SetProperty(ref _reduceFpsUnfocused, value); }
    public bool PauseMinimized { get => _pauseMinimized; set => SetProperty(ref _pauseMinimized, value); }
    public bool PauseMonitorOff { get => _pauseMonitorOff; set => SetProperty(ref _pauseMonitorOff, value); }
    public bool ReduceUpdateFrequency { get => _reduceUpdateFrequency; set => SetProperty(ref _reduceUpdateFrequency, value); }
    public bool SuspendBackgroundRendering { get => _suspendBackgroundRendering; set => SetProperty(ref _suspendBackgroundRendering, value); }
    public int MaxCpuUsageLimit { get => _maxCpuUsageLimit; set => SetProperty(ref _maxCpuUsageLimit, Math.Clamp(value, 10, 100)); }
    public int MaxGpuUsageLimit { get => _maxGpuUsageLimit; set => SetProperty(ref _maxGpuUsageLimit, Math.Clamp(value, 10, 100)); }
    public int MaxRamUsageLimitMb { get => _maxRamUsageLimitMb; set => SetProperty(ref _maxRamUsageLimitMb, Math.Clamp(value, 256, 32768)); }
    public int MaxVramUsageLimitMb { get => _maxVramUsageLimitMb; set => SetProperty(ref _maxVramUsageLimitMb, Math.Clamp(value, 128, 24576)); }
    public ResourceExceedAction ResourceLimitExceededAction { get => _resourceLimitExceededAction; set => SetProperty(ref _resourceLimitExceededAction, value); }

    public int EffectiveFps => FpsLimit switch
    {
        FpsLimitMode.Fps5 => 5,
        FpsLimitMode.Fps15 => 15,
        FpsLimitMode.Fps30 => 30,
        FpsLimitMode.Fps45 => 45,
        FpsLimitMode.Fps60 => 60,
        FpsLimitMode.Fps90 => 90,
        FpsLimitMode.Fps120 => 120,
        FpsLimitMode.Fps144 => 144,
        _ => 0
    };

    public void ApplyPowerProfile(PowerProfileMode profile)
    {

        switch (profile)
        {
            case PowerProfileMode.UltraPerformance:
                FpsLimit = FpsLimitMode.Fps120;
                HardwareAcceleration = HardwareAccelerationMode.D3D11VA;
                ThumbnailFps = 12;
                ThumbnailMaxConcurrentPlayers = 2;
                RenderScale = 1.0;
                DecodeThreadCount = Math.Max(4, DecodeThreadCount);
                GlowIntensity = 0.42;
                ShadowIntensity = 0.5;
                break;
            case PowerProfileMode.Balanced:
                FpsLimit = FpsLimitMode.Fps30;
                HardwareAcceleration = HardwareAccelerationMode.D3D11VA;
                ThumbnailFps = 8;
                ThumbnailMaxConcurrentPlayers = 1;
                RenderScale = 1.0;
                DecodeThreadCount = 2;
                GlowIntensity = 0.26;
                ShadowIntensity = 0.38;
                break;
            case PowerProfileMode.BatterySaver:
                FpsLimit = FpsLimitMode.Fps15;
                HardwareAcceleration = HardwareAccelerationMode.Auto;
                ThumbnailFps = 5;
                ThumbnailMaxConcurrentPlayers = 1;
                RenderScale = 0.75;
                DecodeThreadCount = 1;
                GlowIntensity = 0.12;
                ShadowIntensity = 0.2;
                break;
            case PowerProfileMode.MinimalResource:
                FpsLimit = FpsLimitMode.Fps15;
                HardwareAcceleration = HardwareAccelerationMode.Auto;
                ThumbnailFps = 5;
                ThumbnailMaxConcurrentPlayers = 1;
                RenderScale = 0.5;
                DecodeThreadCount = 1;
                DynamicFrameThrottlingEnabled = true;
                MemorySaverEnabled = true;
                ReduceBackgroundUsageEnabled = true;
                GlowIntensity = 0.08;
                ShadowIntensity = 0.14;
                break;
        }
    }

    public void ApplyUserPerformanceMode(UserPerformanceMode mode)
    {
        UserPerformanceMode = mode;

        switch (mode)
        {
            case UserPerformanceMode.UltraSmooth:
                ApplyPowerProfile(PowerProfileMode.UltraPerformance);
                FpsLimit = FpsLimitMode.Fps60;
                MemorySaverEnabled = false;
                ReduceBackgroundUsageEnabled = false;
                break;
            case UserPerformanceMode.Balanced:
                ApplyPowerProfile(PowerProfileMode.Balanced);
                MemorySaverEnabled = true;
                ReduceBackgroundUsageEnabled = true;
                break;
            case UserPerformanceMode.PowerSaver:
                ApplyPowerProfile(PowerProfileMode.BatterySaver);
                BatterySaverEnabled = true;
                MemorySaverEnabled = true;
                ReduceBackgroundUsageEnabled = true;
                break;
            case UserPerformanceMode.GamingMode:
                ApplyPowerProfile(PowerProfileMode.MinimalResource);
                PauseFullscreenGame = true;
                PauseHighGpuUsage = true;
                MemorySaverEnabled = true;
                ReduceBackgroundUsageEnabled = true;
                break;
        }
    }
}
