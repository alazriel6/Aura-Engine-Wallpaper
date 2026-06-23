using System.Collections.ObjectModel;
using System.Windows.Threading;
using LiveWallpaperApp.Models;

namespace LiveWallpaperApp.Services;

public sealed class PlaylistService : IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly Random _random = new();
    private readonly WallpaperLibraryService _libraryService;
    private WallpaperPlaylist _playlist = new();
    private int _index = -1;

    public ObservableCollection<WallpaperModel> Items { get; } = new();

    public PlaylistService(WallpaperLibraryService libraryService)
    {
        _libraryService = libraryService;
        _timer = new DispatcherTimer(DispatcherPriority.Background);
        _timer.Tick += (_, _) => RaiseNextWallpaper();
    }

    public event EventHandler<WallpaperModel>? WallpaperDue;

    public async Task InitializeAsync()
    {
        var manifest = await _libraryService.LoadAsync().ConfigureAwait(false);
        _playlist = manifest.Playlist;

        // Populate observable collection
        foreach (var item in _playlist.Items)
        {
            Items.Add(item);
        }

        ApplySettings();
    }

    public void ApplySettings()
    {
        _timer.Stop();

        // 0 means "On Startup", we don't start the timer, just run once.
        if (_playlist.Interval == TimeSpan.Zero)
        {
            RaiseNextWallpaper();
        }
        else
        {
            _timer.Interval = _playlist.Interval;
            _timer.Start();
            RaiseNextWallpaper(); // Trigger immediately when applying new settings
        }
    }

    public bool IsSequential
    {
        get => _playlist.IsSequential;
        set
        {
            _playlist.IsSequential = value;
            _ = SaveAsync();
        }
    }

    public TimeSpan Interval
    {
        get => _playlist.Interval;
        set
        {
            _playlist.Interval = value;
            ApplySettings();
            _ = SaveAsync();
        }
    }

    public async Task AddToPlaylistAsync(WallpaperModel wallpaper)
    {
        if (_playlist.Items.Any(w => w.FilePath == wallpaper.FilePath)) return;

        _playlist.Items.Add(wallpaper);
        System.Windows.Application.Current.Dispatcher.Invoke(() => Items.Add(wallpaper));
        await SaveAsync();
    }

    public async Task RemoveFromPlaylistAsync(WallpaperModel wallpaper)
    {
        var item = _playlist.Items.FirstOrDefault(w => w.FilePath == wallpaper.FilePath);
        if (item != null)
        {
            _playlist.Items.Remove(item);
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var obsItem = Items.FirstOrDefault(w => w.FilePath == wallpaper.FilePath);
                if (obsItem != null) Items.Remove(obsItem);
            });
            await SaveAsync();
        }
    }

    public async Task MoveItemAsync(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= _playlist.Items.Count || newIndex < 0 || newIndex >= _playlist.Items.Count)
            return;

        var item = _playlist.Items[oldIndex];
        _playlist.Items.RemoveAt(oldIndex);
        _playlist.Items.Insert(newIndex, item);

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var obsItem = Items[oldIndex];
            Items.RemoveAt(oldIndex);
            Items.Insert(newIndex, obsItem);
        });

        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        var manifest = await _libraryService.LoadAsync().ConfigureAwait(false);
        manifest.Playlist = _playlist;
        await _libraryService.SaveAsync(manifest).ConfigureAwait(false);
    }

    public void RaiseNextWallpaper()
    {
        if (_playlist is null || _playlist.Items.Count == 0)
        {
            return;
        }

        WallpaperModel next;
        if (!_playlist.IsSequential)
        {
            next = _playlist.Items[_random.Next(_playlist.Items.Count)];
        }
        else
        {
            _index = (_index + 1) % _playlist.Items.Count;
            next = _playlist.Items[_index];
        }

        WallpaperDue?.Invoke(this, next);
    }

    public void Dispose()
    {
        _timer.Stop();
    }
}
