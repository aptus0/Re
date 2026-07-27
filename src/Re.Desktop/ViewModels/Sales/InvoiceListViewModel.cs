using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Collections.Generic;
using Re.Desktop.Services;
using Re.Contracts.Sales;
using System.Threading.Tasks;

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

    // Form Kontrolü (Ekleme/Düzenleme)
    [ObservableProperty] private bool _isFormOpen;
    [ObservableProperty] private InvoiceFormModel _formModel = new();
    [ObservableProperty] private string _formTitle = "Yeni Fatura";

    public ObservableCollection<InvoiceItem> Invoices { get; } = new();

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
            var response = await _api.GetAsync<Re.Contracts.Common.PagedResponse<InvoiceListResponse>>("api/invoices?page=1&size=100");
            
            if (response != null && response.Items != null)
            {
                _allInvoices.Clear();
                foreach (var inv in response.Items)
                {
                    _allInvoices.Add(new InvoiceItem
                    {
                        Id = inv.Id,
                        DocumentNumber = inv.DocumentNumber,
                        DocumentDate = inv.DocumentDate,
                        CustomerName = string.IsNullOrWhiteSpace(inv.CustomerName) ? "Cari Belirtilmemiş" : inv.CustomerName,
                        TotalAmount = inv.TotalAmount,
                        Status = inv.Status == "Draft" ? "Taslak" : 
                                 inv.Status == "Approved" ? "Onaylandı" : 
                                 inv.Status == "Cancelled" ? "İptal Edildi" : inv.Status
                    });
                }
            }
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _allInvoices.Clear();
            Invoices.Clear();
            TotalCount = "0";
            TotalSales = "0,00 ₺";
            _dialog?.Error($"Fatura listesi yüklenemedi.\n{ex.Message}", "Fatura Merkezi");
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
        var total = Invoices.Where(i => i.Status != "İptal Edildi").Sum(i => i.TotalAmount);
        TotalSales = total.ToString("N2") + " ₺";
    }

    [RelayCommand] private void ClosePanel() { IsPanelOpen = false; SelectedInvoice = null; }

    [RelayCommand]
    private void NewInvoice()
    {
        FormTitle = "Yeni Satış Faturası";
        FormModel = new InvoiceFormModel();
        
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
                _dialog?.Error("Fatura detayları getirilemedi.");
                return;
            }

            FormTitle = "Fatura Düzenle";
            FormModel = new InvoiceFormModel
            {
                Id = data.Id,
                DocumentNumber = data.DocumentNumber,
                DocumentDate = data.DocumentDate,
                CustomerId = data.CustomerId,
                Notes = data.Notes ?? string.Empty,
                Status = data.Status
            };

            foreach (var line in data.Lines)
            {
                FormModel.Lines.Add(new InvoiceLineFormModel
                {
                    Id = line.Id,
                    ProductId = line.ProductId,
                    ProductName = line.ProductName,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    VatRate = line.VatRate,
                    SortOrder = line.SortOrder
                });
            }
            FormModel.RecalculateTotals();

            IsPanelOpen = false;
            IsFormOpen = true;
        }
        catch (Exception ex)
        {
            _dialog?.Error($"Fatura detayları açılırken hata oluştu.\n{ex.Message}", "Fatura Merkezi");
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
        if (_dialog != null && !_dialog.Confirm("Fatura iptal edilecek. Devam edilsin mi?")) return;
        if (_api == null) return;

        IsLoading = true;
        try
        {
            var success = await _api.DeleteAsync($"api/invoices/{invoice.Id}");
            if (success)
            {
                _dialog?.Info("Fatura iptal edildi.");
                ClosePanel();
                await LoadInvoicesAsync();
            }
            else
            {
                _dialog?.Error("Fatura iptal edilirken bir hata oluştu.");
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
        var order = FormModel.Lines.Count + 1;
        FormModel.Lines.Add(new InvoiceLineFormModel { SortOrder = order });
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
            _dialog?.Error("Belge numarası zorunludur.");
            return;
        }

        if (_api == null) return;
        IsLoading = true;

        try
        {
            var isNew = _allInvoices.All(i => i.Id != FormModel.Id);
            
            // Satırları maple
            if (isNew)
            {
                var reqLines = FormModel.Lines.Select(l => new CreateInvoiceLineRequest(
                    ProductId: l.ProductId,
                    ProductVariantId: null,
                    UnitId: null,
                    ProductName: string.IsNullOrWhiteSpace(l.ProductName) ? "Bilinmeyen Ürün" : l.ProductName,
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
                    WarehouseId: null,
                    Notes: FormModel.Notes,
                    Lines: reqLines
                );

                var result = await _api.PostAsync<InvoiceResponse>("api/invoices", req);
                if (result != null)
                {
                    _dialog?.Info("Fatura başarıyla kaydedildi.", "Başarılı");
                    IsFormOpen = false;
                    await LoadInvoicesAsync();
                }
                else
                {
                    _dialog?.Error("Fatura kaydedilemedi.");
                }
            }
            else
            {
                var reqLines = FormModel.Lines.Select(l => new UpdateInvoiceLineRequest(
                    Id: l.Id == Guid.Empty ? null : l.Id,
                    ProductId: l.ProductId,
                    ProductVariantId: null,
                    UnitId: null,
                    ProductName: string.IsNullOrWhiteSpace(l.ProductName) ? "Bilinmeyen Ürün" : l.ProductName,
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
                    WarehouseId: null,
                    Notes: FormModel.Notes,
                    Lines: reqLines
                );

                var result = await _api.PutAsync<InvoiceResponse>($"api/invoices/{FormModel.Id}", req);
                if (result != null)
                {
                    _dialog?.Info("Fatura başarıyla güncellendi.", "Başarılı");
                    IsFormOpen = false;
                    await LoadInvoicesAsync();
                }
                else
                {
                    _dialog?.Error("Fatura güncellenemedi.");
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
            _dialog?.Error("Önce faturayı taslak olarak kaydetmelisiniz.");
            return;
        }

        var confirm = _dialog?.Confirm("Fatura onaylandıktan sonra değiştirilemez, stok ve cari bakiyeler etkilenecektir. Onaylıyor musunuz?", "Faturayı Onayla");
        if (confirm != true) return;

        if (_api == null) return;
        IsLoading = true;

        try
        {
            var result = await _api.PostAsync<InvoiceResponse>($"api/invoices/{FormModel.Id}/approve", new { });
            if (result != null)
            {
                _dialog?.Info("Fatura başarıyla onaylandı. Stok çıkışları ve cari hareketleri işlendi.", "Başarılı");
                IsFormOpen = false;
                await LoadInvoicesAsync();
            }
            else
            {
                _dialog?.Error("Fatura onaylanırken bir hata oluştu.");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand] private void CloseForm() { IsFormOpen = false; }
}

public partial class InvoiceFormModel : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [ObservableProperty] private string _documentNumber = "FAT-" + DateTime.Now.ToString("yyyyMMddHHmmss");
    [ObservableProperty] private DateTime _documentDate = DateTime.Now;
    [ObservableProperty] private Guid? _customerId; // İleride Combobox ile bağlanacak
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsDraft))] private string _status = "Draft";

    public bool IsDraft => Status == "Draft";

    public ObservableCollection<InvoiceLineFormModel> Lines { get; } = new();

    // Alt Toplamlar
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
    
    // Gerçekte bir arama/seçme ekranından (Dialog) ProductId alınacak.
    // Şimdilik default dummy değerlerle UI test edilecek.
    [ObservableProperty] private Guid _productId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(LineTotal))] [NotifyPropertyChangedFor(nameof(TaxAmount))]
    private string _productName = string.Empty;
    
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(LineTotal))] [NotifyPropertyChangedFor(nameof(TaxAmount))]
    private decimal _quantity = 1;
    
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(LineTotal))] [NotifyPropertyChangedFor(nameof(TaxAmount))]
    private decimal _unitPrice = 100;
    
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(LineTotal))] [NotifyPropertyChangedFor(nameof(TaxAmount))]
    private decimal _discountPercent = 0;
    
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(LineTotal))] [NotifyPropertyChangedFor(nameof(TaxAmount))]
    private decimal _discountAmount = 0;
    
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TaxAmount))]
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
    public string Status { get; set; } = string.Empty;
    
    public string Initials => "FT";
}
