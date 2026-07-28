using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Re.Contracts.Inventory;
using Re.Desktop.Services;
using Re.Desktop.ViewModels.Products;

namespace Re.Desktop.ViewModels.StockMovements;

public partial class StockAdjustmentViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly IDialogService _dialog;
    public ProductDetailItem Product { get; }
    public ObservableCollection<WarehouseLookupItem> Warehouses { get; } = [];
    [ObservableProperty] private WarehouseLookupItem? _selectedWarehouse;
    [ObservableProperty] private decimal _quantity;
    [ObservableProperty] private decimal _unitCost;
    [ObservableProperty] private string _reason = "Physical count correction";
    [ObservableProperty] private string _referenceNumber = "";
    [ObservableProperty] private bool _isBusy;
    public event Action? Saved;

    public StockAdjustmentViewModel(ApiClient api, IDialogService dialog, ProductDetailItem product)
    {
        _api = api; _dialog = dialog; Product = product; UnitCost = product.PurchasePrice;
        _ = LoadWarehousesAsync();
    }

    private async Task LoadWarehousesAsync()
    {
        var items = await _api.GetAsync<IReadOnlyCollection<WarehouseLookupItem>>("api/stock-movements/warehouses");
        foreach (var item in items ?? []) Warehouses.Add(item);
        SelectedWarehouse = Warehouses.FirstOrDefault();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedWarehouse is null || Quantity == 0 || string.IsNullOrWhiteSpace(Reason))
        {
            _dialog.Warning("Warehouse, non-zero quantity and reason are required.", "Stock Adjustment");
            return;
        }
        IsBusy = true;
        try
        {
            var result = await _api.PostAsync<StockAdjustmentResponse>("api/stock-movements/adjustments",
                new CreateStockAdjustmentRequest(Product.Id, SelectedWarehouse.Id, Quantity,
                    UnitCost, Reason, ReferenceNumber));
            if (result is null)
            {
                _dialog.Error("Stock adjustment could not be posted. Negative stock is not allowed.", "Stock Adjustment");
                return;
            }
            _dialog.Success(
                $"Stock posted successfully.\nPrevious: {result.PreviousStock:0.###}\nCurrent: {result.CurrentStock:0.###}",
                "Stock Adjustment");
            Saved?.Invoke();
        }
        finally { IsBusy = false; }
    }
}
