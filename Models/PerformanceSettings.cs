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
    private bool _autoShuffle;
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
        set => SetProperty(ref _powerProfile, value);
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

    public bool AutoShuffle
    {
        get => _autoShuffle;
        set => SetProperty(ref _autoShuffle, value);
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

    public int EffectiveFps => FpsLimit switch
    {
        FpsLimitMode.Fps5 => 5,
        FpsLimitMode.Fps15 => 15,
        FpsLimitMode.Fps30 => 30,
        FpsLimitMode.Fps60 => 60,
        FpsLimitMode.Fps120 => 120,
        _ => 0
    };

    public void ApplyPowerProfile(PowerProfileMode profile)
    {
        PowerProfile = profile;

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
