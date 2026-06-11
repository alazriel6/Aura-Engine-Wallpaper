namespace LiveWallpaperApp.Services;

public sealed class PreviewRenderService
{
    private readonly object _gate = new();
    private readonly HashSet<Guid> _activeSlots = new();

    public int MaximumActivePreviews { get; set; } = 1;

    public bool TryAcquire(Guid ownerId)
    {
        lock (_gate)
        {
            if (_activeSlots.Contains(ownerId))
            {
                return true;
            }

            if (_activeSlots.Count >= Math.Max(0, MaximumActivePreviews))
            {
                return false;
            }

            _activeSlots.Add(ownerId);
            return true;
        }
    }

    public void Release(Guid ownerId)
    {
        lock (_gate)
        {
            _activeSlots.Remove(ownerId);
        }
    }

    public void ReleaseAll()
    {
        lock (_gate)
        {
            _activeSlots.Clear();
        }
    }
}

public static class PreviewRenderCoordinator
{
    public static PreviewRenderService Shared { get; } = new();
}
