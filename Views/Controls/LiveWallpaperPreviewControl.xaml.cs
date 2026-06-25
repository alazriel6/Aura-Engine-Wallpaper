using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using LiveWallpaperApp.Services;

namespace LiveWallpaperApp.Views.Controls;

public partial class LiveWallpaperPreviewControl : UserControl, IDisposable
{
    public static readonly DependencyProperty VideoPathProperty = DependencyProperty.Register(
        nameof(VideoPath),
        typeof(string),
        typeof(LiveWallpaperPreviewControl),
        new PropertyMetadata(string.Empty, OnVideoPathChanged));

    public static readonly DependencyProperty PlayerOptionsProperty = DependencyProperty.Register(
        nameof(PlayerOptions),
        typeof(IEnumerable<string>),
        typeof(LiveWallpaperPreviewControl),
        new PropertyMetadata(Array.Empty<string>()));

    public static readonly DependencyProperty IsPreviewActiveProperty = DependencyProperty.Register(
        nameof(IsPreviewActive),
        typeof(bool),
        typeof(LiveWallpaperPreviewControl),
        new PropertyMetadata(true, OnPreviewActivityChanged));

    private readonly DispatcherTimer _activityTimer;
    private MediaPlayer? _mediaPlayer;
    private Media? _media;
    private bool _disposed;

    public LiveWallpaperPreviewControl()
    {
        InitializeComponent();

        _activityTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _activityTimer.Tick += (_, _) => SyncPreviewState();
    }

    public string VideoPath
    {
        get => (string)GetValue(VideoPathProperty);
        set => SetValue(VideoPathProperty, value);
    }

    public IEnumerable<string> PlayerOptions
    {
        get => (IEnumerable<string>)GetValue(PlayerOptionsProperty);
        set => SetValue(PlayerOptionsProperty, value);
    }

    public bool IsPreviewActive
    {
        get => (bool)GetValue(IsPreviewActiveProperty);
        set => SetValue(IsPreviewActiveProperty, value);
    }

    public string StateText { get; private set; } = "Select a wallpaper to preview";

    private static void OnVideoPathChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is LiveWallpaperPreviewControl control && control.IsLoaded)
        {
            control.RestartPreview();
        }
    }

    private static void OnPreviewActivityChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is LiveWallpaperPreviewControl control)
        {
            control.SyncPreviewState();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _activityTimer.Start();
        SyncPreviewState();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _activityTimer.Stop();
        ReleasePlayer();
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        SyncPreviewState();
    }

    private void SyncPreviewState()
    {
        if (_disposed)
        {
            return;
        }

        var shouldRun = IsLoaded
            && IsVisible
            && IsPreviewActive
            && !string.IsNullOrWhiteSpace(VideoPath)
            && File.Exists(VideoPath);

        if (shouldRun)
        {
            EnsurePlayer();
            _mediaPlayer?.SetPause(false);
            StateText = "Live preview running";
            return;
        }

        if (!IsVisible || !IsLoaded || !IsPreviewActive)
        {
            ReleasePlayer();
            StateText = IsPreviewActive ? "Preview paused" : "Preview reduced while inactive";
            return;
        }

        if (_mediaPlayer is not null)
        {
            _mediaPlayer.SetPause(true);
            StateText = IsPreviewActive ? "Preview paused" : "Preview reduced while inactive";
        }
    }

    private void EnsurePlayer()
    {
        var previewVlc = PreviewVlcHost.GetSharedPreviewVlc(PlayerOptions);

        if (_mediaPlayer is null)
        {
            _mediaPlayer = new MediaPlayer(previewVlc)
            {
                Mute = true,
                Volume = 0
            };
            PreviewVideo.MediaPlayer = _mediaPlayer;
        }

        if (_media != null && _media.Mrl == new Uri(VideoPath).AbsoluteUri)
        {
            return;
        }

        var newMedia = new Media(previewVlc, VideoPath, FromType.FromPath);
        newMedia.AddOption(":input-repeat=65535");
        newMedia.AddOption(":no-audio");
        newMedia.AddOption(":file-caching=250");
        
        _media = newMedia;
        _mediaPlayer.Play(newMedia);
    }

    private void RestartPreview()
    {
        SyncPreviewState();
    }

    private void ReleasePlayer()
    {
        if (PreviewVideo.MediaPlayer != null)
        {
            PreviewVideo.MediaPlayer = null;
        }
        _mediaPlayer?.Stop();
        _mediaPlayer?.Dispose();
        _media?.Dispose();
        _mediaPlayer = null;
        _media = null;
        StateText = "Select a wallpaper to preview";
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _activityTimer.Stop();
        ReleasePlayer();
        GC.SuppressFinalize(this);
    }
}
