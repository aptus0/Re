using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Re.Contracts.Inventory;
using Re.Desktop.Services;

namespace Re.Desktop.ViewModels.Products;

public partial class ProductDashboardViewModel(
    INavigationService navigationService, ApiClient api, IDialogService dialog) : ObservableObject
{
    [ObservableProperty] private int _totalProducts;
    [ObservableProperty] private int _activeProducts;
    [ObservableProperty] private decimal _totalStockQuantity;
    [ObservableProperty] private decimal _totalStockValue;
    [ObservableProperty] private int _criticalStockCount;
    [ObservableProperty] private int _outOfStockCount;
    [ObservableProperty] private int _negativeStockCount;
    [ObservableProperty] private int _inactive30DaysCount;
    [ObservableProperty] private decimal _todayInbound;
    [ObservableProperty] private decimal _todayOutbound;
    [ObservableProperty] private string _lastUpdated = "Not updated yet";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private CriticalStockItem? _selectedCriticalProduct;

    public ObservableCollection<CriticalStockItem> CriticalProducts { get; } = [];
    public ObservableCollection<RecentStockMovementItem> RecentMovements { get; } = [];
    public bool HasNoCriticalProducts => !IsLoading && CriticalProducts.Count == 0;
    public bool HasNoMovements => !IsLoading && RecentMovements.Count == 0;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var result = await api.GetAsync<InventoryDashboardResponse>("api/inventory-dashboard");
            if (result is null) { dialog.Error("Inventory control center data could not be loaded."); return; }
            TotalProducts = result.TotalProducts; ActiveProducts = result.ActiveProducts;
            TotalStockQuantity = result.TotalStockQuantity; TotalStockValue = result.TotalStockValue;
            CriticalStockCount = result.CriticalStockCount; OutOfStockCount = result.OutOfStockCount;
            NegativeStockCount = result.NegativeStockCount; Inactive30DaysCount = result.Inactive30DaysCount;
            TodayInbound = result.TodayInbound; TodayOutbound = result.TodayOutbound;
            CriticalProducts.Clear(); foreach (var item in result.CriticalProducts) CriticalProducts.Add(item);
            RecentMovements.Clear(); foreach (var item in result.RecentMovements) RecentMovements.Add(item);
            LastUpdated = result.RefreshedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");
            OnPropertyChanged(nameof(HasNoCriticalProducts)); OnPropertyChanged(nameof(HasNoMovements));
        }
        catch (Exception ex) { dialog.Error($"Inventory control center could not be loaded.\n{ex.GetBaseException().Message}"); }
        finally { IsLoading = false; OnPropertyChanged(nameof(HasNoCriticalProducts)); OnPropertyChanged(nameof(HasNoMovements)); }
    }

    [RelayCommand] private void NavigateToProducts() => navigationService.NavigateTo("Products");
    [RelayCommand] private void NavigateToMovements() => navigationService.NavigateTo("StockMovements");
}
