# Architecture

## Lightweight Commercial Architecture

The app is optimized around a simple principle: the desktop wallpaper may be live, but the dashboard should not
quietly become a second wallpaper engine. The current design keeps one real desktop renderer, one dashboard preview
renderer, and a tiny number of hover-only library previews.

Core runtime pieces:

- `WallpaperService`: creates and controls WorkerW wallpaper renderer windows.
- `WallpaperWindow`: pure LibVLC video surface with no dashboard UI.
- `GPUOptimizationService`: converts performance settings into VLC runtime arguments.
- `PerformanceService`: samples process CPU/RAM, system RAM, and LibreHardwareMonitor CPU/GPU/VRAM sensors.
- `AutoPauseService`: pauses wallpaper under fullscreen apps, high load, battery state, idle state, and rules.
- `PreviewRenderService`: enforces a global live-preview budget so the library cannot spawn many video players.
- `ThumbnailService`: prepares low-bitrate preview videos and purges the preview cache.
- `AnimatedThumbnailControl`: plays live previews only while hovered, visible, and granted a preview slot.
- `LiveWallpaperPreviewControl`: one lightweight dashboard preview that pauses when the dashboard is inactive.
- `MemoryCleanupService`: periodically prunes preview cache and triggers conservative managed heap cleanup.

Default Balanced settings are intentionally conservative:

- wallpaper target: 30 FPS profile;
- live library previews: 1 at a time;
- thumbnail preview FPS: 8;
- preview cache: 256 MB;
- dashboard glow/shadow: reduced;
- memory saver and reduced background usage: on.
- dashboard preview surface: fixed size, so resizing the app does not create a larger preview decode/composition area.

## Why Live Wallpaper Apps Use Resources

### CPU Usage

CPU rises when video decode falls back from hardware decode to software decode, when multiple monitors start
separate decoders, when thumbnail cards decode full videos, or when the UI thread is asked to load files and
thumbnails synchronously. H.264/H.265 4K video can be cheap on D3D11VA and very expensive on software decode.

This app reduces CPU pressure by:

- using hardware decode options through `GPUOptimizationService`;
- disabling the audio graph for wallpapers and previews;
- limiting thumbnail preview concurrency;
- lazy loading preview players only when visible;
- async library import and preview generation;
- auto-pausing when fullscreen apps or high system load are detected.

### RAM Usage And The 1 GB Problem

RAM grows when every wallpaper and thumbnail owns decoded frame buffers, VLC media objects, and cached IO
buffers. Full-resolution previews are a common hidden cost because every preview card becomes a tiny video
player with its own media graph.

The most common reason a WPF wallpaper dashboard reaches 1 GB is not the wallpaper itself. It is many preview
players. Every LibVLC preview owns native buffers, decoder state, file cache, frame queues, and GPU surfaces. A
grid of 20 animated cards can accidentally become 20 tiny media players.

This app reduces RAM pressure by:

- disposing `Media`, `MediaPlayer`, and `LibVLC` when renderer windows or thumbnails unload;
- sharing one preview `LibVLC` runtime across dashboard and library preview players instead of creating one VLC runtime per card;
- sharing one wallpaper `LibVLC` runtime across multiple monitor renderer windows for the same applied wallpaper;
- using low-bitrate preview videos when FFmpeg is available;
- pruning `%LOCALAPPDATA%\LiveWallpaperApp\PreviewCache`;
- avoiding synchronous bulk loading in the wallpaper library;
- using virtualized `ListBox` preview cards instead of a non-virtualized wrap grid;
- animating library cards only on hover;
- enforcing a global preview player budget with `PreviewRenderService`;
- reducing default thumbnail FPS and cache size.

### GPU And VRAM Usage

GPU decode uses fixed-function blocks where possible, but decoded frames still occupy GPU memory. 4K frames,
multiple monitors, and live thumbnails can multiply VRAM usage. Texture upload and DWM composition also cost
time when high-refresh displays ask the desktop to present frequently.

This app reduces GPU/VRAM pressure by:

- preferring D3D11VA for active wallpaper video;
- using preview videos scaled to 426px width;
- unloading hidden preview players;
- pausing under fullscreen apps;
- exposing GPU load thresholds for auto-pause;
- preparing a DirectX shared-texture path in the architecture for future multi-monitor deduplication.

## WorkerW Flow

1. Find Explorer's `Progman` window.
2. Send message `0x052C` to ask Explorer to create a secondary WorkerW layer.
3. Enumerate top-level windows until `SHELLDLL_DefView` is found.
4. Find the blank `WorkerW` that follows that icon host.
5. Attach wallpaper renderer HWNDs to that blank WorkerW.

Desktop icons remain visible because icons are owned by `SHELLDLL_DefView`; the wallpaper is placed on a sibling
layer below it. `SetParent` is used because VLC renders through an HWND, and WorkerW lets DWM compose that HWND
as part of Explorer's desktop tree instead of as a foreground app.

## Render Engines

### VLC Rendering

Active implementation. Best compatibility with MP4/WebM/MKV and simple to ship with `VideoLAN.LibVLC.Windows`.
The cost is that each active player owns a decode graph, so thumbnails and multi-monitor playback need careful
lifetime management.

### DirectX Rendering

Prepared architecture. This is the best future path for shared textures, synchronized displays, shaders, HDR,
and true FPS limiting. DirectX can reduce duplicate decoding by sharing decoded surfaces across monitor outputs.

### SkiaSharp Rendering

Good for 2D procedural effects, audio visualizers, widgets, scanlines, and bloom overlays. It should not be the
primary 4K video decoder.

### WebView2 Rendering

Required for HTML/CSS/JS wallpapers. Powerful but memory-heavy if too many pages or previews stay alive. A
production version should suspend hidden WebViews aggressively.

## Hardware Acceleration

The UI exposes Auto, D3D11VA, DXVA2, NVDEC, AMD AMF, Intel QuickSync, and Disabled. In LibVLC on Windows, vendor
decode hardware is usually reached through D3D11VA/DXVA2. The explicit vendor choices are preserved because users
think in GPU vendor terms, but `GPUOptimizationService` maps them to safe VLC options until a custom DirectX
backend is implemented.

Hardware decode lowers CPU usage, but it can increase VRAM because decoded frames live in GPU surfaces. Software
decode can reduce GPU load but may spike CPU and battery usage.

## FPS And Frame Pacing

VLC does not expose a perfect Wallpaper Engine-style render loop FPS limiter for all file types. The current
implementation uses frame dropping, frame skipping, profile-specific cache sizes, auto-pause, and preview FPS
limits. A future DirectX renderer should own the presentation loop and present at exactly 5/15/30/60/120 FPS.

For 144 Hz displays, smoothness is improved by:

- avoiding UI-thread work during playback;
- using GPU decode;
- keeping preview players unloaded when hidden;
- pausing under exclusive fullscreen apps;
- using lower-bitrate loop-friendly wallpaper files;
- avoiding long GOP boundaries at loop points.

## Dashboard Live Preview

The home page includes a live preview panel. It uses the same low-cost LibVLC options as thumbnails, is muted, and
is separate from the WorkerW renderer. The preview panel has a fixed visual size to avoid larger dashboard windows
causing larger preview composition work. When the dashboard is minimized, hidden, or loses focus, the preview releases
its player so the desktop wallpaper remains the only active video path.

This is a deliberate resource tradeoff:

- one preview gives users confidence before applying;
- one preview does not create a library-wide decode storm;
- release-on-inactive prevents the dashboard from holding native video buffers while sitting behind other apps.

## Animated Thumbnail System

Live thumbnails are expensive because each moving card can become a separate decoder, a separate frame queue, and
a separate GPU upload path. Wallpaper Engine avoids this by using cached previews, low-resolution streams, lazy
loading, and strict visibility rules.

This app follows the same shape, with an even stricter default:

- `ThumbnailService` looks for FFmpeg and creates an 8-second, 426px-wide, low-bitrate preview MP4.
- If FFmpeg is unavailable, the preview falls back to the source video but is still hover-loaded.
- `AnimatedThumbnailControl` creates LibVLC only when hovered, visible, and granted a preview slot.
- The library page uses virtualization and recycling to unload hidden cards.
- `PreviewRenderService` defaults Balanced mode to one active animated card preview at a time.
- `MemoryCleanupService` keeps the preview cache under the configured size.
- Active/selected wallpapers are marked with an `ACTIVE` badge in Library and Playlist views.

## User-Friendly Settings

The main UI avoids developer wording:

- decoder hardware acceleration becomes **Video Performance** in Advanced;
- frame limiter becomes **Smoothness**;
- VRAM optimization becomes **Memory Saver**;
- process detection becomes **Pause While Gaming**;
- technical render choices live in **Advanced**.

The four public modes are:

- **Ultra Smooth**: higher motion and visual quality;
- **Balanced**: recommended daily mode with low resource use;
- **Power Saver**: lower FPS, fewer animations, minimal previews;
- **Gaming Mode**: pause while gaming and keep memory pressure low.

## Smart Auto-Pause

`AutoPauseService` evaluates:

- fullscreen foreground apps;
- maximized foreground apps when enabled;
- process blacklist and whitelist rules;
- laptop unplugged state;
- low battery state;
- GPU load;
- CPU load;
- CPU temperature;
- user idle time.

Manual pause and auto-pause are separated. If the user manually pauses, releasing auto-pause will not unexpectedly
resume playback.

## Multi-Monitor Optimization

The current VLC path creates one renderer window per monitor. This is reliable and compatible but can duplicate
decode work if every monitor plays the same source. The next performance tier is a DirectX shared decoder:

1. Decode once into a D3D11 texture.
2. Share the texture with per-monitor child windows.
3. Present each monitor at its own FPS or sleep state.
4. Release textures when monitors sleep or wallpapers pause.

The current service architecture keeps this upgrade possible by isolating WorkerW attachment from render policy.

## Plugin And Wallpaper Type Architecture

Prepared wallpaper types:

- MP4 / WebM video
- GIF
- HTML/CSS/JS through WebView2
- Unity wallpapers
- Shader wallpapers
- Image slideshow
- Audio reactive wallpapers

Plugin contracts should be added around:

- renderer factories;
- wallpaper package importers;
- shader/effect providers;
- desktop widget providers;
- marketplace providers;
- scripting hosts.

## Practical Optimization Checklist

- Prefer H.264/H.265 files that GPU decode supports.
- Use loop-friendly videos with matching first/last frames.
- Generate low-bitrate preview videos for the library.
- Keep thumbnail concurrency at 1 on laptops or mid-range GPUs.
- Use Battery Saver or Minimal Resource mode on unplugged systems.
- Enable fullscreen auto-pause for games.
- Avoid applying separate 4K videos to every monitor unless needed.
- Reapply the wallpaper after changing decode mode or power profile so VLC is recreated with new arguments.
