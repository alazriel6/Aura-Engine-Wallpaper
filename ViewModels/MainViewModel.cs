using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using LiveWallpaperApp.Helpers;
using LiveWallpaperApp.Models;
using LiveWallpaperApp.Services;

namespace LiveWallpaperApp.ViewModels;

public sealed class MainViewModel : ObservableObject
{
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

    private string _selectedPage = "Dashboard";
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
    private readonly System.Windows.Threading.DispatcherTimer _shuffleTimer;

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
        Settings = settings;

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
        Categories = new ObservableCollection<string>(["All"]);
        RenderEngines = new ObservableCollection<WallpaperRenderEngine>([WallpaperRenderEngine.DirectX]);
        HardwareModes = new ObservableCollection<HardwareAccelerationMode>(Enum.GetValues<HardwareAccelerationMode>());
        FpsModes = new ObservableCollection<FpsLimitMode>(Enum.GetValues<FpsLimitMode>());
        PowerProfiles = new ObservableCollection<PowerProfileMode>(Enum.GetValues<PowerProfileMode>());
        TextureFilteringModes = new ObservableCollection<TextureFilteringMode>(Enum.GetValues<TextureFilteringMode>());
        SortModes = new ObservableCollection<WallpaperSortMode>(Enum.GetValues<WallpaperSortMode>());
        PerformanceModes = new ObservableCollection<OptionItem<UserPerformanceMode>>
        {
            new(UserPerformanceMode.UltraSmooth, "Ultra Smooth", "Best motion and visuals. Uses more GPU."),
            new(UserPerformanceMode.Balanced, "Balanced", "Recommended. Smooth wallpaper with low resource use."),
            new(UserPerformanceMode.PowerSaver, "Power Saver", "Lower background usage for laptops."),
            new(UserPerformanceMode.GamingMode, "Gaming Mode", "Pauses while gaming and keeps memory low.")
        };
        _selectedPerformanceMode = PerformanceModes.First(option => option.Value == UserPerformanceMode.Balanced);

        BrowseCommand = new RelayCommand(BrowseForVideo);
        AddFolderCommand = new AsyncRelayCommand(AddFolderToLibraryAsync);
        ApplyCommand = new RelayCommand(ApplyWallpaper, CanApplyWallpaper);
        PauseResumeCommand = new RelayCommand(PauseResumeWallpaper, () => _wallpaperService.IsRunning);
        StopCommand = new RelayCommand(StopWallpaper, () => _wallpaperService.IsRunning);
        SelectPageCommand = new RelayCommand(parameter => SelectedPage = parameter?.ToString() ?? "Dashboard");
        SelectThemeCommand = new RelayCommand(parameter => SelectedTheme = parameter?.ToString() ?? SelectedTheme);
        SetAccentCommand = new RelayCommand(parameter =>
        {
            AccentColorHex = parameter?.ToString() ?? AccentColorHex;
            ApplyAccentColor();
        });
        ApplyAccentCommand = new RelayCommand(ApplyAccentColor);
        DownloadMarketplaceItemCommand = new AsyncRelayCommand(parameter => DownloadMarketplaceItemAsync(parameter as MarketplaceItem));
        ClearCacheCommand = new RelayCommand(ClearThumbnailCache);
        TrimMemoryCommand = new RelayCommand(TrimMemory);
        ImportToLibraryCommand = new AsyncRelayCommand(ImportToLibraryAsync, () => File.Exists(VideoPath));
        RefreshLibraryCommand = new AsyncRelayCommand(RefreshLibraryAsync);
        SelectLibraryItemCommand = new RelayCommand(parameter =>
        {
            if (parameter is not WallpaperPreviewItem preview)
            {
                return;
            }

            VideoPath = preview.FilePath;
            preview.Wallpaper.LastUsedAt = DateTimeOffset.Now;
            SelectedPage = "Dashboard";
            CurrentStatus = $"Loaded {preview.DisplayName} from the library.";
            ApplyLibraryFilters();
            UpdateActivePreviewFlags();
        });
        ApplyPowerProfileCommand = new RelayCommand(parameter =>
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

        Settings.PropertyChanged += OnSettingsChanged;
        _wallpaperService.StatusChanged += (_, message) => CurrentStatus = message;
        _performanceService.SnapshotUpdated += (_, snapshot) => PerformanceSnapshot = snapshot;
        _autoPauseService.StateChanged += (_, state) => AutoPauseState = state;
        _wallpaperService.ActiveWallpapersChanged += OnActiveWallpapersChanged;

        ThumbnailVlcOptions = _gpuOptimizationService.BuildThumbnailVlcArguments(Settings);
        _shuffleTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(5)
        };
        _shuffleTimer.Tick += OnShuffleTimerTick;
        if (Settings.AutoShuffle)
        {
            _shuffleTimer.Start();
        }
        _previewRenderService.MaximumActivePreviews = Settings.ThumbnailMaxConcurrentPlayers;
        RefreshMonitors();
        _isStartupEnabled = _startupService.IsEnabled();
        SelectedTheme = "Minimal Dark";
        _ = RefreshLibraryAsync();

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

    public PerformanceSettings Settings { get; }
    public ObservableCollection<string> AvailableThemes { get; }
    public ObservableCollection<ThemeSwatchViewModel> ThemeSwatches { get; }
    public ObservableCollection<MonitorSelection> MonitorSelections { get; }
    public ObservableCollection<MonitorCardViewModel> MonitorCards { get; }
    public ObservableCollection<MarketplaceItem> MarketplaceItems { get; }
    public ObservableCollection<WallpaperModel> LibraryItems { get; }
    public ObservableCollection<string> WatchedFolders { get; }
    public ObservableCollection<WallpaperPreviewItem> LibraryPreviews { get; }
    public ObservableCollection<WallpaperPreviewItem> FilteredLibraryPreviews { get; }
    public ObservableCollection<string> Categories { get; }
    public ObservableCollection<WallpaperRenderEngine> RenderEngines { get; }
    public ObservableCollection<HardwareAccelerationMode> HardwareModes { get; }
    public ObservableCollection<FpsLimitMode> FpsModes { get; }
    public ObservableCollection<PowerProfileMode> PowerProfiles { get; }
    public ObservableCollection<TextureFilteringMode> TextureFilteringModes { get; }
    public ObservableCollection<WallpaperSortMode> SortModes { get; }
    public ObservableCollection<OptionItem<UserPerformanceMode>> PerformanceModes { get; }

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
    public RelayCommand ClearCacheCommand { get; }
    public RelayCommand TrimMemoryCommand { get; }
    public RelayCommand SelectLibraryItemCommand { get; }
    public RelayCommand ApplyPowerProfileCommand { get; }
    public RelayCommand ToggleFavoriteCommand { get; }
    public AsyncRelayCommand ImportToLibraryCommand { get; }
    public AsyncRelayCommand RefreshLibraryCommand { get; }
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
        MonitorSelections.Add(new MonitorSelection("*", "All displays"));

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

        SelectedMonitorDeviceName = "*";
    }

    public void PauseResumeWallpaper()
    {
        _wallpaperService.TogglePause();
        PauseResumeCommand.RaiseCanExecuteChanged();
        StopCommand.RaiseCanExecuteChanged();
    }

    public void StopWallpaper()
    {
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

    private bool CanApplyWallpaper()
    {
        return File.Exists(VideoPath);
    }

    private void ApplyWallpaper()
    {
        try
        {
            CurrentStatus = "Applying wallpaper...";
            _wallpaperService.ApplyWallpaper(VideoPath, SelectedMonitorDeviceName, Settings);
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
            _themeService.ApplyAccentColor(AccentColorHex);
            CurrentStatus = $"Accent {AccentColorHex} applied.";
        }
        catch (Exception ex)
        {
            CurrentStatus = ex.Message;
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
            
            Application.Current.Dispatcher.Invoke(() => 
            {
                WatchedFolders.Clear();
                foreach (var folder in manifest.WatchedFolders)
                {
                    WatchedFolders.Add(folder);
                }
            });

            LibraryItems.Clear();
            foreach (var item in manifest.Wallpapers)
            {
                LibraryItems.Add(item);
            }

            foreach (var folder in manifest.WatchedFolders)
            {
                if (Directory.Exists(folder))
                {
                    var videoFiles = Directory.EnumerateFiles(folder, "*.*")
                        .Where(f => f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                                    f.EndsWith(".webm", StringComparison.OrdinalIgnoreCase) ||
                                    f.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase) ||
                                    f.EndsWith(".mov", StringComparison.OrdinalIgnoreCase));

                    foreach (var file in videoFiles)
                    {
                        if (!LibraryItems.Any(item => PathsMatch(item.FilePath, file)))
                        {
                            LibraryItems.Add(new WallpaperModel
                            {
                                DisplayName = Path.GetFileNameWithoutExtension(file),
                                FilePath = file,
                                Category = "Folder",
                                ImportedAt = File.GetCreationTimeUtc(file),
                                LastUsedAt = DateTimeOffset.MinValue
                            });
                        }
                    }
                }
            }

            if (File.Exists(VideoPath) && !LibraryItems.Any(item => PathsMatch(item.FilePath, VideoPath)))
            {
                LibraryItems.Add(new WallpaperModel
                {
                    DisplayName = Path.GetFileNameWithoutExtension(VideoPath),
                    FilePath = VideoPath,
                    Category = "Session",
                    ImportedAt = DateTimeOffset.Now,
                    LastUsedAt = DateTimeOffset.Now
                });
            }

            var previews = await _thumbnailService.BuildPreviewItemsAsync(LibraryItems, Settings).ConfigureAwait(true);
            LibraryPreviews.Clear();
            foreach (var preview in previews)
            {
                LibraryPreviews.Add(preview);
            }

            RebuildCategories();
            UpdateActivePreviewFlags();
            ApplyLibraryFilters();
        }
        catch (Exception ex)
        {
            CurrentStatus = ex.Message;
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
            _ => results.OrderByDescending(item => item.Wallpaper.LastUsedAt == default ? item.Wallpaper.ImportedAt : item.Wallpaper.LastUsedAt)
        };

        FilteredLibraryPreviews.Clear();
        foreach (var item in results)
        {
            FilteredLibraryPreviews.Add(item);
        }
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

        if (e.PropertyName == nameof(PerformanceSettings.AutoShuffle))
        {
            if (Settings.AutoShuffle) _shuffleTimer.Start();
            else _shuffleTimer.Stop();
        }
    }

    private void OnShuffleTimerTick(object? sender, EventArgs e)
    {
        if (!Settings.AutoShuffle || !LibraryItems.Any()) return;
        var next = LibraryItems[Random.Shared.Next(LibraryItems.Count)];
        VideoPath = next.FilePath;
        ApplyCommand.Execute(null);
    }

    private void OnActiveWallpapersChanged(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var activeWallpapers = _wallpaperService.GetActiveWallpapers();
            foreach (var card in MonitorCards)
            {
                var activePath = activeWallpapers.TryGetValue(card.DeviceName, out var path) ? path : null;
                card.ActiveWallpaperName = string.IsNullOrEmpty(activePath) ? "None" : Path.GetFileNameWithoutExtension(activePath);
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
            var destinationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "LiveWallpapers");
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
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LiveWallpaperApp", "Thumbnails");

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
}

public sealed record MonitorSelection(string DeviceName, string DisplayName)
{
    public override string ToString() => DisplayName;
}
