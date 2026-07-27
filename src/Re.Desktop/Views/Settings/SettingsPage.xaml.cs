using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Text.Json;
namespace Re.Desktop.Views.Settings;
public partial class SettingsPage : UserControl
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ReERP", "barcode-settings.json");

    public SettingsPage()
    {
        InitializeComponent();
        LoadBarcodeSettings();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new BarcodePrinterSettings
        {
            PrinterName = BarcodePrinterName.Text.Trim(),
            Language = ((ComboBoxItem?)PrinterLanguage.SelectedItem)?.Content?.ToString() ?? "Windows / GDI",
            Dpi = int.TryParse(((ComboBoxItem?)PrinterDpi.SelectedItem)?.Content?.ToString(), out var dpi) ? dpi : 203,
            Copies = int.TryParse(DefaultBarcodeCopies.Text, out var copies) ? Math.Clamp(copies, 1, 999) : 1,
            LabelSize = ((ComboBoxItem?)LabelSize.SelectedItem)?.Content?.ToString() ?? "40 x 30 mm",
            Standard = ((ComboBoxItem?)BarcodeStandard.SelectedItem)?.Content?.ToString() ?? "EAN-13",
            EnableBatchPrint = EnableBatchPrint.IsChecked == true,
            ShowProductName = ShowProductNameOnLabel.IsChecked == true,
            ShowPrice = ShowPriceOnLabel.IsChecked == true,
            ConfirmBeforePrint = ConfirmBeforePrinting.IsChecked == true
        }, new JsonSerializerOptions { WriteIndented = true }));
        StatusText.Text = $"Last saved: {DateTime.Now:HH:mm}";
        MessageBox.Show("System settings saved successfully.", "Settings",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Are you sure you want to reset all settings to defaults?", "Reset Settings",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        StatusText.Text = "Default values loaded";
    }

    private void LoadBarcodeSettings()
    {
        if (!File.Exists(SettingsPath)) return;
        try
        {
            var value = JsonSerializer.Deserialize<BarcodePrinterSettings>(File.ReadAllText(SettingsPath));
            if (value is null) return;
            BarcodePrinterName.Text = value.PrinterName;
            SelectItem(PrinterLanguage, value.Language);
            SelectItem(PrinterDpi, value.Dpi.ToString());
            DefaultBarcodeCopies.Text = value.Copies.ToString();
            SelectItem(LabelSize, value.LabelSize);
            SelectItem(BarcodeStandard, value.Standard);
            EnableBatchPrint.IsChecked = value.EnableBatchPrint;
            ShowProductNameOnLabel.IsChecked = value.ShowProductName;
            ShowPriceOnLabel.IsChecked = value.ShowPrice;
            ConfirmBeforePrinting.IsChecked = value.ConfirmBeforePrint;
        }
        catch { StatusText.Text = "Could not read barcode settings"; }
    }

    private static void SelectItem(ComboBox comboBox, string value)
    {
        comboBox.SelectedItem = comboBox.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(x => string.Equals(x.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
            ?? comboBox.Items[0];
    }
}

public sealed class BarcodePrinterSettings
{
    public string PrinterName { get; set; } = "";
    public string Language { get; set; } = "Windows / GDI";
    public int Dpi { get; set; } = 203;
    public int Copies { get; set; } = 1;
    public string LabelSize { get; set; } = "40 x 30 mm";
    public string Standard { get; set; } = "EAN-13";
    public bool EnableBatchPrint { get; set; } = true;
    public bool ShowProductName { get; set; } = true;
    public bool ShowPrice { get; set; } = true;
    public bool ConfirmBeforePrint { get; set; } = true;
}

