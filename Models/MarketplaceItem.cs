using LiveWallpaperApp.Helpers;

namespace LiveWallpaperApp.Models;

public class MarketplaceItem : ObservableObject
{
    private bool _isDownloading;
    private double _downloadProgress;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;

    public bool IsDownloading
    {
        get => _isDownloading;
        set => SetProperty(ref _isDownloading, value);
    }

    public double DownloadProgress
    {
        get => _downloadProgress;
        set => SetProperty(ref _downloadProgress, value);
    }
}
