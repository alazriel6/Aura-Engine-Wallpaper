using System;
using System.Globalization;
using System.Windows.Data;

namespace LiveWallpaperApp.Helpers;

public class NotEqualConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        return !value.Equals(parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // One-way converter usually
        return Binding.DoNothing;
    }
}
