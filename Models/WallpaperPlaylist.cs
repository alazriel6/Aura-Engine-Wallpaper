using LiveWallpaperApp.Helpers;

namespace LiveWallpaperApp.Models;

public sealed class WallpaperPlaylist : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    private string _name = "New Playlist";
    public string Name 
    { 
        get => _name; 
        set => SetProperty(ref _name, value); 
    }

    public string Description { get; set; } = "";
    public string IconGlyph { get; set; } = "\uE142";
    public bool IsSequential { get; set; } = true;
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(30);
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public List<Guid> WallpaperIds { get; set; } = new();

    public int WallpaperCount => WallpaperIds.Count;

    public void NotifyCountChanged()
    {
        OnPropertyChanged(nameof(WallpaperCount));
    }

    /// <summary>
    /// Legacy property kept for backward-compatible deserialization only.
    /// Will be migrated to WallpaperIds on first load.
    /// </summary>
    public List<WallpaperModel>? Items { get; set; }
}
