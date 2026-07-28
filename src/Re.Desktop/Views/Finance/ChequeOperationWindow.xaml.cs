using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using System.Threading.Tasks;
using Re.Contracts.Accounts;
using Re.Desktop.Services;

namespace Re.Desktop.Views.Finance;

public partial class ChequeOperationWindow : Window, INotifyPropertyChanged
{
    private readonly IChequeNoteService _service;
    private readonly ApiClient _api;
    private readonly IDialogService _dialog;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Number { get; set; } = "CK-" + new Random().Next(10000, 99999);
    
    private string _selectedChequeType = "Customer Cheque";
    public string SelectedChequeType
    {
        get => _selectedChequeType;
        set
        {
            _selectedChequeType = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedChequeType)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBankEnabled)));
        }
    }

    public List<string> ChequeTypes { get; } = new()
    {
        "Customer Cheque",
        "Customer Promissory Note",
        "Our Cheque",
        "Our Promissory Note"
    };

    public List<AccountListResponse> Accounts { get; } = new();
    public AccountListResponse? SelectedAccount { get; set; }

    public string Amount { get; set; } = "0.00";
    public string Currency { get; set; } = "TRY";
    public List<string> Currencies { get; } = new() { "TRY", "USD", "EUR" };

    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);
    public string BankName { get; set; } = string.Empty;
    public string Drawer { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public bool IsBankEnabled => SelectedChequeType.Contains("Cheque");

    public ChequeOperationWindow(IChequeNoteService service, ApiClient api, IDialogService dialog)
    {
        InitializeComponent();
        _service = service;
        _api = api;
        _dialog = dialog;
        DataContext = this;
        _ = LoadAccountsAsync();
    }

    private async Task LoadAccountsAsync()
    {
        try
        {
            var response = await _api.GetAsync<Re.Contracts.Common.PagedResponse<AccountListResponse>>("api/accounts?page=1&size=1000");
            if (response?.Items != null)
            {
                Accounts.Clear();
                foreach (var a in response.Items) Accounts.Add(a);
            }
        }
        catch { }
    }

    private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            this.DragMove();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedAccount == null)
        {
            _dialog.Error("Please select a target account.", "Validation Error");
            return;
        }

        if (!decimal.TryParse(Amount, out var amt) || amt <= 0)
        {
            _dialog.Error("Please enter a valid positive amount.", "Validation Error");
            return;
        }

        try
        {
            var item = new ChequeNoteItem
            {
                Id = Guid.NewGuid(),
                Number = Number.Trim(),
                Type = MapType(SelectedChequeType),
                AccountId = SelectedAccount.Id,
                AccountName = SelectedAccount.Name,
                Amount = amt,
                Currency = Currency,
                DueDate = DueDate,
                IssueDate = DateTime.Today,
                BankName = IsBankEnabled ? BankName.Trim() : "",
                Drawer = Drawer.Trim(),
                Description = Description.Trim(),
                Status = ChequeNoteStatus.Portfolio
            };

            await _service.SaveAsync(item);
            _dialog.Success("Document saved to portfolio successfully.", "Success");
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            _dialog.Error($"Failed to save document: {ex.Message}", "Error");
        }
    }

    private static ChequeNoteType MapType(string selectedType)
    {
        return selectedType switch
        {
            "Customer Promissory Note" => ChequeNoteType.CustomerNote,
            "Our Cheque" => ChequeNoteType.SupplierCheque,
            "Our Promissory Note" => ChequeNoteType.SupplierNote,
            _ => ChequeNoteType.CustomerCheque
        };
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}