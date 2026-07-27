using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
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

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _transactionType = "Tahsilat (Giriş)";
    public ObservableCollection<string> TransactionTypes { get; } = new() { "Tahsilat (Giriş)", "Ödeme (Çıkış)" };

    [ObservableProperty] private AccountListResponse? _selectedAccount;
    public ObservableCollection<AccountListResponse> Accounts { get; } = new();

    [ObservableProperty] private string _paymentMethod = "Kasa";
    public ObservableCollection<string> PaymentMethods { get; } = new() { "Kasa", "Banka" };

    [ObservableProperty] private CashRegisterItem? _selectedCashRegister;
    public ObservableCollection<CashRegisterItem> CashRegisters { get; } = new();

    [ObservableProperty] private BankAccountItem? _selectedBankAccount;
    public ObservableCollection<BankAccountItem> BankAccounts { get; } = new();

    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private DateTime _transactionDate = DateTime.Now;

    // View control
    public bool IsCashSelected => PaymentMethod == "Kasa";
    public bool IsBankSelected => PaymentMethod == "Banka";

    public FinanceViewModel()
    {
        // Design time
    }

    public FinanceViewModel(ApiClient api, IDialogService dialog)
    {
        _api = api;
        _dialog = dialog;
        _ = LoadInitialDataAsync();
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
            _dialog?.Error("Lütfen bir cari hesap seçiniz.");
            return;
        }

        if (Amount <= 0)
        {
            _dialog?.Error("Tutar sıfırdan büyük olmalıdır.");
            return;
        }

        if (IsCashSelected && SelectedCashRegister == null)
        {
            _dialog?.Error("Lütfen bir kasa seçiniz.");
            return;
        }

        if (IsBankSelected && SelectedBankAccount == null)
        {
            _dialog?.Error("Lütfen bir banka hesabı seçiniz.");
            return;
        }

        if (_api == null) return;

        IsLoading = true;
        try
        {
            Guid? cashId = IsCashSelected ? SelectedCashRegister?.Id : null;
            Guid? bankId = IsBankSelected ? SelectedBankAccount?.Id : null;

            if (TransactionType == "Tahsilat (Giriş)")
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
                    _dialog?.Info("Tahsilat işlemi başarıyla kaydedildi.", "Başarılı");
                    ResetForm();
                }
                else
                {
                    _dialog?.Error("Tahsilat işlemi kaydedilirken hata oluştu.");
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
                    _dialog?.Info("Ödeme işlemi başarıyla kaydedildi.", "Başarılı");
                    ResetForm();
                }
                else
                {
                    _dialog?.Error("Ödeme işlemi kaydedilirken hata oluştu.");
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
