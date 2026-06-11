# LiveWallpaperApp

A production-oriented WPF live wallpaper shell built with .NET 8, LibVLCSharp, VideoLAN LibVLC, WorkerW
desktop embedding, dynamic themes, tray integration, startup registration, and multi-monitor rendering.

## Project Structure

```text
LiveWallpaperApp/
├── App.xaml
├── App.xaml.cs
├── Views/
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── WallpaperWindow.xaml
│   └── WallpaperWindow.xaml.cs
├── ViewModels/
│   └── MainViewModel.cs
├── Services/
│   ├── WallpaperService.cs
│   ├── ThemeService.cs
│   ├── TrayService.cs
│   ├── StartupService.cs
│   ├── MonitorService.cs
│   ├── PlaylistService.cs
│   ├── SchedulerService.cs
│   └── WallpaperLibraryService.cs
├── Native/
│   └── Win32.cs
├── Themes/
│   ├── DarkTheme.xaml
│   ├── NeonTheme.xaml
│   └── PurpleTheme.xaml
├── Models/
│   ├── WallpaperModel.cs
│   ├── MonitorInfo.cs
│   ├── WallpaperPlaylist.cs
│   ├── WallpaperScheduleProfile.cs
│   └── WallpaperPackManifest.cs
├── Helpers/
│   ├── ObservableObject.cs
│   ├── RelayCommand.cs
│   ├── AsyncRelayCommand.cs
│   └── PageVisibilityConverter.cs
├── Config/
│   └── wallpaper.config.sample.json
├── Docs/
│   └── Architecture.md
└── Assets/
```

## NuGet Packages

The project pins these packages:

```powershell
dotnet add package LibVLCSharp.WPF --version 3.9.7.1
dotnet add package VideoLAN.LibVLC.Windows --version 3.0.23.1
dotnet add package Hardcodet.NotifyIcon.Wpf --version 2.0.1
dotnet add package LibreHardwareMonitorLib --version 0.9.6
```

They are already included in `LiveWallpaperApp.csproj`.

## Visual Studio Setup

1. Install Visual Studio 2022 or newer with the `.NET desktop development` workload.
2. Open `LiveWallpaperApp.csproj`.
3. Confirm the target framework is `net8.0-windows`.
4. Restore NuGet packages.
5. Build for `x64` when testing VLC native runtime loading.
6. Run the app, choose an MP4, select `All displays` or a specific display, and click `Apply Wallpaper`.

## CLI Setup

```powershell
cd D:\App\LiveWallpaperApp
dotnet restore
dotnet build -c Debug
dotnet run -c Debug
```

For release:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

## How It Works

`App.xaml.cs` calls `Core.Initialize()` before any VLC view is created.

`WallpaperService` creates one `WallpaperWindow` per target monitor. It locates the blank WorkerW desktop
layer through `Win32.EnsureWorkerW()`, then calls `SetParent` so each WPF/VLC renderer becomes a desktop
child window behind icons.

`WallpaperWindow` owns `LibVLC`, `MediaPlayer`, and `Media`. It applies no-audio looping media options,
stretches the VLC render surface to the monitor, and disposes all native VLC resources on close. It uses
manual window placement instead of WPF `WindowState=Maximized` because WPF rejects maximized windows that
also use `ShowActivated=false`; the service still makes the renderer fullscreen with monitor bounds and
`MoveWindow`.

`MainWindow` is the control dashboard. It never renders wallpaper video directly. It handles the borderless
premium shell, custom title bar, sidebar navigation, video selection, dynamic themes, accent color switching,
library import, startup toggle, tray behavior, and drag/drop.

## WorkerW Details

Windows Explorer owns the desktop. The visible icon layer is hosted by `SHELLDLL_DefView`. Sending message
`0x052C` to `Progman` causes Explorer to create a blank `WorkerW` sibling layer. Attaching the wallpaper
window to that layer keeps Explorer functional, preserves icon selection, avoids taskbar overlap, and prevents
the wallpaper renderer from becoming a foreground application.

## Dynamic Themes

Themes are standard WPF `ResourceDictionary` files:

- `DarkTheme.xaml`
- `NeonTheme.xaml`
- `PurpleTheme.xaml`

The UI uses `DynamicResource` for background, panel, border, text, accent, glow, and preview resources.
`ThemeService.ApplyTheme()` swaps the dictionary at runtime, and `ApplyAccentColor()` updates accent brushes
without restarting the dashboard.

## Tray and Startup

`TrayService` uses `Hardcodet.NotifyIcon.Wpf` to expose:

- Open dashboard
- Pause / resume wallpaper
- Stop wallpaper
- Exit

`StartupService` writes to:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

The startup command includes `--minimized`; `MainWindow` reads that flag and starts hidden in the tray.

## Debugging Tips

- If the wallpaper appears above icons, restart Explorer and test `Win32.EnsureWorkerW()` again.
- If VLC fails to initialize, confirm `VideoLAN.LibVLC.Windows` restored and the app is running as x64.
- If restore fails behind a proxy, configure NuGet credentials or a corporate package source.
- If videos flicker at loop points, prefer MP4 files encoded with identical first/last frames and no long GOP
  boundary at the loop.
- If DPI placement is wrong on mixed-DPI monitors, keep `ApplicationHighDpiMode` set to `PerMonitorV2` and
  verify monitor bounds with `System.Windows.Forms.Screen.AllScreens`.
- If CPU usage is high, use H.264/H.265 files that can be decoded by D3D11 hardware acceleration.

## Advanced Roadmap

- Steam Workshop-like library: package manifests, thumbnails, author metadata, tags, ratings, and import/export.
- Playlist mode: use `PlaylistService` to cycle wallpapers on timed intervals with optional shuffle.
- Scheduler: use `SchedulerService` to select morning, daytime, evening, and night wallpapers.
- Animated thumbnail grid: `ThumbnailService` creates low-bitrate previews when FFmpeg is available and
  `AnimatedThumbnailControl` unloads hidden VLC players to keep GPU/RAM bounded.
- Smart auto-pause: `AutoPauseService` pauses for fullscreen apps, battery state, high load, high
  temperature, process rules, and user idle time.
- Resource monitor: `PerformanceService` samples process CPU/RAM, system RAM, and LibreHardwareMonitor
  CPU/GPU/VRAM sensors when available.
- Lightweight preview mode: the dashboard has one live preview panel, and library cards only start VLC when
  hovered and when the global preview budget allows it. Balanced mode defaults to one active card preview.
- Lower RAM preview runtime: dashboard and library previews share one LibVLC runtime, and multi-monitor wallpaper
  windows share one LibVLC runtime per applied wallpaper.
- Web wallpapers: add WebView2 renderer windows and parent them through the same WorkerW native path.
- Audio reactive effects: capture WASAPI loopback, calculate FFT bands, and drive overlays or web scripts.
- GPU/RAM monitoring: integrate LibreHardwareMonitor and render Rainmeter-style widgets.
- Discord Rich Presence: publish current wallpaper, playlist, and theme state.
- DirectX upgrade: replace WPF/VLC surfaces with Direct3D swapchain renderers for custom shader pipelines.
- WinUI 3 migration: move the dashboard to WinUI 3/Mica while keeping renderer processes isolated.
- SkiaSharp effects: add scanlines, bloom, waveform, and particles as transparent overlay layers.
- FFmpeg integration: generate thumbnails, probe duration, normalize codecs, and build wallpaper packs.
