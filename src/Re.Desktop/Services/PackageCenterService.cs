using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Re.Desktop.Services;

public enum PackageKind { Core, Optional }

public sealed class RePackage : ObservableObject
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Version { get; init; }
    public required PackageKind Kind { get; init; }
    public string? Route { get; init; }
    private bool _isInstalled;
    public bool IsInstalled
    {
        get => _isInstalled;
        set
        {
            if (!SetProperty(ref _isInstalled, value)) return;
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(ActionText));
        }
    }
    public bool IsCore => Kind == PackageKind.Core;
    public string StatusText => IsCore ? "Built-in" : IsInstalled ? "Installed" : "Available";
    public string ActionText => IsInstalled ? "Uninstall" : "Install";
}

public interface IPackageCenterService
{
    ReadOnlyObservableCollection<RePackage> Packages { get; }
    event Action? PackagesChanged;
    bool IsRouteEnabled(string route);
    bool SetInstalled(string packageId, bool installed);
}

public sealed class PackageCenterService : IPackageCenterService
{
    private readonly ObservableCollection<RePackage> _packages;
    private readonly string _statePath;
    public ReadOnlyObservableCollection<RePackage> Packages { get; }
    public event Action? PackagesChanged;

    public PackageCenterService()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ReSoft", "Re", "Packages");
        Directory.CreateDirectory(root);
        _statePath = Path.Combine(root, "installed-packages.json");
        var installed = LoadInstalled();

        _packages =
        [
            Core("core.accounts", "Accounts", "Customer and supplier current accounts, balances and risk tracking."),
            Core("core.inventory", "Inventory", "Products, warehouses, stock balances and stock movements."),
            Core("core.invoices", "Invoicing", "Sales invoices, taxes, approval and financial posting."),
            Core("core.finance", "Finance", "Cash, bank and finance operations."),
            Optional("retail.pos", "Retail POS", "Fast checkout and cashier workspace.", "POS", installed),
            Optional("barcode.professional", "Barcode Professional", "Barcode lookup, label printing and scanner workflows.", null, installed),
            Optional("analytics.reports", "Advanced Reports", "Management reports and operational analytics.", "Reports", installed),
            Optional("finance.funding", "Funding Intelligence", "AI-assisted funding and underwriting workspace.", "FundingIntelligence", installed)
        ];
        Packages = new(_packages);
        Save();
    }

    public bool IsRouteEnabled(string route)
    {
        var package = _packages.FirstOrDefault(x =>
            string.Equals(x.Route, route, StringComparison.OrdinalIgnoreCase));
        return package is null || package.IsInstalled;
    }

    public bool SetInstalled(string packageId, bool installed)
    {
        var package = _packages.FirstOrDefault(x => x.Id == packageId);
        if (package is null || package.IsCore) return false;
        package.IsInstalled = installed;
        Save();
        PackagesChanged?.Invoke();
        return true;
    }

    private static RePackage Core(string id, string name, string description) => new()
    {
        Id = id, Name = name, Description = description, Version = "1.1.0",
        Kind = PackageKind.Core, IsInstalled = true
    };

    private static RePackage Optional(string id, string name, string description, string? route,
        HashSet<string> installed) => new()
    {
        Id = id, Name = name, Description = description, Version = "1.1.0",
        Kind = PackageKind.Optional, Route = route,
        IsInstalled = installed.Contains(id) || id == "barcode.professional"
    };

    private HashSet<string> LoadInstalled()
    {
        try
        {
            return File.Exists(_statePath)
                ? JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(_statePath)) ?? []
                : ["retail.pos", "barcode.professional", "analytics.reports"];
        }
        catch { return []; }
    }

    private void Save()
    {
        var installed = _packages.Where(x => x.IsInstalled).Select(x => x.Id).ToHashSet();
        File.WriteAllText(_statePath, JsonSerializer.Serialize(installed,
            new JsonSerializerOptions { WriteIndented = true }));
    }
}
