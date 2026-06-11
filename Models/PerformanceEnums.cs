namespace LiveWallpaperApp.Models;

public enum WallpaperRenderEngine
{
    Vlc,
    DirectX,
    SkiaSharp,
    WebView2
}

public enum HardwareAccelerationMode
{
    Auto,
    D3D11VA,
    DXVA2,
    NVDEC,
    AmdAmf,
    IntelQuickSync,
    Disabled
}

public enum FpsLimitMode
{
    Fps5,
    Fps15,
    Fps30,
    Fps60,
    Fps120,
    Unlimited
}

public enum PowerProfileMode
{
    UltraPerformance,
    Balanced,
    BatterySaver,
    MinimalResource
}

public enum UserPerformanceMode
{
    UltraSmooth,
    Balanced,
    PowerSaver,
    GamingMode
}

public enum TextureFilteringMode
{
    Nearest,
    Bilinear,
    Bicubic,
    Anisotropic
}

public enum WallpaperType
{
    Mp4,
    WebM,
    Gif,
    Web,
    Unity,
    Shader,
    ImageSlideshow,
    AudioReactive
}

public enum WallpaperSortMode
{
    RecentlyUsed,
    Title,
    Author,
    Resolution,
    Duration,
    FavoriteFirst
}
