using LiveWallpaperApp.Helpers;

namespace LiveWallpaperApp.Models;

public sealed class WallpaperPreviewItem : ObservableObject
{
    private string _previewPath = string.Empty;
    private bool _isFavorite;
    private bool _isPreviewReady;
    private bool _usesSourceAsPreviewFallback;
    private bool _isActiveWallpaper;

    public WallpaperModel Wallpaper { get; init; } = new();

    public void RaisePropertyChanged(string propertyName) => OnPropertyChanged(propertyName);

    public string DisplayName => Wallpaper.DisplayName;
    public string Author => Wallpaper.Author;
    public string Resolution => Wallpaper.Resolution;
    public string Duration => Wallpaper.Duration;
    public string Fps => Wallpaper.Fps <= 0 ? "Unknown FPS" : $"{Wallpaper.Fps:0} FPS";
    public string TagsText => Wallpaper.Tags.Count == 0 ? $"local  /  {System.IO.Path.GetExtension(FilePath).TrimStart('.').ToLowerInvariant()}" : string.Join("  /  ", Wallpaper.Tags);
    public string FilePath => Wallpaper.FilePath;
    public string FileSizeText => Wallpaper.FileSizeBytes == 0 ? "Unknown Size" : $"{(Wallpaper.FileSizeBytes / 1024.0 / 1024.0):0.0} MB";
    public string Type => Wallpaper.Type.ToString();
    public int Rating
    {
        get => Wallpaper.Rating;
        set
        {
            if (Wallpaper.Rating != value)
            {
                Wallpaper.Rating = value;
                OnPropertyChanged();
            }
        }
    }

    public string PreviewPath
    {
        get => _previewPath;
        set => SetProperty(ref _previewPath, value);
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        set => SetProperty(ref _isFavorite, value);
    }

    public bool IsPreviewReady
    {
        get => _isPreviewReady;
        set => SetProperty(ref _isPreviewReady, value);
    }

    public bool UsesSourceAsPreviewFallback
    {
        get => _usesSourceAsPreviewFallback;
        set => SetProperty(ref _usesSourceAsPreviewFallback, value);
    }

    public bool IsActiveWallpaper
    {
        get => _isActiveWallpaper;
        set => SetProperty(ref _isActiveWallpaper, value);
    }
}
