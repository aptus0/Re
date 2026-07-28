using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Re.Desktop.Converters;

/// <summary>bool → Visibility dönüşümü</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

/// <summary>bool → Visibility (ters)</summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public static readonly InverseBoolToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is not Visibility.Visible;
}

/// <summary>decimal → Para formatı Türk kültürüyle (1.234,56 ₺)</summary>
public class CurrencyConverter : IValueConverter
{
    public static readonly CurrencyConverter Instance = new();
    private static readonly CultureInfo TrCulture = new("tr-TR");

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is decimal d ? d.ToString("N2", TrCulture) + " ₺" : "-";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = value?.ToString()?.Replace("₺", "").Replace(" ", "").Trim() ?? "";
        return decimal.TryParse(s, NumberStyles.Number, TrCulture, out var d) ? d : 0m;
    }
}

/// <summary>decimal ↔ TextBox için Türkçe para girişi (1.234,56)</summary>
public class TurkishAmountConverter : IValueConverter
{
    public static readonly TurkishAmountConverter Instance = new();
    private static readonly CultureInfo TrCulture = new("tr-TR");

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is decimal d && d != 0 ? d.ToString("N2", TrCulture) : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = value?.ToString()?.Replace(" ", "").Replace("₺", "").Trim() ?? "";
        if (decimal.TryParse(s, NumberStyles.Number, TrCulture, out var d)) return d;
        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d2)) return d2;
        return 0m;
    }
}

/// <summary>DateTime → Kısa tarih (dd.MM.yyyy)</summary>
public class ShortDateConverter : IValueConverter
{
    public static readonly ShortDateConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is DateTime dt ? dt.ToString("dd.MM.yyyy") : "-";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DateTime.TryParse(value?.ToString(), out var d) ? d : DateTime.MinValue;
}

/// <summary>Converts DocumentStatus values to English labels.</summary>
public class DocumentStatusConverter : IValueConverter
{
    public static readonly DocumentStatusConverter Instance = new();

    private static readonly Dictionary<string, string> Labels = new()
    {
        ["Draft"]         = "Draft",
        ["Approved"]      = "Approved",
        ["Posted"]        = "Posted",
        ["Cancelled"]     = "Cancelled",
        ["Reversed"]      = "Reversed",
        ["PartiallyPaid"] = "Partially Paid",
        ["FullyPaid"]     = "Fully Paid",
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => Labels.GetValueOrDefault(value?.ToString() ?? "", value?.ToString() ?? "-");

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value;
}

/// <summary>string null/empty → Visibility.Collapsed</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public static readonly NullToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.IsNullOrEmpty(value?.ToString()) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value;
}

