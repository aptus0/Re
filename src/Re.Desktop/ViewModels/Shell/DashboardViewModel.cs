using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Re.Contracts.Common;
using Re.Contracts.Dashboard;
using Re.Desktop.Services;

namespace Re.Desktop.ViewModels.Shell;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ApiClient? _api;

    [ObservableProperty] private int _totalAccountCount;
    [ObservableProperty] private decimal _totalReceivables;
    [ObservableProperty] private decimal _totalPayables;
    [ObservableProperty] private decimal _totalCashBalance;
    [ObservableProperty] private decimal _totalBankBalance;
    [ObservableProperty] private decimal _currentMonthSalesTotal;
    [ObservableProperty] private int _currentMonthSalesCount;

    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<RecentTransactionItem> RecentTransactions { get; } = new();

    public DashboardViewModel() { } // Design-time

    public DashboardViewModel(ApiClient api)
    {
        _api = api;
    }

    [RelayCommand]
    private async Task LoadDashboardAsync()
    {
        if (_api == null) return;

        IsLoading = true;
        try
        {
            var data = await _api.GetAsync<DashboardSummaryResponse>("api/dashboard/summary");
            if (data != null)
            {
                TotalAccountCount = data.TotalAccountCount;
                TotalReceivables = data.TotalReceivables;
                TotalPayables = data.TotalPayables;
                TotalCashBalance = data.TotalCashBalance;
                TotalBankBalance = data.TotalBankBalance;
                CurrentMonthSalesTotal = data.CurrentMonthSalesTotal;
                CurrentMonthSalesCount = data.CurrentMonthSalesCount;

                RecentTransactions.Clear();
                foreach (var item in data.RecentTransactions)
                {
                    RecentTransactions.Add(item);
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
