using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Re.Desktop.Services;
using Re.Contracts.Products;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Re.Desktop.Views.Products;
using Microsoft.Extensions.DependencyInjection;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Xl = DocumentFormat.OpenXml.Spreadsheet;

namespace Re.Desktop.ViewModels.Products;

public partial class ProductListViewModel : ObservableObject
{
    private readonly ApiClient? _api;
    private readonly IDialogService? _dialog;
    private readonly INavigationService? _navigation;

    private int _pageSize = 25;
    private List<ProductDetailItem> _allProducts = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isSearchEmpty = true;
    [ObservableProperty] private string _selectedCategory = "Tümü";
    [ObservableProperty] private string _selectedStockFilter = "Tümü";
    [ObservableProperty] private string _pagingLabel = "";

    // Panel Kontrolü (Görüntüleme)
    [ObservableProperty] private bool _isPanelOpen;
    [ObservableProperty] private ProductDetailItem? _selectedProduct;

    // Form Kontrolü (Ekleme/Düzenleme)
    [ObservableProperty] private bool _isFormOpen;
    [ObservableProperty] private ProductFormModel _formModel = new();
    [ObservableProperty] private string _formTitle = "Yeni Ürün Ekle";

    public ObservableCollection<ProductDetailItem> Products { get; } = [];
    public ObservableCollection<string> Categories { get; } = ["Tümü"];
    public ObservableCollection<CatalogItemResponse> CatalogCategories { get; } = [];
    public ObservableCollection<CatalogItemResponse> CatalogBrands { get; } = [];
    public List<string> StockFilters { get; } = ["Tümü", "Stokta Var", "Düşük Stok", "Tükenmiş"];

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage * _pageSize < TotalCount;

    partial void OnSelectedProductChanged(ProductDetailItem? value)
    {
        IsPanelOpen = value != null;
        if (value != null) IsFormOpen = false; // Bir panel açılırken diğerini kapat
    }

    partial void OnSearchTextChanged(string value)
    {
        IsSearchEmpty = string.IsNullOrEmpty(value);
        ApplyFilter();
    }
    partial void OnSelectedCategoryChanged(string value) => ApplyFilter();
    partial void OnSelectedStockFilterChanged(string value) => ApplyFilter();
    partial void OnCurrentPageChanged(int value)
    {
        OnPropertyChanged(nameof(HasPreviousPage));
        OnPropertyChanged(nameof(HasNextPage));
        UpdatePagingLabel();
    }

    public ProductListViewModel()
    {
        // Design-time
    }

    public ProductListViewModel(ApiClient api, IDialogService dialog, INavigationService navigation)
    {
        _api = api; _dialog = dialog; _navigation = navigation;
        _ = LoadProductsAsync();
    }

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        IsLoading = true;
        try
        {
            if (_api == null) return;
            await LoadCatalogsAsync();
            var response = await _api.GetAsync<Re.Contracts.Common.PagedResponse<ProductListResponse>>($"api/products?page=1&size=100");
            
            if (response != null && response.Items != null)
            {
                _allProducts = response.Items.Select(p => new ProductDetailItem
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    PurchasePrice = p.PurchasePrice,
                    SalePrice = p.SalePrice,
                    DealerPrice = p.DealerPrice,
                    VatRate = (int)p.VatRate,
                    CategoryId = p.CategoryId,
                    CategoryName = p.CategoryName ?? "Tanımsız",
                    BrandId = p.BrandId,
                    Brand = p.BrandName ?? "Tanımsız",
                    Barcode = p.Barcode ?? string.Empty,
                    ImagePath = p.ImagePath ?? string.Empty,
                    IsActive = p.IsActive,
                    AverageCost = p.PurchasePrice,
                    StockLevel = (int)p.StockLevel,
                    ReservedStock = 0,
                    MinStockLevel = (int)p.MinStockLevel,
                    MaxStockLevel = p.MaxStockLevel,
                    Warehouse = p.Warehouse ?? string.Empty,
                    LastUpdate = p.LastUpdatedAt
                }).ToList();
            }
            else
            {
                _allProducts = new List<ProductDetailItem>();
            }

            ApplyFilter();
        }
        finally { IsLoading = false; }
    }

    private async Task LoadCatalogsAsync()
    {
        if (_api is null) return;
        var categories = await _api.GetAsync<IReadOnlyCollection<CatalogItemResponse>>("api/product-catalog/categories");
        var brands = await _api.GetAsync<IReadOnlyCollection<CatalogItemResponse>>("api/product-catalog/brands");
        CatalogCategories.Clear();
        foreach (var item in categories?.Where(x => x.IsActive) ?? []) CatalogCategories.Add(item);
        CatalogBrands.Clear();
        foreach (var item in brands?.Where(x => x.IsActive) ?? []) CatalogBrands.Add(item);
        var selected = SelectedCategory;
        Categories.Clear();
        Categories.Add("Tümü");
        foreach (var name in CatalogCategories.Select(x => x.Name).Distinct()) Categories.Add(name);
        SelectedCategory = Categories.Contains(selected) ? selected : "Tümü";
    }

    private void ApplyFilter()
    {
        var filtered = _allProducts.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(p =>
                p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                p.Code.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        
        if (SelectedCategory != "Tümü")
            filtered = filtered.Where(p => p.CategoryName == SelectedCategory);

        if (SelectedStockFilter == "Stokta Var")
            filtered = filtered.Where(p => p.StockLevel > p.MinStockLevel);
        else if (SelectedStockFilter == "Düşük Stok")
            filtered = filtered.Where(p => p.StockLevel > 0 && p.StockLevel <= p.MinStockLevel);
        else if (SelectedStockFilter == "Tükenmiş")
            filtered = filtered.Where(p => p.StockLevel == 0);

        var list = filtered.ToList();
        TotalCount = list.Count;
        var page = list.Skip((CurrentPage - 1) * _pageSize).Take(_pageSize);
        
        Products.Clear();
        foreach (var p in page) Products.Add(p);
        
        UpdatePagingLabel();
    }

    private void UpdatePagingLabel()
    {
        var from = (CurrentPage - 1) * _pageSize + 1;
        var to   = Math.Min(CurrentPage * _pageSize, TotalCount);
        PagingLabel = TotalCount > 0 ? $"{from}-{to} / {TotalCount} ürün" : "Ürün bulunamadı";
    }

    [RelayCommand] private void ClosePanel() { IsPanelOpen = false; SelectedProduct = null; }
    
    // Form İşlemleri
    [RelayCommand] 
    private async Task NewProductAsync()
    { 
        await LoadCatalogsAsync();
        if (CatalogCategories.Count == 0 || CatalogBrands.Count == 0)
        {
            _dialog?.Error("Ürün eklemeden önce en az bir aktif kategori ve marka tanımlamalısınız.", "Katalog Gerekli");
            ManageCatalog();
            await LoadCatalogsAsync();
            if (CatalogCategories.Count == 0 || CatalogBrands.Count == 0) return;
        }
        FormTitle = "Yeni Ürün Ekle";
        FormModel = new ProductFormModel
        {
            SelectedCategory = CatalogCategories.First(),
            SelectedBrand = CatalogBrands.First()
        };
        IsPanelOpen = false;
        ShowProductEditor();
    }

    [RelayCommand] 
    private async Task EditProductAsync(ProductDetailItem? product)
    { 
        if (product == null) return;
        await LoadCatalogsAsync();
        FormTitle = "Ürün Düzenle";
        FormModel = new ProductFormModel
        {
            Id = product.Id,
            Code = product.Code,
            Name = product.Name,
            CategoryName = product.CategoryName,
            Brand = product.Brand,
            SelectedCategory = CatalogCategories.FirstOrDefault(x => x.Id == product.CategoryId),
            SelectedBrand = CatalogBrands.FirstOrDefault(x => x.Id == product.BrandId),
            Barcode1 = product.Barcode,
            PurchasePrice = product.PurchasePrice,
            SalePrice = product.SalePrice,
            VatRate = product.VatRate,
            Warehouse = product.Warehouse,
            StockLevel = product.StockLevel,
            MinStockLevel = product.MinStockLevel
        };
        IsPanelOpen = false;
        ShowProductEditor();
    }

    [RelayCommand]
    private void AddVariant() => FormModel.Variants.Add(new ProductVariantDraft
    {
        Code = $"{FormModel.Code}-V{FormModel.Variants.Count + 1:00}",
        SalePrice = FormModel.SalePrice
    });

    [RelayCommand]
    private void RemoveVariant(ProductVariantDraft? variant)
    {
        if (variant is not null) FormModel.Variants.Remove(variant);
    }

    private void ShowProductEditor()
    {
        try
        {
            var editor = new ProductEditorWindow { DataContext = this };
            var owner = Application.Current.Windows
                .OfType<Re.Desktop.Views.Shell.MainWindow>()
                .FirstOrDefault(x => x.IsVisible);
            if (owner is not null)
                editor.Owner = owner;
            editor.ShowDialog();
        }
        catch (Exception ex)
        {
            _dialog?.Error($"Ürün kartı açılamadı.\n{ex.GetBaseException().Message}", "Ürün Yönetimi");
        }
    }

    [RelayCommand]
    private void ManageVariants(ProductDetailItem? product)
    {
        if (product is null || _api is null || _dialog is null) return;
        var window = new ProductVariantWindow { DataContext = new ProductVariantViewModel(_api, _dialog, product) };
        var owner = Application.Current.Windows.OfType<Re.Desktop.Views.Shell.MainWindow>().FirstOrDefault(x => x.IsVisible);
        if (owner is not null) window.Owner = owner;
        window.ShowDialog();
    }

    [RelayCommand]
    private async Task SaveProduct()
    {
        if (string.IsNullOrWhiteSpace(FormModel.Code) || string.IsNullOrWhiteSpace(FormModel.Name))
        {
            _dialog?.Error("Ürün kodu ve ürün adı zorunludur.");
            return;
        }
        if (FormModel.SelectedCategory is null || FormModel.SelectedBrand is null)
        {
            _dialog?.Error("Kategori ve marka seçimi zorunludur.");
            return;
        }

        if (FormModel.MinStockLevel < 0 || FormModel.MaxStockLevel < FormModel.MinStockLevel)
        {
            _dialog?.Error("Maksimum stok minimum stoktan küçük olamaz; stok eşikleri negatif girilemez.");
            return;
        }

        if (FormModel.ReservedStock < 0 || FormModel.ReservedStock > FormModel.StockLevel)
        {
            _dialog?.Error("Rezerve stok sıfırdan küçük veya mevcut stoktan büyük olamaz.");
            return;
        }

        if (FormModel.SalePrice > 0 && FormModel.PurchasePrice > FormModel.SalePrice &&
            _dialog is not null &&
            !_dialog.Confirm("Satış fiyatı alış fiyatının altında. Yetkili onayıyla devam edilsin mi?", "Maliyet Altı Satış"))
            return;
        
        if (_api == null) return;

        IsLoading = true;
        try
        {
            var isNew = _allProducts.All(p => p.Id != FormModel.Id);
            
            if (isNew)
            {
                var req = new CreateProductRequest(
                    Code: FormModel.Code,
                    Name: FormModel.Name,
                    SalePrice: FormModel.SalePrice,
                    VatRate: FormModel.VatRate,
                    PurchasePrice: FormModel.PurchasePrice,
                    DealerPrice: FormModel.DealerPrice,
                    MinStockLevel: FormModel.MinStockLevel,
                    MaxStockLevel: FormModel.MaxStockLevel,
                    Barcode1: FormModel.Barcode1,
                    Barcode2: FormModel.Barcode2,
                    Warehouse: FormModel.Warehouse,
                    SupplierName: FormModel.SupplierName,
                    LeadTimeDays: FormModel.LeadTimeDays,
                    Color: FormModel.Color,
                    Size: FormModel.Size,
                    IsPublishedEcommerce: FormModel.IsPublishedEcommerce,
                    SeoTitle: FormModel.SeoTitle,
                    PurchaseAccountCode: FormModel.PurchaseAccountCode,
                    SalesAccountCode: FormModel.SalesAccountCode,
                    ShortName: null, Description: null,
                    CategoryId: FormModel.SelectedCategory.Id,
                    BrandId: FormModel.SelectedBrand.Id,
                    UnitId: null, TrackStock: true,
                    ImagePath: FormModel.ImagePath,
                    Variants: FormModel.Variants.Select(v => new SaveProductVariantRequest(
                        v.Code, v.Color, v.Size, v.Attribute1, v.Attribute2,
                        v.SalePrice, true)).ToList()
                );
                
                var result = await _api.PostAsync<ProductResponse>("api/products", req);
                if (result != null)
                {
                    _dialog?.Info("Ürün başarıyla kaydedildi.", "Başarılı");
                    IsFormOpen = false;
                    Application.Current.Windows.OfType<ProductEditorWindow>().FirstOrDefault()?.Close();
                    ClearFilters();
                    await LoadProductsAsync();
                }
                else
                {
                    _dialog?.Error("Ürün kaydedilirken bir hata oluştu veya bu kod zaten var.");
                }
            }
            else
            {
                var req = new UpdateProductRequest(
                    Name: FormModel.Name,
                    SalePrice: FormModel.SalePrice,
                    VatRate: FormModel.VatRate,
                    PurchasePrice: FormModel.PurchasePrice,
                    DealerPrice: FormModel.DealerPrice,
                    MinStockLevel: FormModel.MinStockLevel,
                    MaxStockLevel: FormModel.MaxStockLevel,
                    Barcode1: FormModel.Barcode1,
                    Barcode2: FormModel.Barcode2,
                    Warehouse: FormModel.Warehouse,
                    SupplierName: FormModel.SupplierName,
                    LeadTimeDays: FormModel.LeadTimeDays,
                    Color: FormModel.Color,
                    Size: FormModel.Size,
                    IsPublishedEcommerce: FormModel.IsPublishedEcommerce,
                    SeoTitle: FormModel.SeoTitle,
                    PurchaseAccountCode: FormModel.PurchaseAccountCode,
                    SalesAccountCode: FormModel.SalesAccountCode,
                    ShortName: null, Description: null,
                    CategoryId: FormModel.SelectedCategory.Id,
                    BrandId: FormModel.SelectedBrand.Id,
                    UnitId: null, IsActive: true,
                    ImagePath: FormModel.ImagePath
                );

                var result = await _api.PutAsync<ProductResponse>($"api/products/{FormModel.Id}", req);
                if (result != null)
                {
                    _dialog?.Info("Ürün başarıyla güncellendi.", "Başarılı");
                    IsFormOpen = false;
                    Application.Current.Windows.OfType<ProductEditorWindow>().FirstOrDefault()?.Close();
                    await LoadProductsAsync();
                }
                else
                {
                    _dialog?.Error("Ürün güncellenirken bir hata oluştu.");
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand] private void CloseForm() { IsFormOpen = false; }
    
    [RelayCommand]
    private async Task DeleteProduct(ProductDetailItem? product)
    {
        if (product is null) return;
        if (_dialog is not null &&
            !_dialog.Confirm("Ürün silinecek (pasife alınacak). Devam edilsin mi?"))
            return;

        if (_api == null) return;

        IsLoading = true;
        try
        {
            var success = await _api.DeleteAsync($"api/products/{product.Id}");
            if (success)
            {
                _dialog?.Info("Ürün başarıyla silindi.", "Başarılı");
                ClosePanel();
                await LoadProductsAsync();
            }
            else
            {
                _dialog?.Error("Ürün silinirken hata oluştu.");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
    [RelayCommand]
    private void PrintBarcodes()
    {
        var checkedItems = Products.Where(x => x.IsChecked).ToList();
        List<ProductDetailItem> items = checkedItems.Count > 0
            ? checkedItems
            : SelectedProduct is not null ? [SelectedProduct] : [];
        if (items.Count == 0)
        {
            _dialog?.Info("Barkod basmak için tablodan en az bir ürün işaretleyin.", "Barkod Yazdırma");
            return;
        }

        var document = new FlowDocument
        {
            PagePadding = new Thickness(24),
            ColumnWidth = double.PositiveInfinity,
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
        };
        foreach (var product in items)
        {
            var block = new Paragraph { Margin = new Thickness(0, 0, 0, 12) };
            block.Inlines.Add(new Bold(new Run(product.Name)));
            block.Inlines.Add(new LineBreak());
            block.Inlines.Add(new Run($"Ürün Kodu: {product.Code}"));
            block.Inlines.Add(new LineBreak());
            block.Inlines.Add(new Run($"EAN-13: {product.Barcode}"));
            block.Inlines.Add(new LineBreak());
            block.Inlines.Add(new Run($"Fiyat: {product.SalePrice:N2} ₺"));
            document.Blocks.Add(block);
        }

        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() == true)
            printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Re ERP Barkod Etiketleri");
    }

    [RelayCommand]
    private void Export()
    {
        if (Products.Count == 0)
        {
            _dialog?.Info("Dışa aktarılacak ürün bulunamadı.", "Excel / CSV");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Ürün listesini dışa aktar",
            Filter = "Excel Çalışma Kitabı (*.xlsx)|*.xlsx",
            FileName = $"urun-listesi-{DateTime.Now:yyyyMMdd-HHmm}.xlsx"
        };
        if (dialog.ShowDialog() != true) return;

        var rows = new List<IReadOnlyList<string>>
        {
            new[] { "Ürün Kodu", "Barkod", "Ürün Adı", "Kategori", "Marka", "Depo",
                "Mevcut Stok", "Kullanılabilir", "Alış Fiyatı", "Satış Fiyatı", "Bayi Fiyatı", "KDV", "Durum" }
        };
        rows.AddRange(Products.Select(p => (IReadOnlyList<string>)new[]
        {
            p.Code, p.Barcode, p.Name, p.CategoryName, p.Brand, p.Warehouse,
            p.StockLevel.ToString("0.###"), p.AvailableStock.ToString("0.###"),
            p.PurchasePrice.ToString("0.00"), p.SalePrice.ToString("0.00"),
            p.DealerPrice.ToString("0.00"), p.VatRate.ToString(), p.IsActive ? "Aktif" : "Pasif"
        }));
        WriteExcel(dialog.FileName, rows);
        _dialog?.Info($"{Products.Count} ürün dışa aktarıldı.", "Aktarım Tamamlandı");
    }

    private static void WriteExcel(string path, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        using var document = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook);
        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Xl.Workbook();
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        var sheetData = new Xl.SheetData();
        worksheetPart.Worksheet = new Xl.Worksheet(sheetData);
        var sheets = workbookPart.Workbook.AppendChild(new Xl.Sheets());
        sheets.Append(new Xl.Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Ürünler" });
        foreach (var sourceRow in rows)
        {
            var row = new Xl.Row();
            foreach (var value in sourceRow)
                row.Append(new Xl.Cell { DataType = Xl.CellValues.InlineString, InlineString = new Xl.InlineString(new Xl.Text(value ?? string.Empty)) });
            sheetData.Append(row);
        }
        workbookPart.Workbook.Save();
    }
    [RelayCommand] private void ClearFilters()
    {
        SearchText = ""; SelectedCategory = "Tümü"; SelectedStockFilter = "Tümü"; CurrentPage = 1;
    }
    [RelayCommand]
    private void SelectAll()
    {
        var shouldCheck = Products.Any(x => !x.IsChecked);
        foreach (var product in Products) product.IsChecked = shouldCheck;
    }
    [RelayCommand]
    private void ManageCatalog()
    {
        try
        {
            var window = App.Services.GetRequiredService<ProductCatalogWindow>();
            var owner = Application.Current.Windows
                .OfType<Re.Desktop.Views.Shell.MainWindow>()
                .FirstOrDefault(x => x.IsVisible);
            if (owner is not null) window.Owner = owner;
            window.ShowDialog();
            _ = LoadProductsAsync();
        }
        catch (Exception ex)
        {
            _dialog?.Error($"Katalog yönetimi açılamadı.\n{ex.GetBaseException().Message}", "Katalog Yönetimi");
        }
    }
    [RelayCommand] private void PreviousPage() { if (HasPreviousPage) { CurrentPage--; ApplyFilter(); } }
    [RelayCommand] private void NextPage() { if (HasNextPage) { CurrentPage++; ApplyFilter(); } }
}

public partial class ProductFormModel : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _categoryName = string.Empty;
    [ObservableProperty] private string _brand = string.Empty;
    [ObservableProperty] private CatalogItemResponse? _selectedCategory;
    [ObservableProperty] private CatalogItemResponse? _selectedBrand;
    [ObservableProperty] private string _imagePath = string.Empty;
    
    // Tab 2: Barkod
    [ObservableProperty] private string _barcode1 = string.Empty;
    [ObservableProperty] private string _barcode2 = string.Empty;
    [ObservableProperty] private string _unit = "Adet";

    // Tab 3: Fiyatlandırma
    [ObservableProperty] private decimal _purchasePrice;
    [ObservableProperty] private decimal _salePrice;
    [ObservableProperty] private decimal _dealerPrice;
    [ObservableProperty] private int _vatRate = 20;

    // Tab 4: Stok Ayarları
    [ObservableProperty] private int _minStockLevel = 10;
    [ObservableProperty] private int _maxStockLevel = 100;
    [ObservableProperty] private int _safetyStockLevel = 5;
    [ObservableProperty] private int _reorderPoint = 15;
    [ObservableProperty] private int _economicOrderQuantity = 50;
    [ObservableProperty] private bool _allowNegativeStock;
    [ObservableProperty] private bool _useReservations = true;
    [ObservableProperty] private bool _isLotTrackingEnabled;
    [ObservableProperty] private bool _isSerialTrackingEnabled;
    [ObservableProperty] private bool _isExpiryTrackingEnabled;
    [ObservableProperty] private string _valuationMethod = "Hareketli Ortalama";

    // Tab 5: Depo Stokları
    [ObservableProperty] private string _warehouse = "Merkez Depo";
    [ObservableProperty] private string _location = "A-01-01";
    [ObservableProperty] private int _stockLevel;
    [ObservableProperty] private int _reservedStock;
    public int AvailableStock => Math.Max(0, StockLevel - ReservedStock);

    partial void OnStockLevelChanged(int value) => OnPropertyChanged(nameof(AvailableStock));
    partial void OnReservedStockChanged(int value) => OnPropertyChanged(nameof(AvailableStock));

    // Tab 6: Tedarik
    [ObservableProperty] private string _supplierName = string.Empty;
    [ObservableProperty] private string _supplierProductCode = string.Empty;
    [ObservableProperty] private int _leadTimeDays = 3;
    [ObservableProperty] private int _minimumOrderQuantity = 1;
    [ObservableProperty] private string _supplierCurrency = "TRY";
    [ObservableProperty] private decimal _supplierDiscountRate;

    // Tab 7: Varyant
    [ObservableProperty] private string _color = string.Empty;
    [ObservableProperty] private string _size = string.Empty;
    [ObservableProperty] private string _season = string.Empty;
    [ObservableProperty] private string _pattern = string.Empty;
    public ObservableCollection<ProductVariantDraft> Variants { get; } = [];

    // Tab 8: E-ticaret
    [ObservableProperty] private bool _isPublishedEcommerce;
    [ObservableProperty] private string _seoTitle = string.Empty;
    [ObservableProperty] private string _ecommerceTitle = string.Empty;
    [ObservableProperty] private string _ecommerceUrl = string.Empty;
    [ObservableProperty] private int _channelStock;
    [ObservableProperty] private decimal _shippingWeight;

    // Tab 9: Muhasebe
    [ObservableProperty] private string _purchaseAccountCode = "153.01";
    [ObservableProperty] private string _salesAccountCode = "600.01";
    [ObservableProperty] private string _salesReturnAccountCode = "610.01";
    [ObservableProperty] private string _purchaseReturnAccountCode = "153.02";
    [ObservableProperty] private string _costAccountCode = "621.01";
    [ObservableProperty] private string _wasteAccountCode = "689.01";
}

public class ProductDetailItem : ObservableObject
{
    private bool _isChecked;
    public bool IsChecked
    {
        get => _isChecked;
        set => SetProperty(ref _isChecked, value);
    }
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public decimal DealerPrice { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal AverageCost { get; set; }
    public int VatRate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    
    public int StockLevel { get; set; }
    public int ReservedStock { get; set; }
    public int MinStockLevel { get; set; }
    public decimal MaxStockLevel { get; set; }
    public string Warehouse { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public DateTime LastUpdate { get; set; }

    public int AvailableStock => StockLevel - ReservedStock;
    public string StockStatus => StockLevel == 0 ? "Tükendi" : (AvailableStock <= MinStockLevel ? "Kritik Stok" : "Stok Yeterli");
    public string StockStatusColor => StockLevel == 0 ? "#EF4444" : (AvailableStock <= MinStockLevel ? "#F59E0B" : "#10B981");
    public string StockStatusBg => StockLevel == 0 ? "#FEE2E2" : (AvailableStock <= MinStockLevel ? "#FEF3C7" : "#DCFCE7");
    public decimal ProfitMargin => AverageCost > 0 ? ((SalePrice - AverageCost) / AverageCost) * 100 : 100;
}

public partial class ProductVariantDraft : ObservableObject
{
    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _color = string.Empty;
    [ObservableProperty] private string _size = string.Empty;
    [ObservableProperty] private string _attribute1 = string.Empty;
    [ObservableProperty] private string _attribute2 = string.Empty;
    [ObservableProperty] private decimal _salePrice;
}
