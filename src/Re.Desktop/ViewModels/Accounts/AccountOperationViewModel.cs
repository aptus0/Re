using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Re.Contracts.Accounts;
using Re.Desktop.Services;

namespace Re.Desktop.ViewModels.Accounts;

public partial class AccountOperationViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly IDialogService _dialog;
    public AccountItem Account { get; }
    public IReadOnlyList<string> OperationTypes { get; } = ["Debit", "Credit", "Collection", "Payment"];
    public IReadOnlyList<string> Currencies { get; } = ["TRY", "USD", "EUR", "GBP"];
    [ObservableProperty] private string _selectedOperationType = "Collection";
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private string _currency = "TRY";
    [ObservableProperty] private decimal _exchangeRate = 1;
    [ObservableProperty] private DateTime _movementDate = DateTime.Today;
    [ObservableProperty] private DateTime? _dueDate;
    [ObservableProperty] private string _description = "Account collection";
    [ObservableProperty] private string _referenceNumber = "";
    [ObservableProperty] private bool _isBusy;
    public string EffectText => SelectedOperationType is "Debit" or "Payment"
        ? "This operation increases the account balance."
        : "This operation decreases the account balance.";
    public event Action? Saved;

    public AccountOperationViewModel(ApiClient api, IDialogService dialog, AccountItem account)
    {
        _api = api; _dialog = dialog; Account = account;
    }

    partial void OnSelectedOperationTypeChanged(string value)
    {
        Description = value switch
        {
            "Collection" => "Account collection", "Payment" => "Account payment",
            "Debit" => "Manual debit entry", _ => "Manual credit entry"
        };
        OnPropertyChanged(nameof(EffectText));
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Amount <= 0 || ExchangeRate <= 0 || string.IsNullOrWhiteSpace(Description))
        {
            _dialog.Warning("Positive amount, exchange rate and description are required.", "Account Operation");
            return;
        }
        IsBusy = true;
        try
        {
            var result = await _api.PostAsync<AccountOperationResponse>(
                $"api/accounts/{Account.Id}/operations",
                new CreateAccountOperationRequest(SelectedOperationType, Amount, Currency,
                    ExchangeRate, MovementDate, DueDate, Description, ReferenceNumber));
            if (result is null)
            {
                _dialog.Error("Account operation could not be posted.", "Account Operation");
                return;
            }
            _dialog.Success(
                $"{result.OperationNumber} posted.\nPrevious balance: ₺{result.PreviousBalance:N2}\nCurrent balance: ₺{result.CurrentBalance:N2}",
                "Account Operation");
            Saved?.Invoke();
        }
        finally { IsBusy = false; }
    }
}
