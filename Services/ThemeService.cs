using System.Windows;
using System.Windows.Media;

namespace LiveWallpaperApp.Services;

public sealed class ThemeService
{
    private static readonly Dictionary<string, string> ThemeFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Cyber Neon"] = "CyberNeonTheme.xaml",
        ["RGB Gamer"] = "RGBGamerTheme.xaml",
        ["Matrix Green"] = "MatrixGreenTheme.xaml",
        ["Deep Space"] = "DeepSpaceTheme.xaml",
        ["Purple Synthwave"] = "PurpleSynthwaveTheme.xaml",
        ["Minimal Dark"] = "MinimalDarkTheme.xaml",
        ["Glass Transparent"] = "GlassTransparentTheme.xaml",
        ["Dark"] = "DarkTheme.xaml",
        ["Neon"] = "NeonTheme.xaml",
        ["Purple"] = "PurpleTheme.xaml"
    };

    public IReadOnlyList<string> AvailableThemes { get; } =
    [
        "Cyber Neon",
        "RGB Gamer",
        "Matrix Green",
        "Deep Space",
        "Purple Synthwave",
        "Minimal Dark",
        "Glass Transparent",
        "Dark",
        "Neon",
        "Purple"
    ];

    public string CurrentTheme { get; private set; } = "Dark";

    public void ApplyTheme(string themeName)
    {
        if (!ThemeFiles.TryGetValue(themeName, out var themeFile))
        {
            throw new ArgumentOutOfRangeException(nameof(themeName), $"Unknown theme '{themeName}'.");
        }

        var appResources = Application.Current.Resources;
        var dictionaries = appResources.MergedDictionaries;

        for (var i = dictionaries.Count - 1; i >= 0; i--)
        {
            var source = dictionaries[i].Source?.OriginalString ?? string.Empty;
            if (source.IndexOf("Theme.xaml", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                dictionaries.RemoveAt(i);
            }
        }

        dictionaries.Add(new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/LiveWallpaperApp;component/Themes/{themeFile}", UriKind.Absolute)
        });

        CurrentTheme = themeName;
    }

    public void ApplyVisualEffects(Models.PerformanceSettings settings)
    {
        var appResources = Application.Current.Resources;
        appResources["GlobalBlurRadius"] = settings.BlurStrength;
        appResources["GlobalGlowRadius"] = settings.GlowIntensity * 100.0;
        appResources["GlobalCornerRadius"] = new CornerRadius(settings.BorderRadius);
        appResources["GlobalPanelOpacity"] = settings.PanelOpacity;
    }

    public void ApplyAccentColor(string colorHex)
    {
        if (string.IsNullOrWhiteSpace(colorHex))
        {
            return;
        }

        var normalized = colorHex.StartsWith('#') ? colorHex : $"#{colorHex}";
        if (ColorConverter.ConvertFromString(normalized) is not Color color)
        {
            throw new ArgumentException($"'{colorHex}' is not a valid WPF color.", nameof(colorHex));
        }

        var resources = Application.Current.Resources;
        resources["AccentColor"] = color;
        resources["BorderGlowColor"] = color;
        resources["AccentBrush"] = new SolidColorBrush(color);
        resources["BorderGlowBrush"] = new SolidColorBrush(Color.FromArgb(180, color.R, color.G, color.B));
        resources["SelectionBrush"] = new SolidColorBrush(Color.FromArgb(80, color.R, color.G, color.B));
    }
}
