using System.Windows;

namespace Re.Desktop.Services;

public enum ThemeMode { Light, Dark }

public static class ThemeManager
{
    public static void ApplyTheme(ThemeMode mode)
    {
        var app = Application.Current;
        if (app is null) return;

        var bg = mode == ThemeMode.Dark ? "#121824" : "#F4F6F9";
        var fg = mode == ThemeMode.Dark ? "#FFFFFF" : "#172B4D";

        app.Resources["AppBackground"] = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(bg));
        app.Resources["AppForeground"] = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(fg));
    }
}
