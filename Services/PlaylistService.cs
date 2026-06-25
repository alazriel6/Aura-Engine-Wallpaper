using System.Collections.ObjectModel;
using System.Windows.Threading;
using LiveWallpaperApp.Helpers;
using LiveWallpaperApp.Models;

namespace LiveWallpaperApp.Services;

public sealed class PlaylistService : ObservableObject, IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly Random _random = new();
    private readonly WallpaperLibraryService _libraryService;
    private readonly ThumbnailService _thumbnailService;
    private WallpaperPlaylist? _selectedPlaylist;
    private WallpaperPlaylist? _activePlaylist;
    private int _index = -1;
    private List<WallpaperModel> _allWallpapers = new();

    public ObservableCollection<WallpaperPlaylist> AllPlaylists { get; } = new();
    public ObservableCollection<WallpaperPreviewItem> SelectedPlaylistItems { get; } = new();

    public PlaylistService(WallpaperLibraryService libraryService, ThumbnailService thumbnailService)
    {
        _libraryService = libraryService;
        _thumbnailService = thumbnailService;
        _timer = new DispatcherTimer(DispatcherPriority.Background);
        _timer.Tick += (_, _) => RaiseNextWallpaper();
    }

    public event EventHandler<WallpaperModel>? WallpaperDue;

    public WallpaperPlaylist? SelectedPlaylist
    {
        get => _selectedPlaylist;
        set
        {
            if (SetProperty(ref _selectedPlaylist, value))
            {
                _ = RefreshSelectedPlaylistItemsAsync();
            }
        }
    }

    public WallpaperPlaylist? ActivePlaylist
    {
        get => _activePlaylist;
        private set => SetProperty(ref _activePlaylist, value);
    }

    public async Task InitializeAsync()
    {
        var manifest = await _libraryService.LoadAsync().ConfigureAwait(false);
        _allWallpapers = manifest.Wallpapers;

        // ── Migration: convert legacy single Playlist → Playlists list ──
        if (manifest.Playlist != null && manifest.Playlist.Items != null && manifest.Playlist.Items.Count > 0
            && manifest.Playlists.Count == 0)
        {
            var legacy = manifest.Playlist;
            var migrated = new WallpaperPlaylist
            {
                Id = legacy.Id == Guid.Empty ? Guid.NewGuid() : legacy.Id,
                Name = string.IsNullOrWhiteSpace(legacy.Name) ? "Default Playlist" : legacy.Name,
                IsSequential = legacy.IsSequential,
                Interval = legacy.Interval,
                WallpaperIds = legacy.Items.Select(w => w.Id).ToList()
            };
            manifest.Playlists.Add(migrated);
            manifest.ActivePlaylistId = migrated.Id;
            manifest.Playlist = null; // clear legacy
            await _libraryService.SaveAsync(manifest).ConfigureAwait(false);
        }
        else if (manifest.Playlist != null)
        {
            manifest.Playlist = null; // clear empty legacy
            await _libraryService.SaveAsync(manifest).ConfigureAwait(false);
        }

        // ── Populate observable collection ──
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            AllPlaylists.Clear();
            foreach (var pl in manifest.Playlists)
            {
                // Clear legacy Items to avoid confusion
                pl.Items = null;
                AllPlaylists.Add(pl);
            }

            // Set active playlist
            ActivePlaylist = manifest.ActivePlaylistId.HasValue
                ? AllPlaylists.FirstOrDefault(p => p.Id == manifest.ActivePlaylistId.Value)
                : null;

            if (ActivePlaylist != null)
            {
                _index = manifest.ActivePlaylistIndex;
            }

            // Auto-select first playlist
            SelectedPlaylist = AllPlaylists.FirstOrDefault();
        });

        ApplyTimerSettings();
    }

    // ══════════════════════════════════════════
    // CRUD Operations
    // ══════════════════════════════════════════

    public async Task<WallpaperPlaylist> CreatePlaylistAsync(string name)
    {
        var playlist = new WallpaperPlaylist
        {
            Name = string.IsNullOrWhiteSpace(name) ? "New Playlist" : name
        };

        System.Windows.Application.Current.Dispatcher.Invoke(() => AllPlaylists.Add(playlist));
        await SavePlaylistsAsync().ConfigureAwait(false);

        // Auto-select the newly created playlist
        SelectedPlaylist = playlist;
        return playlist;
    }

    public async Task DeletePlaylistAsync(WallpaperPlaylist playlist)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() => AllPlaylists.Remove(playlist));

        if (ActivePlaylist?.Id == playlist.Id)
        {
            _timer.Stop();
            ActivePlaylist = AllPlaylists.FirstOrDefault();
            ApplyTimerSettings();
        }

        if (SelectedPlaylist?.Id == playlist.Id)
        {
            SelectedPlaylist = AllPlaylists.FirstOrDefault();
        }

        await SavePlaylistsAsync().ConfigureAwait(false);
    }

    public async Task RenamePlaylistAsync(WallpaperPlaylist playlist, string newName)
    {
        playlist.Name = newName;
        await SavePlaylistsAsync().ConfigureAwait(false);
        // Force UI refresh
        OnPropertyChanged(nameof(SelectedPlaylist));
    }

    public async Task SetActivePlaylistAsync(WallpaperPlaylist? playlist)
    {
        ActivePlaylist = playlist;
        _index = -1;
        ApplyTimerSettings();
        await SavePlaylistsAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════
    // Wallpaper Management within Playlist
    // ══════════════════════════════════════════

    public async Task AddWallpaperToPlaylistAsync(Guid playlistId, WallpaperModel wallpaper)
    {
        var playlist = AllPlaylists.FirstOrDefault(p => p.Id == playlistId);
        if (playlist == null) return;
        if (playlist.WallpaperIds.Contains(wallpaper.Id)) return;

        playlist.WallpaperIds.Add(wallpaper.Id);
        playlist.NotifyCountChanged();
        await SavePlaylistsAsync().ConfigureAwait(false);

        // If this is the currently selected playlist, refresh the item view
        if (SelectedPlaylist?.Id == playlistId)
        {
            await RefreshSelectedPlaylistItemsAsync();
        }
    }

    public async Task RemoveWallpaperFromPlaylistAsync(Guid playlistId, Guid wallpaperId)
    {
        var playlist = AllPlaylists.FirstOrDefault(p => p.Id == playlistId);
        if (playlist == null) return;

        playlist.WallpaperIds.Remove(wallpaperId);
        playlist.NotifyCountChanged();
        await SavePlaylistsAsync().ConfigureAwait(false);

        if (SelectedPlaylist?.Id == playlistId)
        {
            await RefreshSelectedPlaylistItemsAsync();
        }
    }

    public async Task MoveItemAsync(int oldIndex, int newIndex)
    {
        if (SelectedPlaylist == null) return;
        var ids = SelectedPlaylist.WallpaperIds;
        if (oldIndex < 0 || oldIndex >= ids.Count || newIndex < 0 || newIndex >= ids.Count) return;

        var id = ids[oldIndex];
        ids.RemoveAt(oldIndex);
        ids.Insert(newIndex, id);

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var item = SelectedPlaylistItems[oldIndex];
            SelectedPlaylistItems.RemoveAt(oldIndex);
            SelectedPlaylistItems.Insert(newIndex, item);
        });

        await SavePlaylistsAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════
    // Settings per-playlist
    // ══════════════════════════════════════════

    public async Task SetPlaylistModeAsync(WallpaperPlaylist playlist, bool isSequential)
    {
        playlist.IsSequential = isSequential;
        if (ActivePlaylist?.Id == playlist.Id)
        {
            _index = -1;
        }
        await SavePlaylistsAsync().ConfigureAwait(false);
    }

    public async Task SetPlaylistIntervalAsync(WallpaperPlaylist playlist, TimeSpan interval)
    {
        playlist.Interval = interval;
        if (ActivePlaylist?.Id == playlist.Id)
        {
            ApplyTimerSettings();
        }
        await SavePlaylistsAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════
    // Resolve Preview Items (fix NO_SIGNAL)
    // ══════════════════════════════════════════

    public async Task RefreshSelectedPlaylistItemsAsync()
    {
        if (SelectedPlaylist == null)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => SelectedPlaylistItems.Clear());
            return;
        }

        // Reload wallpapers from manifest to stay in sync
        var manifest = await _libraryService.LoadAsync().ConfigureAwait(false);
        _allWallpapers = manifest.Wallpapers;

        var items = new List<WallpaperPreviewItem>();
        foreach (var wallpaperId in SelectedPlaylist.WallpaperIds)
        {
            var wallpaper = _allWallpapers.FirstOrDefault(w => w.Id == wallpaperId);
            if (wallpaper == null) continue;

            var previewItem = new WallpaperPreviewItem
            {
                Wallpaper = wallpaper,
                IsFavorite = wallpaper.IsFavorite
            };

            // Resolve preview path from ThumbnailService cache
            var previewPath = GetCachedPreviewPath(wallpaper.FilePath);
            if (!string.IsNullOrEmpty(previewPath) && System.IO.File.Exists(previewPath))
            {
                previewItem.PreviewPath = previewPath;
                previewItem.IsPreviewReady = true;
            }
            else if (System.IO.File.Exists(wallpaper.FilePath))
            {
                // Fallback: use source file as preview
                previewItem.PreviewPath = wallpaper.FilePath;
                previewItem.UsesSourceAsPreviewFallback = true;
                previewItem.IsPreviewReady = true;
            }

            items.Add(previewItem);
        }

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            SelectedPlaylistItems.Clear();
            foreach (var item in items)
            {
                SelectedPlaylistItems.Add(item);
            }
        });
    }

    private string? GetCachedPreviewPath(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !System.IO.File.Exists(sourcePath))
            return null;

        try
        {
            var identity = $"{sourcePath}|{System.IO.File.GetLastWriteTimeUtc(sourcePath).Ticks}";
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(identity)));
            var path = System.IO.Path.Combine(_thumbnailService.CacheRoot, $"{hash[..20]}.preview.jpg");
            return System.IO.File.Exists(path) ? path : null;
        }
        catch
        {
            return null;
        }
    }

    // ══════════════════════════════════════════
    // Timer / Playback
    // ══════════════════════════════════════════

    private void ApplyTimerSettings()
    {
        _timer.Stop();
        if (ActivePlaylist == null || ActivePlaylist.WallpaperIds.Count == 0) return;

        if (ActivePlaylist.Interval == TimeSpan.Zero)
        {
            RaiseNextWallpaper();
        }
        else
        {
            _timer.Interval = ActivePlaylist.Interval;
            _timer.Start();
            RaiseNextWallpaper();
        }
    }

    public void RaiseNextWallpaper()
    {
        if (ActivePlaylist == null || ActivePlaylist.WallpaperIds.Count == 0) return;

        var ids = ActivePlaylist.WallpaperIds;
        Guid nextId;

        if (!ActivePlaylist.IsSequential)
        {
            nextId = ids[_random.Next(ids.Count)];
        }
        else
        {
            _index = (_index + 1) % ids.Count;
            // Prevent index out of bounds if items were removed
            if (_index >= ids.Count) _index = 0; 
            nextId = ids[_index];
        }

        // Fire and forget save so we don't delay playback
        _ = SavePlaylistsAsync();

        var wallpaper = _allWallpapers.FirstOrDefault(w => w.Id == nextId);
        if (wallpaper != null)
        {
            WallpaperDue?.Invoke(this, wallpaper);
        }
    }

    // ══════════════════════════════════════════
    // Persistence
    // ══════════════════════════════════════════

    private async Task SavePlaylistsAsync()
    {
        var manifest = await _libraryService.LoadAsync().ConfigureAwait(false);
        manifest.Playlists = AllPlaylists.ToList();
        manifest.ActivePlaylistId = ActivePlaylist?.Id;
        manifest.ActivePlaylistIndex = _index;
        manifest.Playlist = null; // always clear legacy
        await _libraryService.SaveAsync(manifest).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _timer.Stop();
    }
}
