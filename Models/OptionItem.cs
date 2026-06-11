namespace LiveWallpaperApp.Models;

public sealed record OptionItem<T>(T Value, string Title, string Description)
{
    public override string ToString()
    {
        return Title;
    }
}
