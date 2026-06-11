using System.Windows;
using LibVLCSharp.Shared;
using LiveWallpaperApp.Models;
using System.Windows.Interop;

namespace LiveWallpaperApp.Views;

public partial class WallpaperWindow : Window, IDisposable
{
    private readonly MonitorInfo _monitor;
    private readonly IReadOnlyList<string> _vlcOptions;
    private LibVLC? _libVlc;
    private readonly LibVLC? _sharedLibVlc;
    private readonly bool _ownsLibVlc = true;
    private MediaPlayer? _mediaPlayer;
    private Media? _currentMedia;
    private string? _currentPath;
    private bool _disposed;

    public MonitorInfo Monitor => _monitor;
    public string? CurrentPath => _currentPath;

    public WallpaperWindow(MonitorInfo monitor, IReadOnlyList<string> vlcOptions)
    {
        _monitor = monitor;
        _vlcOptions = vlcOptions;

        InitializeComponent();

        WindowState = WindowState.Normal;
        Left = monitor.Bounds.Left;
        Top = monitor.Bounds.Top;
        Width = monitor.Bounds.Width;
        Height = monitor.Bounds.Height;
    }

    public WallpaperWindow(MonitorInfo monitor, LibVLC sharedLibVlc)
    {
        _monitor = monitor;
        _vlcOptions = Array.Empty<string>();
        _sharedLibVlc = sharedLibVlc;
        _libVlc = sharedLibVlc;
        _ownsLibVlc = false;

        InitializeComponent();

        WindowState = WindowState.Normal;
        Left = monitor.Bounds.Left;
        Top = monitor.Bounds.Top;
        Width = monitor.Bounds.Width;
        Height = monitor.Bounds.Height;
    }

    public void Play(string videoPath)
    {
        ThrowIfDisposed();

        if (!File.Exists(videoPath))
        {
            throw new FileNotFoundException("Wallpaper video was not found.", videoPath);
        }

        EnsurePlayer();

        _currentPath = videoPath;
        _currentMedia?.Dispose();
        _currentMedia = new Media(_libVlc!, videoPath, FromType.FromPath);
        _currentMedia.AddOption(":input-repeat=65535");
        _currentMedia.AddOption(":no-audio");
        _currentMedia.AddOption(":file-caching=500");
        _currentMedia.AddOption(":network-caching=500");

        _mediaPlayer!.Play(_currentMedia);
    }

    public void Pause()
    {
        _mediaPlayer?.SetPause(true);
    }

    public void Resume()
    {
        _mediaPlayer?.SetPause(false);
    }

    public void Stop()
    {
        // Stop on a thread pool thread to avoid blocking the WPF UI thread.
        // MediaPlayer.Stop() is synchronous in LibVLC and can take 200-500ms.
        var player = _mediaPlayer;
        if (player is not null)
        {
            Task.Run(() =>
            {
                try { player.Stop(); } catch { }
            }).Wait(2000); // cap at 2s to avoid infinite hang
        }
    }

    private void EnsurePlayer()
    {
        if (_mediaPlayer is not null)
        {
            return;
        }

        _libVlc ??= _sharedLibVlc ?? new LibVLC(_vlcOptions.ToArray());
        _mediaPlayer = new MediaPlayer(_libVlc)
        {
            Mute = true,
            Volume = 0,
            Fullscreen = false,
            AspectRatio = $"{_monitor.Bounds.Width}:{_monitor.Bounds.Height}",
            Scale = 0
        };

        VideoView.MediaPlayer = _mediaPlayer;
    }

    protected override void OnClosed(EventArgs e)
    {
        Dispose();
        base.OnClosed(e);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        VideoView.MediaPlayer = null;

        // Stop on background thread to prevent UI deadlock
        var player = _mediaPlayer;
        if (player is not null)
        {
            Task.Run(() =>
            {
                try { player.Stop(); } catch { }
                try { player.Dispose(); } catch { }
            }).Wait(3000);
        }

        _currentMedia?.Dispose();
        if (_ownsLibVlc)
        {
            _libVlc?.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
