using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using LiveWallpaperApp.Models;
using LiveWallpaperApp.Services;
using LiveWallpaperApp.ViewModels;

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
    private readonly MemoryCleanupService _memoryCleanupService;
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
        _memoryCleanupService = new MemoryCleanupService(_thumbnailService, _performanceSettings);
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
            _performanceSettings);

        DataContext = _viewModel;

        _trayService.Initialize(
            RestoreFromTray,
            _viewModel.PauseResumeWallpaper,
            _viewModel.StopWallpaper,
            ExitApplication);

        StateChanged += OnWindowStateChanged;
        Loaded += OnLoaded;
        Activated += OnActivated;
        Deactivated += OnDeactivated;
        _performanceService.Start();
        _autoPauseService.Start();
        _memoryCleanupService.Start();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        EnableSystemBackdrop();
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
        ExitApplication();
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
            return;
        }

        if (_performanceSettings.MinimizeToTray && _performanceSettings.ShowTrayIcon)
        {
            Hide();
            _trayService.ShowInfo("Live Wallpaper App", "Dashboard minimized to tray.");
        }
        
        // Aggressively free RAM when the app goes into the background
        Task.Run(async () => 
        {
            await Task.Delay(500); // Give UI time to hide
            _memoryCleanupService.TrimMemory();
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
        _memoryCleanupService.Dispose();
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

    private const int DwmUseImmersiveDarkMode = 20;
    private const int DwmSystemBackdropType = 38;
    private const int DwmSystemBackdropMainWindow = 2;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
}
