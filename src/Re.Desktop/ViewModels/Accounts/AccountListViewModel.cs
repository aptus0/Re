using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Collections.Generic;
using Re.Desktop.Services;
using Re.Contracts.Accounts;
using Re.Desktop.Views.Accounts;
using System.Windows;
using Microsoft.Win32;
using System.IO;
using System.Text;

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
    [ObservableProperty] private string _selectedAccountGroup = "All";
    public IReadOnlyList<string> AccountGroups { get; } = ["All", "Customers", "Suppliers", "Customer + Supplier"];

    // Panel Kontrolü (Görüntüleme)
    [ObservableProperty] private bool _isPanelOpen;
    [ObservableProperty] private AccountItem? _selectedAccount;

    // Form Kontrolü (Ekleme/Editme)
    [ObservableProperty] private bool _isFormOpen;
    [ObservableProperty] private AccountFormModel _formModel = new();
    [ObservableProperty] private string _formTitle = "Add Account";

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
                    Phone = string.IsNullOrWhiteSpace(a.Phone) ? "Not specified" : a.Phone,
                    TaxNumber = a.TaxNumber ?? "",
                    Balance = a.CurrentBalance,
                    BalanceType = a.CurrentBalance >= 0 ? "Receivable" : "Payable",
                    Status = a.IsActive ? "Active" : "Inactive",
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
        if (value != null) _ = LoadAccount360Async(value);
    }

    private async Task LoadAccount360Async(AccountItem account)
    {
        if (_api == null) return;
        var detail = await _api.GetAsync<AccountResponse>($"api/accounts/{account.Id}");
        if (detail != null)
        {
            account.Phone = detail.Phone ?? detail.MobilePhone ?? "Not specified";
            account.Email = detail.Email ?? "Not specified";
            account.Address = string.Join(", ", new[] { detail.AddressLine1, detail.District, detail.City }.Where(x => !string.IsNullOrWhiteSpace(x)));
            account.RiskLimit = detail.CreditLimit;
            account.OnPropertyChanged();
        }
        var summary = await _api.GetAsync<AccountInvoiceSummaryResponse>($"api/accounts/{account.Id}/360");
        if (summary == null) return;
        account.InvoiceCount = summary.InvoiceCount;
        account.TotalInvoiced = summary.TotalInvoiced;
        account.OpenInvoiceBalance = summary.OpenBalance;
        account.UnitsSold = summary.UnitsSold;
        account.StockDocumentCount = summary.StockDocumentCount;
        account.CustomerSegment = summary.CustomerSegment;
        account.RiskScore = summary.RiskScore;
        account.RiskLevel = summary.RiskLevel;
        account.AgingCurrent = summary.Aging.Current;
        account.Aging1To30 = summary.Aging.Days1To30;
        account.Aging31To60 = summary.Aging.Days31To60;
        account.Aging61To90 = summary.Aging.Days61To90;
        account.AgingOver90 = summary.Aging.Over90;
        account.TotalOverdue = summary.Aging.TotalOverdue;
        account.OverdueInvoiceCount = summary.Aging.OverdueInvoiceCount;
        account.MaximumDaysOverdue = summary.Aging.MaximumDaysOverdue;
        account.TopProducts.Clear();
        foreach (var product in summary.TopProducts)
            account.TopProducts.Add(new AccountProductItem
            {
                Code = product.ProductCode, Name = product.ProductName,
                Quantity = product.Quantity, NetAmount = product.NetAmount
            });
        account.RecentInvoices.Clear();
        foreach (var invoice in summary.RecentInvoices)
            account.RecentInvoices.Add(new AccountInvoiceItem
            {
                DocumentNumber = invoice.DocumentNumber, Date = invoice.DocumentDate,
                Status = invoice.Status, TotalAmount = invoice.TotalAmount,
                PaidAmount = invoice.PaidAmount, LineCount = invoice.LineCount
            });
        account.Transactions.Clear();
        foreach (var item in summary.RecentActivities)
        {
            account.Transactions.Add(new AccountTransaction
            {
                Date = item.Date, Description = item.Description, Amount = item.Amount,
                Type = item.Type == "Debit" ? "Debit" : "Credit"
            });
        }
        account.OnPropertyChanged();
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
        if (SelectedAccountGroup == "Customers")
            query = query.Where(x => x.Type.Contains("Customer", StringComparison.OrdinalIgnoreCase));
        else if (SelectedAccountGroup == "Suppliers")
            query = query.Where(x => x.Type.Contains("Supplier", StringComparison.OrdinalIgnoreCase));
        else if (SelectedAccountGroup == "Customer + Supplier")
            query = query.Where(x => x.Type.Contains("Both", StringComparison.OrdinalIgnoreCase));

        Accounts.Clear();
        foreach (var account in query)
            Accounts.Add(account);

        UpdateSummaries();
    }

    private void UpdateSummaries()
    {
        TotalCount = Accounts.Count.ToString();
        var receivables = Accounts.Where(a => a.BalanceType == "Receivable").Sum(a => a.Balance);
        var payables = Math.Abs(Accounts.Where(a => a.BalanceType == "Payable").Sum(a => a.Balance));

        TotalReceivables = receivables.ToString("N2") + " ₺";
        TotalPayables = payables.ToString("N2") + " ₺";
        CustomerCount = Accounts.Count(a =>
            a.Type.Contains("Customer", StringComparison.OrdinalIgnoreCase) ||
            a.Type.Contains("Customer", StringComparison.OrdinalIgnoreCase) ||
            a.Type.Contains("Both", StringComparison.OrdinalIgnoreCase));
        SupplierCount = Accounts.Count(a =>
            a.Type.Contains("Supplier", StringComparison.OrdinalIgnoreCase) ||
            a.Type.Contains("Supplier", StringComparison.OrdinalIgnoreCase) ||
            a.Type.Contains("Both", StringComparison.OrdinalIgnoreCase));
    }

    [RelayCommand] private void ClosePanel() { IsPanelOpen = false; SelectedAccount = null; }

    [RelayCommand]
    private void ShowAccountDetailWindow(AccountItem? account)
    {
        account ??= SelectedAccount;
        if (account is null) return;
        var window = new AccountDetailWindow(account.Id, _api!, _dialog!)
        {
            Owner = Application.Current.Windows.OfType<Re.Desktop.Views.Shell.MainWindow>().FirstOrDefault(x => x.IsVisible)
        };
        window.ShowDialog();
    }

    [RelayCommand]
    private async Task NewOperation(AccountItem? account)
    {
        account ??= SelectedAccount;
        if (account is null || _api is null || _dialog is null)
        {
            _dialog?.Info("Select an account before posting an operation.", "Account Operation");
            return;
        }
        var window = new AccountOperationWindow
        {
            DataContext = new AccountOperationViewModel(_api, _dialog, account)
        };
        window.Owner = Application.Current.Windows.OfType<Re.Desktop.Views.Shell.MainWindow>()
            .FirstOrDefault(x => x.IsVisible);
        window.ShowDialog();
        await LoadAccountsAsync();
        var refreshed = _allAccounts.FirstOrDefault(x => x.Id == account.Id);
        if (refreshed is not null) SelectedAccount = refreshed;
    }

    [RelayCommand]
    private void ExportStatement(AccountItem? account)
    {
        account ??= SelectedAccount;
        if (account is null)
        {
            _dialog?.Info("Select an account to export its statement.", "Account Statement");
            return;
        }
        var dialog = new SaveFileDialog
        {
            Filter = "CSV Statement (*.csv)|*.csv",
            FileName = $"{account.Code}-statement-{DateTime.Now:yyyyMMdd}.csv"
        };
        if (dialog.ShowDialog() != true) return;
        var csv = new StringBuilder()
            .AppendLine("Date,Description,Direction,Amount");
        foreach (var item in account.Transactions)
            csv.AppendLine($"{item.Date:yyyy-MM-dd},\"{item.Description.Replace("\"", "\"\"")}\",{item.Type},{item.Amount:0.00}");
        csv.AppendLine($",Closing Balance,,{account.Balance:0.00}");
        File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(true));
        _dialog?.Success("Account statement exported successfully.", "Account Statement");
    }

    [RelayCommand]
    private void NewAccount()
    {
        FormTitle = "Add Account";
        FormModel = new AccountFormModel();
        IsPanelOpen = false;
        IsFormOpen = true;
    }

    [RelayCommand]
    private async Task DeleteAccount(AccountItem? account)
    {
        if (account is null) return;
        if (_dialog is not null &&
            !_dialog.Confirm("The account will be deactivated. Continue?"))
            return;

        if (_api == null) return;

        IsLoading = true;
        try
        {
            var success = await _api.DeleteAsync($"api/accounts/{account.Id}");
            if (success)
            {
                _dialog?.Success("Account deactivated successfully.", "Success");
                ClosePanel();
                await LoadAccountsAsync();
            }
            else
            {
                _dialog?.Error("An error occurred while deactivating the account.");
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
        FormTitle = "Accounts Edit";
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
            InitialBalance = account.Balance // Editme anında mevcut bakiye
        };
        IsPanelOpen = false;
        IsFormOpen = true;
    }

    [RelayCommand]
    private async Task SaveAccount()
    {
        if (string.IsNullOrWhiteSpace(FormModel.Code) || string.IsNullOrWhiteSpace(FormModel.Name))
        {
            _dialog?.Error("Account code and account name are required.");
            return;
        }

        if (_api == null) return;

        var isNew = _allAccounts.All(a => a.Id != FormModel.Id);

        if (isNew)
        {
            var req = new Re.Contracts.Accounts.CreateAccountRequest(
                Code: FormModel.Code,
                Name: FormModel.Name,
                AccountType: FormModel.Type == "Customer" ? "Customer" : FormModel.Type == "Supplier" ? "Supplier" : "Both",
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
                _dialog?.Success("Account saved successfully.", "Success");
                IsFormOpen = false;
                await LoadAccountsAsync();
            }
            else
            {
                _dialog?.Error("The account could not be saved or the code already exists.");
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
                _dialog?.Success("Account updated successfully.", "Success");
                IsFormOpen = false;
                await LoadAccountsAsync();
            }
            else
            {
                _dialog?.Error("An error occurred while updating the account.");
            }
        }
    }

    [RelayCommand] private void CloseForm() { IsFormOpen = false; }
}

public partial class AccountFormModel : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Tab 1: Genel Informationler
    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _type = "Customer";
    [ObservableProperty] private string _taxNumber = string.Empty;
    [ObservableProperty] private string _taxOffice = string.Empty;
    [ObservableProperty] private string _tcKimlik = string.Empty;

    // Tab 2: Contact
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _mobilePhone = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _website = string.Empty;

    // Tab 3: Adresler
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _city = string.Empty;
    [ObservableProperty] private string _district = string.Empty;
    [ObservableProperty] private string _postalCode = string.Empty;

    // Tab 4: Financeal Informationler
    [ObservableProperty] private int _paymentTermDays = 0;
    [ObservableProperty] private string _currency = "TRY";
    [ObservableProperty] private string _bankAccount = string.Empty;

    // Tab 5: Bakiye ve Risk
    [ObservableProperty] private decimal _riskLimit = 50000m;
    [ObservableProperty] private decimal _initialBalance = 0m;

    // Tab 8: CRM
    [ObservableProperty] private string _representative = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;

    // Tab 10: E-Transformation
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

    public string Phone { get; set; } = "Not specified";
    public string Email { get; set; } = "Not specified";
    public string Address { get; set; } = "Adres bilgisi yok.";
    public decimal RiskLimit { get; set; }
    public DateTime RegisterDate { get; set; }
    public ObservableCollection<AccountTransaction> Transactions { get; set; } = new();
    public int InvoiceCount { get; set; }
    public decimal TotalInvoiced { get; set; }
    public decimal OpenInvoiceBalance { get; set; }
    public decimal UnitsSold { get; set; }
    public int StockDocumentCount { get; set; }
    public ObservableCollection<AccountProductItem> TopProducts { get; } = [];
    public ObservableCollection<AccountInvoiceItem> RecentInvoices { get; } = [];
    public string CustomerSegment { get; set; } = "Standard";
    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = "Low";
    public decimal AgingCurrent { get; set; }
    public decimal Aging1To30 { get; set; }
    public decimal Aging31To60 { get; set; }
    public decimal Aging61To90 { get; set; }
    public decimal AgingOver90 { get; set; }
    public decimal TotalOverdue { get; set; }
    public int OverdueInvoiceCount { get; set; }
    public int MaximumDaysOverdue { get; set; }
    public string RiskColor => RiskScore >= 75 ? "#BA0517" : RiskScore >= 50 ? "#B45309" :
        RiskScore >= 25 ? "#0176D3" : "#107C10";

    public void OnPropertyChanged() => base.OnPropertyChanged(string.Empty);

    public bool IsActive => Status == "Active";
    public string NameInitials => string.Join("", Name.Split(' ').Take(2).Select(s => s.Length > 0 ? s[0].ToString() : "")).ToUpper();
    public decimal RiskPercentage => RiskLimit > 0
        ? Math.Clamp(Math.Max(0, Balance) / RiskLimit * 100, 0, 100) : 0;
}

public class AccountProductItem
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal NetAmount { get; set; }
}

public class AccountInvoiceItem
{
    public string DocumentNumber { get; set; } = "";
    public DateTime Date { get; set; }
    public string Status { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int LineCount { get; set; }
    public decimal OpenAmount => TotalAmount - PaidAmount;
}

public class AccountTransaction
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
}
