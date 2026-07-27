using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Collections.Generic;
using Re.Desktop.Services;
using Re.Contracts.Accounts;

namespace Re.Desktop.ViewModels.Accounts;

public partial class AccountListViewModel : ObservableObject
{
    private readonly IDialogService? _dialog;
    private readonly List<AccountItem> _allAccounts = [];
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isSearchEmpty = true;
    [ObservableProperty] private string _totalCount = "0";
    [ObservableProperty] private string _totalReceivables = "0,00 ₺";
    [ObservableProperty] private string _totalPayables = "0,00 ₺";
    [ObservableProperty] private int _customerCount;
    [ObservableProperty] private int _supplierCount;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _selectedAccountGroup = "Tümü";
    public IReadOnlyList<string> AccountGroups { get; } = ["Tümü", "Müşteriler", "Tedarikçiler", "Müşteri + Tedarikçi"];
    
    // Panel Kontrolü (Görüntüleme)
    [ObservableProperty] private bool _isPanelOpen;
    [ObservableProperty] private AccountItem? _selectedAccount;

    // Form Kontrolü (Ekleme/Düzenleme)
    [ObservableProperty] private bool _isFormOpen;
    [ObservableProperty] private AccountFormModel _formModel = new();
    [ObservableProperty] private string _formTitle = "Yeni Cari Ekle";

    public ObservableCollection<AccountItem> Accounts { get; } = new();

    private readonly ApiClient? _api;

    public AccountListViewModel()
    {
        // Design-time
    }

    public AccountListViewModel(ApiClient api, IDialogService dialog)
    {
        _api = api;
        _dialog = dialog;
        _ = LoadAccountsAsync();
    }

    [RelayCommand]
    private async Task LoadAccountsAsync()
    {
        if (_api == null) return;
        var response = await _api.GetAsync<Re.Contracts.Common.PagedResponse<Re.Contracts.Accounts.AccountListResponse>>("api/accounts?page=1&size=100");
        
        if (response != null && response.Items != null)
        {
            _allAccounts.Clear();
            foreach (var a in response.Items)
            {
                _allAccounts.Add(new AccountItem
                {
                    Id = a.Id,
                    Code = a.Code,
                    Name = a.Name,
                    Type = a.AccountType,
                    Phone = string.IsNullOrWhiteSpace(a.Phone) ? "Belirtilmemiş" : a.Phone,
                    TaxNumber = a.TaxNumber ?? "",
                    Balance = a.CurrentBalance,
                    BalanceType = a.CurrentBalance >= 0 ? "Alacaklı" : "Borçlu",
                    Status = a.IsActive ? "Aktif" : "Pasif",
                    RegisterDate = DateTime.Now
                });
            }
        }
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value)
    {
        IsSearchEmpty = string.IsNullOrEmpty(value);
        ApplyFilter();
    }
    partial void OnSelectedAccountGroupChanged(string value) => ApplyFilter();

    partial void OnSelectedAccountChanged(AccountItem? value)
    {
        IsPanelOpen = value != null;
        if (value != null) IsFormOpen = false;
    }

    private void ApplyFilter()
    {
        var query = _allAccounts.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(a =>
                a.Code.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                a.TaxNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                a.Phone.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
        if (SelectedAccountGroup == "Müşteriler")
            query = query.Where(x => x.Type.Contains("Customer", StringComparison.OrdinalIgnoreCase));
        else if (SelectedAccountGroup == "Tedarikçiler")
            query = query.Where(x => x.Type.Contains("Supplier", StringComparison.OrdinalIgnoreCase));
        else if (SelectedAccountGroup == "Müşteri + Tedarikçi")
            query = query.Where(x => x.Type.Contains("Both", StringComparison.OrdinalIgnoreCase));

        Accounts.Clear();
        foreach (var account in query)
            Accounts.Add(account);

        UpdateSummaries();
    }

    private void UpdateSummaries()
    {
        TotalCount = Accounts.Count.ToString();
        var receivables = Accounts.Where(a => a.BalanceType == "Borçlu").Sum(a => a.Balance);
        var payables = Accounts.Where(a => a.BalanceType == "Alacaklı").Sum(a => a.Balance);

        TotalReceivables = receivables.ToString("N2") + " ₺";
        TotalPayables = payables.ToString("N2") + " ₺";
        CustomerCount = Accounts.Count(a =>
            a.Type.Contains("Customer", StringComparison.OrdinalIgnoreCase) ||
            a.Type.Contains("Alıcı", StringComparison.OrdinalIgnoreCase) ||
            a.Type.Contains("Both", StringComparison.OrdinalIgnoreCase));
        SupplierCount = Accounts.Count(a =>
            a.Type.Contains("Supplier", StringComparison.OrdinalIgnoreCase) ||
            a.Type.Contains("Satıcı", StringComparison.OrdinalIgnoreCase) ||
            a.Type.Contains("Both", StringComparison.OrdinalIgnoreCase));
    }

    [RelayCommand] private void ClosePanel() { IsPanelOpen = false; SelectedAccount = null; }

    [RelayCommand]
    private void NewAccount()
    {
        FormTitle = "Yeni Cari Ekle";
        FormModel = new AccountFormModel();
        IsPanelOpen = false;
        IsFormOpen = true;
    }

    [RelayCommand]
    private async Task DeleteAccount(AccountItem? account)
    {
        if (account is null) return;
        if (_dialog is not null &&
            !_dialog.Confirm("Cari pasife alınacaktır. Devam edilsin mi?"))
            return;

        if (_api == null) return;

        IsLoading = true;
        try
        {
            var success = await _api.DeleteAsync($"api/accounts/{account.Id}");
            if (success)
            {
                _dialog?.Info("Cari başarıyla silindi (pasife alındı).", "Başarılı");
                ClosePanel();
                await LoadAccountsAsync();
            }
            else
            {
                _dialog?.Error("Cari silinirken hata oluştu.");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void EditAccount(AccountItem? account)
    {
        if (account == null) return;
        FormTitle = "Cari Düzenle";
        FormModel = new AccountFormModel
        {
            Id = account.Id,
            Code = account.Code,
            Name = account.Name,
            Type = account.Type,
            Phone = account.Phone,
            Email = account.Email,
            Address = account.Address,
            TaxNumber = account.TaxNumber,
            RiskLimit = account.RiskLimit,
            InitialBalance = account.Balance // Düzenleme anında mevcut bakiye
        };
        IsPanelOpen = false;
        IsFormOpen = true;
    }

    [RelayCommand]
    private async Task SaveAccount()
    {
        if (string.IsNullOrWhiteSpace(FormModel.Code) || string.IsNullOrWhiteSpace(FormModel.Name))
        {
            _dialog?.Error("Cari kodu ve cari unvanı zorunludur.");
            return;
        }

        if (_api == null) return;

        var isNew = _allAccounts.All(a => a.Id != FormModel.Id);

        if (isNew)
        {
            var req = new Re.Contracts.Accounts.CreateAccountRequest(
                Code: FormModel.Code,
                Name: FormModel.Name,
                AccountType: FormModel.Type == "Alıcı" ? "Customer" : FormModel.Type == "Satıcı" ? "Supplier" : "Both",
                TaxNumber: FormModel.TaxNumber,
                TaxOffice: FormModel.TaxOffice,
                TcKimlik: FormModel.TcKimlik,
                Phone: FormModel.Phone,
                MobilePhone: FormModel.MobilePhone,
                Phone2: null,
                Email: FormModel.Email,
                Website: FormModel.Website,
                AddressLine1: FormModel.Address,
                City: FormModel.City,
                District: FormModel.District,
                PostalCode: FormModel.PostalCode,
                CreditLimit: FormModel.RiskLimit,
                DefaultPaymentTermDays: FormModel.PaymentTermDays,
                Currency: FormModel.Currency,
                BankAccount: FormModel.BankAccount,
                Representative: FormModel.Representative,
                IsEInvoicePayer: FormModel.IsEInvoicePayer,
                EInvoiceAlias: FormModel.EInvoiceAlias
            );

            var result = await _api.PostAsync<Re.Contracts.Accounts.AccountResponse>("api/accounts", req);
            if (result != null)
            {
                _dialog?.Info("Cari başarıyla kaydedildi.", "Başarılı");
                IsFormOpen = false;
                await LoadAccountsAsync();
            }
            else
            {
                _dialog?.Error("Cari kaydedilirken bir hata oluştu veya bu kod zaten var.");
            }
        }
        else
        {
            var req = new UpdateAccountRequest(
                Name: FormModel.Name,
                AccountType: FormModel.Type,
                TaxNumber: FormModel.TaxNumber,
                TaxOffice: FormModel.TaxOffice,
                TcKimlik: FormModel.TcKimlik,
                Phone: FormModel.Phone,
                MobilePhone: FormModel.MobilePhone,
                Phone2: null,
                Email: FormModel.Email,
                Website: FormModel.Website,
                AddressLine1: FormModel.Address,
                City: FormModel.City,
                District: FormModel.District,
                PostalCode: FormModel.PostalCode,
                CreditLimit: FormModel.RiskLimit,
                DefaultPaymentTermDays: FormModel.PaymentTermDays,
                Currency: FormModel.Currency,
                BankAccount: FormModel.BankAccount,
                Representative: FormModel.Representative,
                IsEInvoicePayer: FormModel.IsEInvoicePayer,
                EInvoiceAlias: FormModel.EInvoiceAlias,
                IsActive: true
            );

            var result = await _api.PutAsync<AccountResponse>($"api/accounts/{FormModel.Id}", req);
            if (result != null)
            {
                _dialog?.Info("Cari başarıyla güncellendi.", "Başarılı");
                IsFormOpen = false;
                await LoadAccountsAsync();
            }
            else
            {
                _dialog?.Error("Cari güncellenirken bir hata oluştu.");
            }
        }
    }

    [RelayCommand] private void CloseForm() { IsFormOpen = false; }
}

public partial class AccountFormModel : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // Tab 1: Genel Bilgiler
    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _type = "Alıcı";
    [ObservableProperty] private string _taxNumber = string.Empty;
    [ObservableProperty] private string _taxOffice = string.Empty;
    [ObservableProperty] private string _tcKimlik = string.Empty;

    // Tab 2: İletişim
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _mobilePhone = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _website = string.Empty;

    // Tab 3: Adresler
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _city = string.Empty;
    [ObservableProperty] private string _district = string.Empty;
    [ObservableProperty] private string _postalCode = string.Empty;

    // Tab 4: Finansal Bilgiler
    [ObservableProperty] private int _paymentTermDays = 0;
    [ObservableProperty] private string _currency = "TRY";
    [ObservableProperty] private string _bankAccount = string.Empty;

    // Tab 5: Bakiye ve Risk
    [ObservableProperty] private decimal _riskLimit = 50000m;
    [ObservableProperty] private decimal _initialBalance = 0m;

    // Tab 8: CRM
    [ObservableProperty] private string _representative = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;

    // Tab 10: E-Dönüşüm
    [ObservableProperty] private bool _isEInvoicePayer = false;
    [ObservableProperty] private string _eInvoiceAlias = string.Empty;
}

public class AccountItem : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string BalanceType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    
    public string Phone { get; set; } = "Belirtilmemiş";
    public string Email { get; set; } = "Belirtilmemiş";
    public string Address { get; set; } = "Adres bilgisi yok.";
    public decimal RiskLimit { get; set; }
    public DateTime RegisterDate { get; set; }
    public ObservableCollection<AccountTransaction> Transactions { get; set; } = new();

    public bool IsActive => Status == "Aktif";
    public string NameInitials => string.Join("", Name.Split(' ').Take(2).Select(s => s.Length > 0 ? s[0].ToString() : "")).ToUpper();
    public decimal RiskPercentage => RiskLimit > 0 ? Math.Min((Balance / RiskLimit) * 100, 100) : 0;
}

public class AccountTransaction
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
}
