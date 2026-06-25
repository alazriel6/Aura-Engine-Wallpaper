using System.Text.Json.Serialization;

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

    /// <summary>New multi-playlist list.</summary>
    public List<WallpaperPlaylist> Playlists { get; set; } = new();

    /// <summary>The currently active playlist Id.</summary>
    public Guid? ActivePlaylistId { get; set; }

    /// <summary>The last played index in the active playlist to resume from.</summary>
    public int ActivePlaylistIndex { get; set; } = -1;

    /// <summary>
    /// Legacy single playlist property. Kept for backward-compatible deserialization.
    /// The PlaylistService will migrate this into <see cref="Playlists"/> on first load.
    /// </summary>
    public WallpaperPlaylist? Playlist { get; set; }
}
