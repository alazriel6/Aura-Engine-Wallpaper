using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LiveWallpaperApp.Helpers;

public class BooleanToRedHeartConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isFavorite && isFavorite)
        {
            return new SolidColorBrush(Color.FromRgb(244, 63, 94)); // Rose-500
        }
        return new SolidColorBrush(Colors.White);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
