using Re.Desktop.Services;
using Re.Desktop.ViewModels.Auth;
using Re.Desktop.ViewModels.Sales;
using Re.Desktop.ViewModels.Products;
using Re.Desktop.ViewModels.Shell;
using Re.Desktop.ViewModels.Agenda;
using Re.Desktop.ViewModels.Accounts;
using Re.Desktop.Views.Accounts;
using Re.Desktop.Views.Auth;
using Re.Desktop.Views.Finance;
using Re.Desktop.ViewModels.Finance;
using Re.Desktop.Views.Sales;
using Re.Desktop.Views.POS;
using Re.Desktop.Views.Products;
using Re.Desktop.Views.Reports;
using Re.Desktop.Views.Settings;
using Re.Desktop.Views.Shell;
using Re.Desktop.Views.Agenda;
using Re.Desktop.Views.StockMovements;
using Re.Desktop.ViewModels.Salesforce;
using Re.Desktop.ViewModels.StockMovements;
using Re.Desktop.Views.Salesforce;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace Re.Desktop;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        ApiRunnerService.StartApi();

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        var loginWindow = Services.GetRequiredService<LoginWindow>();
        loginWindow.Show();
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        // Logging
        services.AddLogging(b => b.AddDebug().SetMinimumLevel(LogLevel.Debug));

        // Singleton: session ve navigation
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<INavigationService>(sp =>
        {
            var nav = new NavigationService(sp);
            return nav;
        });
        services.AddTransient<IDialogService, DialogService>();

        services.AddHttpClient<ApiClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:5188/");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ProductDashboardViewModel>();
        services.AddTransient<ProductListViewModel>();
        services.AddTransient<ProductCatalogViewModel>();
        services.AddTransient<InvoiceListViewModel>();
        services.AddTransient<AgendaViewModel>();
        services.AddTransient<AccountListViewModel>();
        services.AddTransient<FinanceViewModel>();
        services.AddTransient<TreasuryViewModel>();
        // Salesforce WebView ve ViewModel tek örnek tutulur. Sekme kapatılıp
        // yeniden açıldığında tarayıcı motoru, RAM cache ve oturum yeniden kurulmaz.
        services.AddSingleton<SalesforceCloudViewModel>();
        services.AddTransient<StockMovementsViewModel>();

        // Windows
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();

        // Pages (NavigationService tarafından çözümlenir)
        services.AddTransient<DashboardPage>();
        services.AddTransient<AgendaPage>();
        services.AddTransient<ProductDashboardPage>();
        services.AddTransient<ProductListPage>();
        services.AddTransient<ProductCatalogWindow>();
        services.AddTransient<InvoicePage>();
        services.AddTransient<StockMovementsPage>();
        services.AddTransient<PosPage>();
        services.AddTransient<AccountListPage>();
        services.AddTransient<CashPage>();
        services.AddTransient<BankPage>();
        services.AddTransient<FinancePage>();
        services.AddTransient<ReportsPage>();
        services.AddTransient<SettingsPage>();
        services.AddSingleton<SalesforceCloudPage>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ApiRunnerService.StopApi();
        base.OnExit(e);
        if (Services is IDisposable d) d.Dispose();
    }
}

