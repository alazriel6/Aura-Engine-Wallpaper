using LiveWallpaperApp.Models;

namespace LiveWallpaperApp.Services;

public sealed class GPUOptimizationService
{
    /// <summary>
    /// Builds LibVLC arguments from the active profile.
    ///
    /// Wallpaper apps burn resources mainly in four places:
    /// 1. decode work, where compressed frames become raw GPU/CPU surfaces;
    /// 2. texture upload/copy, where decoded frames reach the compositor;
    /// 3. frame pacing, where high-refresh displays can make late frames expensive;
    /// 4. duplication, where every monitor or preview starts a separate decode graph.
    ///
    /// Wallpaper Engine reduces this by choosing hardware decode, throttling background
    /// work, using low-resolution previews, and pausing when another app owns the screen.
    /// LibVLC does not expose every Wallpaper Engine knob, but these options bias VLC
    /// toward GPU decode, bounded caches, fewer late-frame stalls, and no audio graph.
    /// </summary>
    public IReadOnlyList<string> BuildWallpaperVlcArguments(PerformanceSettings settings)
    {
        var options = new List<string>
        {
            settings.LoopWallpaper ? "--input-repeat=65535" : "",
            "--no-audio",
            "--no-video-title-show",
            "--no-mouse-events",
            "--no-keyboard-events",
            "--drop-late-frames",
            "--skip-frames",
            "--avcodec-fast",
            $"--avcodec-threads={settings.DecodeThreadCount}",
            $"--file-caching={GetFileCacheMs(settings)}",
            $"--network-caching={GetNetworkCacheMs(settings)}",
            ToHardwareDecodeOption(settings.HardwareAcceleration)
        };

        if (settings.PowerProfile is PowerProfileMode.BatterySaver or PowerProfileMode.MinimalResource)
        {
            options.Add("--no-spu");
            options.Add("--no-osd");
        }

        return options.Where(static option => !string.IsNullOrWhiteSpace(option)).ToArray();
    }

    public IReadOnlyList<string> BuildThumbnailVlcArguments(PerformanceSettings settings)
    {
        return
        [
            "--input-repeat=65535",
            "--no-audio",
            "--no-video-title-show",
            "--avcodec-hw=d3d11va",
            "--file-caching=300",
            "--network-caching=300",
            "--drop-late-frames",
            "--skip-frames",
            "--no-spu",
            "--no-osd",
            "--avcodec-fast",
            "--avcodec-threads=1"
        ];
    }

    public int GetThumbnailIntervalMs(PerformanceSettings settings)
    {
        return 1000 / Math.Max(1, settings.ThumbnailFps);
    }

    public string DescribeRenderEngine(WallpaperRenderEngine engine)
    {
        return engine switch
        {
            WallpaperRenderEngine.Vlc => "Best compatibility for MP4/WebM today; moderate overhead because every active player owns a decode graph.",
            WallpaperRenderEngine.DirectX => "Lowest latency future path; ideal for shared textures, shaders, HDR, and synchronized multi-monitor rendering.",
            WallpaperRenderEngine.SkiaSharp => "Great for 2D procedural effects and overlays; not ideal as the main 4K video decoder.",
            WallpaperRenderEngine.WebView2 => "Required for HTML/CSS/JS wallpapers; memory-heavy if many previews or pages stay alive.",
            _ => "Unknown renderer."
        };
    }

    private static int GetFileCacheMs(PerformanceSettings settings)
    {
        return settings.PowerProfile switch
        {
            PowerProfileMode.UltraPerformance => 600,
            PowerProfileMode.Balanced => 1000,
            PowerProfileMode.BatterySaver => 1400,
            PowerProfileMode.MinimalResource => 1800,
            _ => 1000
        };
    }

    private static int GetNetworkCacheMs(PerformanceSettings settings)
    {
        return settings.PowerProfile switch
        {
            PowerProfileMode.UltraPerformance => 800,
            PowerProfileMode.Balanced => 1200,
            PowerProfileMode.BatterySaver => 1800,
            PowerProfileMode.MinimalResource => 2200,
            _ => 1200
        };
    }

    private static string ToHardwareDecodeOption(HardwareAccelerationMode mode)
    {
        return mode switch
        {
            HardwareAccelerationMode.Auto => "--avcodec-hw=any",
            HardwareAccelerationMode.D3D11VA => "--avcodec-hw=d3d11va",
            HardwareAccelerationMode.DXVA2 => "--avcodec-hw=dxva2",
            HardwareAccelerationMode.Disabled => "--avcodec-hw=none",
            // VLC on Windows usually reaches vendor blocks through D3D11VA/DXVA2. These
            // names are exposed in the UI because users think in NVDEC/AMF/QuickSync,
            // but the safe LibVLC path is still D3D11VA unless a custom DirectX backend
            // is implemented.
            HardwareAccelerationMode.NVDEC => "--avcodec-hw=d3d11va",
            HardwareAccelerationMode.AmdAmf => "--avcodec-hw=d3d11va",
            HardwareAccelerationMode.IntelQuickSync => "--avcodec-hw=d3d11va",
            _ => "--avcodec-hw=d3d11va"
        };
    }
}
