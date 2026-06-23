using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LiveWallpaperApp.Helpers;

public sealed class InverseCountToVisibilityConverter : IValueConverter
{
    public object VisibilityWhenZero { get; set; } = Visibility.Visible;
    public object VisibilityWhenNotZero { get; set; } = Visibility.Collapsed;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0 ? VisibilityWhenZero : VisibilityWhenNotZero;
        }

        return VisibilityWhenNotZero;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
