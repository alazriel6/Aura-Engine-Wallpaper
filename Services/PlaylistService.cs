using System.Windows.Threading;
using LiveWallpaperApp.Models;

namespace LiveWallpaperApp.Services;

public sealed class PlaylistService : IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly Random _random = new();
    private WallpaperPlaylist? _playlist;
    private int _index = -1;

    public PlaylistService()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMinutes(15)
        };

        _timer.Tick += (_, _) => RaiseNextWallpaper();
    }

    public event EventHandler<WallpaperModel>? WallpaperDue;

    public void Start(WallpaperPlaylist playlist)
    {
        _playlist = playlist;
        _timer.Interval = playlist.Interval;
        _timer.Start();
        RaiseNextWallpaper();
    }

    public void Stop()
    {
        _timer.Stop();
        _playlist = null;
        _index = -1;
    }

    private void RaiseNextWallpaper()
    {
        if (_playlist is null || _playlist.Items.Count == 0)
        {
            return;
        }

        WallpaperModel next;
        if (_playlist.Shuffle)
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
        Stop();
    }
}
