using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Re.Desktop.Services;
using Re.Desktop.Views.Auth;
using Re.Desktop.ViewModels.Products;
using Re.Desktop.ViewModels.Accounts;
using Re.Desktop.ViewModels.Sales;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Threading;

namespace Re.Desktop.ViewModels.Shell;

public partial class MainViewModel : ObservableObject
{
    private readonly ISessionService _session;
    private readonly INavigationService _navigation;
    private readonly ProductListViewModel _productListVm;
    private readonly AccountListViewModel _accountListVm;
    private readonly InvoiceListViewModel _invoiceListVm;
    private readonly IPackageCenterService _packages;

    [ObservableProperty] private string _userFullName = "Kullanıcı";
    [ObservableProperty] private string _userInitials = "K";
    [ObservableProperty] private string _companyName = "Firma";
    [ObservableProperty] private string _windowsUser = Environment.UserName;
    [ObservableProperty] private string _localIpAddress = "IP: Hesaplanıyor";
    [ObservableProperty] private string _liveClock = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
    [ObservableProperty] private string _connectionStatus = "● API SUNUCUSU AKTİF";
    [ObservableProperty] private string _sessionUsername = "-";
    [ObservableProperty] private bool _isPosInstalled;
    [ObservableProperty] private bool _isReportsInstalled;
    [ObservableProperty] private bool _isFundingInstalled;
    private readonly DispatcherTimer _clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    // Sekmeler (Tabs) Koleksiyonu
    public ObservableCollection<WorkspaceTab> OpenTabs { get; } = new();

    [ObservableProperty]
    private WorkspaceTab? _activeTab;

    private static readonly Dictionary<string, string> RouteNames = new()
    {
        ["Dashboard"]           = "Gösterge Paneli",
        ["Agenda"]              = "Takvim & Ajanda",
        ["Products"]            = "Ürün Kataloğu & Kartlar",
        ["ProductDashboard"]    = "Stok Özet Paneli",
        ["StockMovements"]      = "Stok Hareket Kayıtları",
        ["Invoices"]            = "Satış Faturaları",
        ["ReturnInvoices"]      = "İade Faturaları",
        ["PurchaseInvoices"]    = "Alış Faturaları",
        ["Orders"]              = "Sipariş Yönetimi",
        ["POS"]                 = "Hızlı Satış (POS)",
        ["Accounts"]            = "Cari Hesap Kartları",
        ["Cash"]                = "Kasa Yönetimi",
        ["Bank"]                = "Banka Hesapları",
        ["Finance"]             = "Tahsilat & Ödemeler",
        ["Accounting"]          = "Genel Muhasebe & Yevmiye",
        ["FundingIntelligence"] = "AI Finansal Tahminleme",
        ["SalesforceCloud"]     = "Salesforce LWC Bulut Kontrol",
        ["Reports"]             = "Yönetim Raporları",
        ["Settings"]            = "Sistem Ayarları",
        ["PackageCenter"]       = "Paket Merkezi",
        ["UpdateCenter"]        = "Güncelleme Merkezi",
    };

    public MainViewModel(ISessionService session, INavigationService navigation,
        ProductListViewModel productListVm, AccountListViewModel accountListVm,
        InvoiceListViewModel invoiceListVm, IPackageCenterService packages)
    {
        _session = session;
        _navigation = navigation;
        _productListVm = productListVm;
        _accountListVm = accountListVm;
        _invoiceListVm = invoiceListVm;
        _packages = packages;
        RefreshPackageState();
        _packages.PackagesChanged += RefreshPackageState;

        if (session.CurrentUser is { } user)
        {
            UserFullName = user.FullName;
            SessionUsername = user.Username;
            CompanyName  = user.CompanyId.ToString()[..8] + "...";
            var parts    = user.FullName.Split(' ');
            UserInitials = parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
                : user.FullName[..1].ToUpperInvariant();
        }
        LocalIpAddress = ResolveLocalIp();
        _clockTimer.Tick += (_, _) => LiveClock = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
        _clockTimer.Start();

        // Navigation Servisinden gelen sekme açma isteklerini dinle
        _navigation.PageRequested += OnPageRequested;

        // Startta Dashboard'u aç
        _navigation.NavigateTo("Dashboard");
    }

    private void RefreshPackageState()
    {
        IsPosInstalled = _packages.IsRouteEnabled("POS");
        IsReportsInstalled = _packages.IsRouteEnabled("Reports");
        IsFundingInstalled = _packages.IsRouteEnabled("FundingIntelligence");
    }

    private static string ResolveLocalIp()
    {
        try
        {
            return Dns.GetHostEntry(Dns.GetHostName()).AddressList
                .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork)?.ToString()
                ?? "IP bulunamadı";
        }
        catch { return "IP bulunamadı"; }
    }

    private void OnPageRequested(string route, object pageContent)
    {
        // Eğer zaten açıksa ona odaklan
        var existingTab = OpenTabs.FirstOrDefault(t => t.Route == route);
        if (existingTab != null)
        {
            ActiveTab = existingTab;
            return;
        }

        // Yeni sekme oluştur
        var newTab = new WorkspaceTab
        {
            Title = RouteNames.GetValueOrDefault(route, route),
            Route = route,
            Content = pageContent,
            IsCloseable = route != "Dashboard" // Dashboard kapatılamaz olsun
        };

        OpenTabs.Add(newTab);
        ActiveTab = newTab;
    }

    [RelayCommand]
    private void Navigate(string route)
    {
        _navigation.NavigateTo(route);
    }

    [RelayCommand]
    private void ShowInvoices()
    {
        _navigation.NavigateTo("Invoices");
    }

    [RelayCommand]
    private void CloseTab(WorkspaceTab tab)
    {
        if (tab == null || !tab.IsCloseable) return;

        OpenTabs.Remove(tab);

        if (ActiveTab == tab)
        {
            ActiveTab = OpenTabs.LastOrDefault();
        }
    }

    [RelayCommand]
    private void CloseOtherTabs(WorkspaceTab tab)
    {
        foreach (var item in OpenTabs.Where(x => x != tab && x.IsCloseable).ToList())
            OpenTabs.Remove(item);
        ActiveTab = tab;
    }

    [RelayCommand]
    private void CloseAllTabs()
    {
        foreach (var item in OpenTabs.Where(x => x.IsCloseable).ToList())
            OpenTabs.Remove(item);
        ActiveTab = OpenTabs.FirstOrDefault();
    }

    [RelayCommand]
    private void Logout()
    {
        if (MessageBox.Show("Oturumu kapatmak istediğinize emin misiniz?", "Oturumu Kapat",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        _session.ClearSession();
        var loginWindow = App.Services.GetRequiredService<LoginWindow>();
        loginWindow.Show();
        Application.Current.Windows.OfType<Views.Shell.MainWindow>().FirstOrDefault()?.Close();
    }
}
