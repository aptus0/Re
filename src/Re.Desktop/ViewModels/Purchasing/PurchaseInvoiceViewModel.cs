using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Re.Contracts.Accounts;
using Re.Contracts.Common;
using Re.Contracts.Inventory;
using Re.Contracts.Products;
using Re.Contracts.Purchasing;
using Re.Desktop.Services;

namespace Re.Desktop.ViewModels.Purchasing;

public partial class PurchaseInvoiceViewModel(ApiClient api, IDialogService dialog) : ObservableObject
{
    public ObservableCollection<PurchaseInvoiceListResponse> Invoices { get; } = [];
    public ObservableCollection<AccountListResponse> Suppliers { get; } = [];
    public ObservableCollection<WarehouseLookupItem> Warehouses { get; } = [];
    public ObservableCollection<ProductListResponse> Products { get; } = [];
    public ObservableCollection<PurchaseLineDraft> Lines { get; } = [];
    [ObservableProperty] private PurchaseInvoiceListResponse? _selectedInvoice;
    [ObservableProperty] private AccountListResponse? _selectedSupplier;
    [ObservableProperty] private WarehouseLookupItem? _selectedWarehouse;
    [ObservableProperty] private ProductListResponse? _selectedProduct;
    [ObservableProperty] private string _documentNumber = $"PUR-{DateTime.Now:yyyyMMddHHmmss}";
    [ObservableProperty] private string _supplierDocumentNumber = "";
    [ObservableProperty] private DateTime _documentDate = DateTime.Today;
    [ObservableProperty] private DateTime? _dueDate = DateTime.Today.AddDays(30);
    [ObservableProperty] private string _currency = "TRY";
    [ObservableProperty] private decimal _exchangeRate = 1;
    [ObservableProperty] private string _notes = "";
    [ObservableProperty] private bool _isEditorOpen;
    [ObservableProperty] private bool _isLoading;
    public decimal SubTotal => Lines.Sum(x => x.NetAmount);
    public decimal TaxAmount => Lines.Sum(x => x.TaxAmount);
    public decimal TotalAmount => SubTotal + TaxAmount;

    public async Task InitializeAsync() => await LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var invoices = await api.GetAsync<PagedResponse<PurchaseInvoiceListResponse>>("api/purchase-invoices?page=1&size=200");
            Invoices.Clear(); foreach (var item in invoices?.Items ?? []) Invoices.Add(item);
            if (Suppliers.Count == 0)
            {
                var accounts = await api.GetAsync<PagedResponse<AccountListResponse>>("api/accounts?isActive=true&page=1&size=500");
                foreach (var item in accounts?.Items.Where(x => x.AccountType.Contains("Supplier")) ?? []) Suppliers.Add(item);
                var warehouses = await api.GetAsync<IReadOnlyCollection<WarehouseLookupItem>>("api/stock-movements/warehouses");
                foreach (var item in warehouses ?? []) Warehouses.Add(item);
                var products = await api.GetAsync<PagedResponse<ProductListResponse>>("api/products?isActive=true&page=1&size=500");
                foreach (var item in products?.Items ?? []) Products.Add(item);
            }
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void New()
    {
        DocumentNumber = $"PUR-{DateTime.Now:yyyyMMddHHmmss}";
        SupplierDocumentNumber = ""; DocumentDate = DateTime.Today;
        DueDate = DateTime.Today.AddDays(30); Currency = "TRY"; ExchangeRate = 1; Notes = "";
        SelectedSupplier = Suppliers.FirstOrDefault(); SelectedWarehouse = Warehouses.FirstOrDefault();
        Lines.Clear(); IsEditorOpen = true; NotifyTotals();
    }

    [RelayCommand]
    private void AddLine()
    {
        if (SelectedProduct is null) { dialog.Warning("Select a product."); return; }
        var existing = Lines.FirstOrDefault(x => x.ProductId == SelectedProduct.Id);
        if (existing is not null) existing.Quantity++;
        else
        {
            var line = new PurchaseLineDraft
            {
                ProductId = SelectedProduct.Id, ProductCode = SelectedProduct.Code,
                ProductName = SelectedProduct.Name, Quantity = 1,
                UnitPrice = SelectedProduct.PurchasePrice, VatRate = SelectedProduct.VatRate
            };
            line.PropertyChanged += (_, _) => NotifyTotals(); Lines.Add(line);
        }
        NotifyTotals();
    }

    [RelayCommand] private void RemoveLine(PurchaseLineDraft? line) { if (line is not null) Lines.Remove(line); NotifyTotals(); }
    [RelayCommand] private void CloseEditor() => IsEditorOpen = false;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedSupplier is null || SelectedWarehouse is null || Lines.Count == 0)
        { dialog.Warning("Supplier, warehouse and at least one line are required."); return; }
        if (Lines.Any(x => x.Quantity <= 0 || x.UnitPrice < 0))
        { dialog.Warning("Line quantities must be positive and prices cannot be negative."); return; }
        var request = new CreatePurchaseInvoiceRequest(SelectedSupplier.Id, SelectedWarehouse.Id,
            DocumentNumber, SupplierDocumentNumber, DocumentDate, DueDate, Currency, ExchangeRate,
            Notes, Lines.Select(x => new SavePurchaseInvoiceLineRequest(x.ProductId, null,
                x.Quantity, x.UnitPrice, x.DiscountPercent, x.VatRate, x.LotNumber,
                x.SerialNumber, x.ExpiryDate)).ToList());
        var result = await api.PostAsync<PurchaseInvoiceResponse>("api/purchase-invoices", request);
        if (result is null) { dialog.Error("Purchase invoice could not be saved."); return; }
        dialog.Success("Purchase invoice saved as draft."); IsEditorOpen = false; await LoadAsync();
    }

    [RelayCommand]
    private async Task ApproveAsync()
    {
        if (SelectedInvoice is null) { dialog.Info("Select a purchase invoice."); return; }
        if (SelectedInvoice.Status != "Draft") { dialog.Warning("Only draft invoices can be approved."); return; }
        if (!dialog.Confirm("Approval will increase warehouse stock and supplier payable. Continue?", "Approve Purchase Invoice")) return;
        var result = await api.PostAsync<PurchaseInvoiceResponse>($"api/purchase-invoices/{SelectedInvoice.Id}/approve", new { });
        if (result is null) { dialog.Error("Purchase invoice could not be approved."); return; }
        dialog.Success("Purchase invoice approved. Stock and supplier account were posted."); await LoadAsync();
    }
    private void NotifyTotals() { OnPropertyChanged(nameof(SubTotal)); OnPropertyChanged(nameof(TaxAmount)); OnPropertyChanged(nameof(TotalAmount)); }
}

public partial class PurchaseLineDraft : ObservableObject
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(NetAmount))] [NotifyPropertyChangedFor(nameof(TaxAmount))] private decimal _quantity = 1;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(NetAmount))] [NotifyPropertyChangedFor(nameof(TaxAmount))] private decimal _unitPrice;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(NetAmount))] [NotifyPropertyChangedFor(nameof(TaxAmount))] private decimal _discountPercent;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TaxAmount))] private decimal _vatRate = 20;
    [ObservableProperty] private string _lotNumber = "";
    [ObservableProperty] private string _serialNumber = "";
    [ObservableProperty] private DateTime? _expiryDate;
    public decimal NetAmount => Quantity * UnitPrice * (1 - DiscountPercent / 100m);
    public decimal TaxAmount => NetAmount * VatRate / 100m;
}
