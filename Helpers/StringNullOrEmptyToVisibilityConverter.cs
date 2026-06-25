using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LiveWallpaperApp.Helpers;

public sealed class StringNullOrEmptyToVisibilityConverter : IValueConverter
{
    public Visibility WhenNullOrEmpty { get; set; } = Visibility.Visible;
    public Visibility WhenNotNullOrEmpty { get; set; } = Visibility.Collapsed;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var str = value as string;
        return string.IsNullOrEmpty(str) ? WhenNullOrEmpty : WhenNotNullOrEmpty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
