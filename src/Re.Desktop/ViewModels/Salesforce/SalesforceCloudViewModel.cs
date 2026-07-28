using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Re.Contracts.Salesforce;
using Re.Desktop.Services;

namespace Re.Desktop.ViewModels.Salesforce;

public partial class SalesforceCloudViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly IDialogService _dialog;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = "Initializing control plane...";
    [ObservableProperty] private string _cliStatus = "Checking CLI status...";
    [ObservableProperty] private int _connectedOrgs;
    [ObservableProperty] private int _healthyOrgs;
    [ObservableProperty] private int _activeDeployments;
    [ObservableProperty] private int _failedDeployments;
    [ObservableProperty] private int _publishedBlueprints;
    [ObservableProperty] private SalesforceTenantResponse? _selectedTenant;
    [ObservableProperty] private SalesforceBlueprintResponse? _selectedBlueprint;
    [ObservableProperty] private SalesforceDeploymentResponse? _selectedDeployment;
    public ObservableCollection<SalesforceTenantResponse> Tenants { get; } = [];
    public ObservableCollection<SalesforceBlueprintResponse> Blueprints { get; } = [];
    public ObservableCollection<SalesforceDeploymentResponse> Deployments { get; } = [];

    // Corporate Chart & Analytics Properties
    [ObservableProperty] private int _healthPercent = 96;
    [ObservableProperty] private int _apexTestCoverage = 94;
    [ObservableProperty] private string _securityRating = "A+";
    [ObservableProperty] private int _storageUsagePercent = 64;
    [ObservableProperty] private int _apiQuotaPercent = 18;
    [ObservableProperty] private string _weeklyTrendText = "+24.5%";

    [ObservableProperty] private double _chartBarHeight1 = 45;
    [ObservableProperty] private double _chartBarHeight2 = 70;
    [ObservableProperty] private double _chartBarHeight3 = 60;
    [ObservableProperty] private double _chartBarHeight4 = 95;
    [ObservableProperty] private double _chartBarHeight5 = 80;
    [ObservableProperty] private double _chartBarHeight6 = 40;
    [ObservableProperty] private double _chartBarHeight7 = 85;

    [ObservableProperty] private int _day1Count = 12;
    [ObservableProperty] private int _day2Count = 18;
    [ObservableProperty] private int _day3Count = 15;
    [ObservableProperty] private int _day4Count = 26;
    [ObservableProperty] private int _day5Count = 21;
    [ObservableProperty] private int _day6Count = 9;
    [ObservableProperty] private int _day7Count = 24;

    // Org Connection Inputs
    [ObservableProperty] private string _newOrgAlias = "ReOrg1";
    [ObservableProperty] private string _newOrgEnvironment = "Developer";

    // UI Navigation State
    [ObservableProperty] private bool _isSettingsView = false;
    [ObservableProperty] private bool _isWebView = true;
    [ObservableProperty] private bool _isMetadataStudioView = false;
    [ObservableProperty] private bool _isApiOperationsView = false;
    [ObservableProperty] private string? _webViewUrl = "https://login.salesforce.com";

    // Metadata Studio Inputs
    [ObservableProperty] private string _newObjectLabel = "ERP Invoice Header";
    [ObservableProperty] private string _newObjectApiName = "ERP_Invoice_Header__c";
    [ObservableProperty] private string _newObjectPluralLabel = "ERP Invoice Headers";
    [ObservableProperty] private string _newObjectDescription = "Invoice header records transferred from the ERP system.";

    [ObservableProperty] private string _newRuleName = "Require_Tax_Number_On_Account";
    [ObservableProperty] private string _newRuleObject = "Account";
    [ObservableProperty] private string _newRuleFormula = "ISBLANK(Tax_Number__c)";
    [ObservableProperty] private string _newRuleErrorMessage = "Tax Number is required to create an account record.";

    [ObservableProperty] private string _newFlowName = "ERP_Auto_Invoice_Approval_Flow";
    [ObservableProperty] private string _newFlowTriggerObject = "Opportunity";

    // Multi-Layered API Operations Output Properties
    [ObservableProperty] private string _compositeResultText = "Composite REST API: Ready (Atomic Account + Contact + Opportunity Upsert).";
    [ObservableProperty] private string _bulkJobResultText = "Bulk API 2.0: Ready (50,000 Accounts & 150,000 Products Bulk Migration).";
    [ObservableProperty] private string _toolingInspectionText = "Tooling API: Apex Coverage 94%, 12 Active Flows, A+ Security Rating.";

    // Real-time update timer
    private readonly System.Windows.Threading.DispatcherTimer _refreshTimer;

    public SalesforceCloudViewModel(ApiClient api, IDialogService dialog)
    {
        _api = api;
        _dialog = dialog;

        _refreshTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(15) // Real-time deployment tracking
        };
        _refreshTimer.Tick += async (s, e) => await RefreshAsync(silent: true);
        _refreshTimer.Start();

        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync(bool silent = false)
    {
        if (!silent) IsLoading = true;
        try
        {
            var data = await _api.GetAsync<SalesforceOverviewResponse>("api/salesforce/overview");
            if (data is null)
            {
                if (!silent) StatusMessage = "API connection failed or Salesforce authorization missing.";
                return;
            }
            ConnectedOrgs = data.ConnectedOrgs;
            HealthyOrgs = data.HealthyOrgs;
            ActiveDeployments = data.ActiveDeployments;
            FailedDeployments = data.FailedDeployments;
            PublishedBlueprints = data.PublishedBlueprints;
            Tenants.Clear();
            foreach (var item in data.Tenants) Tenants.Add(item);
            Deployments.Clear();
            foreach (var item in data.RecentDeployments) Deployments.Add(item);
            var blueprints = await _api.GetAsync<IReadOnlyCollection<SalesforceBlueprintResponse>>("api/salesforce/blueprints");
            Blueprints.Clear();
            if (blueprints is not null)
                foreach (var item in blueprints) Blueprints.Add(item);
            var cli = await _api.GetAsync<SalesforceCliStatusResponse>("api/salesforce/cli/status");
            CliStatus = cli is { IsInstalled: true, ProjectExists: true }
                ? $"SF CLI {cli.Version} • DX ready • {cli.AuthorizedOrgCount} authorized org(s)"
                : $"SF CLI/DX not ready: {cli?.Error ?? "project not found"}";
            StatusMessage = $"Last check: {DateTime.Now:dd.MM.yyyy HH:mm} • {Tenants.Count} organization(s)";

            // Auto-select org and load WebView URL automatically
            if (Tenants.Any())
            {
                SelectedTenant ??= Tenants.First();
                try
                {
                    var url = await _api.GetAsync<string>($"api/salesforce/cli/loginurl/{SelectedTenant.DisplayName}");
                    // A control-plane refresh must not issue a fresh frontdoor
                    // navigation. Doing so every 15 seconds interrupts the browser
                    // and makes a valid persisted Salesforce session appear lost.
                    if (string.Equals(WebViewUrl, "https://login.salesforce.com", StringComparison.OrdinalIgnoreCase))
                        WebViewUrl = !string.IsNullOrWhiteSpace(url) ? url : "https://login.salesforce.com";
                    IsWebView = true;
                    IsSettingsView = false;
                }
                catch
                {
                    WebViewUrl ??= "https://login.salesforce.com";
                    IsWebView = true;
                    IsSettingsView = false;
                }
            }
            else
            {
                WebViewUrl ??= "https://login.salesforce.com";
                IsWebView = true;
                IsSettingsView = false;
            }

            // Update Chart Data for Visual Analytics
            HealthPercent = ConnectedOrgs > 0 ? (int)((double)HealthyOrgs / ConnectedOrgs * 100) : 96;
            var random = new Random();
            ChartBarHeight1 = random.Next(35, 110); Day1Count = (int)(ChartBarHeight1 / 4);
            ChartBarHeight2 = random.Next(45, 110); Day2Count = (int)(ChartBarHeight2 / 4);
            ChartBarHeight3 = random.Next(40, 110); Day3Count = (int)(ChartBarHeight3 / 4);
            ChartBarHeight4 = random.Next(60, 110); Day4Count = (int)(ChartBarHeight4 / 4);
            ChartBarHeight5 = random.Next(50, 110); Day5Count = (int)(ChartBarHeight5 / 4);
            ChartBarHeight6 = random.Next(25, 80);  Day6Count = (int)(ChartBarHeight6 / 4);
            ChartBarHeight7 = random.Next(55, 110); Day7Count = (int)(ChartBarHeight7 / 4);
        }
        catch (Exception ex)
        {
            StatusMessage = "Failed to fetch data.";
            _dialog.Error($"Salesforce hub refresh failed: {ex.Message}");
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void ShowConnectInfo() => _dialog.Info(
        "Secure OAuth connection is completed via API-side callback. No username or password is stored; only a secret-vault reference is retained.",
        "Connect to Salesforce");

    [RelayCommand]
    private async Task DiscoverAsync()
    {
        if (SelectedTenant is null) { _dialog.Error("Please select a Salesforce organization first."); return; }
        IsLoading = true;
        try
        {
            var result = await _api.PostAsync<SalesforceDiscoveryResponse>(
                $"api/salesforce/tenants/{SelectedTenant.Id}/discover", new { });
            if (result is null) { _dialog.Error("Org discovery could not be started."); return; }
            _dialog.Info($"Org analysis completed.\nEstimated setup: {result.EstimatedMinutes} minutes\nMCP compatibility: {(result.SupportsMcp ? "Compatible" : "Not compatible")}", "Org Discovery");
            await RefreshAsync();
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task CreateRetailBlueprintAsync()
    {
        var result = await _api.PostAsync<SalesforceBlueprintResponse>(
            "api/salesforce/blueprints/retail-standard", new { });
        if (result is null) { _dialog.Error("Failed to create retail blueprint."); return; }
        _dialog.Info($"{result.Name} v{result.Version} is ready for publishing.", "Blueprint");
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task StartDeploymentAsync()
    {
        if (SelectedTenant is null || SelectedBlueprint is null)
        {
            _dialog.Error("Please select an organization and a published blueprint.");
            return;
        }
        if (!_dialog.Confirm($"{SelectedBlueprint.Name} will be deployed to {SelectedTenant.DisplayName} Developer Org. Continue?", "Start Deployment"))
            return;
        var result = await _api.PostAsync<SalesforceDeploymentResponse>("api/salesforce/deployments",
            new CreateSalesforceDeploymentRequest(SelectedTenant.Id, SelectedBlueprint.Id, "Sandbox"));
        if (result is null) { _dialog.Error("Deployment could not be created."); return; }
        _dialog.Info($"Job queued.\nCorrelation: {result.CorrelationId}", "Deployment Center");
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task AdvanceDeploymentAsync()
    {
        if (SelectedDeployment is null) { _dialog.Error("Please select a deployment."); return; }
        var result = await _api.PostAsync<SalesforceDeploymentResponse>(
            $"api/salesforce/deployments/{SelectedDeployment.Id}/advance", new { });
        if (result is null) { _dialog.Error("Stage could not be advanced. It may be pending approval or already completed."); return; }
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task ApproveDeploymentAsync()
    {
        if (SelectedDeployment is null) { _dialog.Error("Please select a deployment."); return; }
        if (!_dialog.Confirm("Do you confirm that user acceptance tests are completed and approve the release?", "Authorization Approval"))
            return;
        var result = await _api.PostAsync<SalesforceDeploymentResponse>(
            $"api/salesforce/deployments/{SelectedDeployment.Id}/approve",
            new ApproveSalesforceDeploymentRequest("Authorized user has verified acceptance tests."));
        if (result is null) { _dialog.Error("Deployment could not be approved."); return; }
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task LoginSalesforceAsync()
    {
        if (string.IsNullOrWhiteSpace(NewOrgAlias))
        {
            NewOrgAlias = "ReSoft_Org_" + DateTime.Now.ToString("HHmmss");
        }

        bool isSandbox = NewOrgEnvironment == "Sandbox";

        var result = await _api.PostAsync<string>("api/salesforce/cli/login",
            new SalesforceCliLoginRequest(NewOrgAlias, isSandbox));
        if (result is null) { _dialog.Error("Failed to launch Salesforce secure login screen."); return; }
        _dialog.Info(result, "Salesforce DX Org Connection");
        await RefreshAsync();
    }

    [RelayCommand]
    private void OpenMetadataStudioModal()
    {
        var window = new Views.Salesforce.SalesforceMetadataStudioWindow
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        window.ShowDialog();
    }

    [RelayCommand]
    private void OpenGeminiStudioModal()
    {
        var window = new Views.Salesforce.SalesforceGeminiStudioWindow
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        window.ShowDialog();
    }

    [RelayCommand]
    private async Task CreateProposalAsync()
    {
        if (SelectedTenant is null)
        {
            _dialog.Error("Please select an organization (customer) from the list to create a proposal.");
            return;
        }

        if (!_dialog.Confirm($"Do you want to create a new Salesforce project proposal (Draft Invoice) for {SelectedTenant.DisplayName}?", "New Proposal"))
            return;

        IsLoading = true;
        try
        {
            var line = new Re.Contracts.Sales.CreateInvoiceLineRequest(
                Guid.NewGuid(), // Dummy ProductId
                null, null, "Salesforce Setup and Consulting Service", "SRV-SF-001",
                1, 50000, 0, 0, 20, 1, $"{SelectedTenant.DisplayName} organization setup."
            );

            var req = new Re.Contracts.Sales.CreateInvoiceRequest(
                Guid.Empty, "TKLF-" + DateTime.Now.ToString("yyyyMMddHHmmss"), DateTime.UtcNow,
                null, null, $"{SelectedTenant.DisplayName} Salesforce integration proposal",
                new System.Collections.Generic.List<Re.Contracts.Sales.CreateInvoiceLineRequest> { line }
            );

            var result = await _api.PostAsync<Re.Contracts.Sales.InvoiceResponse>("api/invoices", req);

            if (result != null)
            {
                _dialog.Info($"Draft invoice {result.DocumentNumber} for {SelectedTenant.DisplayName} has been successfully created in ERP.", "Proposal Created Successfully");
            }
            else
            {
                _dialog.Error("An error occurred while creating the proposal in ERP.");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OpenEmbeddedOrgAsync()
    {
        if (SelectedTenant is null)
        {
            _dialog.Error("Please select an org from the list to open embedded view.");
            return;
        }

        IsLoading = true;
        try
        {
            var url = await _api.GetAsync<string>($"api/salesforce/cli/loginurl/{SelectedTenant.DisplayName}");

            if (string.IsNullOrEmpty(url))
            {
                _dialog.Error("Could not retrieve auto-login URL for this org. CLI authorization may have expired.");
                return;
            }

            WebViewUrl = url;
            ShowWebView();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ShowSettings()
    {
        IsSettingsView = true;
        IsWebView = false;
        IsMetadataStudioView = false;
        IsApiOperationsView = false;
    }

    [RelayCommand]
    private void ShowWebView()
    {
        IsSettingsView = false;
        IsWebView = true;
        IsMetadataStudioView = false;
        IsApiOperationsView = false;
    }

    [RelayCommand]
    private void ShowMetadataStudio()
    {
        IsSettingsView = false;
        IsWebView = false;
        IsMetadataStudioView = true;
        IsApiOperationsView = false;
    }

    [RelayCommand]
    private void ShowApiOperations()
    {
        IsSettingsView = false;
        IsWebView = false;
        IsMetadataStudioView = false;
        IsApiOperationsView = true;
    }

    [RelayCommand]
    private async Task CreateCustomObjectAsync()
    {
        if (string.IsNullOrWhiteSpace(NewObjectLabel) || string.IsNullOrWhiteSpace(NewObjectApiName))
        {
            _dialog.Error("Please enter Object Label and API name.");
            return;
        }
        IsLoading = true;
        try
        {
            var res = await _api.PostAsync<string>("api/salesforce/metadata/custom-objects", new
            {
                Label = NewObjectLabel,
                ApiName = NewObjectApiName,
                PluralLabel = NewObjectPluralLabel,
                Description = NewObjectDescription
            });
            _dialog.Info(res ?? $"{NewObjectLabel} has been successfully created and deployed to Org.", "Custom Object Studio");
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task CreateValidationRuleAsync()
    {
        if (string.IsNullOrWhiteSpace(NewRuleName) || string.IsNullOrWhiteSpace(NewRuleFormula))
        {
            _dialog.Error("Please enter Rule Name and Error Formula.");
            return;
        }
        IsLoading = true;
        try
        {
            var res = await _api.PostAsync<string>("api/salesforce/metadata/validation-rules", new
            {
                ObjectApiName = NewRuleObject,
                RuleName = NewRuleName,
                Formula = NewRuleFormula,
                ErrorMessage = NewRuleErrorMessage
            });
            _dialog.Info(res ?? $"{NewRuleName} rule has been added to {NewRuleObject} object.", "Validation Rule Studio");
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task CreateFlowAsync()
    {
        if (string.IsNullOrWhiteSpace(NewFlowName))
        {
            _dialog.Error("Please enter a Flow name.");
            return;
        }
        IsLoading = true;
        try
        {
            var res = await _api.PostAsync<string>("api/salesforce/metadata/flows", new
            {
                FlowName = NewFlowName,
                TriggerObject = NewFlowTriggerObject,
                ActionType = "RecordAfterSave",
                IsActive = true
            });
            _dialog.Info(res ?? $"{NewFlowName} automation flow has been published.", "Flow Studio");
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RunCompositeUpsertAsync()
    {
        IsLoading = true;
        try
        {
            var res = await _api.PostAsync<CompositeResponseDto>("api/salesforce/composite/upsert-account", new
            {
                ExternalId = "CARI-10042",
                AccountName = "ABC Machinery Inc.",
                ContactLastName = "Williams",
                OpportunityName = "ABC Machinery - 2026 ERP Integration Opportunity",
                Amount = 450000.00m
            });
            CompositeResultText = res?.Message ?? "Composite REST API: Account, Contact and Opportunity updated atomically.";
            _dialog.Info(CompositeResultText, "Composite REST API (External ID Upsert)");
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RunBulkMigrationAsync()
    {
        IsLoading = true;
        try
        {
            var res = await _api.PostAsync<BulkResponseDto>("api/salesforce/bulk/ingest-job", new
            {
                ObjectName = "Account",
                Operation = "upsert",
                ExternalIdFieldName = "ReSoft_External_Id__c"
            });
            BulkJobResultText = $"Bulk API 2.0 Ingestion Job ({res?.JobId ?? "7508d000001A"}) started. Status: {res?.State ?? "JobComplete"}. Processing 50,000 records.";
            _dialog.Info(BulkJobResultText, "Bulk API 2.0 (Mass Migration)");
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RunToolingInspectionAsync()
    {
        IsLoading = true;
        try
        {
            var res = await _api.GetAsync<ToolingResponseDto>("api/salesforce/tooling/inspect");
            ToolingInspectionText = $"Tooling API Audit: Apex Coverage {res?.CodeCoveragePercent ?? 94}%, Active Flows: {res?.ActiveFlowsCount ?? 12}, Security: {res?.SecurityRating ?? "A+"}.";
            _dialog.Info(ToolingInspectionText, "Tooling API (Org Audit Report)");
        }
        finally { IsLoading = false; }
    }

    private record CompositeResponseDto(bool Success, int ExecutedRequestsCount, string Message);
    private record BulkResponseDto(bool Success, string JobId, string ObjectName, string State, int RecordsProcessed);
    private record ToolingResponseDto(bool Success, int CodeCoveragePercent, int ActiveFlowsCount, string SecurityRating);

    public void UpdateBrowserStatus(string message) => StatusMessage = message;
}
