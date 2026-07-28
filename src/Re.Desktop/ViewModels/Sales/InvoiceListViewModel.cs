using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Collections.Generic;
using Re.Desktop.Services;
using Re.Contracts.Sales;
using System.Threading.Tasks;
using Re.Contracts.Accounts;
using Re.Contracts.Products;
using Re.Contracts.Inventory;
using Re.Contracts.Common;
using Re.Contracts.Purchasing;

namespace Re.Desktop.ViewModels.Sales;

public partial class InvoiceListViewModel : ObservableObject
{
    private readonly IDialogService? _dialog;
    private readonly ApiClient? _api;
    private readonly List<InvoiceItem> _allInvoices = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isSearchEmpty = true;
    [ObservableProperty] private string _totalCount = "0";
    [ObservableProperty] private string _totalSales = "0,00 ₺";
    [ObservableProperty] private bool _isLoading;

    // Panel Kontrolü (Görüntüleme)
    [ObservableProperty] private bool _isPanelOpen;
    [ObservableProperty] private InvoiceItem? _selectedInvoice;

    // Form Kontrolü (Ekleme/Editme)
    [ObservableProperty] private bool _isFormOpen;
    [ObservableProperty] private InvoiceFormModel _formModel = new();
    [ObservableProperty] private string _formTitle = "New Invoice";

    public ObservableCollection<InvoiceItem> Invoices { get; } = new();
    public ObservableCollection<AccountListResponse> Customers { get; } = new();
    public ObservableCollection<ProductListResponse> Products { get; } = new();
    public ObservableCollection<WarehouseLookupItem> Warehouses { get; } = new();
    [ObservableProperty] private ProductListResponse? _selectedNewProduct;
    [ObservableProperty] private string _barcodeInput = string.Empty;
    [ObservableProperty] private string _barcodeStatus = "Scanner ready · Focus this field and scan a product";
    [ObservableProperty] private bool _lastScanSucceeded;

    public InvoiceListViewModel() { } // Design-time

    public InvoiceListViewModel(ApiClient api, IDialogService dialog)
    {
        _api = api;
        _dialog = dialog;
        _ = LoadInvoicesAsync();
    }

    [RelayCommand]
    private async Task LoadInvoicesAsync()
    {
        if (_api == null) return;

        IsLoading = true;
        try
        {
            _allInvoices.Clear();

            // Load Sales Invoices
            var response = await _api.GetAsync<Re.Contracts.Common.PagedResponse<InvoiceListResponse>>("api/invoices?page=1&size=100");
            if (response != null && response.Items != null)
            {
                foreach (var inv in response.Items)
                {
                    _allInvoices.Add(new InvoiceItem
                    {
                        Id = inv.Id,
                        DocumentNumber = inv.DocumentNumber,
                        DocumentDate = inv.DocumentDate,
                        CustomerName = string.IsNullOrWhiteSpace(inv.CustomerName) ? "Account Not Specified" : inv.CustomerName,
                        TotalAmount = inv.TotalAmount,
                        PaidAmount = inv.PaidAmount,
                        RemainingAmount = inv.RemainingAmount,
                        DueDate = inv.DueDate,
                        Currency = inv.Currency,
                        EInvoiceStatus = inv.EInvoiceStatus ?? "Not Prepared",
                        Status = inv.Status,
                        DocumentType = "SalesInvoice"
                    });
                }
            }

            // Load Purchase Invoices
            try
            {
                var pResponse = await _api.GetAsync<Re.Contracts.Common.PagedResponse<PurchaseInvoiceListResponse>>("api/purchase-invoices?page=1&size=100");
                if (pResponse != null && pResponse.Items != null)
                {
                    foreach (var inv in pResponse.Items)
                    {
                        _allInvoices.Add(new InvoiceItem
                        {
                            Id = inv.Id,
                            DocumentNumber = inv.DocumentNumber,
                            DocumentDate = inv.DocumentDate,
                            CustomerName = string.IsNullOrWhiteSpace(inv.SupplierName) ? "Supplier Not Specified" : inv.SupplierName,
                            TotalAmount = inv.TotalAmount,
                            PaidAmount = inv.TotalAmount,
                            RemainingAmount = 0,
                            DueDate = inv.DocumentDate.AddDays(30),
                            Currency = inv.Currency,
                            EInvoiceStatus = "N/A",
                            Status = inv.Status,
                            DocumentType = "PurchaseInvoice"
                        });
                    }
                }
            }
            catch
            {
                // Gracefully ignore purchase invoice load failures if table empty/not initialized
            }
            if (Customers.Count == 0)
            {
                var accounts = await _api.GetAsync<PagedResponse<AccountListResponse>>("api/accounts?isActive=true&page=1&size=500");
                foreach (var item in accounts?.Items ?? []) Customers.Add(item);
            }
            if (Products.Count == 0)
            {
                var products = await _api.GetAsync<PagedResponse<ProductListResponse>>("api/products?page=1&size=500");
                foreach (var item in products?.Items.Where(x => x.IsActive) ?? []) Products.Add(item);
            }
            if (Warehouses.Count == 0)
            {
                var warehouses = await _api.GetAsync<IReadOnlyCollection<WarehouseLookupItem>>("api/stock-movements/warehouses");
                foreach (var item in warehouses ?? []) Warehouses.Add(item);
            }
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _allInvoices.Clear();
            Invoices.Clear();
            TotalCount = "0";
            TotalSales = "0,00 ₺";
            _dialog?.Error($"Invoice list could not be loaded.\n{ex.Message}", "Invoice Center");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        IsSearchEmpty = string.IsNullOrEmpty(value);
        ApplyFilter();
    }

    partial void OnSelectedInvoiceChanged(InvoiceItem? value)
    {
        IsPanelOpen = value != null;
        if (value != null) IsFormOpen = false;
    }

    private void ApplyFilter()
    {
        var query = _allInvoices.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(i =>
                i.DocumentNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        Invoices.Clear();
        foreach (var inv in query)
            Invoices.Add(inv);

        TotalCount = Invoices.Count.ToString();
        var total = Invoices.Where(i => i.Status != "Cancelled").Sum(i => i.TotalAmount);
        TotalSales = total.ToString("N2") + " ₺";
    }

    [RelayCommand] private void ClosePanel() { IsPanelOpen = false; SelectedInvoice = null; }

    [RelayCommand]
    private void NewInvoice()
    {
        FormTitle = "New Sales Invoice";
        FormModel = new InvoiceFormModel();
        FormModel.WarehouseId = Warehouses.FirstOrDefault()?.Id;
        FormModel.CustomerId = Customers.FirstOrDefault()?.Id;

        IsPanelOpen = false;
        IsFormOpen = true;
    }

    [RelayCommand]
    private async Task EditInvoice(InvoiceItem? invoice)
    {
        if (invoice == null) return;
        if (_api == null) return;

        IsLoading = true;
        try
        {
            var data = await _api.GetAsync<InvoiceResponse>($"api/invoices/{invoice.Id}");
            if (data == null)
            {
                _dialog?.Error("Invoice details could not be loaded.");
                return;
            }

            FormTitle = "Edit Invoice";
            FormModel = new InvoiceFormModel
            {
                Id = data.Id,
                DocumentNumber = data.DocumentNumber,
                DocumentDate = data.DocumentDate,
                CustomerId = data.CustomerId,
                WarehouseId = data.WarehouseId,
                Notes = data.Notes ?? string.Empty,
                Status = data.Status,
                DueDate = data.DueDate,
                Currency = data.Currency,
                ExchangeRate = data.ExchangeRate
            };

            foreach (var line in data.Lines)
            {
                var formLine = new InvoiceLineFormModel
                {
                    Id = line.Id,
                    ProductId = line.ProductId,
                    ProductName = line.ProductName,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    VatRate = line.VatRate,
                    SortOrder = line.SortOrder,
                    ProductCode = line.ProductCode ?? string.Empty
                };
                formLine.PropertyChanged += (_, _) => FormModel.RecalculateTotals();
                FormModel.Lines.Add(formLine);
            }
            FormModel.RecalculateTotals();

            IsPanelOpen = false;
            IsFormOpen = true;
        }
        catch (Exception ex)
        {
            _dialog?.Error($"An error occurred while opening invoice details.\n{ex.Message}", "Invoice Center");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteInvoice(InvoiceItem? invoice)
    {
        if (invoice == null) return;
        if (_dialog != null && !_dialog.Confirm("Invoice iptal edilecek. Devam edilsin mi?")) return;
        if (_api == null) return;

        IsLoading = true;
        try
        {
            var success = await _api.DeleteAsync($"api/invoices/{invoice.Id}");
            if (success)
            {
                _dialog?.Success("Invoice cancelled.");
                ClosePanel();
                await LoadInvoicesAsync();
            }
            else
            {
                _dialog?.Error("An error occurred while cancelling the invoice.");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void AddLine()
    {
        if (SelectedNewProduct is null)
        {
            _dialog?.Error("Select a real product before adding an invoice line.");
            return;
        }
        AddOrIncrementProduct(SelectedNewProduct);
    }

    private void AddOrIncrementProduct(ProductListResponse product)
    {
        var existing = FormModel.Lines.FirstOrDefault(x => x.ProductId == product.Id);
        if (existing is not null)
        {
            existing.Quantity += 1;
            FormModel.RecalculateTotals();
            return;
        }
        var order = FormModel.Lines.Count + 1;
        var line = new InvoiceLineFormModel
        {
            ProductId = product.Id,
            ProductCode = product.Code,
            ProductName = product.Name,
            UnitPrice = product.SalePrice,
            VatRate = product.VatRate,
            SortOrder = order
        };
        line.PropertyChanged += (_, _) => FormModel.RecalculateTotals();
        FormModel.Lines.Add(line);
        FormModel.RecalculateTotals();
    }

    [RelayCommand]
    private async Task AddByBarcode()
    {
        var barcode = BarcodeInput.Trim();
        if (string.IsNullOrWhiteSpace(barcode)) return;

        var product = Products.FirstOrDefault(x =>
            string.Equals(x.Barcode, barcode, StringComparison.OrdinalIgnoreCase));
        if (product is null && _api is not null)
        {
            var detail = await _api.GetAsync<ProductResponse>(
                $"api/products/byBarcode/{Uri.EscapeDataString(barcode)}");
            if (detail is not null)
            {
                product = Products.FirstOrDefault(x => x.Id == detail.Id) ??
                    new ProductListResponse(detail.Id, detail.Code, detail.Name,
                        detail.PurchasePrice, detail.SalePrice, detail.DealerPrice,
                        detail.VatRate, detail.MinStockLevel, detail.MaxStockLevel,
                        detail.Barcode1, detail.CategoryName, null, detail.BrandName,
                        null, detail.Warehouse, 0, detail.ImagePath,
                        detail.CreatedAt, detail.IsActive);
            }
        }

        if (product is null)
        {
            LastScanSucceeded = false;
            BarcodeStatus = $"Barcode {barcode} was not found in the product catalog.";
            _dialog?.Warning(BarcodeStatus, "Barcode Scanner");
            return;
        }

        AddOrIncrementProduct(product);
        LastScanSucceeded = true;
        BarcodeStatus = $"{product.Code} · {product.Name} added at {DateTime.Now:HH:mm:ss}";
        BarcodeInput = string.Empty;
    }

    [RelayCommand]
    private void RemoveLine(InvoiceLineFormModel? line)
    {
        if (line != null)
        {
            FormModel.Lines.Remove(line);
            FormModel.RecalculateTotals();
        }
    }

    [RelayCommand]
    private async Task SaveInvoice()
    {
        if (string.IsNullOrWhiteSpace(FormModel.DocumentNumber))
        {
            _dialog?.Error("Document number is required.");
            return;
        }

        if (_api == null) return;
        IsLoading = true;

        try
        {
            // Purchase Invoice Branch
            if (FormModel.DocumentType == "PurchaseInvoice")
            {
                var pLines = FormModel.Lines.Select(l => new SavePurchaseInvoiceLineRequest(
                    ProductId: l.ProductId,
                    ProductVariantId: null,
                    Quantity: l.Quantity,
                    UnitPrice: l.UnitPrice,
                    DiscountPercent: l.DiscountPercent,
                    VatRate: l.VatRate,
                    LotNumber: null,
                    SerialNumber: null,
                    ExpiryDate: null
                )).ToList();

                var req = new CreatePurchaseInvoiceRequest(
                    SupplierId: FormModel.CustomerId ?? Guid.Empty,
                    WarehouseId: FormModel.WarehouseId ?? Guid.Empty,
                    DocumentNumber: FormModel.DocumentNumber,
                    SupplierDocumentNumber: FormModel.DocumentNumber,
                    DocumentDate: FormModel.DocumentDate,
                    DueDate: FormModel.DueDate,
                    Currency: FormModel.Currency,
                    ExchangeRate: FormModel.ExchangeRate,
                    Notes: FormModel.Notes,
                    Lines: pLines
                );

                var result = await _api.PostAsync<PurchaseInvoiceResponse>("api/purchase-invoices", req);
                if (result != null)
                {
                    try
                    {
                        // Auto-approve to update inventory & balance instantly
                        await _api.PostAsync<object>($"api/purchase-invoices/{result.Id}/approve", new { });
                    }
                    catch { }

                    _dialog?.Success("Purchase invoice saved and approved successfully.", "Success");
                    IsFormOpen = false;
                    await LoadInvoicesAsync();
                }
                else
                {
                    _dialog?.Error("Failed to save Purchase invoice.");
                }
                return;
            }

            var isNew = _allInvoices.All(i => i.Id != FormModel.Id);

            // Satırları maple
            if (isNew)
            {
                var reqLines = FormModel.Lines.Select(l => new CreateInvoiceLineRequest(
                    ProductId: l.ProductId,
                    ProductVariantId: null,
                    UnitId: null,
                    ProductName: string.IsNullOrWhiteSpace(l.ProductName) ? "Unknown Product" : l.ProductName,
                    ProductCode: null,
                    Quantity: l.Quantity,
                    UnitPrice: l.UnitPrice,
                    DiscountPercent: l.DiscountPercent,
                    DiscountAmount: l.DiscountAmount,
                    VatRate: l.VatRate,
                    SortOrder: l.SortOrder,
                    Notes: null
                )).ToList();

                var req = new CreateInvoiceRequest(
                    BranchId: Guid.Empty, // Backend handle edecek
                    DocumentNumber: FormModel.DocumentNumber,
                    DocumentDate: FormModel.DocumentDate,
                    CustomerId: FormModel.CustomerId,
                    WarehouseId: FormModel.WarehouseId,
                    Notes: FormModel.Notes,
                    Lines: reqLines,
                    DueDate: FormModel.DueDate,
                    Currency: FormModel.Currency,
                    ExchangeRate: FormModel.ExchangeRate,
                    PaymentType: FormModel.PaymentType
                );

                var result = await _api.PostAsync<InvoiceResponse>("api/invoices", req);
                if (result != null)
                {
                    _dialog?.Success("Invoice saved successfully.", "Success");
                    IsFormOpen = false;
                    await LoadInvoicesAsync();
                }
                else
                {
                    _dialog?.Error("Invoice kaydedilemedi.");
                }
            }
            else
            {
                var reqLines = FormModel.Lines.Select(l => new UpdateInvoiceLineRequest(
                    Id: l.Id == Guid.Empty ? null : l.Id,
                    ProductId: l.ProductId,
                    ProductVariantId: null,
                    UnitId: null,
                    ProductName: string.IsNullOrWhiteSpace(l.ProductName) ? "Unknown Product" : l.ProductName,
                    ProductCode: null,
                    Quantity: l.Quantity,
                    UnitPrice: l.UnitPrice,
                    DiscountPercent: l.DiscountPercent,
                    DiscountAmount: l.DiscountAmount,
                    VatRate: l.VatRate,
                    SortOrder: l.SortOrder,
                    Notes: null
                )).ToList();

                var req = new UpdateInvoiceRequest(
                    DocumentNumber: FormModel.DocumentNumber,
                    DocumentDate: FormModel.DocumentDate,
                    CustomerId: FormModel.CustomerId,
                    WarehouseId: FormModel.WarehouseId,
                    Notes: FormModel.Notes,
                    Lines: reqLines,
                    DueDate: FormModel.DueDate,
                    Currency: FormModel.Currency,
                    ExchangeRate: FormModel.ExchangeRate,
                    PaymentType: FormModel.PaymentType
                );

                var result = await _api.PutAsync<InvoiceResponse>($"api/invoices/{FormModel.Id}", req);
                if (result != null)
                {
                    _dialog?.Success("Invoice updated successfully.", "Success");
                    IsFormOpen = false;
                    await LoadInvoicesAsync();
                }
                else
                {
                    _dialog?.Error("The invoice could not be updated.");
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ApproveInvoice()
    {
        if (FormModel.Id == Guid.Empty)
        {
            _dialog?.Error("Save the invoice as a draft first.");
            return;
        }

        var confirm = _dialog?.Confirm("After approval, the invoice cannot be changed and inventory/account balances will be affected. Continue?", "Approve Invoice");
        if (confirm != true) return;

        if (_api == null) return;
        IsLoading = true;

        try
        {
            var result = await _api.PostAsync<InvoiceResponse>($"api/invoices/{FormModel.Id}/approve", new { });
            if (result != null)
            {
                _dialog?.Success("Invoice approved successfully. Inventory and account movements were posted.", "Success");
                IsFormOpen = false;
                await LoadInvoicesAsync();
            }
            else
            {
                _dialog?.Error("An error occurred while approving the invoice.");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task PrepareElectronicDocument(InvoiceItem? invoice)
    {
        invoice ??= SelectedInvoice;
        if (invoice is null || _api is null) { _dialog?.Info("Select an invoice."); return; }
        var result = await _api.PostAsync<ElectronicDocumentPreparationResponse>(
            $"api/invoices/{invoice.Id}/prepare-electronic-document", new { });
        if (result is not null)
        {
            _dialog?.Success($"{result.DocumentType} package prepared.\nUUID: {result.Uuid}",
                "Electronic Document");
            await LoadInvoicesAsync();
        }
    }

    [RelayCommand]
    private async Task ReverseInvoice(InvoiceItem? invoice)
    {
        invoice ??= SelectedInvoice;
        if (invoice is null || _api is null) { _dialog?.Info("Select an invoice."); return; }
        if (!_dialog!.Confirm("This creates audited reverse account and stock movements. Continue?",
            "Reverse Posted Invoice")) return;
        var result = await _api.PostAsync<object>($"api/invoices/{invoice.Id}/reverse",
            new ReverseInvoiceRequest("Reversed by authorized desktop user"));
        if (result is not null) { _dialog.Success("Invoice reversed with audit movements."); await LoadInvoicesAsync(); }
    }

    [RelayCommand] private void CloseForm() { IsFormOpen = false; }
}

public partial class InvoiceFormModel : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty] private string _documentNumber = "FAT-" + DateTime.Now.ToString("yyyyMMddHHmmss");
    [ObservableProperty] private string _documentType = "SalesInvoice";
    [ObservableProperty] private DateTime _documentDate = DateTime.Now;
    [ObservableProperty] private Guid? _customerId;
    [ObservableProperty] private Guid? _warehouseId;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private DateTime? _dueDate = DateTime.Today.AddDays(30);
    [ObservableProperty] private string _currency = "TRY";
    [ObservableProperty] private decimal _exchangeRate = 1;
    [ObservableProperty] private string _paymentType = "OpenAccount";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsDraft))] private string _status = "Draft";

    public bool IsDraft => Status == "Draft";

    public ObservableCollection<InvoiceLineFormModel> Lines { get; } = new();

    // Alt Totallar
    [ObservableProperty] private decimal _subTotal;
    [ObservableProperty] private decimal _taxAmount;
    [ObservableProperty] private decimal _totalAmount;

    public InvoiceFormModel()
    {
        Lines.CollectionChanged += (s, e) => RecalculateTotals();
    }

    public void RecalculateTotals()
    {
        SubTotal = Lines.Sum(l => l.LineTotal);
        TaxAmount = Lines.Sum(l => l.TaxAmount);
        TotalAmount = SubTotal + TaxAmount;
    }
}

public partial class InvoiceLineFormModel : ObservableObject
{
    public Guid Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;

    // Gerçekte bir arama/seçme ekranından (Dialog) ProductId alınacak.
    // Şimdilik default dummy değerlerle UI test edilecek.
    [ObservableProperty] private Guid _productId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(LineTotal))] [NotifyPropertyChangedFor(nameof(TaxAmount))]
    private string _productName = string.Empty;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(LineTotal))] [NotifyPropertyChangedFor(nameof(TaxAmount))] [NotifyPropertyChangedFor(nameof(LineTotalWithTax))]
    private decimal _quantity = 1;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(LineTotal))] [NotifyPropertyChangedFor(nameof(TaxAmount))] [NotifyPropertyChangedFor(nameof(LineTotalWithTax))]
    private decimal _unitPrice = 100;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(LineTotal))] [NotifyPropertyChangedFor(nameof(TaxAmount))] [NotifyPropertyChangedFor(nameof(LineTotalWithTax))]
    private decimal _discountPercent = 0;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(LineTotal))] [NotifyPropertyChangedFor(nameof(TaxAmount))] [NotifyPropertyChangedFor(nameof(LineTotalWithTax))]
    private decimal _discountAmount = 0;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TaxAmount))] [NotifyPropertyChangedFor(nameof(LineTotalWithTax))]
    private decimal _vatRate = 20;

    public int SortOrder { get; set; }

    public decimal LineTotal => (Quantity * UnitPrice) - DiscountAmount - (Quantity * UnitPrice * DiscountPercent / 100);
    public decimal TaxAmount => LineTotal * VatRate / 100;
    public decimal LineTotalWithTax => LineTotal + TaxAmount;

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        // Hangi prop değişirse değişsin faturayı yeniden hesaplatmamız lazım (Parent'a trigger yollamak zorundayız, UI şimdilik yetecek)
        // İdealde Parent'tan yakalanır (Event ile).
    }
}

public class InvoiceItem : ObservableObject
{
    public Guid Id { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime? DueDate { get; set; }
    public string Currency { get; set; } = "TRY";
    public string EInvoiceStatus { get; set; } = "Not Prepared";
    public string Status { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "SalesInvoice";

    public string DocumentTypeDisplay => DocumentType switch
    {
        "SalesInvoice" => "Sales Invoice",
        "PurchaseInvoice" => "Purchase Invoice",
        "ReturnInvoice" => "Return Invoice",
        "ExchangeInvoice" => "Exchange Invoice",
        "PriceDifference" => "Price Diff. Invoice",
        _ => "Invoice"
    };

    public string Initials => DocumentType switch
    {
        "PurchaseInvoice" => "PI",
        "ReturnInvoice" => "RI",
        "ExchangeInvoice" => "EI",
        "PriceDifference" => "PD",
        _ => "FT"
    };
}
