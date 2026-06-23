namespace LiveWallpaperApp.Models;

public sealed class WallpaperPlaylist
{
    public string Name { get; set; } = "Default Playlist";
    public bool IsSequential { get; set; } = true;
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(30);
    public List<WallpaperModel> Items { get; set; } = new();
}
