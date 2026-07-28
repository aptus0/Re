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
        var packageCenter = _services.GetService<IPackageCenterService>();
        if (packageCenter is not null && !packageCenter.IsRouteEnabled(route))
        {
            _services.GetService<IDialogService>()?.Warning(
                $"The {route} capability is not installed. Install it from Package Center.",
                "Package Required");
            return;
        }

        Type? pageType = route switch
        {
            "Dashboard"      => typeof(Views.Shell.DashboardPage),
            "Agenda"         => typeof(Views.Agenda.AgendaPage),
            "ProductDashboard"=> typeof(Views.Products.ProductDashboardPage),
            "Products"       => typeof(Views.Products.ProductListPage),
            "Invoices"       => typeof(Views.Sales.InvoicePage),
            "PurchaseInvoices" => typeof(Views.Purchasing.PurchaseInvoicePage),
            "Orders"          => typeof(Views.Orders.OrderPage),
            "StockMovements" => typeof(Views.StockMovements.StockMovementsPage),
            "Accounts"       => typeof(Views.Accounts.AccountListPage),
            "POS"            => typeof(Views.POS.PosPage),
            "Cash"           => typeof(Views.Finance.CashPage),
            "Bank"           => typeof(Views.Finance.BankPage),
            "Finance"        => typeof(Views.Finance.FinancePage),
            "FundingIntelligence" => typeof(Views.Funding.FundingIntelligencePage),
            "Reports"        => typeof(Views.Reports.ReportsPage),
            "Settings"       => typeof(Views.Settings.SettingsPage),
            "PackageCenter"  => typeof(Views.Settings.PackageCenterPage),
            "UpdateCenter"   => typeof(Views.Settings.UpdateCenterPage),
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
                    $"{route} screen could not be opened.\n{ex.GetBaseException().Message}", "Screen Navigation Error");
            }
        }
    }
}

/// <summary>
/// Diyalog servisi – MessageBox soyutlaması.
/// </summary>
public interface IDialogService
{
    bool Confirm(string message, string? title = null);
    void Info(string message, string? title = null);
    void Success(string message, string? title = null);
    void Warning(string message, string? title = null);
    void Error(string message, string? title = null);
}

public enum NotificationKind { Information, Success, Warning, Error, Confirmation }

public sealed class DialogService(IUiLocalizationService localization) : IDialogService
{
    public bool Confirm(string message, string? title = null)
        => Show(title, message, NotificationKind.Confirmation, true) == true;

    public void Info(string message, string? title = null)
        => Show(title, message, NotificationKind.Information);

    public void Success(string message, string? title = null)
        => Show(title, message, NotificationKind.Success);

    public void Warning(string message, string? title = null)
        => Show(title, message, NotificationKind.Warning);

    public void Error(string message, string? title = null)
        => Show(title, message, NotificationKind.Error);

    private bool? Show(string? title, string message, NotificationKind kind, bool confirmation = false) =>
        new Re.Desktop.Views.Common.AlertWindow(
            title ?? localization.Translate($"Dialog.{kind}"),
            message, kind, confirmation, localization).ShowDialog();
}
