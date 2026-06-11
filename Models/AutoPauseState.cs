namespace LiveWallpaperApp.Models;

public sealed class AutoPauseState
{
    public bool ShouldPause { get; init; }
    public string Reason { get; init; } = "Active";
    public string ForegroundProcessName { get; init; } = string.Empty;
    public IReadOnlyList<string> Reasons { get; init; } = Array.Empty<string>();

    public static AutoPauseState Active { get; } = new()
    {
        ShouldPause = false,
        Reason = "Active"
    };
}
