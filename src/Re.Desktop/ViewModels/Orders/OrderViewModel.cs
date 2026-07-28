using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Re.Contracts.Accounts;
using Re.Contracts.Common;
using Re.Contracts.Inventory;
using Re.Contracts.Orders;
using Re.Contracts.Products;
using Re.Desktop.Services;
using System.Collections.ObjectModel;

namespace Re.Desktop.ViewModels.Orders;

public partial class OrderViewModel(ApiClient api, IDialogService dialog) : ObservableObject
{
    public ObservableCollection<OrderListResponse> Orders { get; } = [];
    public ObservableCollection<AccountListResponse> Accounts { get; } = [];
    public ObservableCollection<WarehouseLookupItem> Warehouses { get; } = [];
    public ObservableCollection<ProductListResponse> Products { get; } = [];
    public ObservableCollection<OrderLineDraft> Lines { get; } = [];
    public IReadOnlyList<string> OrderTypes { get; } = ["Sales", "Purchase"];
    [ObservableProperty] private OrderListResponse? _selectedOrder;
    [ObservableProperty] private AccountListResponse? _selectedAccount;
    [ObservableProperty] private WarehouseLookupItem? _selectedWarehouse;
    [ObservableProperty] private ProductListResponse? _selectedProduct;
    [ObservableProperty] private string _selectedType = "Sales";
    [ObservableProperty] private string _orderNumber = $"SO-{DateTime.Now:yyyyMMddHHmmss}";
    [ObservableProperty] private DateTime _orderDate = DateTime.Today;
    [ObservableProperty] private DateTime? _deliveryDate = DateTime.Today.AddDays(7);
    [ObservableProperty] private string _customerReference = "";
    [ObservableProperty] private string _notes = "";
    [ObservableProperty] private bool _isEditorOpen;
    [ObservableProperty] private bool _isLoading;
    public decimal SubTotal => Lines.Sum(x => x.NetAmount);
    public decimal VatTotal => Lines.Sum(x => x.TaxAmount);
    public decimal GrandTotal => SubTotal + VatTotal;

    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            await LoadOrders();
            var accounts = await api.GetAsync<PagedResponse<AccountListResponse>>("api/accounts?isActive=true&page=1&size=500");
            Accounts.Clear(); foreach (var x in accounts?.Items ?? []) Accounts.Add(x);
            var warehouses = await api.GetAsync<IReadOnlyCollection<WarehouseLookupItem>>("api/stock-movements/warehouses");
            Warehouses.Clear(); foreach (var x in warehouses ?? []) Warehouses.Add(x);
            var products = await api.GetAsync<PagedResponse<ProductListResponse>>("api/products?isActive=true&page=1&size=500");
            Products.Clear(); foreach (var x in products?.Items ?? []) Products.Add(x);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task Refresh() => await LoadOrders();
    private async Task LoadOrders()
    {
        var data = await api.GetAsync<PagedResponse<OrderListResponse>>("api/orders?page=1&size=250");
        Orders.Clear(); foreach (var x in data?.Items ?? []) Orders.Add(x);
    }

    [RelayCommand]
    private void New()
    {
        SelectedType = "Sales"; OrderNumber = $"SO-{DateTime.Now:yyyyMMddHHmmss}";
        OrderDate = DateTime.Today; DeliveryDate = DateTime.Today.AddDays(7);
        CustomerReference = ""; Notes = ""; Lines.Clear();
        SelectedAccount = Accounts.FirstOrDefault(); SelectedWarehouse = Warehouses.FirstOrDefault();
        IsEditorOpen = true; TotalsChanged();
    }

    partial void OnSelectedTypeChanged(string value) =>
        OrderNumber = $"{(value == "Purchase" ? "PO" : "SO")}-{DateTime.Now:yyyyMMddHHmmss}";

    [RelayCommand]
    private void AddLine()
    {
        if (SelectedProduct is null) { dialog.Warning("Select a product."); return; }
        var existing = Lines.FirstOrDefault(x => x.ProductId == SelectedProduct.Id);
        if (existing is not null) existing.Quantity++;
        else
        {
            var line = new OrderLineDraft
            {
                ProductId = SelectedProduct.Id, ProductCode = SelectedProduct.Code,
                ProductName = SelectedProduct.Name, Quantity = 1,
                UnitPrice = SelectedType == "Purchase" ? SelectedProduct.PurchasePrice : SelectedProduct.SalePrice,
                VatRate = SelectedProduct.VatRate
            };
            line.PropertyChanged += (_, _) => TotalsChanged(); Lines.Add(line);
        }
        TotalsChanged();
    }

    [RelayCommand] private void RemoveLine(OrderLineDraft? line) { if (line is not null) Lines.Remove(line); TotalsChanged(); }
    [RelayCommand] private void CloseEditor() => IsEditorOpen = false;

    [RelayCommand]
    private async Task Save()
    {
        if (SelectedAccount is null || SelectedWarehouse is null || Lines.Count == 0)
        { dialog.Warning("Account, warehouse and at least one product are required."); return; }
        var request = new CreateOrderRequest(SelectedAccount.Id, SelectedWarehouse.Id, OrderNumber,
            SelectedType, OrderDate, DeliveryDate, "TRY", 1, CustomerReference, Notes,
            Lines.Select(x => new SaveOrderLineRequest(x.ProductId, null, x.Quantity, x.UnitPrice,
                x.DiscountPercent, x.VatRate, x.Notes)).ToList());
        var result = await api.PostAsync<OrderResponse>("api/orders", request);
        if (result is null) { dialog.Error("Order could not be saved."); return; }
        IsEditorOpen = false; dialog.Success("Order saved as draft."); await LoadOrders();
    }

    [RelayCommand]
    private async Task Confirm()
    {
        if (SelectedOrder is null) { dialog.Info("Select an order."); return; }
        if (SelectedOrder.Status != "Draft") { dialog.Warning("Only draft orders can be confirmed."); return; }
        var result = await api.PostAsync<OrderResponse>($"api/orders/{SelectedOrder.Id}/confirm", new { });
        if (result is not null) { dialog.Success("Order confirmed."); await LoadOrders(); }
    }

    [RelayCommand]
    private async Task Fulfil()
    {
        if (SelectedOrder is null) { dialog.Info("Select an order."); return; }
        var detail = await api.GetAsync<OrderResponse>($"api/orders/{SelectedOrder.Id}");
        if (detail is null) return;
        var remaining = detail.Lines.Where(x => x.RemainingQuantity > 0)
            .Select(x => new FulfilOrderLineRequest(x.Id, x.RemainingQuantity)).ToList();
        if (remaining.Count == 0) { dialog.Info("This order has no remaining quantity."); return; }
        if (!dialog.Confirm($"Post all remaining quantities for {detail.OrderNumber} to warehouse stock?", "Fulfil Order")) return;
        var result = await api.PostAsync<OrderResponse>($"api/orders/{detail.Id}/fulfil", remaining);
        if (result is not null) { dialog.Success("Warehouse fulfilment posted."); await LoadOrders(); }
    }

    [RelayCommand]
    private async Task CreateInvoice()
    {
        if (SelectedOrder is null || SelectedOrder.Type != "Sales") { dialog.Info("Select a sales order."); return; }
        var result = await api.PostAsync<object>($"api/orders/{SelectedOrder.Id}/create-invoice", new { });
        if (result is not null) { dialog.Success("Draft sales invoice created from order."); await LoadOrders(); }
    }

    private void TotalsChanged()
    { OnPropertyChanged(nameof(SubTotal)); OnPropertyChanged(nameof(VatTotal)); OnPropertyChanged(nameof(GrandTotal)); }
}

public partial class OrderLineDraft : ObservableObject
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(NetAmount))] [NotifyPropertyChangedFor(nameof(TaxAmount))] private decimal _quantity = 1;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(NetAmount))] [NotifyPropertyChangedFor(nameof(TaxAmount))] private decimal _unitPrice;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(NetAmount))] [NotifyPropertyChangedFor(nameof(TaxAmount))] private decimal _discountPercent;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TaxAmount))] private decimal _vatRate = 20;
    [ObservableProperty] private string _notes = "";
    public decimal NetAmount => Quantity * UnitPrice * (1 - DiscountPercent / 100m);
    public decimal TaxAmount => NetAmount * VatRate / 100m;
}
