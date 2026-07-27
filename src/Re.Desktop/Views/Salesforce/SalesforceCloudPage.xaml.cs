using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Re.Desktop.ViewModels.Salesforce;

namespace Re.Desktop.Views.Salesforce;

public partial class SalesforceCloudPage : UserControl
{
    private const string SalesforceLoginUrl = "https://login.salesforce.com";
    private readonly SalesforceCloudViewModel _viewModel;
    private bool _initializationStarted;
    private bool _viewModelEventsAttached;

    public SalesforceCloudPage(SalesforceCloudViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachViewModelEvents();
        if (_initializationStarted)
            return;

        _initializationStarted = true;
        await InitializeBrowserAsync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (!_viewModelEventsAttached)
            return;

        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        _viewModelEventsAttached = false;
    }

    private void AttachViewModelEvents()
    {
        if (_viewModelEventsAttached)
            return;

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        _viewModelEventsAttached = true;
    }

    private async Task InitializeBrowserAsync()
    {
        try
        {
            var profileFolder = GetPersistentProfileFolder();
            Directory.CreateDirectory(profileFolder);

            var options = new CoreWebView2EnvironmentOptions
            {
                AllowSingleSignOnUsingOSPrimaryAccount = true,
                // WebView2 uses the persistent Chromium HTTP cache in the user
                // data folder. Reserve enough room for Lightning's JS/CSS bundles.
                AdditionalBrowserArguments = "--disk-cache-size=536870912"
            };
            var environment = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: profileFolder,
                options: options);

            await SfWebView.EnsureCoreWebView2Async(environment);
            ConfigureBrowser();
            NavigateTo(_viewModel.WebViewUrl ?? SalesforceLoginUrl);
            _viewModel.UpdateBrowserStatus("Salesforce tarayıcı profili hazır · Oturum kalıcı olarak korunuyor");
        }
        catch (Exception ex)
        {
            _initializationStarted = false;
            _viewModel.UpdateBrowserStatus($"Salesforce WebView başlatılamadı: {ex.Message}");
        }
    }

    private static string GetPersistentProfileFolder()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "ReSoft", "Envanzo", "WebView2", "Salesforce");
    }

    private void ConfigureBrowser()
    {
        var core = SfWebView.CoreWebView2;
        var downloadFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ReSoft", "Envanzo", "WebView2", "SalesforceDownloads");
        Directory.CreateDirectory(downloadFolder);
        core.Profile.DefaultDownloadFolderPath = downloadFolder;

        core.Settings.AreDefaultContextMenusEnabled = true;
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.IsStatusBarEnabled = true;
        core.Settings.IsZoomControlEnabled = true;
        core.Settings.IsPasswordAutosaveEnabled = false;
        core.Settings.IsGeneralAutofillEnabled = false;

        core.NavigationStarting += (_, _) =>
            _viewModel.UpdateBrowserStatus("Salesforce açılıyor...");
        core.NavigationCompleted += Browser_NavigationCompleted;
        core.NewWindowRequested += Browser_NewWindowRequested;
        core.ProcessFailed += (_, args) =>
            _viewModel.UpdateBrowserStatus($"WebView işlemi durdu ({args.ProcessFailedKind}). Sayfayı yenileyin.");
    }

    private async void Browser_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            _viewModel.UpdateBrowserStatus($"Salesforce sayfası yüklenemedi: {e.WebErrorStatus}");
            return;
        }

        if (!Uri.TryCreate(SfWebView.Source?.AbsoluteUri, UriKind.Absolute, out var uri))
            return;

        try
        {
            var cookies = await SfWebView.CoreWebView2.CookieManager.GetCookiesAsync(
                uri.GetLeftPart(UriPartial.Authority));
            var hasSalesforceSession = cookies.Any(cookie =>
                cookie.Name.Equals("sid", StringComparison.OrdinalIgnoreCase));
            _viewModel.UpdateBrowserStatus(hasSalesforceSession
                ? $"Salesforce oturumu aktif · Yerel cache hazır · {uri.Host}"
                : $"Salesforce giriş bekleniyor · {uri.Host}");
        }
        catch
        {
            _viewModel.UpdateBrowserStatus($"Salesforce açık · {uri.Host}");
        }
    }

    private void Browser_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) || !IsSalesforceHost(uri.Host))
            return;

        e.Handled = true;
        NavigateTo(e.Uri);
    }

    private static bool IsSalesforceHost(string host) =>
        host.Equals("salesforce.com", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".salesforce.com", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".force.com", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".salesforce-sites.com", StringComparison.OrdinalIgnoreCase);

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SalesforceCloudViewModel.WebViewUrl) ||
            string.IsNullOrWhiteSpace(_viewModel.WebViewUrl))
            return;

        Dispatcher.InvokeAsync(() => NavigateTo(_viewModel.WebViewUrl));
    }

    private void NavigateTo(string? address)
    {
        if (SfWebView.CoreWebView2 is null ||
            !Uri.TryCreate(address, UriKind.Absolute, out var target))
            return;

        var current = SfWebView.Source?.AbsoluteUri;
        if (!string.Equals(current, target.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
            SfWebView.CoreWebView2.Navigate(target.AbsoluteUri);
    }

    private void ReloadBrowser_Click(object sender, RoutedEventArgs e)
    {
        SfWebView.CoreWebView2?.Reload();
    }

    private void ResetSalesforceSession_Click(object sender, RoutedEventArgs e)
    {
        if (SfWebView.CoreWebView2 is null)
            return;

        var result = MessageBox.Show(
            "Kaydedilmiş Salesforce oturumu ve çerezleri temizlenecek. Devam edilsin mi?",
            "Salesforce Oturumunu Sıfırla",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
            return;

        SfWebView.CoreWebView2.CookieManager.DeleteAllCookies();
        _viewModel.WebViewUrl = SalesforceLoginUrl;
        NavigateTo(SalesforceLoginUrl);
        _viewModel.UpdateBrowserStatus("Salesforce oturumu temizlendi");
    }
}
