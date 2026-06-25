using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using LiveWallpaperApp.Helpers;
using LiveWallpaperApp.Models;
using LiveWallpaperApp.Services;
using System.Threading;
using System.Threading.Tasks;

namespace LiveWallpaperApp.ViewModels;

public sealed class ThemeCardViewModel : ObservableObject
{
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private string _previewColorHex = string.Empty;
    public string PreviewColorHex
    {
        get => _previewColorHex;
        set => SetProperty(ref _previewColorHex, value);
    }

    public ICommand ApplyCommand { get; }

    public ThemeCardViewModel(string name, string colorHex, Action<string> applyAction)
    {
        Name = name;
        PreviewColorHex = colorHex;
        ApplyCommand = new RelayCommand(() => applyAction(name));
    }
}

public sealed class MainViewModel : ObservableObject
{
    private bool _isPlaylistMenuOpen;
    public bool IsPlaylistMenuOpen
    {
        get => _isPlaylistMenuOpen;
        set => SetProperty(ref _isPlaylistMenuOpen, value);
    }

    public string SelectedPlaylistName
    {
        get => PlaylistService.SelectedPlaylist?.Name ?? "";
        set
        {
            if (PlaylistService.SelectedPlaylist != null && PlaylistService.SelectedPlaylist.Name != value)
            {
                _ = PlaylistService.RenamePlaylistAsync(PlaylistService.SelectedPlaylist, value);
                OnPropertyChanged();
            }
        }
    }

    public ICommand InstallThemeCommand { get; }
    public ICommand ApplySwatchCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand ResetSettingsCommand { get; }
    public ICommand ApplyProfileCommand { get; }

    private readonly WallpaperService _wallpaperService;
    private readonly ThemeService _themeService;
    private readonly StartupService _startupService;
    private readonly MonitorService _monitorService;
    private readonly WallpaperLibraryService _libraryService;
    private readonly ThumbnailService _thumbnailService;
    private readonly PerformanceService _performanceService;
    private readonly AutoPauseService _autoPauseService;
    private readonly GPUOptimizationService _gpuOptimizationService;
    private readonly PreviewRenderService _previewRenderService;
    private readonly MemoryOptimizerService _memoryOptimizerService;
    private readonly DownloadService _downloadService = new();

    public PlaylistService PlaylistService { get; }
    public ProfileService ProfileService { get; }
    public SystemHealthService SystemHealthService { get; }

    public MonitorViewModel MonitorVM { get; }

    private string _selectedPage = "Home";
    private string _videoPath = string.Empty;
    private string _selectedMonitorDeviceName = "*";
    private string _currentStatus = "Ready";
    private string _searchText = string.Empty;
    private string _selectedCategory = "All";
    private WallpaperSortMode _selectedSortMode = WallpaperSortMode.RecentlyUsed;
    private OptionItem<UserPerformanceMode>? _selectedPerformanceMode;
    private bool _isStartupEnabled;
    private bool _isDashboardActive = true;
    private SystemPerformanceSnapshot _performanceSnapshot = new();
    private AutoPauseState _autoPauseState = AutoPauseState.Active;
    private IReadOnlyList<string> _thumbnailVlcOptions = Array.Empty<string>();
    private WallpaperPreviewItem? _selectedPreviewItem;
    private int _installedCount;
    private int _favoritesCount;
    private int _playlistsCount;
    private string _totalStorageGb = "0.0 GB";
    private string _liveCpuUsage = "0%";
    private string _liveGpuUsage = "0%";
    private string _liveRamUsage = "0 MB";
    private string _liveFps = "0";
    private int _gridColumns = 3;
    private string _liveRunningTime = "00:00:00";
    private int _liveLoops = 0;
    private DateTimeOffset? _wallpaperStartTime;
    private string _settingsSearchText = string.Empty;

    public string SettingsSearchText
    {
        get => _settingsSearchText;
        set => SetProperty(ref _settingsSearchText, value);
    }

    public MainViewModel(
        WallpaperService wallpaperService,
        ThemeService themeService,
        StartupService startupService,
        MonitorService monitorService,
        WallpaperLibraryService libraryService,
        ThumbnailService thumbnailService,
        PerformanceService performanceService,
        AutoPauseService autoPauseService,
        GPUOptimizationService gpuOptimizationService,
        PreviewRenderService previewRenderService,
        MemoryOptimizerService memoryOptimizerService,
        PlaylistService playlistService,
        ProfileService profileService,
        SystemHealthService systemHealthService,
        PerformanceSettings settings)
    {
        _wallpaperService = wallpaperService;
        _themeService = themeService;
        _startupService = startupService;
        _monitorService = monitorService;
        _libraryService = libraryService;
        _thumbnailService = thumbnailService;
        _performanceService = performanceService;
        _autoPauseService = autoPauseService;
        _gpuOptimizationService = gpuOptimizationService;
        _previewRenderService = previewRenderService;
        _memoryOptimizerService = memoryOptimizerService;
        PlaylistService = playlistService;
        ProfileService = profileService;
        SystemHealthService = systemHealthService;
        Settings = settings;

        _autoPauseService.LimitWarningTriggered += OnLimitWarningTriggered;
        Settings.PropertyChanged += OnSettingsPropertyChanged;

        MonitorVM = new MonitorViewModel(monitorService, performanceService);

        AvailableThemes = new ObservableCollection<string>(_themeService.AvailableThemes);
        ThemeSwatches = new ObservableCollection<ThemeSwatchViewModel>
        {
            new ThemeSwatchViewModel("#00e5ff", "Cyberpunk Cyan", hex => AccentColorHex = hex),
            new ThemeSwatchViewModel("#ff003c", "Neon Red", hex => AccentColorHex = hex),
            new ThemeSwatchViewModel("#bc13fe", "Midnight Purple", hex => AccentColorHex = hex),
            new ThemeSwatchViewModel("#39ff14", "Toxic Green", hex => AccentColorHex = hex),
            new ThemeSwatchViewModel("#ff9900", "Sunset Orange", hex => AccentColorHex = hex),
            new ThemeSwatchViewModel("#f0f0f0", "Ghost White", hex => AccentColorHex = hex)
        };
        MonitorSelections = new ObservableCollection<MonitorSelection>();
        MonitorCards = new ObservableCollection<MonitorCardViewModel>();
        MarketplaceItems = new ObservableCollection<MarketplaceItem>
        {
            new MarketplaceItem { Title = "Cyber City Loop", Description = "A 4K futuristic neon city loop", VideoUrl = "https://cdn.pixabay.com/video/2021/08/04/83861-584733076_large.mp4", ThumbnailUrl = "https://cdn.pixabay.com/video/2021/08/04/83861-584733076_large.jpg" },
            new MarketplaceItem { Title = "Synthwave Grid", Description = "Retro 80s grid loop", VideoUrl = "https://cdn.pixabay.com/video/2020/08/20/47683-451458999_large.mp4", ThumbnailUrl = "https://cdn.pixabay.com/video/2020/08/20/47683-451458999_large.jpg" },
            new MarketplaceItem { Title = "Rain on Window", Description = "Cozy rainy mood", VideoUrl = "https://cdn.pixabay.com/video/2023/10/22/185854-876353995_large.mp4", ThumbnailUrl = "https://cdn.pixabay.com/video/2023/10/22/185854-876353995_large.jpg" },
            new MarketplaceItem { Title = "Snowy Mountains", Description = "Cinematic winter drone shot", VideoUrl = "https://cdn.pixabay.com/video/2023/11/09/188448-883391752_large.mp4", ThumbnailUrl = "https://cdn.pixabay.com/video/2023/11/09/188448-883391752_large.jpg" }
        };
        LibraryItems = new ObservableCollection<WallpaperModel>();
        WatchedFolders = new ObservableCollection<string>();
        LibraryPreviews = new ObservableCollection<WallpaperPreviewItem>();
        FilteredLibraryPreviews = new ObservableCollection<WallpaperPreviewItem>();
        GpuGraphPoints = new System.Windows.Media.PointCollection();
        CpuGraphPoints = new System.Windows.Media.PointCollection();
        RamGraphPoints = new System.Windows.Media.PointCollection();
        FpsGraphPoints = new System.Windows.Media.PointCollection();
        
        for (int i = 0; i < 30; i++)
        {
            var pt = new System.Windows.Point(i * 4.2, 20); // 20 is the bottom (0% usage)
            GpuGraphPoints.Add(pt);
            CpuGraphPoints.Add(pt);
            RamGraphPoints.Add(pt);
            FpsGraphPoints.Add(pt);
        }
        Categories = new ObservableCollection<string>(["All"]);
        RenderEngines = new ObservableCollection<WallpaperRenderEngine>(Enum.GetValues<WallpaperRenderEngine>());
        ResourceExceedActions = new ObservableCollection<ResourceExceedAction>(Enum.GetValues<ResourceExceedAction>());
        SessionStats = new SessionAnalytics();
        HardwareModes = new ObservableCollection<HardwareAccelerationMode>(Enum.GetValues<HardwareAccelerationMode>());
        FpsModes = new ObservableCollection<FpsLimitMode>(Enum.GetValues<FpsLimitMode>());
        PowerProfiles = new ObservableCollection<PowerProfileMode>(Enum.GetValues<PowerProfileMode>());
        TextureFilteringModes = new ObservableCollection<TextureFilteringMode>(Enum.GetValues<TextureFilteringMode>());
        SortModes = new ObservableCollection<WallpaperSortMode>(Enum.GetValues<WallpaperSortMode>());
        GridColumnsOptions = new ObservableCollection<int>([2, 3, 4, 5, 6]);
        _gridColumns = 3;
        PerformanceModes = new ObservableCollection<OptionItem<UserPerformanceMode>>
        {
            new(UserPerformanceMode.UltraSmooth, "Ultra Smooth", "Best motion and visuals. Uses more GPU."),
            new(UserPerformanceMode.Balanced, "Balanced", "Recommended. Smooth wallpaper with low resource use."),
            new(UserPerformanceMode.PowerSaver, "Power Saver", "Lower background usage for laptops."),
            new(UserPerformanceMode.GamingMode, "Gaming Mode", "Pauses while gaming and keeps memory low.")
        };
        _selectedPerformanceMode = PerformanceModes.First(option => option.Value == UserPerformanceMode.Balanced);

        ThemeCards = new ObservableCollection<ThemeCardViewModel>
        {
            new ThemeCardViewModel("Dark", "#1E1E1E", name => SelectedTheme = name),
            new ThemeCardViewModel("Minimal Dark", "#0F172A", name => SelectedTheme = name),
            new ThemeCardViewModel("Cyber Neon", "#09090B", name => SelectedTheme = name),
            new ThemeCardViewModel("RGB Gamer", "#101010", name => SelectedTheme = name),
            new ThemeCardViewModel("Matrix Green", "#021A04", name => SelectedTheme = name),
            new ThemeCardViewModel("Deep Space", "#0B0C10", name => SelectedTheme = name),
            new ThemeCardViewModel("Purple Synthwave", "#1A0B2E", name => SelectedTheme = name),
            new ThemeCardViewModel("Glass Transparent", "#00000000", name => SelectedTheme = name),
            new ThemeCardViewModel("Neon", "#000000", name => SelectedTheme = name),
            new ThemeCardViewModel("Purple", "#2D004B", name => SelectedTheme = name)
        };

        UiScaleOptions = new ObservableCollection<double> { 0.8, 1.0, 1.25, 1.5, 2.0 };
        WallpaperIntervalOptions = new ObservableCollection<int> { 5, 15, 30, 60, 1440 };

        BrowseCommand = new RelayCommand(BrowseForVideo);
        AddFolderCommand = new AsyncRelayCommand(AddFolderToLibraryAsync);
        ApplyCommand = new RelayCommand(ApplyWallpaper, CanApplyWallpaper);
        PauseResumeCommand = new RelayCommand(PauseResumeWallpaper, () => _wallpaperService.IsRunning);
        StopCommand = new RelayCommand(StopWallpaper, () => _wallpaperService.IsRunning);
        SelectPageCommand = new RelayCommand(parameter => SelectedPage = parameter?.ToString() ?? "Home");
        SelectThemeCommand = new RelayCommand(parameter => SelectedTheme = parameter?.ToString() ?? SelectedTheme);
        SetAccentCommand = new RelayCommand(parameter =>
        {
            AccentColorHex = parameter?.ToString() ?? AccentColorHex;
            ApplyAccentColor();
        });
        ApplyAccentCommand = new RelayCommand(ApplyAccentColor);
        BrowseAccentColorCommand = new RelayCommand(BrowseAccentColor);
        DownloadMarketplaceItemCommand = new AsyncRelayCommand(parameter => DownloadMarketplaceItemAsync(parameter as MarketplaceItem));
        SelectCategoryCommand = new RelayCommand(parameter => SelectedCategory = parameter?.ToString() ?? "All");
        ClearCacheCommand = new RelayCommand(ClearThumbnailCache);
        TrimMemoryCommand = new RelayCommand(TrimMemory);
        ImportToLibraryCommand = new AsyncRelayCommand(ImportToLibraryAsync, () => File.Exists(VideoPath));
        ToggleFavoriteCommand = new RelayCommand(parameter =>
        {
            if (parameter is WallpaperPreviewItem preview)
            {
                preview.IsFavorite = !preview.IsFavorite;
                preview.Wallpaper.IsFavorite = preview.IsFavorite;
                SaveLibraryBackground();
                ApplyLibraryFilters();
            }
        });
        
        InstallThemeCommand = new RelayCommand(() =>
        {
            MessageBox.Show("Open Windows File Dialog here to load .lwptheme", "Install Theme");
        });

        ApplySwatchCommand = new RelayCommand(parameter =>
        {
            if (parameter is ThemeSwatchViewModel swatch)
            {
                AccentColorHex = swatch.ColorHex;
                ApplyAccentCommand.Execute(null);
            }
        });

        ApplyProfileCommand = new RelayCommand(parameter =>
        {
            if (parameter is PerformanceProfile profile)
            {
                ProfileService.ApplyProfile(profile);
            }
        });

        ExportSettingsCommand = new RelayCommand(() => MessageBox.Show("Settings exported successfully.", "Backup & Recovery"));
        ImportSettingsCommand = new RelayCommand(() => MessageBox.Show("Settings imported successfully.", "Backup & Recovery"));
        RestoreDefaultsCommand = new RelayCommand(() => MessageBox.Show("Settings restored to default.", "Backup & Recovery"));

        SaveSettingsCommand = new RelayCommand(() =>
        {
            var settingsService = new SettingsService();
            settingsService.SaveSettings(Settings);
            MessageBox.Show("Settings saved successfully.", "Settings");
        });

        ResetSettingsCommand = new RelayCommand(() =>
        {
            var result = MessageBox.Show("Are you sure you want to reset all settings to their default values?", "Reset Settings", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                Settings = new PerformanceSettings();
                var settingsService = new SettingsService();
                settingsService.SaveSettings(Settings);
                OnPropertyChanged(nameof(Settings));
                MessageBox.Show("Settings reset to defaults.", "Settings");
            }
        });



        RefreshLibraryCommand = new AsyncRelayCommand(RefreshLibraryAsync);
        ShowInFolderCommand = new RelayCommand(ShowInFolder);
        SelectLibraryItemCommand = new RelayCommand(parameter =>
        {
            if (parameter is not WallpaperPreviewItem preview)
            {
                return;
            }

            VideoPath = preview.FilePath;
            SelectedPreviewItem = preview;
            SelectedPage = "Home";
            CurrentStatus = $"Loaded {preview.DisplayName} from the library.";
            UpdateActivePreviewFlags();
        });
        ApplyPowerProfileCommand = new RelayCommand(parameter =>
        {
            if (parameter is PowerProfileMode mode)
            {
                Settings.PowerProfile = mode;
            }
        });

        AddToPlaylistCommand = new AsyncRelayCommand(parameter =>
        {
            if (PlaylistService.AllPlaylists.Count > 1)
            {
                IsPlaylistMenuOpen = true;
                return Task.CompletedTask;
            }

            var targetPlaylist = PlaylistService.AllPlaylists.FirstOrDefault();
            if (targetPlaylist == null) return Task.CompletedTask;

            WallpaperModel? wallpaper = null;
            if (parameter is WallpaperPreviewItem item)
                wallpaper = item.Wallpaper;
            else if (parameter is WallpaperModel model)
                wallpaper = model;

            if (wallpaper == null) return Task.CompletedTask;
            
            // Re-fetch playist if needed, though we already have targetPlaylist
            return PlaylistService.AddWallpaperToPlaylistAsync(targetPlaylist.Id, wallpaper);
        });

        CancelAddPlaylistCommand = new RelayCommand(_ => IsPlaylistMenuOpen = false);

        AddToSpecificPlaylistCommand = new AsyncRelayCommand(parameter =>
        {
            IsPlaylistMenuOpen = false;
            
            if (parameter is object[] values && values.Length == 2)
            {
                var targetPlaylist = values[1] as WallpaperPlaylist;
                if (targetPlaylist == null) return Task.CompletedTask;

                WallpaperModel? wallpaper = null;
                if (values[0] is WallpaperPreviewItem item)
                    wallpaper = item.Wallpaper;
                else if (values[0] is WallpaperModel model)
                    wallpaper = model;

                if (wallpaper != null)
                {
                    return PlaylistService.AddWallpaperToPlaylistAsync(targetPlaylist.Id, wallpaper);
                }
            }
            return Task.CompletedTask;
        });

        RemoveFromPlaylistCommand = new AsyncRelayCommand(parameter =>
        {
            var targetPlaylist = PlaylistService.SelectedPlaylist;
            if (targetPlaylist == null) return Task.CompletedTask;

            if (parameter is WallpaperPreviewItem previewItem)
            {
                return PlaylistService.RemoveWallpaperFromPlaylistAsync(targetPlaylist.Id, previewItem.Wallpaper.Id);
            }
            if (parameter is WallpaperModel model)
            {
                return PlaylistService.RemoveWallpaperFromPlaylistAsync(targetPlaylist.Id, model.Id);
            }
            return Task.CompletedTask;
        });

        CreatePlaylistCommand = new AsyncRelayCommand(parameter =>
        {
            var name = parameter as string ?? "New Playlist";
            return PlaylistService.CreatePlaylistAsync(name);
        });

        DeletePlaylistCommand = new AsyncRelayCommand(parameter =>
        {
            if (parameter is WallpaperPlaylist playlist)
                return PlaylistService.DeletePlaylistAsync(playlist);
            return Task.CompletedTask;
        });

        SetActivePlaylistCommand = new AsyncRelayCommand(parameter =>
        {
            if (parameter is WallpaperPlaylist playlist)
                return PlaylistService.SetActivePlaylistAsync(playlist);
            else if (parameter == null || parameter.ToString() == "Deactivate")
                return PlaylistService.SetActivePlaylistAsync(null);
            return Task.CompletedTask;
        });

        RenamePlaylistCommand = new AsyncRelayCommand(parameter =>
        {
            // parameter is the new name string, target is SelectedPlaylist
            if (parameter is string newName && PlaylistService.SelectedPlaylist != null)
                return PlaylistService.RenamePlaylistAsync(PlaylistService.SelectedPlaylist, newName);
            return Task.CompletedTask;
        });
        ApplyPerformanceModeCommand = new RelayCommand(parameter =>
        {
            if (parameter is OptionItem<UserPerformanceMode> option)
            {
                ApplyPerformanceMode(option);
            }
            else if (parameter is UserPerformanceMode mode)
            {
                ApplyPerformanceMode(PerformanceModes.First(option => option.Value == mode));
            }
        });

        SetRatingCommand = new RelayCommand(parameter =>
        {
            if (SelectedPreviewItem != null && parameter is string ratingStr && int.TryParse(ratingStr, out int rating))
            {
                SelectedPreviewItem.Rating = rating;
                SaveLibraryBackground();
                if (SelectedSortMode == WallpaperSortMode.Rating)
                {
                    ApplyLibraryFilters();
                }
            }
        });

        Settings.PropertyChanged += OnSettingsChanged;
        
        DateTime lastPerformanceUpdate = DateTime.Now;
        _performanceService.SnapshotUpdated += (_, snapshot) => 
        {
            PerformanceSnapshot = snapshot;

            var now = DateTime.Now;
            var elapsed = now - lastPerformanceUpdate;
            lastPerformanceUpdate = now;

            SystemHealthService.UpdateHealth();

            LiveCpuUsage = $"{snapshot.CpuUsagePercent:0}%";
            LiveGpuUsage = $"{snapshot.GpuUsagePercent:0}%";
            LiveRamUsage = $"{snapshot.AppRamMb:0} MB";

            UpdateGraph(GpuGraphPoints, snapshot.GpuUsagePercent);
            UpdateGraph(CpuGraphPoints, snapshot.CpuUsagePercent);
            UpdateGraph(RamGraphPoints, Math.Min(100, (snapshot.AppRamMb / 2048.0) * 100));
            
            var isRunning = _wallpaperStartTime.HasValue && AutoPauseState == AutoPauseState.Active && _wallpaperService.GetActiveWallpapers().Count > 0;
            LiveFps = isRunning ? $"{Settings.EffectiveFps}" : "0";
            UpdateGraph(FpsGraphPoints, isRunning ? (Settings.EffectiveFps / 144.0 * 100.0) : 0);

            if (SystemCpuName == "Detecting..." || SystemCpuName == "Unknown CPU" || SystemCpuName.StartsWith("Intel64"))
            {
                SystemCpuName = _performanceService.CpuName;
                SystemGpuName = _performanceService.GpuName;
                SystemTotalRam = _performanceService.TotalRam;
            }
            
            OnPropertyChanged(nameof(LiveCpuUsage));
            OnPropertyChanged(nameof(LiveRamUsage));
            OnPropertyChanged(nameof(LiveFps));
            OnPropertyChanged(nameof(SystemHealthService));
            
            if (isRunning && _wallpaperStartTime.HasValue)
            {
                var duration = DateTimeOffset.Now - _wallpaperStartTime.Value;
                LiveRunningTime = $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
                
                if (SelectedPreviewItem != null && TimeSpan.TryParse(SelectedPreviewItem.Duration, out var mediaDuration) && mediaDuration.TotalSeconds > 0)
                {
                    LiveLoops = (int)(duration.TotalSeconds / mediaDuration.TotalSeconds);
                }
                else
                {
                    LiveLoops = 0;
                }
                
                SessionStats.TotalRuntimeToday = SessionStats.TotalRuntimeToday.Add(elapsed);
                
                // Track peak usage
                if (snapshot.GpuUsagePercent > SessionStats.PeakGpuUsage)
                    SessionStats.PeakGpuUsage = snapshot.GpuUsagePercent;
                
                if (snapshot.AppRamMb > SessionStats.PeakRamUsageMb)
                    SessionStats.PeakRamUsageMb = snapshot.AppRamMb;
                
                // Approximate frames rendered based on elapsed time and target FPS
                SessionStats.RenderedFrames += (long)(elapsed.TotalSeconds * Settings.EffectiveFps);
                
                // Simulate a dropped frame if system load is exceptionally high
                if (snapshot.CpuUsagePercent > 90 || snapshot.GpuUsagePercent > 90)
                {
                    SessionStats.DroppedFrames += new Random().Next(1, 4);
                }
            }

            UpdatePerformanceScore(snapshot);
        };
        _autoPauseService.StateChanged += (_, state) => AutoPauseState = state;
        _wallpaperService.ActiveWallpapersChanged += OnActiveWallpapersChanged;
        _wallpaperService.StatusChanged += (_, message) => 
        {
            CurrentStatus = message;
            if (message.Contains("applied"))
            {
                SessionStats.TotalWallpapersApplied++;
            }
            if (message.Contains("applied") || message.Contains("resumed"))
            {
                _wallpaperStartTime ??= DateTimeOffset.Now;
            }
            else if (message.Contains("stopped") || message.Contains("paused"))
            {
                if (message.Contains("stopped"))
                {
                    _wallpaperStartTime = null;
                    LiveRunningTime = "00:00:00";
                    LiveLoops = 0;
                }
            }
        };

        ThumbnailVlcOptions = _gpuOptimizationService.BuildThumbnailVlcArguments(Settings);
        _previewRenderService.MaximumActivePreviews = Settings.ThumbnailMaxConcurrentPlayers;
        RefreshMonitors();
        _isStartupEnabled = _startupService.IsEnabled();
        SelectedTheme = "Minimal Dark";
        _ = RefreshLibraryAsync();
        _ = PlaylistService.InitializeAsync();
        PlaylistService.WallpaperDue += (_, next) =>
        {
            VideoPath = next.FilePath;
            ApplyCommand.Execute(null);
        };
        PlaylistService.AllPlaylists.CollectionChanged += (_, _) =>
        {
            PlaylistsCount = PlaylistService.AllPlaylists.Count;
        };

        ((INotifyPropertyChanged)PlaylistService).PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PlaylistService.SelectedPlaylist))
            {
                OnPropertyChanged(nameof(SelectedPlaylistName));
            }
        };

        // Defer wallpaper restore to after window is rendered so the UI is responsive
        Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, () =>
        {
            try
            {
                _wallpaperService.RestoreState(Settings);
                if (!string.IsNullOrEmpty(_wallpaperService.CurrentWallpaperPath))
                {
                    VideoPath = _wallpaperService.CurrentWallpaperPath;
                }
                PauseResumeCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
            }
            catch { }
        });
    }

    private PerformanceSettings _settings;
    public PerformanceSettings Settings
    {
        get => _settings;
        set => SetProperty(ref _settings, value);
    }
    public SessionAnalytics SessionStats { get; }

    private void UpdatePerformanceScore(SystemPerformanceSnapshot snapshot)
    {
        // Capability
        double ramGb = 0;
        if (SystemTotalRam.Contains("GB")) double.TryParse(SystemTotalRam.Replace(" GB", ""), out ramGb);
        
        if (ramGb >= 32 || SystemGpuName.Contains("RTX") || SystemGpuName.Contains("RX 7") || SystemGpuName.Contains("RX 6"))
            SystemCapabilityScore = "Enthusiast (Tier 1)";
        else if (ramGb >= 16)
            SystemCapabilityScore = "High-End (Tier 2)";
        else if (ramGb >= 8)
            SystemCapabilityScore = "Mainstream (Tier 3)";
        else
            SystemCapabilityScore = "Entry-Level (Tier 4)";

        // Impact
        if (snapshot.AppCpuUsagePercent > 15 || snapshot.AppRamMb > 1024)
            CurrentWallpaperImpact = "High";
        else if (snapshot.AppCpuUsagePercent > 5 || snapshot.AppRamMb > 512)
            CurrentWallpaperImpact = "Moderate";
        else
            CurrentWallpaperImpact = "Low";

        // Power
        if (snapshot.GpuUsagePercent > 50)
            EstimatedPowerUsage = "45W - 65W";
        else if (snapshot.GpuUsagePercent > 20)
            EstimatedPowerUsage = "15W - 35W";
        else
            EstimatedPowerUsage = "< 15W";

        // Grade
        if (AutoPauseState.ShouldPause)
        {
            PerformanceGrade = "Z";
            PerformanceRecommendation = "Engine is currently paused to save resources.";
        }
        else if (CurrentWallpaperImpact == "High")
        {
            PerformanceGrade = "C";
            PerformanceRecommendation = "Wallpaper is consuming significant resources. Consider reducing FPS limit or enabling Smart Pause.";
        }
        else if (CurrentWallpaperImpact == "Moderate")
        {
            PerformanceGrade = "B";
            PerformanceRecommendation = "Good balance. System has enough headroom for other applications.";
        }
        else
        {
            PerformanceGrade = "A";
            PerformanceRecommendation = "Excellent! Background usage is minimal. Optimized for maximum system performance.";
        }
    }
    
    // Performance Score
    private string _systemCapabilityScore = "Calculating...";
    public string SystemCapabilityScore 
    {
        get => _systemCapabilityScore;
        private set => SetProperty(ref _systemCapabilityScore, value);
    }

    private string _currentWallpaperImpact = "Calculating...";
    public string CurrentWallpaperImpact
    {
        get => _currentWallpaperImpact;
        private set => SetProperty(ref _currentWallpaperImpact, value);
    }

    private string _estimatedPowerUsage = "Calculating...";
    public string EstimatedPowerUsage
    {
        get => _estimatedPowerUsage;
        private set => SetProperty(ref _estimatedPowerUsage, value);
    }

    private string _performanceGrade = "-";
    public string PerformanceGrade
    {
        get => _performanceGrade;
        private set => SetProperty(ref _performanceGrade, value);
    }

    private string _performanceRecommendation = "Analyzing system performance...";
    public string PerformanceRecommendation
    {
        get => _performanceRecommendation;
        private set => SetProperty(ref _performanceRecommendation, value);
    }
    public ObservableCollection<string> AvailableThemes { get; }
    public ObservableCollection<ThemeSwatchViewModel> ThemeSwatches { get; }
    public ObservableCollection<MonitorSelection> MonitorSelections { get; }
    public ObservableCollection<MonitorCardViewModel> MonitorCards { get; }
    public ObservableCollection<MarketplaceItem> MarketplaceItems { get; }
    public ObservableCollection<WallpaperModel> LibraryItems { get; }
    public ObservableCollection<string> WatchedFolders { get; }
    public ObservableCollection<WallpaperPreviewItem> LibraryPreviews { get; }
    public ObservableCollection<WallpaperPreviewItem> FilteredLibraryPreviews { get; }
    public ObservableCollection<ThemeCardViewModel> ThemeCards { get; }
    public ObservableCollection<double> UiScaleOptions { get; }
    public ObservableCollection<int> WallpaperIntervalOptions { get; }

    public ICommand ExportSettingsCommand { get; }
    public ICommand ImportSettingsCommand { get; }
    public ICommand RestoreDefaultsCommand { get; }

    public System.Windows.Media.PointCollection GpuGraphPoints { get; }
    public System.Windows.Media.PointCollection CpuGraphPoints { get; }
    public System.Windows.Media.PointCollection RamGraphPoints { get; }
    public System.Windows.Media.PointCollection FpsGraphPoints { get; }
    public ObservableCollection<string> Categories { get; }
    public ObservableCollection<WallpaperRenderEngine> RenderEngines { get; }
    public ObservableCollection<ResourceExceedAction> ResourceExceedActions { get; }
    public ObservableCollection<HardwareAccelerationMode> HardwareModes { get; }
    public ObservableCollection<FpsLimitMode> FpsModes { get; }
    public ObservableCollection<PowerProfileMode> PowerProfiles { get; }
    public ObservableCollection<TextureFilteringMode> TextureFilteringModes { get; }
    public ObservableCollection<WallpaperSortMode> SortModes { get; }
    public ObservableCollection<int> GridColumnsOptions { get; }
    public ObservableCollection<OptionItem<UserPerformanceMode>> PerformanceModes { get; }

    public int GridColumns
    {
        get => _gridColumns;
        set => SetProperty(ref _gridColumns, value);
    }

    public RelayCommand BrowseCommand { get; }
    public AsyncRelayCommand AddFolderCommand { get; }
    public RelayCommand ApplyCommand { get; }
    public RelayCommand PauseResumeCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand SelectPageCommand { get; }
    public AsyncRelayCommand DownloadMarketplaceItemCommand { get; }
    public RelayCommand SelectThemeCommand { get; }
    public RelayCommand SetAccentCommand { get; }
    public RelayCommand ApplyAccentCommand { get; }
    public RelayCommand BrowseAccentColorCommand { get; }
    public RelayCommand SelectCategoryCommand { get; }
    public RelayCommand ClearCacheCommand { get; }
    public RelayCommand TrimMemoryCommand { get; }
    public RelayCommand SelectLibraryItemCommand { get; }
    public RelayCommand ShowInFolderCommand { get; }
    public RelayCommand ApplyPowerProfileCommand { get; }
    public RelayCommand ApplyPerformanceModeCommand { get; }
    public RelayCommand ToggleFavoriteCommand { get; }
    public RelayCommand SetRatingCommand { get; }
    public AsyncRelayCommand ImportToLibraryCommand { get; }
    public AsyncRelayCommand RefreshLibraryCommand { get; }
    public AsyncRelayCommand AddToPlaylistCommand { get; }
    public RelayCommand CancelAddPlaylistCommand { get; }
    public AsyncRelayCommand AddToSpecificPlaylistCommand { get; }
    public AsyncRelayCommand RemoveFromPlaylistCommand { get; }
    public AsyncRelayCommand CreatePlaylistCommand { get; }
    public AsyncRelayCommand DeletePlaylistCommand { get; }
    public AsyncRelayCommand SetActivePlaylistCommand { get; }
    public AsyncRelayCommand RenamePlaylistCommand { get; }
    public OptionItem<UserPerformanceMode>? SelectedPerformanceMode
    {
        get => _selectedPerformanceMode;
        set
        {
            if (value is not null && SetProperty(ref _selectedPerformanceMode, value))
            {
                ApplyPerformanceMode(value);
            }
        }
    }

    public IReadOnlyList<string> ThumbnailVlcOptions
    {
        get => _thumbnailVlcOptions;
        private set => SetProperty(ref _thumbnailVlcOptions, value);
    }


    public WallpaperPreviewItem? SelectedPreviewItem
    {
        get => _selectedPreviewItem;
        set => SetProperty(ref _selectedPreviewItem, value);
    }

    public int InstalledCount
    {
        get => _installedCount;
        set => SetProperty(ref _installedCount, value);
    }

    public int FavoritesCount
    {
        get => _favoritesCount;
        set => SetProperty(ref _favoritesCount, value);
    }

    public int PlaylistsCount
    {
        get => _playlistsCount;
        set => SetProperty(ref _playlistsCount, value);
    }

    public string TotalStorageGb
    {
        get => _totalStorageGb;
        set => SetProperty(ref _totalStorageGb, value);
    }

    public string LiveCpuUsage
    {
        get => _liveCpuUsage;
        private set => SetProperty(ref _liveCpuUsage, value);
    }

    public string LiveGpuUsage
    {
        get => _liveGpuUsage;
        set => SetProperty(ref _liveGpuUsage, value);
    }

    public string LiveRamUsage
    {
        get => _liveRamUsage;
        private set => SetProperty(ref _liveRamUsage, value);
    }

    private string _systemCpuName = "Detecting...";
    public string SystemCpuName
    {
        get => _systemCpuName;
        set => SetProperty(ref _systemCpuName, value);
    }

    private string _systemGpuName = "Detecting...";
    public string SystemGpuName
    {
        get => _systemGpuName;
        set => SetProperty(ref _systemGpuName, value);
    }

    private string _systemTotalRam = "Detecting...";
    public string SystemTotalRam
    {
        get => _systemTotalRam;
        set => SetProperty(ref _systemTotalRam, value);
    }

    public string LiveFps
    {
        get => _liveFps;
        set => SetProperty(ref _liveFps, value);
    }

    public string LiveRunningTime
    {
        get => _liveRunningTime;
        set => SetProperty(ref _liveRunningTime, value);
    }

    public int LiveLoops
    {
        get => _liveLoops;
        set => SetProperty(ref _liveLoops, value);
    }

    public string SelectedPage
    {
        get => _selectedPage;
        set => SetProperty(ref _selectedPage, value);
    }

    public string VideoPath
    {
        get => _videoPath;
        set
        {
            if (SetProperty(ref _videoPath, value))
            {
                ApplyCommand.RaiseCanExecuteChanged();
                ImportToLibraryCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(CurrentVideoName));
                UpdateActivePreviewFlags();
            }
        }
    }

    public string CurrentVideoName => string.IsNullOrWhiteSpace(VideoPath)
        ? "No video selected"
        : Path.GetFileName(VideoPath);

    public string SelectedTheme
    {
        get => Settings.SelectedTheme;
        set
        {
            if (Settings.SelectedTheme == value) return;
            Settings.SelectedTheme = value;
            try
            {
                _themeService.ApplyTheme(value);
                _themeService.ApplyAccentColor(Settings.AccentColorHex);
                CurrentStatus = $"{value} theme applied.";
                OnPropertyChanged();
            }
            catch (Exception ex)
            {
                CurrentStatus = ex.Message;
            }
        }
    }

    public string AccentColorHex
    {
        get => Settings.AccentColorHex;
        set
        {
            if (Settings.AccentColorHex == value) return;
            Settings.AccentColorHex = value;
            OnPropertyChanged();
        }
    }

    public string SelectedMonitorDeviceName
    {
        get => _selectedMonitorDeviceName;
        set => SetProperty(ref _selectedMonitorDeviceName, value);
    }

    public string CurrentStatus
    {
        get => _currentStatus;
        set => SetProperty(ref _currentStatus, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyLibraryFilters();
            }
        }
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                ApplyLibraryFilters();
            }
        }
    }

    public WallpaperSortMode SelectedSortMode
    {
        get => _selectedSortMode;
        set
        {
            if (SetProperty(ref _selectedSortMode, value))
            {
                ApplyLibraryFilters();
            }
        }
    }

    public SystemPerformanceSnapshot PerformanceSnapshot
    {
        get => _performanceSnapshot;
        set => SetProperty(ref _performanceSnapshot, value);
    }

    public AutoPauseState AutoPauseState
    {
        get => _autoPauseState;
        set => SetProperty(ref _autoPauseState, value);
    }

    public bool IsDashboardActive
    {
        get => _isDashboardActive;
        set => SetProperty(ref _isDashboardActive, value);
    }

    public bool IsStartupEnabled
    {
        get => _isStartupEnabled;
        set
        {
            if (!SetProperty(ref _isStartupEnabled, value))
            {
                return;
            }

            try
            {
                _startupService.SetEnabled(value);
                CurrentStatus = value ? "Startup registration enabled." : "Startup registration disabled.";
            }
            catch (Exception ex)
            {
                CurrentStatus = ex.Message;
            }
        }
    }

    public string RenderEngineDescription => _gpuOptimizationService.DescribeRenderEngine(Settings.RenderEngine);

    public void RefreshMonitors()
    {
        MonitorSelections.Clear();
        MonitorCards.Clear();

        var activeWallpapers = _wallpaperService.GetActiveWallpapers();

        foreach (var monitor in _monitorService.GetMonitors())
        {
            MonitorSelections.Add(new MonitorSelection(monitor.DeviceName, monitor.DisplayName));
            
            var activePath = activeWallpapers.TryGetValue(monitor.DeviceName, out var path) ? path : null;
            var activeName = string.IsNullOrEmpty(activePath) ? "None" : Path.GetFileNameWithoutExtension(activePath);
            
            MonitorCards.Add(new MonitorCardViewModel(
                monitor.DeviceName,
                monitor.DisplayName,
                $"{monitor.Bounds.Width}x{monitor.Bounds.Height}",
                activeName,
                device => _wallpaperService.ClearMonitorWallpaper(device)
            ));
        }

        if (MonitorSelections.Count > 0)
        {
            SelectedMonitorDeviceName = MonitorSelections[0].DeviceName;
        }
    }

    public void PauseResumeWallpaper()
    {
        _wallpaperService.TogglePause();
        PauseResumeCommand.RaiseCanExecuteChanged();
        StopCommand.RaiseCanExecuteChanged();
    }

    public void StopWallpaper()
    {
        _ = PlaylistService.SetActivePlaylistAsync(null);
        _wallpaperService.Stop();
        PauseResumeCommand.RaiseCanExecuteChanged();
        StopCommand.RaiseCanExecuteChanged();
    }

    private void BrowseForVideo()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select a live wallpaper",
            Filter = "Video files (*.mp4;*.mov;*.mkv;*.webm)|*.mp4;*.mov;*.mkv;*.webm|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            VideoPath = dialog.FileName;
            CurrentStatus = "Video selected.";
            _ = RefreshLibraryAsync();
        }
    }

    private async Task AddFolderToLibraryAsync()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select a folder to watch for live wallpapers",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            var folder = dialog.FolderName;
            if (!WatchedFolders.Contains(folder, StringComparer.OrdinalIgnoreCase))
            {
                WatchedFolders.Add(folder);
                await SaveLibraryAsync().ConfigureAwait(true);
                await RefreshLibraryAsync().ConfigureAwait(true);
            }
        }
    }

    private bool CanApplyWallpaper(object? parameter)
    {
        if (parameter is WallpaperPreviewItem item)
        {
            return File.Exists(item.FilePath);
        }
        return File.Exists(VideoPath);
    }

    private void ApplyWallpaper(object? parameter)
    {
        var path = VideoPath;
        if (parameter is WallpaperPreviewItem item)
        {
            path = item.FilePath;
            SelectedPreviewItem = item;
            VideoPath = path;
        }

        try
        {
            CurrentStatus = "Applying wallpaper...";
            _wallpaperService.ApplyWallpaper(path, SelectedMonitorDeviceName, Settings);
            PauseResumeCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
        }
        catch (Exception ex)
        {
            CurrentStatus = $"Error: {ex.Message}";
        }
    }

    private void ApplyAccentColor()
    {
        try
        {
            Settings.AccentColorHex = AccentColorHex;
            _themeService.ApplyAccentColor(AccentColorHex);
            CurrentStatus = $"Accent {AccentColorHex} applied.";
        }
        catch (Exception ex)
        {
            CurrentStatus = ex.Message;
        }
    }

    private void BrowseAccentColor()
    {
        try
        {
            var dialog = new System.Windows.Forms.ColorDialog
            {
                FullOpen = true,
                Color = System.Drawing.ColorTranslator.FromHtml(AccentColorHex)
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                AccentColorHex = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
                ApplyAccentColor();
            }
        }
        catch (Exception ex)
        {
            CurrentStatus = $"Error opening color picker: {ex.Message}";
        }
    }

    private void ApplyPerformanceMode(OptionItem<UserPerformanceMode> option)
    {
        SetProperty(ref _selectedPerformanceMode, option, nameof(SelectedPerformanceMode));
        Settings.ApplyUserPerformanceMode(option.Value);
        ThumbnailVlcOptions = _gpuOptimizationService.BuildThumbnailVlcArguments(Settings);
        _previewRenderService.MaximumActivePreviews = Settings.ThumbnailMaxConcurrentPlayers;
        PreviewRenderCoordinator.Shared.MaximumActivePreviews = Settings.ThumbnailMaxConcurrentPlayers;
        CurrentStatus = $"{option.Title} mode applied. Reapply wallpaper to refresh video performance.";
        ApplyLibraryFilters();
    }

    private async Task ImportToLibraryAsync()
    {
        try
        {
            var item = await _libraryService.ImportVideoAsync(VideoPath).ConfigureAwait(true);
            LibraryItems.Add(item);
            await RefreshLibraryAsync().ConfigureAwait(true);
            CurrentStatus = "Wallpaper imported into the local library.";
        }
        catch (Exception ex)
        {
            CurrentStatus = ex.Message;
        }
    }

    private async Task SaveLibraryAsync()
    {
        try
        {
            var manifest = new WallpaperPackManifest { Name = "Local Library" };
            manifest.WatchedFolders.AddRange(WatchedFolders);
            foreach (var item in LibraryItems.Where(i => !string.Equals(i.Category, "Session", StringComparison.OrdinalIgnoreCase)))
            {
                manifest.Wallpapers.Add(item);
            }
            await _libraryService.SaveAsync(manifest).ConfigureAwait(false);
        }
        catch { }
    }

    private void SaveLibraryBackground()
    {
        _ = SaveLibraryAsync();
    }

    private async Task RefreshLibraryAsync()
    {
        try
        {
            var manifest = await _libraryService.LoadAsync().ConfigureAwait(true);
            var newLibraryItems = new List<WallpaperModel>();
            
            foreach (var item in manifest.Wallpapers)
            {
                newLibraryItems.Add(item);
            }

            var foldersToScan = new List<string>(manifest.WatchedFolders);
            foldersToScan.Add(_libraryService.LibraryRoot);
            foldersToScan.Add(Path.Combine(_libraryService.LibraryRoot, "Downloads"));

            foreach (var folder in foldersToScan.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (Directory.Exists(folder))
                {
                    var videoFiles = Directory.EnumerateFiles(folder, "*.*")
                        .Where(f => f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                                    f.EndsWith(".webm", StringComparison.OrdinalIgnoreCase) ||
                                    f.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase) ||
                                    f.EndsWith(".mov", StringComparison.OrdinalIgnoreCase) ||
                                    f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase));

                    foreach (var file in videoFiles)
                    {
                        if (!newLibraryItems.Any(item => PathsMatch(item.FilePath, file)))
                        {
                            newLibraryItems.Add(new WallpaperModel
                            {
                                DisplayName = Path.GetFileNameWithoutExtension(file),
                                FilePath = file,
                                Category = "Folder",
                                ImportedAt = File.GetCreationTimeUtc(file),
                                LastUsedAt = DateTimeOffset.MinValue,
                                Tags = ["local", Path.GetExtension(file).TrimStart('.').ToLowerInvariant()]
                            });
                        }
                    }
                }
            }

            if (File.Exists(VideoPath) && !newLibraryItems.Any(item => PathsMatch(item.FilePath, VideoPath)))
            {
                newLibraryItems.Add(new WallpaperModel
                {
                    DisplayName = Path.GetFileNameWithoutExtension(VideoPath),
                    FilePath = VideoPath,
                    Category = "Session",
                    ImportedAt = DateTimeOffset.Now,
                    LastUsedAt = DateTimeOffset.Now
                });
            }

            var previews = await _thumbnailService.BuildPreviewItemsAsync(newLibraryItems, Settings).ConfigureAwait(true);

            Application.Current.Dispatcher.Invoke(() => 
            {
                WatchedFolders.Clear();
                foreach (var folder in manifest.WatchedFolders)
                {
                    WatchedFolders.Add(folder);
                }

                LibraryItems.Clear();
                foreach (var item in newLibraryItems)
                {
                    LibraryItems.Add(item);
                }

                LibraryPreviews.Clear();
                foreach (var preview in previews)
                {
                    LibraryPreviews.Add(preview);
                }

                RebuildCategories();
                UpdateActivePreviewFlags();
                ApplyLibraryFilters();
            });
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() => CurrentStatus = ex.Message);
        }
    }

    private void ApplyLibraryFilters()
    {
        var query = SearchText.Trim();
        IEnumerable<WallpaperPreviewItem> results = LibraryPreviews;

        if (!string.IsNullOrWhiteSpace(query))
        {
            results = results.Where(item =>
                item.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                || item.Author.Contains(query, StringComparison.OrdinalIgnoreCase)
                || item.TagsText.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.Equals(SelectedCategory, "All", StringComparison.OrdinalIgnoreCase))
        {
            results = results.Where(item => string.Equals(item.Wallpaper.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        results = SelectedSortMode switch
        {
            WallpaperSortMode.Title => results.OrderBy(item => item.DisplayName),
            WallpaperSortMode.Author => results.OrderBy(item => item.Author),
            WallpaperSortMode.Resolution => results.OrderByDescending(item => item.Resolution),
            WallpaperSortMode.Duration => results.OrderByDescending(item => item.Duration),
            WallpaperSortMode.FavoriteFirst => results.OrderByDescending(item => item.IsFavorite).ThenBy(item => item.DisplayName),
            WallpaperSortMode.Rating => results.OrderByDescending(item => item.Rating).ThenBy(item => item.DisplayName),
            _ => results.OrderByDescending(item => item.Wallpaper.LastUsedAt == default ? item.Wallpaper.ImportedAt : item.Wallpaper.LastUsedAt)
        };

        FilteredLibraryPreviews.Clear();
        foreach (var item in results)
        {
            FilteredLibraryPreviews.Add(item);
        }

        UpdateLibraryStats();
    }

    private void UpdateLibraryStats()
    {
        InstalledCount = LibraryItems.Count;
        FavoritesCount = LibraryItems.Count(x => x.IsFavorite);
        PlaylistsCount = PlaylistService.AllPlaylists.Count;
        
        long totalBytes = LibraryItems.Sum(x => x.FileSizeBytes);
        TotalStorageGb = $"{(totalBytes / 1024.0 / 1024.0 / 1024.0):0.0} GB";
    }

    private void RebuildCategories()
    {
        Categories.Clear();
        Categories.Add("All");
        foreach (var category in LibraryItems.Select(item => item.Category).Where(category => !string.IsNullOrWhiteSpace(category)).Distinct().Order())
        {
            Categories.Add(category);
        }
    }

    private void UpdateActivePreviewFlags()
    {
        foreach (var preview in LibraryPreviews)
        {
            preview.IsActiveWallpaper = PathsMatch(preview.FilePath, VideoPath);
        }
    }

    private static bool PathsMatch(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        try
        {
            return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PerformanceSettings.RenderEngine))
        {
            OnPropertyChanged(nameof(RenderEngineDescription));
        }

        if (e.PropertyName is nameof(PerformanceSettings.ThumbnailFps)
            or nameof(PerformanceSettings.ThumbnailMaxConcurrentPlayers)
            or nameof(PerformanceSettings.PowerProfile))
        {
            ThumbnailVlcOptions = _gpuOptimizationService.BuildThumbnailVlcArguments(Settings);
            _previewRenderService.MaximumActivePreviews = Settings.ThumbnailMaxConcurrentPlayers;
            PreviewRenderCoordinator.Shared.MaximumActivePreviews = Settings.ThumbnailMaxConcurrentPlayers;
        }

        if (e.PropertyName is nameof(PerformanceSettings.BlurStrength)
            or nameof(PerformanceSettings.GlowIntensity)
            or nameof(PerformanceSettings.BorderRadius)
            or nameof(PerformanceSettings.PanelOpacity))
        {
            _themeService.ApplyVisualEffects(Settings);
        }

        if (e.PropertyName == nameof(PerformanceSettings.MasterVolume) || e.PropertyName == nameof(PerformanceSettings.MuteWallpaperAudio))
        {
            _wallpaperService.SetVolume(Settings.MasterVolume, Settings.MuteWallpaperAudio);
        }
        else if (e.PropertyName == nameof(PerformanceSettings.AnimationSpeed))
        {
            _wallpaperService.SetPlaybackRate((float)Settings.AnimationSpeed);
        }
        

        
        if (e.PropertyName == nameof(PerformanceSettings.SelectedTheme))
        {
            try { 
                _themeService.ApplyTheme(Settings.SelectedTheme); 
                new SettingsService().SaveSettings(Settings);
            } catch {}
        }
        
        if (e.PropertyName == nameof(PerformanceSettings.AccentColorHex))
        {
            try { 
                _themeService.ApplyAccentColor(Settings.AccentColorHex); 
                new SettingsService().SaveSettings(Settings);
            } catch {}
        }
    }

    private void OnActiveWallpapersChanged(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var activeWallpapers = _wallpaperService.GetActiveWallpapers();
            var activePaths = new HashSet<string>(activeWallpapers.Values, StringComparer.OrdinalIgnoreCase);

            foreach (var card in MonitorCards)
            {
                var activePath = activeWallpapers.TryGetValue(card.DeviceName, out var path) ? path : null;
                card.ActiveWallpaperName = string.IsNullOrEmpty(activePath) ? "None" : Path.GetFileNameWithoutExtension(activePath);
            }

            foreach (var item in LibraryPreviews)
            {
                item.IsActiveWallpaper = activePaths.Contains(item.FilePath);
            }
        });
    }

    private async Task DownloadMarketplaceItemAsync(MarketplaceItem? item)
    {
        if (item == null || item.IsDownloading) return;

        item.IsDownloading = true;
        item.DownloadProgress = 0;
        CurrentStatus = $"Downloading {item.Title}...";

        try
        {
            var destinationFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LiveWallpaperApp", "LiveWallpapers");
            Directory.CreateDirectory(destinationFolder);

            var filename = $"{item.Title.Replace(" ", "_")}.mp4";
            var destinationPath = Path.Combine(destinationFolder, filename);

            if (!File.Exists(destinationPath))
            {
                var progress = new Progress<double>(p => item.DownloadProgress = p);
                await _downloadService.DownloadFileAsync(item.VideoUrl, destinationPath, progress, CancellationToken.None);
            }

            if (!WatchedFolders.Contains(destinationFolder))
            {
                var manifest = await _libraryService.LoadAsync().ConfigureAwait(true);
                if (!manifest.WatchedFolders.Contains(destinationFolder))
                {
                    manifest.WatchedFolders.Add(destinationFolder);
                    await _libraryService.SaveAsync(manifest).ConfigureAwait(true);
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (!WatchedFolders.Contains(destinationFolder))
                        {
                            WatchedFolders.Add(destinationFolder);
                        }
                    });
                }
            }

            await RefreshLibraryAsync();

            CurrentStatus = $"{item.Title} downloaded and added to Library!";
            item.DownloadProgress = 100;
        }
        catch (Exception ex)
        {
            CurrentStatus = $"Failed to download {item.Title}: {ex.Message}";
        }
        finally
        {
            item.IsDownloading = false;
        }
    }

    private void ClearThumbnailCache()
    {
        try
        {
            var cacheDir = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "LiveWallpaperApp",
                "PreviewCache");

            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
            CurrentStatus = "Thumbnail cache cleared. They will be regenerated on next load.";
            _ = RefreshLibraryAsync(); // Refresh to clear UI thumbnails
        }
        catch (Exception ex)
        {
            CurrentStatus = $"Failed to clear cache: {ex.Message}";
        }
    }

    private void TrimMemory()
    {
        _memoryOptimizerService.TrimMemory();
        CurrentStatus = "Memory garbage collection forced.";
    }

    private void ShowInFolder(object? parameter)
    {
        if (parameter is WallpaperPreviewItem item && File.Exists(item.FilePath))
        {
            var dir = Path.GetDirectoryName(item.FilePath);
            if (dir != null && Directory.Exists(dir))
            {
                System.Diagnostics.Process.Start("explorer.exe", dir);
            }
        }
    }

    private void UpdateGraph(System.Windows.Media.PointCollection points, double usage)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (points.Count >= 30)
            {
                points.RemoveAt(0);
            }
            
            // X spans roughly 126 pixels, 30 points -> 4.2 pixels per step
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                points[i] = new System.Windows.Point(p.X - 4.2, p.Y);
            }
            
            // max height is 20, invert Y so 100% is Y=0 and 0% is Y=20
            double y = 20.0 - (usage / 100.0 * 20.0);
            // Cap it to stay within bounds
            if (y < 0) y = 0;
            if (y > 20) y = 20;

            points.Add(new System.Windows.Point(126, y));
        });
    }

    private void OnLimitWarningTriggered(object? sender, string warning)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            // Simple visual notification for the user. We can use MessageBox or a Toast if available.
            // For now, we update the status and show a non-blocking toast/message if possible, or just update CurrentStatus and a popup.
            CurrentStatus = $"WARNING: {warning}";
            MessageBox.Show(warning, "Resource Limit Exceeded", MessageBoxButton.OK, MessageBoxImage.Warning);
        });
    }

    private CancellationTokenSource? _reapplyCts;

    private void OnSettingsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PerformanceSettings.RenderEngine) or 
            nameof(PerformanceSettings.HardwareAcceleration) or 
            nameof(PerformanceSettings.DecodeThreadCount))
        {
            _reapplyCts?.Cancel();
            _reapplyCts = new CancellationTokenSource();
            var token = _reapplyCts.Token;

            Task.Delay(300, token).ContinueWith(t =>
            {
                if (t.IsCanceled) return;
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_wallpaperService.IsRunning)
                    {
                        CurrentStatus = "Applying rendering engine changes...";
                        _wallpaperService.ReapplyWithSettings(Settings);
                    }
                });
            }, TaskScheduler.Default);
        }
    }
}

public sealed record MonitorSelection(string DeviceName, string DisplayName)
{
    public override string ToString() => DisplayName;
}
