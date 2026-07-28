using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Re.Desktop.Services;
using Re.Desktop.ViewModels.Salesforce;

namespace Re.Desktop.Views.Salesforce;

public partial class SalesforceCloudPage : UserControl
{
    private const string SalesforceLoginUrl = "https://login.salesforce.com";
    private readonly SalesforceCloudViewModel _viewModel;
    private readonly IDialogService _dialog;
    private readonly IUiLocalizationService _localization;
    private bool _initializationStarted;
    private bool _viewModelEventsAttached;

    public SalesforceCloudPage(
        SalesforceCloudViewModel viewModel,
        IDialogService dialog,
        IUiLocalizationService localization)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _dialog = dialog;
        _localization = localization;
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
            _viewModel.UpdateBrowserStatus("Salesforce browser profile is ready | Session persistence is enabled");
        }
        catch (Exception ex)
        {
            _initializationStarted = false;
            _viewModel.UpdateBrowserStatus($"Salesforce WebView could not be started: {ex.Message}");
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

        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.AreDevToolsEnabled = false;
        // The app owns the single bottom status bar. Disable Chromium's second bar.
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = true;
        core.Settings.IsPasswordAutosaveEnabled = false;
        core.Settings.IsGeneralAutofillEnabled = false;

        core.NavigationStarting += (_, _) =>
            _viewModel.UpdateBrowserStatus("Opening Salesforce...");
        core.NavigationCompleted += Browser_NavigationCompleted;
        core.NewWindowRequested += Browser_NewWindowRequested;
        core.ProcessFailed += (_, args) =>
            _viewModel.UpdateBrowserStatus($"The WebView process stopped ({args.ProcessFailedKind}). Refresh the page.");
    }

    private async void Browser_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            _viewModel.UpdateBrowserStatus($"The Salesforce page could not be loaded: {e.WebErrorStatus}");
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
                ? $"Salesforce session active | Local cache ready | {uri.Host}"
                : $"Waiting for Salesforce sign-in | {uri.Host}");
        }
        catch
        {
            _viewModel.UpdateBrowserStatus($"Salesforce is open | {uri.Host}");
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

        if (!_dialog.Confirm(
                _localization.Translate("Salesforce.ResetPrompt"),
                _localization.Translate("Salesforce.ResetSession")))
            return;

        SfWebView.CoreWebView2.CookieManager.DeleteAllCookies();
        _viewModel.WebViewUrl = SalesforceLoginUrl;
        NavigateTo(SalesforceLoginUrl);
        _viewModel.UpdateBrowserStatus(_localization.Translate("Salesforce.SessionCleared"));
    }
}
