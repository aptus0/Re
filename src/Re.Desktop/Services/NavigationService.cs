using System;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Re.Desktop.Services;

/// <summary>
/// Sayfa navigasyonu – Artık MDI / Sekmeli (Tabbed) yapıya hizmet verir.
/// İstendiğinde bir sayfanın açılmasını tetikler.
/// </summary>
public interface INavigationService
{
    void NavigateTo(string route);
    event Action<string, object>? PageRequested;
}

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;
    public event Action<string, object>? PageRequested;

    public NavigationService(IServiceProvider services) => _services = services;

    public void NavigateTo(string route)
    {
        Type? pageType = route switch
        {
            "Dashboard"      => typeof(Views.Shell.DashboardPage),
            "Agenda"         => typeof(Views.Agenda.AgendaPage),
            "ProductDashboard"=> typeof(Views.Products.ProductDashboardPage),
            "Products"       => typeof(Views.Products.ProductListPage),
            "Invoices"       => typeof(Views.Sales.InvoicePage),
            "StockMovements" => typeof(Views.StockMovements.StockMovementsPage),
            "Accounts"       => typeof(Views.Accounts.AccountListPage),
            "POS"            => typeof(Views.POS.PosPage),
            "Cash"           => typeof(Views.Finance.CashPage),
            "Bank"           => typeof(Views.Finance.BankPage),
            "Finance"        => typeof(Views.Finance.FinancePage),
            "Reports"        => typeof(Views.Reports.ReportsPage),
            "Settings"       => typeof(Views.Settings.SettingsPage),
            "SalesforceCloud"=> typeof(Views.Salesforce.SalesforceCloudPage),
            _ => null
        };

        if (pageType is not null)
        {
            try
            {
                var page = _services.GetService(pageType);
                if (page is not null)
                    PageRequested?.Invoke(route, page);
            }
            catch (Exception ex)
            {
                _services.GetService<IDialogService>()?.Error(
                    $"{route} ekranı açılamadı.\n{ex.GetBaseException().Message}", "Ekran Açma Hatası");
            }
        }
    }
}

/// <summary>
/// Diyalog servisi – MessageBox soyutlaması.
/// </summary>
public interface IDialogService
{
    bool Confirm(string message, string title = "Onay");
    void Info(string message, string title = "Bilgi");
    void Error(string message, string title = "Hata");
}

public class DialogService : IDialogService
{
    public bool Confirm(string message, string title = "Onay")
        => new Re.Desktop.Views.Common.AlertWindow(title, message, true).ShowDialog() == true;

    public void Info(string message, string title = "Bilgi")
        => new Re.Desktop.Views.Common.AlertWindow(title, message, false).ShowDialog();

    public void Error(string message, string title = "Hata")
        => new Re.Desktop.Views.Common.AlertWindow(title, message, false, true).ShowDialog();
}
