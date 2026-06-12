using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using LiveWallpaperApp.Services;

namespace LiveWallpaperApp.Views.Controls;

public partial class AnimatedThumbnailControl : UserControl, IDisposable
{
    public static readonly DependencyProperty VideoPathProperty = DependencyProperty.Register(
        nameof(VideoPath),
        typeof(string),
        typeof(AnimatedThumbnailControl),
        new PropertyMetadata(string.Empty, OnVideoPathChanged));

    public static readonly DependencyProperty PlayerOptionsProperty = DependencyProperty.Register(
        nameof(PlayerOptions),
        typeof(IEnumerable<string>),
        typeof(AnimatedThumbnailControl),
        new PropertyMetadata(Array.Empty<string>()));

    private readonly DispatcherTimer _visibilityTimer;
    private MediaPlayer? _mediaPlayer;
    private Media? _media;
    private readonly Guid _ownerId = Guid.NewGuid();
    private bool _isDisposed;
    private bool _isPointerOver;
    private bool _hasPreviewSlot;

    public AnimatedThumbnailControl()
    {
        InitializeComponent();

        _visibilityTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _visibilityTimer.Tick += (_, _) => SyncPlaybackState();
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

    private static void OnVideoPathChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is AnimatedThumbnailControl control && control.IsLoaded)
        {
            control.RestartPreview();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _visibilityTimer.Start();
        SyncPlaybackState();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _visibilityTimer.Stop();
        ReleasePlayer();
        PreviewRenderCoordinator.Shared.Release(_ownerId);
        _hasPreviewSlot = false;
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        SyncPlaybackState();
    }

    private void SyncPlaybackState()
    {
        if (_isDisposed)
        {
            return;
        }

        if (IsVisible
            && IsLoaded
            && _isPointerOver
            && !string.IsNullOrWhiteSpace(VideoPath)
            && File.Exists(VideoPath)
            && AcquirePreviewSlot())
        {
            EnsurePlayer();
            return;
        }

        ReleasePlayer();
    }

    private void EnsurePlayer()
    {
        if (_mediaPlayer is not null)
        {
            return;
        }

        StateText.Text = "Live preview";
        var previewVlc = PreviewVlcHost.GetSharedPreviewVlc(PlayerOptions);
        _mediaPlayer = new MediaPlayer(previewVlc)
        {
            Mute = true,
            Volume = 0
        };

        PreviewVideo.MediaPlayer = _mediaPlayer;
        _media = new Media(previewVlc, VideoPath, FromType.FromPath);
        _media.AddOption(":input-repeat=65535");
        _media.AddOption(":no-audio");
        _media.AddOption(":file-caching=250");
        _mediaPlayer.Play(_media);
    }

    private void RestartPreview()
    {
        ReleasePlayer();
        SyncPlaybackState();
    }

    private void ReleasePlayer()
    {
        if (_mediaPlayer is null && _media is null)
        {
            return;
        }

        PreviewVideo.MediaPlayer = null;
        
        var playerToDispose = _mediaPlayer;
        var mediaToDispose = _media;

        _mediaPlayer = null;
        _media = null;
        
        Task.Run(() => 
        {
            try { playerToDispose?.Stop(); } catch { }
            try { playerToDispose?.Dispose(); } catch { }
            try { mediaToDispose?.Dispose(); } catch { }
        });
        if (_hasPreviewSlot)
        {
            PreviewRenderCoordinator.Shared.Release(_ownerId);
            _hasPreviewSlot = false;
        }
        StateText.Text = "Preview idle";
    }

    private bool AcquirePreviewSlot()
    {
        if (_hasPreviewSlot)
        {
            return true;
        }

        _hasPreviewSlot = PreviewRenderCoordinator.Shared.TryAcquire(_ownerId);
        if (!_hasPreviewSlot)
        {
            StateText.Text = "Preview busy";
        }

        return _hasPreviewSlot;
    }

    private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _isPointerOver = true;
        SyncPlaybackState();
    }

    private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _isPointerOver = false;
        ReleasePlayer();
        PreviewRenderCoordinator.Shared.Release(_ownerId);
        _hasPreviewSlot = false;
        StateText.Text = "Hover to preview";
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _visibilityTimer.Stop();
        ReleasePlayer();
        PreviewRenderCoordinator.Shared.Release(_ownerId);
        GC.SuppressFinalize(this);
    }
}
