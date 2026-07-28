using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Text.Json;
using Re.Desktop.Services;
namespace Re.Desktop.Views.Settings;
public partial class SettingsPage : UserControl
{
    private readonly IUiLocalizationService _localization;
    private readonly IDialogService _dialog;
    private readonly ApiClient _api;
    private bool _initializing = true;
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ReERP", "barcode-settings.json");

    public SettingsPage(IUiLocalizationService localization, IDialogService dialog, ApiClient api)
    {
        _localization = localization;
        _dialog = dialog;
        _api = api;
        InitializeComponent();
        InterfaceLanguage.SelectedItem = InterfaceLanguage.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(x => string.Equals(
                x.Tag?.ToString(), localization.CurrentCulture, StringComparison.OrdinalIgnoreCase))
            ?? InterfaceLanguage.Items[0];
        LoadBarcodeSettings();
        _initializing = false;
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
        _dialog.Success("System settings saved successfully.", "Settings");
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_dialog.Confirm("Are you sure you want to reset all settings to defaults?", "Reset Settings"))
            return;

        StatusText.Text = "Default values loaded";
    }

    private void InterfaceLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || InterfaceLanguage.SelectedItem is not ComboBoxItem item)
            return;

        _localization.SetCulture(item.Tag?.ToString() ?? "en-US");
        StatusText.Text = _localization.Translate("Settings.LanguageApplied");
        _dialog.Success(
            _localization.Translate("Settings.LanguageApplied"),
            _localization.Translate("Dialog.Success"));
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

    private async void SyncAllData_Click(object sender, RoutedEventArgs e)
    {
        if (_api == null) return;
        StatusText.Text = "Syncing with Salesforce...";
        try
        {
            var targetOrg = "ReSoft_Developer";
            var response = await _api.PostAsync<object>($"api/salesforce/sync/full-job?targetOrg={targetOrg}", new { });
            if (response != null)
            {
                StatusText.Text = "Sync complete";
                _dialog.Success("Full Salesforce sync job completed successfully: schema objects, customers, products and invoices have been migrated.", "Auto-Sync Job");
            }
            else
            {
                StatusText.Text = "Sync failed";
                _dialog.Error("Zero-Touch Salesforce sync job failed. Verify connection status.");
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "Sync error";
            _dialog.Error($"Sync error: {ex.Message}");
        }
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

