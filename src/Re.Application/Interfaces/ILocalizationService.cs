namespace Re.Application.Interfaces;

public interface ILocalizationService
{
    string CurrentCulture { get; }
    void SetCulture(string cultureCode);
    string GetString(string key);
    event EventHandler? CultureChanged;
}
