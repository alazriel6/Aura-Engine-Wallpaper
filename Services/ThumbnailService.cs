using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using LiveWallpaperApp.Models;
using LibVLCSharp.Shared;

namespace LiveWallpaperApp.Services;

public sealed class ThumbnailService
{
    private readonly GPUOptimizationService _gpuOptimizationService;
    private readonly SemaphoreSlim _preloadGate = new(2, 2);

    public ThumbnailService(GPUOptimizationService gpuOptimizationService)
    {
        _gpuOptimizationService = gpuOptimizationService;
    }

    public string CacheRoot { get; } = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "LiveWallpaperApp",
        "PreviewCache");

    public async Task<IReadOnlyList<WallpaperPreviewItem>> BuildPreviewItemsAsync(
        IEnumerable<WallpaperModel> wallpapers,
        PerformanceSettings settings,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(CacheRoot);

        var items = wallpapers.Select(wallpaper => new WallpaperPreviewItem
        {
            Wallpaper = wallpaper,
            IsFavorite = wallpaper.IsFavorite
        }).ToList();

        var preloadTasks = items
            .Take(Math.Max(1, settings.ThumbnailMaxConcurrentPlayers * 2))
            .Select(item => PreparePreviewAsync(item, settings, cancellationToken))
            .ToArray();

        await Task.WhenAll(preloadTasks).ConfigureAwait(false);
        return items;
    }

    public async Task PreparePreviewAsync(
        WallpaperPreviewItem item,
        PerformanceSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(item.FilePath) || !File.Exists(item.FilePath))
        {
            return;
        }

        await _preloadGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (item.Wallpaper.FileSizeBytes == 0)
            {
                try
                {
                    var fi = new FileInfo(item.FilePath);
                    item.Wallpaper.FileSizeBytes = fi.Length;
                    item.RaisePropertyChanged(nameof(item.FileSizeText));
                    
                    var probe = new NReco.VideoInfo.FFProbe();
                    var info = probe.GetMediaInfo(item.FilePath);
                    
                    if (info.Duration > TimeSpan.Zero)
                    {
                        item.Wallpaper.Duration = info.Duration.ToString(@"hh\:mm\:ss");
                    }
                    
                    var videoStream = info.Streams.FirstOrDefault(s => s.CodecType == "video");
                    if (videoStream != null)
                    {
                        item.Wallpaper.Resolution = $"{videoStream.Width}x{videoStream.Height}";
                        if (videoStream.FrameRate > 0)
                        {
                            item.Wallpaper.Fps = videoStream.FrameRate;
                        }
                    }
                    
                    item.RaisePropertyChanged(nameof(item.Resolution));
                    item.RaisePropertyChanged(nameof(item.Duration));
                    item.RaisePropertyChanged(nameof(item.Fps));
                }
                catch { }
            }

            var previewPath = GetPreviewPath(item.FilePath);
            var isCached = false;
            if (File.Exists(previewPath))
            {
                var info = new FileInfo(previewPath);
                if (info.Length > 0)
                {
                    isCached = true;
                }
                else
                {
                    try { info.Delete(); } catch { }
                }
            }

            if (!isCached)
            {
                var generated = await TryGenerateLowBitratePreviewAsync(
                    item.FilePath,
                    previewPath,
                    settings,
                    cancellationToken).ConfigureAwait(false);

                if (!generated)
                {
                    // Fallback keeps live thumbnails working without FFmpeg, but it is more
                    // expensive because LibVLC must decode the original source. Hidden cards
                    // are still unloaded by AnimatedThumbnailControl, so the cost is bounded.
                    item.PreviewPath = item.FilePath;
                    item.UsesSourceAsPreviewFallback = true;
                    item.IsPreviewReady = true;
                    return;
                }
            }

            item.PreviewPath = previewPath;
            item.IsPreviewReady = true;
            item.UsesSourceAsPreviewFallback = false;
        }
        finally
        {
            _preloadGate.Release();
        }
    }

    public async Task PurgeCacheIfNeededAsync(int limitMb, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(CacheRoot))
        {
            return;
        }

        var files = Directory.EnumerateFiles(CacheRoot, "*.*")
            .Where(f => f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastAccessTimeUtc)
            .ToList();

        var limitBytes = Math.Max(64, limitMb) * 1024L * 1024L;
        var total = files.Sum(file => file.Length);

        foreach (var file in files.AsEnumerable().Reverse())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (total <= limitBytes)
            {
                break;
            }

            try
            {
                total -= file.Length;
                file.Delete();
            }
            catch
            {
                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private Task<bool> TryGenerateLowBitratePreviewAsync(
        string sourcePath,
        string previewPath,
        PerformanceSettings settings,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(previewPath)!);
        
        return Task.Run(() => 
        {
            try
            {
                var ffMpeg = new NReco.VideoConverter.FFMpegConverter();
                try 
                {
                    ffMpeg.GetVideoThumbnail(sourcePath, previewPath, 1f);
                }
                catch
                {
                    ffMpeg.GetVideoThumbnail(sourcePath, previewPath, 0.1f);
                }

                if (File.Exists(previewPath) && new FileInfo(previewPath).Length > 0)
                {
                    return true;
                }
            }
            catch { }

            try
            {
                if (LiveWallpaperApp.Native.ShellThumbnailProvider.TryExtractThumbnail(sourcePath, previewPath, 426, 240))
                {
                    if (File.Exists(previewPath) && new FileInfo(previewPath).Length > 0)
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }, cancellationToken);
    }

    private string GetPreviewPath(string sourcePath)
    {
        var identity = $"{sourcePath}|{File.GetLastWriteTimeUtc(sourcePath).Ticks}";
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(identity)));
        return Path.Combine(CacheRoot, $"{hash[..20]}.preview.jpg");
    }
}
