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
using Re.Desktop.Views.Funding;
using Re.Desktop.ViewModels.Salesforce;
using Re.Desktop.ViewModels.StockMovements;
using Re.Desktop.ViewModels.Funding;
using Re.Desktop.ViewModels.POS;
using Re.Desktop.Views.Salesforce;
using Re.Desktop.ViewModels.Settings;
using Re.Desktop.ViewModels.Purchasing;
using Re.Desktop.Views.Purchasing;
using Re.Desktop.ViewModels.Orders;
using Re.Desktop.Views.Orders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.IO;
using System.Text;
using System.Windows.Threading;

namespace Re.Desktop;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            WriteCrashLog(args.ExceptionObject as Exception, "AppDomain");
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            WriteCrashLog(args.Exception, "TaskScheduler");
            args.SetObserved();
        };

        base.OnStartup(e);

        try
        {
            ApiRunnerService.StartApi();
        }
        catch (Exception ex)
        {
            WriteCrashLog(ex, "ApiRunnerService.StartApi");
        }

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();
            Services.GetRequiredService<IUiLocalizationService>().ApplyCurrentCulture();

            var loginWindow = Services.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }
        catch (Exception ex)
        {
            WriteCrashLog(ex, "Services initialization");
            MessageBox.Show(
                $"Re encountered an error during initialization.\\n\\n{ex.GetBaseException().Message}\\n\\n" +
                $"Diagnostic details were saved to:\\n{GetCrashLogPath()}",
                "Re Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            this.Shutdown(1);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteCrashLog(e.Exception, "WPF Dispatcher");
        MessageBox.Show(
            $"Re encountered an unexpected error.\n\n{e.Exception.GetBaseException().Message}\n\n" +
            $"Diagnostic details were saved to:\n{GetCrashLogPath()}",
            "Re Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private static string GetCrashLogPath()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ReSoft", "Re", "Logs");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "desktop-crash.log");
    }

    private static void WriteCrashLog(Exception? exception, string source)
    {
        try
        {
            var text = new StringBuilder()
                .AppendLine($"[{DateTimeOffset.Now:O}] {source}")
                .AppendLine(exception?.ToString() ?? "Unknown managed exception")
                .AppendLine(new string('-', 80))
                .ToString();
            File.AppendAllText(GetCrashLogPath(), text);
        }
        catch { }
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        // Logging
        services.AddLogging(b => b.AddDebug().SetMinimumLevel(LogLevel.Debug));

        // Singleton: session ve navigation
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<IUiLocalizationService, UiLocalizationService>();
        services.AddSingleton<IPackageCenterService, PackageCenterService>();
        services.AddSingleton<INavigationService>(sp =>
        {
            var nav = new NavigationService(sp);
            return nav;
        });
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IChequeNoteService, ChequeNoteService>();

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
        services.AddSingleton<FundingIntelligenceViewModel>();
        services.AddTransient<PosViewModel>();
        services.AddTransient<PackageCenterViewModel>();
        services.AddTransient<PurchaseInvoiceViewModel>();
        services.AddTransient<OrderViewModel>();

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
        services.AddTransient<PackageCenterPage>();
        services.AddTransient<UpdateCenterPage>();
        services.AddTransient<PurchaseInvoicePage>();
        services.AddTransient<OrderPage>();
        services.AddSingleton<SalesforceCloudPage>();
        services.AddSingleton<FundingIntelligencePage>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ApiRunnerService.StopApi();
        base.OnExit(e);
        if (Services is IDisposable d) d.Dispose();
    }
}

