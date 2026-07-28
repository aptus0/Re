using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Re.Contracts.Accounts;
using Re.Contracts.Common;
using Re.Contracts.Finance;
using Re.Contracts.Inventory;
using Re.Contracts.Products;
using Re.Contracts.Sales;
using Re.Desktop.Services;
using System.Collections.ObjectModel;

namespace Re.Desktop.ViewModels.POS;

public partial class PosViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly IDialogService _dialog;
    private List<ProductListResponse> _allProducts = [];

    public ObservableCollection<PosCartLine> Cart { get; } = new();
    public ObservableCollection<ProductListResponse> ProductResults { get; } = new();
    public ObservableCollection<AccountListResponse> Customers { get; } = new();
    public ObservableCollection<CashRegisterResponse> CashRegisters { get; } = new();
    public ObservableCollection<WarehouseLookupItem> Warehouses { get; } = new();
    public IReadOnlyList<string> PaymentMethods { get; } = ["Nakit", "Kredi Kartı", "Açık Hesap (Cari)"];

    [ObservableProperty] private bool _isProductSaleEnabled = true;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private ProductListResponse? _selectedProduct;
    [ObservableProperty] private AccountListResponse? _selectedCustomer;
    [ObservableProperty] private CashRegisterResponse? _selectedCashRegister;
    [ObservableProperty] private WarehouseLookupItem? _selectedWarehouse;
    [ObservableProperty] private string _paymentMethod = "Cash";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _documentNumber = $"POS-{DateTime.Now:yyyyMMdd-HHmmss}";
    [ObservableProperty] private decimal _cashTransactionAmount;
    [ObservableProperty] private string _cashTransactionDescription = string.Empty;

    public int ItemCount => Cart.Sum(x => (int)x.Quantity);
    public decimal SubTotal => Cart.Sum(x => x.NetAmount);
    public decimal TaxTotal => Cart.Sum(x => x.TaxAmount);
    public decimal GrandTotal => SubTotal + TaxTotal;
    public bool IsCashOperationMode => !IsProductSaleEnabled;

    public PosViewModel(ApiClient api, IDialogService dialog)
    {
        _api = api;
        _dialog = dialog;
        Cart.CollectionChanged += (_, _) => NotifyTotals();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var products = await _api.GetAsync<PagedResponse<ProductListResponse>>("api/products?page=1&size=500");
            _allProducts = products?.Items.Where(x => x.IsActive).ToList() ?? [];
            ApplyProductFilter();

            var customers = await _api.GetAsync<PagedResponse<AccountListResponse>>("api/accounts?isActive=true&page=1&size=500");
            foreach (var item in customers?.Items ?? []) Customers.Add(item);
            SelectedCustomer = Customers.FirstOrDefault();

            var registers = await _api.GetAsync<IReadOnlyCollection<CashRegisterResponse>>("api/finance/cashregisters");
            foreach (var item in registers ?? []) CashRegisters.Add(item);
            SelectedCashRegister = CashRegisters.FirstOrDefault();

            var warehouses = await _api.GetAsync<IReadOnlyCollection<WarehouseLookupItem>>("api/stock-movements/warehouses");
            foreach (var item in warehouses ?? []) Warehouses.Add(item);
            SelectedWarehouse = Warehouses.FirstOrDefault();
        }
        finally { IsLoading = false; }
    }

    partial void OnSearchTextChanged(string value) => ApplyProductFilter();
    partial void OnIsProductSaleEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(IsCashOperationMode));
        if (!value) Cart.Clear();
    }

    private void ApplyProductFilter()
    {
        var query = _allProducts.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(x => x.Code.Contains(term, StringComparison.OrdinalIgnoreCase)
                || x.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (x.Barcode?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        ProductResults.Clear();
        foreach (var item in query.Take(20)) ProductResults.Add(item);
    }

    [RelayCommand]
    private void AddProduct(ProductListResponse? product)
    {
        product ??= SelectedProduct;
        if (product is null) return;
        var existing = Cart.FirstOrDefault(x => x.ProductId == product.Id);
        if (existing is null)
        {
            var line = new PosCartLine(product.Id, product.Code, product.Name, product.SalePrice, product.VatRate, product.StockLevel);
            line.PropertyChanged += (_, _) => NotifyTotals();
            Cart.Add(line);
        }
        else existing.Quantity++;
        SearchText = string.Empty;
        NotifyTotals();
    }

    [RelayCommand]
    private void RemoveLine(PosCartLine? line)
    {
        if (line is not null) Cart.Remove(line);
        NotifyTotals();
    }

    [RelayCommand]
    private void ClearSale()
    {
        Cart.Clear();
        DocumentNumber = $"POS-{DateTime.Now:yyyyMMdd-HHmmss}";
    }

    [RelayCommand]
    private async Task CompleteSaleAsync()
    {
        if (Cart.Count == 0) { _dialog.Error("Add at least one product."); return; }
        if (SelectedCustomer is null) { _dialog.Error("Select a customer account."); return; }
        if (SelectedWarehouse is null) { _dialog.Error("Select a warehouse."); return; }
        if (PaymentMethod != "On Account" && SelectedCashRegister is null) { _dialog.Error("Select a cash register."); return; }
        if (Cart.Any(x => x.Quantity <= 0 || x.Quantity > x.AvailableStock))
        {
            _dialog.Error("Cart contains an invalid quantity or insufficient stock.");
            return;
        }

        IsLoading = true;
        try
        {
            var request = new CreateInvoiceRequest(
                Guid.Empty, DocumentNumber, DateTime.Now, SelectedCustomer.Id, SelectedWarehouse.Id,
                $"POS sale / {PaymentMethod}",
                Cart.Select((x, i) => new CreateInvoiceLineRequest(
                    x.ProductId, null, null, x.ProductName, x.ProductCode, x.Quantity,
                    x.UnitPrice, 0, 0, x.VatRate, i + 1, null)).ToList());

            var invoice = await _api.PostAsync<InvoiceResponse>("api/invoices", request);
            if (invoice is null) { _dialog.Error("Invoice could not be created."); return; }
            var approved = await _api.PostAsync<InvoiceResponse>($"api/invoices/{invoice.Id}/approve", new { });
            if (approved is null) { _dialog.Error("Invoice was saved but stock approval failed."); return; }

            if (PaymentMethod != "On Account")
            {
                var collection = new CollectionRequest(SelectedCustomer.Id, SelectedCashRegister?.Id, null,
                    approved.TotalAmount, "TRY", 1, $"{approved.DocumentNumber} POS collection", DateTime.Now);
                var paid = await _api.PostAsync<FinanceTransactionResponse>("api/finance/collections", collection);
                if (paid is null)
                {
                    _dialog.Error("Sale was approved, but the cash collection needs review.");
                    return;
                }
            }

            _dialog.Success($"{approved.DocumentNumber} completed. Stock, customer balance and cash entries are linked.", "Sale completed");
            ClearSale();
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SaveCashOperationAsync()
    {
        if (SelectedCustomer is null || SelectedCashRegister is null || CashTransactionAmount <= 0)
        {
            _dialog.Error("Customer, cash register and a positive amount are required.");
            return;
        }

        IsLoading = true;
        try
        {
            var request = new CollectionRequest(SelectedCustomer.Id, SelectedCashRegister.Id, null,
                CashTransactionAmount, "TRY", 1, CashTransactionDescription, DateTime.Now);
            var result = await _api.PostAsync<FinanceTransactionResponse>("api/finance/collections", request);
            if (result is null) { _dialog.Error("Cash transaction could not be saved."); return; }
            _dialog.Success("Cash collection and customer account movement were recorded together.", "Cash operation");
            CashTransactionAmount = 0;
            CashTransactionDescription = string.Empty;
        }
        finally { IsLoading = false; }
    }

    private void NotifyTotals()
    {
        OnPropertyChanged(nameof(ItemCount));
        OnPropertyChanged(nameof(SubTotal));
        OnPropertyChanged(nameof(TaxTotal));
        OnPropertyChanged(nameof(GrandTotal));
    }
}

public partial class PosCartLine : ObservableObject
{
    public PosCartLine(Guid productId, string productCode, string productName, decimal unitPrice, decimal vatRate, decimal availableStock)
    {
        ProductId = productId; ProductCode = productCode; ProductName = productName;
        UnitPrice = unitPrice; VatRate = vatRate; AvailableStock = availableStock;
    }
    public Guid ProductId { get; }
    public string ProductCode { get; }
    public string ProductName { get; }
    public decimal UnitPrice { get; }
    public decimal VatRate { get; }
    public decimal AvailableStock { get; }
    [ObservableProperty] private decimal _quantity = 1;
    public decimal NetAmount => Quantity * UnitPrice;
    public decimal TaxAmount => NetAmount * VatRate / 100m;
    public decimal TotalAmount => NetAmount + TaxAmount;

    partial void OnQuantityChanged(decimal value)
    {
        OnPropertyChanged(nameof(NetAmount));
        OnPropertyChanged(nameof(TaxAmount));
        OnPropertyChanged(nameof(TotalAmount));
    }
}
