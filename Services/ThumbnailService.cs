using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using LiveWallpaperApp.Models;

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
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
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
            var previewPath = GetPreviewPath(item.FilePath);
            if (!File.Exists(previewPath))
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

        var files = Directory.GetFiles(CacheRoot, "*.mp4")
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

    private async Task<bool> TryGenerateLowBitratePreviewAsync(
        string sourcePath,
        string previewPath,
        PerformanceSettings settings,
        CancellationToken cancellationToken)
    {
        var ffmpegPath = FindFfmpeg();
        if (ffmpegPath is null)
        {
            return false;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(previewPath)!);

        var fps = Math.Clamp(settings.ThumbnailFps, 5, 15);
        var arguments =
            $"-y -hide_banner -loglevel error -i \"{sourcePath}\" -t 8 -vf \"scale=426:-2:flags=fast_bilinear,fps={fps}\" -an -c:v libx264 -preset veryfast -crf 32 -movflags +faststart \"{previewPath}\"";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            },
            EnableRaisingEvents = true
        };

        process.Start();
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode == 0 && File.Exists(previewPath);
    }

    private string GetPreviewPath(string sourcePath)
    {
        var identity = $"{sourcePath}|{File.GetLastWriteTimeUtc(sourcePath).Ticks}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        return Path.Combine(CacheRoot, $"{hash[..20]}.preview.mp4");
    }

    private static string? FindFfmpeg()
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory.Trim(), "ffmpeg.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
