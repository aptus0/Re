using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Re.Desktop.Services;

public interface IUiLocalizationService
{
    string CurrentCulture { get; }
    event EventHandler? CultureChanged;
    void ApplyCurrentCulture();
    void SetCulture(string cultureCode);
    string Translate(string key);
}

public sealed class UiLocalizationService : IUiLocalizationService
{
    private const string English = "en-US";
    private const string Turkish = "tr-TR";
    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ReSoft", "Re", "Settings", "ui-language.json");

    public string CurrentCulture { get; private set; } = Turkish;
    public event EventHandler? CultureChanged;

    public UiLocalizationService()
    {
        try
        {
            if (File.Exists(_settingsPath))
                CurrentCulture = JsonSerializer.Deserialize<LanguagePreference>(
                    File.ReadAllText(_settingsPath))?.CultureCode is English ? English : Turkish;
            else
                CurrentCulture = Turkish;
        }
        catch
        {
            CurrentCulture = Turkish;
        }
    }

    public void ApplyCurrentCulture() => Apply(CurrentCulture, persist: false);

    public void SetCulture(string cultureCode)
    {
        var normalized = cultureCode.Equals(Turkish, StringComparison.OrdinalIgnoreCase)
            ? Turkish
            : English;
        if (CurrentCulture == normalized)
            return;

        Apply(normalized, persist: true);
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    public string Translate(string key) =>
        Application.Current.TryFindResource(key)?.ToString() ?? key;

    private void Apply(string cultureCode, bool persist)
    {
        CurrentCulture = cultureCode;
        var culture = CultureInfo.GetCultureInfo(cultureCode);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var existing = dictionaries.FirstOrDefault(x =>
            x.Source?.OriginalString.Contains("/Localization/Strings.", StringComparison.OrdinalIgnoreCase) == true);
        if (existing is not null)
            dictionaries.Remove(existing);

        dictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(
                $"/Re;component/Resources/Localization/Strings.{cultureCode}.xaml",
                UriKind.Relative)
        });

        if (!persist)
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(
            new LanguagePreference(cultureCode),
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private sealed record LanguagePreference(string CultureCode);
}
