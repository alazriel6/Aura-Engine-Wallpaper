namespace LiveWallpaperApp.Models;

public sealed class WallpaperPackManifest
{
    public string SchemaVersion { get; set; } = "1.0";
    public string PackId { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Cyber Pack";
    public string Author { get; set; } = "Unknown";
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public List<WallpaperModel> Wallpapers { get; set; } = new();
    public List<string> WatchedFolders { get; set; } = new();
}
