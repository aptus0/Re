using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Re.Contracts.Common;
using Re.Contracts.Inventory;
using Re.Contracts.Products;
using Re.Desktop.Services;

namespace Re.Desktop.ViewModels.StockMovements;

public partial class InventoryOperationViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly IDialogService _dialog;
    public IReadOnlyList<string> OperationTypes { get; } = ["Receipt", "Issue", "Transfer", "Count", "Waste"];
    public ObservableCollection<ProductListResponse> Products { get; } = [];
    public ObservableCollection<WarehouseLookupItem> Warehouses { get; } = [];
    public ObservableCollection<ProductVariantResponse> Variants { get; } = [];
    [ObservableProperty] private string _selectedOperationType = "Receipt";
    [ObservableProperty] private ProductListResponse? _selectedProduct;
    [ObservableProperty] private WarehouseLookupItem? _sourceWarehouse;
    [ObservableProperty] private WarehouseLookupItem? _destinationWarehouse;
    [ObservableProperty] private ProductVariantResponse? _selectedVariant;
    [ObservableProperty] private decimal _quantity = 1;
    [ObservableProperty] private decimal _unitCost;
    [ObservableProperty] private string _lotNumber = "";
    [ObservableProperty] private string _serialNumber = "";
    [ObservableProperty] private DateTime? _expiryDate;
    [ObservableProperty] private string _referenceNumber = "";
    [ObservableProperty] private string _reason = "";
    [ObservableProperty] private bool _isBusy;
    public bool IsTransfer => SelectedOperationType == "Transfer";
    public string QuantityLabel => SelectedOperationType == "Count" ? "Counted quantity *" : "Quantity *";
    public event Action? Saved;

    public InventoryOperationViewModel(ApiClient api, IDialogService dialog)
    {
        _api = api; _dialog = dialog; _ = LoadAsync();
    }

    partial void OnSelectedOperationTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsTransfer)); OnPropertyChanged(nameof(QuantityLabel));
        Reason = value switch
        {
            "Receipt" => "Manual inventory receipt", "Issue" => "Manual inventory issue",
            "Transfer" => "Warehouse transfer", "Count" => "Physical inventory count",
            _ => "Waste / loss"
        };
    }

    async partial void OnSelectedProductChanged(ProductListResponse? value)
    {
        UnitCost = value?.PurchasePrice ?? 0; Variants.Clear();
        if (value is null) return;
        var variants = await _api.GetAsync<IReadOnlyCollection<ProductVariantResponse>>(
            $"api/products/{value.Id}/variants");
        foreach (var item in variants?.Where(x => x.IsActive) ?? []) Variants.Add(item);
    }

    private async Task LoadAsync()
    {
        var products = await _api.GetAsync<PagedResponse<ProductListResponse>>("api/products?isActive=true&page=1&size=500");
        foreach (var item in products?.Items ?? []) Products.Add(item);
        var warehouses = await _api.GetAsync<IReadOnlyCollection<WarehouseLookupItem>>("api/stock-movements/warehouses");
        foreach (var item in warehouses ?? []) Warehouses.Add(item);
        SelectedProduct = Products.FirstOrDefault(); SourceWarehouse = Warehouses.FirstOrDefault();
        DestinationWarehouse = Warehouses.Skip(1).FirstOrDefault();
        OnSelectedOperationTypeChanged(SelectedOperationType);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedProduct is null || SourceWarehouse is null || Quantity <= 0 ||
            string.IsNullOrWhiteSpace(Reason))
        {
            _dialog.Warning("Product, warehouse, positive quantity and reason are required.", "Inventory Operation");
            return;
        }
        if (IsTransfer && (DestinationWarehouse is null || DestinationWarehouse.Id == SourceWarehouse.Id))
        {
            _dialog.Warning("Select a different destination warehouse.", "Warehouse Transfer");
            return;
        }
        IsBusy = true;
        try
        {
            var request = new InventoryOperationRequest(SelectedOperationType, SelectedProduct.Id,
                SourceWarehouse.Id, IsTransfer ? DestinationWarehouse?.Id : null, Quantity, UnitCost,
                SelectedVariant?.Id, LotNumber, SerialNumber, ExpiryDate, Reason, ReferenceNumber);
            var result = await _api.PostAsync<InventoryOperationResponse>("api/stock-movements/operations", request);
            if (result is null)
            {
                _dialog.Error("Operation was rejected. Check warehouse balance, serial quantity and required fields.",
                    "Inventory Operation");
                return;
            }
            _dialog.Success(
                $"{result.OperationNumber} posted successfully.\nSource balance: {result.SourceBalance:0.###}" +
                (result.DestinationBalance.HasValue ? $"\nDestination balance: {result.DestinationBalance:0.###}" : ""),
                "Inventory Operation");
            Saved?.Invoke();
        }
        finally { IsBusy = false; }
    }
}
