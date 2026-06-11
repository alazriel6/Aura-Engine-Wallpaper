namespace LiveWallpaperApp.Services;

public sealed class MemoryCleanupService : IDisposable
{
    private readonly MemoryOptimizerService _inner;

    public MemoryCleanupService(ThumbnailService thumbnailService, Models.PerformanceSettings settings)
    {
        _inner = new MemoryOptimizerService(thumbnailService, settings);
    }

    public void Start()
    {
        _inner.Start();
    }

    public void Stop()
    {
        _inner.Stop();
    }

    public Task RunCleanupAsync(CancellationToken cancellationToken = default)
    {
        return _inner.RunCleanupAsync(cancellationToken);
    }

    public void Dispose()
    {
        _inner.Dispose();
    }
}
