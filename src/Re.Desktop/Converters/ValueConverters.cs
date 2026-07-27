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

/// <summary>decimal → Para formatı (₺1.234,56)</summary>
public class CurrencyConverter : IValueConverter
{
    public static readonly CurrencyConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is decimal d ? d.ToString("N2") + " ₺" : "-";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => decimal.TryParse(value?.ToString()?.Replace("₺", "").Trim(), out var d) ? d : 0m;
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

/// <summary>DocumentStatus enum → Türkçe etiket</summary>
public class DocumentStatusConverter : IValueConverter
{
    public static readonly DocumentStatusConverter Instance = new();

    private static readonly Dictionary<string, string> Labels = new()
    {
        ["Draft"]         = "Taslak",
        ["Approved"]      = "Onaylı",
        ["Posted"]        = "Muhasebeleşti",
        ["Cancelled"]     = "İptal",
        ["Reversed"]      = "Ters Kayıt",
        ["PartiallyPaid"] = "Kısmi Ödeme",
        ["FullyPaid"]     = "Tam Ödeme",
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

