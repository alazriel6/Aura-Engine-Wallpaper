using System;
using System.Globalization;
using System.Windows.Data;

namespace LiveWallpaperApp.Helpers;

public class BooleanToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string[] options = parameter?.ToString()?.Split('|') ?? new[] { "True", "False" };
        if (value is bool boolValue)
        {
            return boolValue ? options[0] : (options.Length > 1 ? options[1] : string.Empty);
        }
        return options.Length > 1 ? options[1] : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
