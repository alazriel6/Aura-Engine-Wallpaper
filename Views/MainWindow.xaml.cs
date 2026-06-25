using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using LiveWallpaperApp.Models;
using LiveWallpaperApp.Services;
using LiveWallpaperApp.ViewModels;
using Microsoft.Web.WebView2.Core;

namespace LiveWallpaperApp.Views;

public partial class MainWindow : Window
{
    private readonly MonitorService _monitorService;
    private readonly ThemeService _themeService;
    private readonly StartupService _startupService;
    private readonly GPUOptimizationService _gpuOptimizationService;
    private readonly WallpaperService _wallpaperService;
    private readonly WallpaperLibraryService _libraryService;
    private readonly ThumbnailService _thumbnailService;
    private readonly PerformanceService _performanceService;
    private readonly AutoPauseService _autoPauseService;
    private readonly PreviewRenderService _previewRenderService;
    private readonly MemoryOptimizerService _memoryOptimizerService;
    private readonly TrayService _trayService;
    private readonly SettingsService _settingsService;
    private readonly PerformanceSettings _performanceSettings;
    private readonly MainViewModel _viewModel;
    private bool _isExiting;

    public MainWindow()
    {
        InitializeComponent();

        _monitorService = new MonitorService();
        _themeService = new ThemeService();
        _startupService = new StartupService();
        _gpuOptimizationService = new GPUOptimizationService();
        _settingsService = new SettingsService();
        _performanceSettings = _settingsService.LoadSettings();
        _wallpaperService = new WallpaperService(_monitorService, _gpuOptimizationService);
        _libraryService = new WallpaperLibraryService();
        _thumbnailService = new ThumbnailService(_gpuOptimizationService);
        _performanceService = new PerformanceService();
        _autoPauseService = new AutoPauseService(_wallpaperService, _performanceService, _performanceSettings);
        _previewRenderService = PreviewRenderCoordinator.Shared;
        _memoryOptimizerService = new MemoryOptimizerService(_thumbnailService, _performanceSettings);
        var playlistService = new PlaylistService(_libraryService, _thumbnailService);
        var profileService = new ProfileService(_settingsService, _performanceSettings);
        var systemHealthService = new SystemHealthService(_performanceService, _settingsService);
        _trayService = new TrayService();

        _viewModel = new MainViewModel(
            _wallpaperService,
            _themeService,
            _startupService,
            _monitorService,
            _libraryService,
            _thumbnailService,
            _performanceService,
            _autoPauseService,
            _gpuOptimizationService,
            _previewRenderService,
            _memoryOptimizerService,
            playlistService,
            profileService,
            systemHealthService,
            _performanceSettings);

        DataContext = _viewModel;

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        _trayService.Initialize(
            RestoreFromTray,
            _viewModel.PauseResumeWallpaper,
            _viewModel.StopWallpaper,
            ExitApplication);

        StateChanged += OnWindowStateChanged;
        Loaded += OnLoaded;
        Activated += OnActivated;
        Deactivated += OnDeactivated;
        _performanceService.Start(true);
        _autoPauseService.Start();
        _memoryOptimizerService.Start();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        EnableSystemBackdrop();
    }

    private Microsoft.Web.WebView2.Wpf.WebView2? _marketplaceWebView;

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedPage))
        {
            if (_viewModel.SelectedPage == "Marketplace")
            {
                InitializeWebViewAsync();
            }
            else
            {
                DestroyWebView();
            }
        }
    }

    private async void InitializeWebViewAsync()
    {
        if (_marketplaceWebView != null) return;
        
        _marketplaceWebView = new Microsoft.Web.WebView2.Wpf.WebView2
        {
            Source = new Uri("https://search.brave.com/search?q=live+wallpapers+download")
        };
        _marketplaceWebView.NavigationCompleted += MarketplaceWebView_NavigationCompleted;
        MarketplaceContainer.Child = _marketplaceWebView;

        await _marketplaceWebView.EnsureCoreWebView2Async(null);
        
        var downloadFolder = Path.Combine(_libraryService.LibraryRoot, "Downloads");
        Directory.CreateDirectory(downloadFolder);
        _marketplaceWebView.CoreWebView2.Profile.DefaultDownloadFolderPath = downloadFolder;
        
        _marketplaceWebView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
    }

    private void DestroyWebView()
    {
        if (_marketplaceWebView == null) return;
        
        MarketplaceContainer.Child = null;
        _marketplaceWebView.NavigationCompleted -= MarketplaceWebView_NavigationCompleted;
        if (_marketplaceWebView.CoreWebView2 != null)
        {
            _marketplaceWebView.CoreWebView2.DownloadStarting -= CoreWebView2_DownloadStarting;
        }
        
        _marketplaceWebView.Dispose();
        _marketplaceWebView = null;
    }

    private void CoreWebView2_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        var download = e.DownloadOperation;

        download.StateChanged += async (s, args) =>
        {
            if (download.State == CoreWebView2DownloadState.Completed)
            {
                var finalPath = download.ResultFilePath;
                var ext = Path.GetExtension(finalPath).ToLowerInvariant();

                if (ext == ".mp4" || ext == ".webm" || ext == ".gif")
                {
                    await _libraryService.ImportVideoAsync(finalPath);
                    
                    // Clean up the raw file after successful import
                    try { File.Delete(finalPath); } catch { }
                    
                    _viewModel.RefreshLibraryCommand.Execute(null);
                    _trayService.ShowInfo("Download Complete", $"New live wallpaper added to your library!");
                }
            }
        };
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var launchArgs = Environment.GetCommandLineArgs();
        bool isArgMinimized = launchArgs.Any(arg => string.Equals(arg, "--minimized", StringComparison.OrdinalIgnoreCase));

        if (isArgMinimized || _performanceSettings.StartMinimized)
        {
            WindowState = WindowState.Minimized;
            if (_performanceSettings.ShowTrayIcon)
            {
                _trayService.ShowInfo("Live Wallpaper App", "Running in the background.");
            }
            else
            {
                Hide();
            }
        }
        
        _trayService.SetVisibility(_performanceSettings.ShowTrayIcon);
        _themeService.ApplyVisualEffects(_performanceSettings);
        try { _themeService.ApplyTheme(_performanceSettings.SelectedTheme); } catch {}
        try { _themeService.ApplyAccentColor(_performanceSettings.AccentColorHex); } catch {}
        _currentUiScale = 1.0;
        ApplyUiScale(_performanceSettings.UiScale);

        if (_performanceSettings.AutoRestoreWallpaper && !string.IsNullOrWhiteSpace(_performanceSettings.LastWallpaperPath))
        {
            _wallpaperService.ApplyWallpaper(_performanceSettings.LastWallpaperPath, null, _performanceSettings);
            _viewModel.VideoPath = _performanceSettings.LastWallpaperPath;
        }

        _performanceSettings.PropertyChanged += OnPerformanceSettingsChanged;
    }

    private void OnPerformanceSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PerformanceSettings.ShowTrayIcon))
        {
            _trayService.SetVisibility(_performanceSettings.ShowTrayIcon);
        }
        else if (e.PropertyName == nameof(PerformanceSettings.UiScale))
        {
            ApplyUiScale(_performanceSettings.UiScale);
        }
    }

    private double _currentUiScale = 1.0;

    private void ApplyUiScale(double newScale)
    {
        if (Math.Abs(_currentUiScale - newScale) < 0.01 && Content is FrameworkElement existingContent && existingContent.LayoutTransform is System.Windows.Media.ScaleTransform)
        {
            return;
        }

        double ratio = newScale / _currentUiScale;

        // Scale the window itself to prevent layout squishing/overflow
        this.Width = this.Width * ratio;
        this.Height = this.Height * ratio;
        this.MinWidth = 1180 * newScale;
        this.MinHeight = 740 * newScale;

        RootGrid.LayoutTransform = new System.Windows.Media.ScaleTransform(newScale, newScale);

        var chrome = System.Windows.Shell.WindowChrome.GetWindowChrome(this);
        if (chrome != null)
        {
            chrome.CaptionHeight = 54 * newScale;
        }

        // Fix blurry text that occurs when LayoutRounding is active at non-1.0 scales
        var mode = newScale == 1.0 ? System.Windows.Media.TextFormattingMode.Display : System.Windows.Media.TextFormattingMode.Ideal;
        System.Windows.Media.TextOptions.SetTextFormattingMode(this, mode);

        _currentUiScale = newScale;
    }

    private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeRestoreClick(object sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        if (_performanceSettings.CloseToTray && _performanceSettings.ShowTrayIcon)
        {
            Hide();
            _trayService.ShowInfo("Live Wallpaper App", "App is running in the background.");
        }
        else
        {
            ExitApplication();
        }
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        _viewModel.IsDashboardActive = WindowState != WindowState.Minimized;

        if (WindowState != WindowState.Minimized || _isExiting)
        {
            try { _marketplaceWebView?.CoreWebView2?.Resume(); } catch { }
            return;
        }

        try { _marketplaceWebView?.CoreWebView2?.TrySuspendAsync(); } catch { }

        if (_performanceSettings.MinimizeToTray && _performanceSettings.ShowTrayIcon)
        {
            Hide();
            _trayService.ShowInfo("Live Wallpaper App", "Dashboard minimized to tray.");
        }
        
        // Aggressively free RAM when the app goes into the background
        Task.Run(async () => 
        {
            await Task.Delay(500); // Give UI time to hide
            _memoryOptimizerService.TrimMemory();
        });
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        _viewModel.IsDashboardActive = true;
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        _viewModel.IsDashboardActive = false;
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        _isExiting = true;
        Close();
    }

    private void OnWindowDragOver(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnWindowDrop(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
        {
            _viewModel.VideoPath = files[0];
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _settingsService.SaveSettings(_performanceSettings);
        StateChanged -= OnWindowStateChanged;
        Activated -= OnActivated;
        Deactivated -= OnDeactivated;
        _autoPauseService.Dispose();
        _performanceService.Dispose();
        _memoryOptimizerService.Stop();
        _memoryOptimizerService.Dispose();
        _wallpaperService.Dispose();
        _trayService.Dispose();
        PreviewVlcHost.DisposeSharedPreviewVlc();
        base.OnClosed(e);
    }

    private void EnableSystemBackdrop()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var dark = 1;
        _ = DwmSetWindowAttribute(handle, DwmUseImmersiveDarkMode, ref dark, sizeof(int));

        var backdrop = DwmSystemBackdropMainWindow;
        _ = DwmSetWindowAttribute(handle, DwmSystemBackdropType, ref backdrop, sizeof(int));
    }

    private void BrowserBackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_marketplaceWebView != null && _marketplaceWebView.CanGoBack) _marketplaceWebView.GoBack();
    }

    private void BrowserForwardButton_Click(object sender, RoutedEventArgs e)
    {
        if (_marketplaceWebView != null && _marketplaceWebView.CanGoForward) _marketplaceWebView.GoForward();
    }

    private void BrowserRefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _marketplaceWebView?.Reload();
    }

    private void BrowserAddressBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            NavigateToAddress(BrowserAddressBar.Text);
        }
    }

    private void BrowserGoButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateToAddress(BrowserAddressBar.Text);
    }

    private void NavigateToAddress(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || _marketplaceWebView == null) return;
        
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            if (url.Contains(".") && !url.Contains(" "))
            {
                url = "https://" + url;
            }
            else
            {
                url = "https://search.brave.com/search?q=" + Uri.EscapeDataString(url);
            }
        }
        
        try
        {
            _marketplaceWebView.Source = new Uri(url);
        }
        catch { }
    }

    private void MarketplaceWebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (_marketplaceWebView?.Source != null)
        {
            BrowserAddressBar.Text = _marketplaceWebView.Source.ToString();
        }
    }

    private const int DwmUseImmersiveDarkMode = 20;
    private const int DwmSystemBackdropType = 38;
    private const int DwmSystemBackdropMainWindow = 2;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
}
