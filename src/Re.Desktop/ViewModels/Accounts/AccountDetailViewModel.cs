using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Re.Contracts.Accounts;
using Re.Desktop.Services;

namespace Re.Desktop.ViewModels.Accounts;

public partial class AccountDetailViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly IDialogService _dialog;
    private readonly Guid _accountId;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private AccountResponse? _account;
    [ObservableProperty] private AccountInvoiceSummaryResponse? _summary;
    [ObservableProperty] private string _searchText = string.Empty;

    // Totals/Badges
    [ObservableProperty] private string _receivableText = "0.00 ₺";
    [ObservableProperty] private string _payableText = "0.00 ₺";
    [ObservableProperty] private string _netBalanceText = "0.00 ₺";

    public ObservableCollection<DetailedActivityWrapper> Activities { get; } = [];
    public ObservableCollection<DetailedActivityWrapper> Collections { get; } = [];
    public ObservableCollection<DetailedActivityWrapper> Payments { get; } = [];
    public ObservableCollection<AccountProductSummaryResponse> TopProducts { get; } = [];
    public ObservableCollection<AccountInvoiceLinkResponse> RecentInvoices { get; } = [];

    private System.Collections.Generic.List<AccountActivityResponse> _allActivities = [];

    public AccountDetailViewModel(Guid accountId, ApiClient api, IDialogService dialog)
    {
        _accountId = accountId;
        _api = api;
        _dialog = dialog;
        _ = LoadDetailsAsync();
    }

    [RelayCommand]
    public async Task LoadDetailsAsync()
    {
        IsLoading = true;
        try
        {
            var accResult = await _api.GetAsync<AccountResponse>($"api/accounts/{_accountId}");
            if (accResult != null)
            {
                Account = accResult;
                if (Account.CurrentBalance >= 0)
                {
                    ReceivableText = $"{Account.CurrentBalance:N2} {Account.Currency}";
                    PayableText = $"0.00 {Account.Currency}";
                    NetBalanceText = $"{Account.CurrentBalance:N2} {Account.Currency} (Dr)";
                }
                else
                {
                    ReceivableText = $"0.00 {Account.Currency}";
                    PayableText = $"{Math.Abs(Account.CurrentBalance):N2} {Account.Currency}";
                    NetBalanceText = $"{Math.Abs(Account.CurrentBalance):N2} {Account.Currency} (Cr)";
                }
            }

            var sumResult = await _api.GetAsync<AccountInvoiceSummaryResponse>($"api/accounts/{_accountId}/360");
            if (sumResult != null)
            {
                Summary = sumResult;
                
                _allActivities = sumResult.RecentActivities.ToList();
                ApplyActivityFilter();

                TopProducts.Clear();
                foreach (var p in sumResult.TopProducts) TopProducts.Add(p);

                RecentInvoices.Clear();
                foreach (var i in sumResult.RecentInvoices) RecentInvoices.Add(i);
            }
        }
        catch (Exception ex)
        {
            _dialog.Error($"Failed to load details: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyActivityFilter();
    }

    private void ApplyActivityFilter()
    {
        Activities.Clear();
        Collections.Clear();
        Payments.Clear();

        var query = _allActivities.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(x => 
                x.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                x.Type.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (x.ReferenceType != null && x.ReferenceType.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            );
        }

        foreach (var act in query)
        {
            var wrapper = new DetailedActivityWrapper
            {
                Id = act.Id,
                Date = act.Date,
                Type = act.Type,
                Description = act.Description,
                Amount = act.Amount,
                RunningBalance = act.RunningBalance,
                ReferenceType = act.ReferenceType,
                ReferenceId = act.ReferenceId,
                Original = act
            };

            Activities.Add(wrapper);

            // Group into Collections (Credit/Alacak) or Payments (Debit/Borç)
            if (act.Type.Equals("Credit", StringComparison.OrdinalIgnoreCase))
            {
                Collections.Add(wrapper);
            }
            else if (act.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase))
            {
                Payments.Add(wrapper);
            }
        }
    }

    [RelayCommand]
    private void ExportActivity(DetailedActivityWrapper activity)
    {
        _dialog.Info($"Activity reference: {activity.ReferenceType ?? "N/A"}\nAmount: {activity.Amount:N2}\nDescription: {activity.Description}\nDate: {activity.Date:dd.MM.yyyy}\nExport complete.", "Export Activity");
    }

    [RelayCommand]
    private void ViewSourceDocument(DetailedActivityWrapper activity)
    {
        if (activity.ReferenceId == null || activity.ReferenceId == Guid.Empty)
        {
            _dialog.Warning("No source document associated with this movement.", "No Document");
            return;
        }
        _dialog.Info($"Source document reference: {activity.ReferenceType}\nID: {activity.ReferenceId}\nDetails of this document would be opened in its respective viewer.", "Document Viewer");
    }
}

public class DetailedActivityWrapper
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string Year => Date.ToString("yyyy");
    public string Month => Date.ToString("MMMM"); // e.g. "July" / "Temmuz"
    public string DayName => Date.ToString("dddd"); // e.g. "Tuesday" / "Salı"
    public string Day => Date.ToString("dd"); // e.g. "28"
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal RunningBalance { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public AccountActivityResponse Original { get; set; } = null!;
}
