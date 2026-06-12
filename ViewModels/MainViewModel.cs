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

    private string _selectedPage = "Dashboard";
    private string _videoPath = string.Empty;
    private string _selectedTheme = "Minimal Dark";
    private string _accentColorHex = "#33F5FF";
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
        Settings = settings;

        AvailableThemes = new ObservableCollection<string>(_themeService.AvailableThemes);
        MonitorSelections = new ObservableCollection<MonitorSelection>();
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

        ThumbnailVlcOptions = _gpuOptimizationService.BuildThumbnailVlcArguments(Settings);
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
    public ObservableCollection<MonitorSelection> MonitorSelections { get; }
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
    public RelayCommand SelectThemeCommand { get; }
    public RelayCommand SetAccentCommand { get; }
    public RelayCommand ApplyAccentCommand { get; }
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
        get => _selectedTheme;
        set
        {
            if (!SetProperty(ref _selectedTheme, value))
            {
                return;
            }

            try
            {
                _themeService.ApplyTheme(value);
                _themeService.ApplyAccentColor(AccentColorHex);
                CurrentStatus = $"{value} theme applied.";
            }
            catch (Exception ex)
            {
                CurrentStatus = ex.Message;
            }
        }
    }

    public string AccentColorHex
    {
        get => _accentColorHex;
        set => SetProperty(ref _accentColorHex, value);
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
        MonitorSelections.Add(new MonitorSelection("*", "All displays"));

        foreach (var monitor in _monitorService.GetMonitors())
        {
            MonitorSelections.Add(new MonitorSelection(monitor.DeviceName, monitor.DisplayName));
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
    }
}

public sealed record MonitorSelection(string DeviceName, string DisplayName)
{
    public override string ToString() => DisplayName;
}
