using LibVLCSharp.Shared;

namespace LiveWallpaperApp.Services;

public static class PreviewVlcHost
{
    private static readonly object Gate = new();
    private static LibVLC? _sharedPreviewVlc;

    public static LibVLC GetSharedPreviewVlc(IEnumerable<string>? options)
    {
        lock (Gate)
        {
            // LibVLC itself is heavy because it loads plugins, decoder modules, and native
            // runtime state. Creating one per dashboard preview/card is the fastest path to
            // 1GB+ RAM. Preview MediaPlayers can share a single LibVLC instance safely.
            _sharedPreviewVlc ??= new LibVLC(options?.ToArray() ?? Array.Empty<string>());
            return _sharedPreviewVlc;
        }
    }

    public static void DisposeSharedPreviewVlc()
    {
        lock (Gate)
        {
            _sharedPreviewVlc?.Dispose();
            _sharedPreviewVlc = null;
        }
    }
}
