using System.Text.Json;
using LiveWallpaperApp.Models;

namespace LiveWallpaperApp.Services;

public sealed class WallpaperLibraryService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public string LibraryRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LiveWallpaperApp",
        "Library");

    public string ManifestPath => Path.Combine(LibraryRoot, "library.json");

    public async Task<WallpaperPackManifest> LoadAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(LibraryRoot);

        if (!File.Exists(ManifestPath))
        {
            return new WallpaperPackManifest { Name = "Local Library" };
        }

        await using var stream = File.OpenRead(ManifestPath);
        return await JsonSerializer.DeserializeAsync<WallpaperPackManifest>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false)
            ?? new WallpaperPackManifest { Name = "Local Library" };
    }

    public async Task SaveAsync(WallpaperPackManifest manifest, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(LibraryRoot);
        await using var stream = File.Create(ManifestPath);
        await JsonSerializer.SerializeAsync(stream, manifest, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<WallpaperModel> ImportVideoAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Wallpaper video was not found.", sourcePath);
        }

        Directory.CreateDirectory(LibraryRoot);

        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(sourcePath)}";
        var targetPath = Path.Combine(LibraryRoot, fileName);

        await using (var source = File.OpenRead(sourcePath))
        await using (var target = File.Create(targetPath))
        {
            await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
        }

        var manifest = await LoadAsync(cancellationToken).ConfigureAwait(false);
        var wallpaper = new WallpaperModel
        {
            DisplayName = Path.GetFileNameWithoutExtension(sourcePath),
            FilePath = targetPath,
            Author = Environment.UserName,
            Category = "Imported",
            Type = ResolveType(sourcePath),
            Tags = ["imported", Path.GetExtension(sourcePath).TrimStart('.').ToLowerInvariant()],
            Metadata =
            {
                ["source"] = sourcePath,
                ["type"] = "video/mp4"
            }
        };

        manifest.Wallpapers.Add(wallpaper);
        await SaveAsync(manifest, cancellationToken).ConfigureAwait(false);

        return wallpaper;
    }

    private static WallpaperType ResolveType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".webm" => WallpaperType.WebM,
            ".gif" => WallpaperType.Gif,
            ".html" or ".htm" => WallpaperType.Web,
            ".mp4" or ".m4v" or ".mov" or ".mkv" => WallpaperType.Mp4,
            _ => WallpaperType.Mp4
        };
    }
}
