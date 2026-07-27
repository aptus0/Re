using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Re.Contracts.Finance;
using Re.Desktop.Services;

namespace Re.Desktop.ViewModels.Finance;

public partial class TreasuryViewModel(ApiClient api, IDialogService dialog, INavigationService navigation) : ObservableObject
{
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private decimal _totalCash;
    [ObservableProperty] private decimal _totalBank;
    [ObservableProperty] private decimal _todayCashIn;
    [ObservableProperty] private decimal _todayCashOut;
    [ObservableProperty] private decimal _todayBankIn;
    [ObservableProperty] private decimal _todayBankOut;
    [ObservableProperty] private string _lastUpdated = "-";
    public ObservableCollection<CashRegisterResponse> CashRegisters { get; } = [];
    public ObservableCollection<BankAccountResponse> BankAccounts { get; } = [];
    public ObservableCollection<TreasuryMovementResponse> CashMovements { get; } = [];
    public ObservableCollection<TreasuryMovementResponse> BankMovements { get; } = [];
    public bool HasNoCash => !IsLoading && CashRegisters.Count == 0;
    public bool HasNoBank => !IsLoading && BankAccounts.Count == 0;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var result = await api.GetAsync<TreasuryDashboardResponse>("api/finance/treasury-dashboard");
            if (result is null) { dialog.Error("Kasa ve banka bilgileri alınamadı."); return; }
            Replace(CashRegisters, result.CashRegisters); Replace(BankAccounts, result.BankAccounts);
            Replace(CashMovements, result.CashMovements); Replace(BankMovements, result.BankMovements);
            TotalCash = result.TotalCashTRY; TotalBank = result.TotalBankTRY;
            TodayCashIn = result.TodayCashIn; TodayCashOut = result.TodayCashOut;
            TodayBankIn = result.TodayBankIn; TodayBankOut = result.TodayBankOut;
            LastUpdated = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
        }
        finally
        {
            IsLoading = false; OnPropertyChanged(nameof(HasNoCash)); OnPropertyChanged(nameof(HasNoBank));
        }
    }
    [RelayCommand] private void NewTransaction() => navigation.NavigateTo("Finance");
    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
    { target.Clear(); foreach (var item in source) target.Add(item); }
}
