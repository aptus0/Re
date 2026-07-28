using Re.Application.Interfaces;

namespace Re.Infrastructure.Common;

public sealed class LocalizationService : ILocalizationService
{
    private string _currentCulture = "tr-TR";

    public string CurrentCulture => _currentCulture;
    public event EventHandler? CultureChanged;

    private readonly Dictionary<string, Dictionary<string, string>> _dictionary = new()
    {
        ["tr-TR"] = new()
        {
            ["SalesforceCloud"] = "Salesforce Cloud & LWC Workspace",
            ["DeveloperStudio"] = "Developer Studio",
            ["GeminiStudio"] = "Gemini AI Prompt Studio",
            ["Refresh"] = "Yenile",
            ["CreateProposal"] = "Create New Proposal",
            ["OrgBadge"] = "Org Durumu",
            ["AutoDeploy"] = "Otomatik Deploy",
            ["JobWorkerStatus"] = "Job Worker Senkronize",
            ["SettingsTitle"] = "System & Salesforce Integration Settings",
            ["Environment"] = "Environment Type",
            ["LanguageSelect"] = "Uygulama Dili (Language)"
        },
        ["en-US"] = new()
        {
            ["SalesforceCloud"] = "Salesforce Cloud & LWC Workspace",
            ["DeveloperStudio"] = "Developer Studio",
            ["GeminiStudio"] = "Gemini AI Prompt Studio",
            ["Refresh"] = "Refresh",
            ["CreateProposal"] = "Create Proposal",
            ["OrgBadge"] = "Org Status",
            ["AutoDeploy"] = "Auto-Deploy",
            ["JobWorkerStatus"] = "Job Worker Synced",
            ["SettingsTitle"] = "System & Salesforce Integration Settings",
            ["Environment"] = "Environment Type",
            ["LanguageSelect"] = "Application Language"
        }
    };

    public void SetCulture(string cultureCode)
    {
        if (_currentCulture != cultureCode && _dictionary.ContainsKey(cultureCode))
        {
            _currentCulture = cultureCode;
            CultureChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string GetString(string key)
    {
        if (_dictionary.TryGetValue(_currentCulture, out var langDict) && langDict.TryGetValue(key, out var val))
        {
            return val;
        }
        return key;
    }
}
