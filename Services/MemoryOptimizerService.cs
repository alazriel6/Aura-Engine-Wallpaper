using System.Runtime;
using System.Windows.Threading;
using LiveWallpaperApp.Models;

namespace LiveWallpaperApp.Services;

public sealed class MemoryOptimizerService : IDisposable
{
    private readonly ThumbnailService _thumbnailService;
    private readonly PerformanceSettings _settings;
    private readonly DispatcherTimer _timer;
    private bool _disposed;

    public MemoryOptimizerService(ThumbnailService thumbnailService, PerformanceSettings settings)
    {
        _thumbnailService = thumbnailService;
        _settings = settings;
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMinutes(2)
        };
        _timer.Tick += async (_, _) => await RunCleanupAsync().ConfigureAwait(true);
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    public async Task RunCleanupAsync(CancellationToken cancellationToken = default)
    {
        await _thumbnailService.PurgeCacheIfNeededAsync(_settings.ThumbnailCacheLimitMb, cancellationToken)
            .ConfigureAwait(false);

        // WPF + VLC both hold unmanaged allocations. Forcing collections constantly would
        // cause stutter, so cleanup is periodic and only compacts the managed heap after
        // preview cache pruning has reduced pressure.
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, blocking: false, compacting: true);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
    }
}
