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
    [ObservableProperty] private decimal _previousMonthSalesTotal;
    [ObservableProperty] private decimal _salesChangePercent;
    [ObservableProperty] private decimal _todayCollections;
    [ObservableProperty] private decimal _todayPayments;
    [ObservableProperty] private int _draftInvoiceCount;
    [ObservableProperty] private int _overdueInvoiceCount;
    [ObservableProperty] private decimal _overdueInvoiceTotal;
    [ObservableProperty] private int _criticalStockCount;
    [ObservableProperty] private int _outOfStockCount;
    [ObservableProperty] private string _statusMessage = "Preparing data...";

    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<RecentTransactionItem> RecentTransactions { get; } = new();
    public ObservableCollection<DashboardSalesPoint> SalesTrend { get; } = new();
    public ObservableCollection<DashboardTopProductItem> TopProducts { get; } = new();
    public ObservableCollection<DashboardAlertItem> Alerts { get; } = new();

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
                PreviousMonthSalesTotal = data.PreviousMonthSalesTotal;
                SalesChangePercent = data.SalesChangePercent;
                TodayCollections = data.TodayCollections;
                TodayPayments = data.TodayPayments;
                DraftInvoiceCount = data.DraftInvoiceCount;
                OverdueInvoiceCount = data.OverdueInvoiceCount;
                OverdueInvoiceTotal = data.OverdueInvoiceTotal;
                CriticalStockCount = data.CriticalStockCount;
                OutOfStockCount = data.OutOfStockCount;

                Replace(RecentTransactions, data.RecentTransactions);
                Replace(SalesTrend, data.SalesTrend);
                Replace(TopProducts, data.TopProducts);
                Replace(Alerts, data.Alerts);
                StatusMessage = $"Last updated: {System.DateTime.Now:HH:mm:ss}";
            }
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Dashboard could not be loaded: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static void Replace<T>(ObservableCollection<T> target, System.Collections.Generic.IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
            target.Add(item);
    }
}
