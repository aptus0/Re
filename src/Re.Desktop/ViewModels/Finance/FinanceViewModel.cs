using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Re.Desktop.Services;
using Re.Contracts.Finance;
using Re.Contracts.Accounts;
using System.Linq;

namespace Re.Desktop.ViewModels.Finance;

public partial class FinanceViewModel : ObservableObject
{
    private readonly ApiClient? _api;
    private readonly IDialogService? _dialog;
    private readonly IChequeNoteService? _chequeService;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _transactionType = "Collection (Cash In)";
    public ObservableCollection<string> TransactionTypes { get; } = new() { "Collection (Cash In)", "Payment (Cash Out)" };

    [ObservableProperty] private AccountListResponse? _selectedAccount;
    public ObservableCollection<AccountListResponse> Accounts { get; } = new();

    [ObservableProperty] private string _paymentMethod = "Cash";
    public ObservableCollection<string> PaymentMethods { get; } = new() { "Cash", "Bank" };

    [ObservableProperty] private CashRegisterItem? _selectedCashRegister;
    public ObservableCollection<CashRegisterItem> CashRegisters { get; } = new();

    [ObservableProperty] private BankAccountItem? _selectedBankAccount;
    public ObservableCollection<BankAccountItem> BankAccounts { get; } = new();

    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private DateTime _transactionDate = DateTime.Now;

    // Cheques and Notes Properties
    [ObservableProperty] private string _searchChequeText = string.Empty;
    [ObservableProperty] private string _totalChequesReceivable = "0,00 ₺";
    [ObservableProperty] private string _totalNotesReceivable = "0,00 ₺";
    [ObservableProperty] private string _totalChequesPayable = "0,00 ₺";
    [ObservableProperty] private string _totalNotesPayable = "0,00 ₺";
    public ObservableCollection<ChequeNoteItem> ChequesAndNotes { get; } = new();
    private List<ChequeNoteItem> _allCheques = new();

    // View control
    public bool IsCashSelected => PaymentMethod == "Cash";
    public bool IsBankSelected => PaymentMethod == "Bank";

    public FinanceViewModel()
    {
        // Design time
    }

    public FinanceViewModel(ApiClient api, IDialogService dialog, IChequeNoteService chequeService)
    {
        _api = api;
        _dialog = dialog;
        _chequeService = chequeService;
        _ = LoadInitialDataAsync();
        _ = LoadChequesAndNotesAsync();
    }

    partial void OnPaymentMethodChanged(string value)
    {
        OnPropertyChanged(nameof(IsCashSelected));
        OnPropertyChanged(nameof(IsBankSelected));
    }

    [RelayCommand]
    private async Task LoadInitialDataAsync()
    {
        if (_api == null) return;
        IsLoading = true;
        try
        {
            // Load Accounts
            var accResp = await _api.GetAsync<Re.Contracts.Common.PagedResponse<AccountListResponse>>("api/accounts?page=1&size=1000");
            if (accResp?.Items != null)
            {
                Accounts.Clear();
                foreach (var a in accResp.Items) Accounts.Add(a);
            }

            // Load Cash Registers
            var cashResp = await _api.GetAsync<CashRegisterItem[]>("api/finance/cashregisters");
            if (cashResp != null)
            {
                CashRegisters.Clear();
                foreach (var c in cashResp) CashRegisters.Add(c);
                if (CashRegisters.Any()) SelectedCashRegister = CashRegisters.First();
            }

            // Load Bank Accounts
            var bankResp = await _api.GetAsync<BankAccountItem[]>("api/finance/bankaccounts");
            if (bankResp != null)
            {
                BankAccounts.Clear();
                foreach (var b in bankResp) BankAccounts.Add(b);
                if (BankAccounts.Any()) SelectedBankAccount = BankAccounts.First();
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveTransactionAsync()
    {
        if (SelectedAccount == null)
        {
            _dialog?.Error("Select an account.");
            return;
        }

        if (Amount <= 0)
        {
            _dialog?.Error("Amount must be greater than zero.");
            return;
        }

        if (IsCashSelected && SelectedCashRegister == null)
        {
            _dialog?.Error("Select a cash register.");
            return;
        }

        if (IsBankSelected && SelectedBankAccount == null)
        {
            _dialog?.Error("Select a bank account.");
            return;
        }

        if (_api == null) return;

        IsLoading = true;
        try
        {
            Guid? cashId = IsCashSelected ? SelectedCashRegister?.Id : null;
            Guid? bankId = IsBankSelected ? SelectedBankAccount?.Id : null;

            if (TransactionType == "Collection (Cash In)")
            {
                var req = new CollectionRequest(
                    SelectedAccount.Id,
                    cashId,
                    bankId,
                    Amount,
                    "TRY",
                    1m,
                    Description,
                    TransactionDate
                );

                var res = await _api.PostAsync<FinanceTransactionResponse>("api/finance/collections", req);
                if (res != null)
                {
                    _dialog?.Success("Collection saved successfully.", "Success");
                    ResetForm();
                }
                else
                {
                    _dialog?.Error("An error occurred while saving the collection.");
                }
            }
            else
            {
                var req = new PaymentRequest(
                    SelectedAccount.Id,
                    cashId,
                    bankId,
                    Amount,
                    "TRY",
                    1m,
                    Description,
                    TransactionDate
                );

                var res = await _api.PostAsync<FinanceTransactionResponse>("api/finance/payments", req);
                if (res != null)
                {
                    _dialog?.Success("Payment saved successfully.", "Success");
                    ResetForm();
                }
                else
                {
                    _dialog?.Error("An error occurred while saving the payment.");
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ResetForm()
    {
        Amount = 0;
        Description = string.Empty;
        TransactionDate = DateTime.Now;
        SelectedAccount = null;
    }

    // Cheques and Notes Logic
    partial void OnSearchChequeTextChanged(string value)
    {
        ApplyChequeFilter();
    }

    [RelayCommand]
    public async Task LoadChequesAndNotesAsync()
    {
        if (_chequeService == null) return;
        IsLoading = true;
        try
        {
            var data = await _chequeService.GetAllAsync();
            _allCheques = data ?? new List<ChequeNoteItem>();
            ApplyChequeFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyChequeFilter()
    {
        ChequesAndNotes.Clear();
        var query = _allCheques.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchChequeText))
        {
            var term = SearchChequeText.Trim();
            query = query.Where(x => 
                x.Number.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                x.AccountName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                x.BankName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                x.Drawer.Contains(term, StringComparison.OrdinalIgnoreCase)
            );
        }
        foreach (var item in query)
        {
            ChequesAndNotes.Add(item);
        }
        UpdateChequeSummaries();
    }

    private static readonly CultureInfo TrCulture = new("tr-TR");

    private void UpdateChequeSummaries()
    {
        var customerCheques = _allCheques.Where(x => x.Type == ChequeNoteType.CustomerCheque && x.Status == ChequeNoteStatus.Portfolio).Sum(x => x.Amount);
        var customerNotes = _allCheques.Where(x => x.Type == ChequeNoteType.CustomerNote && x.Status == ChequeNoteStatus.Portfolio).Sum(x => x.Amount);
        var supplierCheques = _allCheques.Where(x => x.Type == ChequeNoteType.SupplierCheque && x.Status == ChequeNoteStatus.Portfolio).Sum(x => x.Amount);
        var supplierNotes = _allCheques.Where(x => x.Type == ChequeNoteType.SupplierNote && x.Status == ChequeNoteStatus.Portfolio).Sum(x => x.Amount);

        TotalChequesReceivable = customerCheques.ToString("N2", TrCulture) + " ₺";
        TotalNotesReceivable   = customerNotes.ToString("N2", TrCulture) + " ₺";
        TotalChequesPayable    = supplierCheques.ToString("N2", TrCulture) + " ₺";
        TotalNotesPayable      = supplierNotes.ToString("N2", TrCulture) + " ₺";
    }

    [RelayCommand]
    private void ViewChequeDetails(ChequeNoteItem item)
    {
        if (item == null) return;
        var window = new Views.Finance.ChequeDetailWindow(item)
        {
            Owner = Application.Current.Windows.OfType<Re.Desktop.Views.Shell.MainWindow>().FirstOrDefault(x => x.IsVisible)
        };
        window.ShowDialog();
    }

    [RelayCommand]
    private async Task CreateCheque()
    {
        if (_chequeService == null || _api == null || _dialog == null) return;
        var window = new Views.Finance.ChequeOperationWindow(_chequeService, _api, _dialog)
        {
            Owner = Application.Current.Windows.OfType<Re.Desktop.Views.Shell.MainWindow>().FirstOrDefault(x => x.IsVisible)
        };
        if (window.ShowDialog() == true)
        {
            await LoadChequesAndNotesAsync();
        }
    }

    [RelayCommand]
    private async Task CollectCheque(ChequeNoteItem item)
    {
        if (item == null || _chequeService == null) return;
        if (item.Status != ChequeNoteStatus.Portfolio)
        {
            _dialog?.Error("Only documents in Portfolio status can be collected.");
            return;
        }
        item.Status = ChequeNoteStatus.Collected;
        await _chequeService.SaveAsync(item);
        _dialog?.Success($"Cheque {item.Number} collected successfully.");
        await LoadChequesAndNotesAsync();
    }

    [RelayCommand]
    private async Task EndorseCheque(ChequeNoteItem item)
    {
        if (item == null || _chequeService == null || _dialog == null) return;
        if (item.Status != ChequeNoteStatus.Portfolio)
        {
            _dialog.Error("Only documents in Portfolio status can be endorsed.");
            return;
        }
        item.Status = ChequeNoteStatus.Endorsed;
        await _chequeService.SaveAsync(item);
        _dialog.Success($"Cheque {item.Number} endorsed successfully.");
        await LoadChequesAndNotesAsync();
    }

    [RelayCommand]
    private async Task MarkAsBounced(ChequeNoteItem item)
    {
        if (item == null || _chequeService == null) return;
        item.Status = ChequeNoteStatus.Bounced;
        await _chequeService.SaveAsync(item);
        _dialog?.Warning($"Cheque {item.Number} marked as Bounced/Unpaid.");
        await LoadChequesAndNotesAsync();
    }

    [RelayCommand]
    private async Task DeleteCheque(ChequeNoteItem item)
    {
        if (item == null || _chequeService == null || _dialog == null) return;
        if (!_dialog.Confirm($"Are you sure you want to delete document {item.Number}?")) return;
        await _chequeService.DeleteAsync(item.Id);
        _dialog.Success($"Document {item.Number} deleted.");
        await LoadChequesAndNotesAsync();
    }
}

public class CashRegisterItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }

    public string DisplayName => $"{Code} - {Name} ({CurrentBalance:N2} {Currency})";
}

public class BankAccountItem
{
    public Guid Id { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }

    public string DisplayName => $"{BankName} - {AccountName} ({CurrentBalance:N2} {Currency})";
}
