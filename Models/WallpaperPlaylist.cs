namespace LiveWallpaperApp.Models;

public sealed class WallpaperPlaylist
{
    public string Name { get; set; } = "Default Playlist";
    public bool Shuffle { get; set; }
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(15);
    public List<WallpaperModel> Items { get; set; } = new();
}
