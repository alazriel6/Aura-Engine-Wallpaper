using System;
using System.Globalization;
using System.Windows.Data;

namespace LiveWallpaperApp.Helpers;

public class RatingToStarFontConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int rating && parameter is string starIndexStr && int.TryParse(starIndexStr, out int starIndex))
        {
            // \uE735 = Filled Star, \uE734 = Empty Star
            return rating >= starIndex ? "\uE735" : "\uE734";
        }
        return "\uE734";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
