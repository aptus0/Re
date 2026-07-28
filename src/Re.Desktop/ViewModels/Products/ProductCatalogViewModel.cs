using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Re.Contracts.Products;
using Re.Desktop.Services;

namespace Re.Desktop.ViewModels.Products;

public partial class ProductCatalogViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly IDialogService _dialog;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusText = "Preparing catalog data...";
    [ObservableProperty] private CatalogItemResponse? _selectedCategory;
    [ObservableProperty] private CatalogItemResponse? _selectedBrand;
    [ObservableProperty] private CatalogItemResponse? _selectedCollection;
    [ObservableProperty] private string _categoryCode = "";
    [ObservableProperty] private string _categoryName = "";
    [ObservableProperty] private string _categoryDescription = "";
    [ObservableProperty] private string _brandCode = "";
    [ObservableProperty] private string _brandName = "";
    [ObservableProperty] private string _brandLogoPath = "";
    [ObservableProperty] private string _collectionCode = "";
    [ObservableProperty] private string _collectionName = "";
    [ObservableProperty] private string _collectionSeason = "";
    [ObservableProperty] private DateTime? _collectionStartDate = DateTime.Today;
    [ObservableProperty] private DateTime? _collectionEndDate = DateTime.Today.AddMonths(3);

    public ObservableCollection<CatalogItemResponse> Categories { get; } = [];
    public ObservableCollection<CatalogItemResponse> Brands { get; } = [];
    public ObservableCollection<CatalogItemResponse> Collections { get; } = [];

    public ProductCatalogViewModel(ApiClient api, IDialogService dialog)
    {
        _api = api;
        _dialog = dialog;
        _ = LoadAsync();
    }

    partial void OnSelectedCategoryChanged(CatalogItemResponse? value)
    { if (value is null) return; CategoryCode = value.Code; CategoryName = value.Name; CategoryDescription = value.Detail ?? ""; }
    partial void OnSelectedBrandChanged(CatalogItemResponse? value)
    { if (value is null) return; BrandCode = value.Code; BrandName = value.Name; BrandLogoPath = value.Detail ?? ""; }
    partial void OnSelectedCollectionChanged(CatalogItemResponse? value)
    { if (value is null) return; CollectionCode = value.Code; CollectionName = value.Name; CollectionSeason = value.Detail ?? ""; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await LoadList("api/product-catalog/categories", Categories);
            await LoadList("api/product-catalog/brands", Brands);
            await LoadList("api/product-catalog/collections", Collections);
            StatusText = $"{Categories.Count} kategori • {Brands.Count} marka • {Collections.Count} koleksiyon";
        }
        catch (Exception ex) { StatusText = "Catalog could not be loaded."; _dialog.Error(ex.Message, "Catalog Management"); }
        finally { IsLoading = false; }
    }

    private async Task LoadList(string url, ObservableCollection<CatalogItemResponse> target)
    {
        var items = await _api.GetAsync<IReadOnlyCollection<CatalogItemResponse>>(url);
        target.Clear();
        if (items is not null) foreach (var item in items) target.Add(item);
    }

    [RelayCommand] private void NewCategory() { SelectedCategory = null; CategoryCode = ""; CategoryName = ""; CategoryDescription = ""; }
    [RelayCommand] private void NewBrand() { SelectedBrand = null; BrandCode = ""; BrandName = ""; BrandLogoPath = ""; }
    [RelayCommand] private void NewCollection() { SelectedCollection = null; CollectionCode = ""; CollectionName = ""; CollectionSeason = ""; CollectionStartDate = DateTime.Today; CollectionEndDate = DateTime.Today.AddMonths(3); }

    [RelayCommand]
    private async Task SaveCategoryAsync()
    {
        if (!Validate(CategoryCode, CategoryName)) return;
        var body = new SaveCategoryRequest(CategoryCode, CategoryName, CategoryDescription, null);
        var result = SelectedCategory is null
            ? await _api.PostAsync<CatalogItemResponse>("api/product-catalog/categories", body)
            : await _api.PutAsync<CatalogItemResponse>($"api/product-catalog/categories/{SelectedCategory.Id}", body);
        await FinishSave(result, NewCategory);
    }

    [RelayCommand]
    private async Task SaveBrandAsync()
    {
        if (!Validate(BrandCode, BrandName)) return;
        var body = new SaveBrandRequest(BrandCode, BrandName, BrandLogoPath);
        var result = SelectedBrand is null
            ? await _api.PostAsync<CatalogItemResponse>("api/product-catalog/brands", body)
            : await _api.PutAsync<CatalogItemResponse>($"api/product-catalog/brands/{SelectedBrand.Id}", body);
        await FinishSave(result, NewBrand);
    }

    [RelayCommand]
    private async Task SaveCollectionAsync()
    {
        if (!Validate(CollectionCode, CollectionName)) return;
        if (CollectionStartDate > CollectionEndDate) { _dialog.Error("End date cannot be earlier than start date."); return; }
        var body = new SaveCollectionRequest(CollectionCode, CollectionName, CollectionSeason, CollectionStartDate, CollectionEndDate);
        var result = SelectedCollection is null
            ? await _api.PostAsync<CatalogItemResponse>("api/product-catalog/collections", body)
            : await _api.PutAsync<CatalogItemResponse>($"api/product-catalog/collections/{SelectedCollection.Id}", body);
        await FinishSave(result, NewCollection);
    }

    [RelayCommand] private async Task DeleteCategoryAsync() => await Delete("categories", SelectedCategory);
    [RelayCommand] private async Task DeleteBrandAsync() => await Delete("brands", SelectedBrand);
    [RelayCommand] private async Task DeleteCollectionAsync() => await Delete("collections", SelectedCollection);

    private bool Validate(string code, string name)
    {
        if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name)) return true;
        _dialog.Error("Code and name are required.", "Validation"); return false;
    }
    private async Task FinishSave(CatalogItemResponse? result, Action reset)
    {
        if (result is null) { _dialog.Error("The record could not be saved. Code must be unique."); return; }
        reset(); await LoadAsync(); _dialog.Success("Catalog record saved successfully.");
    }
    private async Task Delete(string segment, CatalogItemResponse? item)
    {
        if (item is null) { _dialog.Error("Select a record first."); return; }
        if (!_dialog.Confirm($"'{item.Name}' will be deactivated. Continue?")) return;
        if (!await _api.DeleteAsync($"api/product-catalog/{segment}/{item.Id}")) { _dialog.Error("The record could not be deactivated."); return; }
        await LoadAsync();
    }
}
