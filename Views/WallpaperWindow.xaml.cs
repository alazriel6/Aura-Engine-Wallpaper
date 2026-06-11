using System.Windows;
using LibVLCSharp.Shared;
using LiveWallpaperApp.Models;

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
    private bool _isStopping;

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

        _isStopping = false;
        _currentPath = videoPath;
        _currentMedia?.Dispose();
        _currentMedia = new Media(_libVlc!, videoPath, FromType.FromPath);

        // Media-level options mirror the process-level LibVLC options so changed media
        // keeps the same no-audio, repeat, and caching behavior without recreating VLC.
        _currentMedia.AddOption(":input-repeat=65535");
        _currentMedia.AddOption(":no-audio");
        _currentMedia.AddOption(":file-caching=1000");
        _currentMedia.AddOption(":network-caching=1000");

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
        _isStopping = true;
        _mediaPlayer?.Stop();
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
        _isStopping = true;

        if (_mediaPlayer is not null)
        {
            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
        }

        _currentMedia?.Dispose();
        if (_ownsLibVlc)
        {
            _libVlc?.Dispose();
        }

        VideoView.MediaPlayer = null;
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
