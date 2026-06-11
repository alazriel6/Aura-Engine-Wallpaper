namespace LiveWallpaperApp.Models;

public sealed class WallpaperModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = "Untitled Wallpaper";
    public string Author { get; set; } = "Local";
    public string FilePath { get; set; } = string.Empty;
    public string PreviewPath { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public string MonitorDeviceName { get; set; } = "*";
    public string ThemeName { get; set; } = "Dark";
    public string AccentColorHex { get; set; } = "#33F5FF";
    public WallpaperType Type { get; set; } = WallpaperType.Mp4;
    public bool IsMuted { get; set; } = true;
    public bool Loop { get; set; } = true;
    public bool IsFavorite { get; set; }
    public string Category { get; set; } = "Imported";
    public string Resolution { get; set; } = "Unknown";
    public double Fps { get; set; }
    public string Duration { get; set; } = "Unknown";
    public DateTimeOffset LastUsedAt { get; set; }
    public DateTimeOffset ImportedAt { get; set; } = DateTimeOffset.Now;
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}
