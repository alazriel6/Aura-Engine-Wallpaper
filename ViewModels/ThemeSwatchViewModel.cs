using System.Windows.Input;
using System.Windows.Media;
using LiveWallpaperApp.Helpers;

namespace LiveWallpaperApp.ViewModels;

public class ThemeSwatchViewModel
{
    public string ColorHex { get; }
    public string Name { get; }
    public ICommand ApplyCommand { get; }

    public Brush ColorBrush => (SolidColorBrush)(new BrushConverter().ConvertFromString(ColorHex) ?? Brushes.Transparent);

    public ThemeSwatchViewModel(string colorHex, string name, Action<string> applyAction)
    {
        ColorHex = colorHex;
        Name = name;
        ApplyCommand = new RelayCommand(() => applyAction(ColorHex));
    }
}
