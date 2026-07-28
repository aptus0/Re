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
    public required string Icon { get; init; }
    public required string Category { get; init; }
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
    public string StatusText => IsCore ? "Dahili Çekirdek (Korumalı)" : IsInstalled ? "Yüklü & Aktif" : "Kullanılabilir";
    public string ActionText => IsInstalled ? "Kaldır" : "Yükle";
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
            Core("core.accounts", "Cari Hesap Yönetimi Modülü", "Müşteri ve tedarikçi cari hesapları, bakiye, ekstresi ve risk takip sistemi.", "👤", "Çekirdek Modüller"),
            Core("core.inventory", "Stok & Depo Kontrol Modülü", "Ürün kartları, depolar, stok bakiyeleri, barkodlama ve hareket kayıtları.", "📦", "Çekirdek Modüller"),
            Core("core.invoices", "Kurumsal Fatura Modülü", "Satış faturaları, KDV matrah hesaplamaları, onay mekanizmaları ve otomatik muhasebe entegrasyonu.", "📄", "Çekirdek Modüller"),
            Core("core.finance", "Finans & Kasa Yönetimi", "Nakit kasa işlemleri, banka hesap hareketleri, çek/senet takibi ve ödeme planlama.", "💰", "Çekirdek Modüller"),
            Optional("retail.pos", "Perakende Hızlı Satış (POS)", "Dokunmatik ekran uyumlu hızlı kasa satışı, fiş/makbuz basımı ve terazi entegrasyonu.", "🛒", "POS & Satış", "POS", installed),
            Optional("barcode.professional", "Barkod & Etiket Profesyonel", "Otomatik barkod üretimi, termal etiket yazıcı desteği ve barkod okuyucu iş akışları.", "🏷️", "Stok & Depo", null, installed),
            Optional("analytics.reports", "Gelişmiş Yönetim Raporları", "Yönetici özet raporları, karlılık analizleri ve operasyonel iş zekası veri panelleri.", "📊", "İş Zekası", "Reports", installed),
            Optional("finance.funding", "AI Yapay Zeka Finansal Tahminleme", "AI destekli nakit akış tahminleme, kredi riski analiz motoru ve finansal yapay zeka.", "🧠", "Finans & AI", "FundingIntelligence", installed),
            Optional("salesforce.managedpackage", "Salesforce 2GP Entegratörü", "Yerel Salesforce Managed 2GP Paketi ile (reerp) çift yönlü nesne senkronizasyonu.", "☁️", "Entegrasyonlar", "SalesforceCloud", installed),
            Optional("efatura.integrator", "e-Fatura & e-Arşiv Entegratörü", "GİB uyumlu e-Fatura, e-Arşiv ve e-İrsaliye doğrudan entegratör gönderim servisi.", "⚡", "Entegrasyonlar", null, installed),
            Optional("bank.mt940", "SWIFT MT940 Banka Aktarıcı", "Banka hesap ekstrelerinin (MT940/OFX) otomatik işlenmesi ve cari hesap eşleştirmesi.", "🏦", "Entegrasyonlar", null, installed)
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

    private static RePackage Core(string id, string name, string description, string icon, string category) => new()
    {
        Id = id, Name = name, Description = description, Version = "1.1.0",
        Kind = PackageKind.Core, Icon = icon, Category = category, IsInstalled = true
    };

    private static RePackage Optional(string id, string name, string description, string icon, string category, string? route,
        HashSet<string> installed) => new()
    {
        Id = id, Name = name, Description = description, Version = "1.1.0",
        Kind = PackageKind.Optional, Icon = icon, Category = category, Route = route,
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
