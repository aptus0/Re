using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Re.Contracts.Products;
using Re.Desktop.Services;

namespace Re.Desktop.ViewModels.Products;

public partial class ProductVariantViewModel : ObservableObject
{
    private readonly ApiClient _api; private readonly IDialogService _dialog;
    public Guid ProductId { get; } public string ProductName { get; }
    public ObservableCollection<ProductVariantResponse> Variants { get; } = [];
    [ObservableProperty] private ProductVariantResponse? _selectedVariant;
    [ObservableProperty] private string _code = "";
    [ObservableProperty] private string _color = "";
    [ObservableProperty] private string _size = "";
    [ObservableProperty] private string _attribute1 = "";
    [ObservableProperty] private string _attribute2 = "";
    [ObservableProperty] private decimal _salePrice;
    [ObservableProperty] private bool _isActive = true;
    public decimal BasePrice { get; }
    public decimal PriceDifference => SalePrice - BasePrice;
    public ProductVariantViewModel(ApiClient api, IDialogService dialog, ProductDetailItem product)
    { _api = api; _dialog = dialog; ProductId = product.Id; ProductName = product.Name; BasePrice = product.SalePrice; SalePrice = product.SalePrice; _ = LoadAsync(); }
    partial void OnSelectedVariantChanged(ProductVariantResponse? value)
    { if (value is null) return; Code=value.Code; Color=value.Color??""; Size=value.Size??""; Attribute1=value.Attribute1??""; Attribute2=value.Attribute2??""; SalePrice=value.SalePrice; IsActive=value.IsActive; }
    partial void OnSalePriceChanged(decimal value) => OnPropertyChanged(nameof(PriceDifference));
    [RelayCommand] private async Task LoadAsync() { var x=await _api.GetAsync<IReadOnlyCollection<ProductVariantResponse>>($"api/products/{ProductId}/variants"); Variants.Clear(); if(x is not null) foreach(var i in x) Variants.Add(i); }
    [RelayCommand] private void NewVariant() { SelectedVariant=null; Code=""; Color=""; Size=""; Attribute1=""; Attribute2=""; SalePrice=BasePrice; IsActive=true; }
    [RelayCommand] private async Task SaveAsync()
    {
        if(string.IsNullOrWhiteSpace(Code)){_dialog.Error("Variant code is required.");return;}
        if (SalePrice < 0) { _dialog.Error("Variant sales price cannot be negative."); return; }
        var body=new SaveProductVariantRequest(Code,Color,Size,Attribute1,Attribute2,SalePrice,IsActive);
        var x=SelectedVariant is null ? await _api.PostAsync<ProductVariantResponse>($"api/products/{ProductId}/variants",body)
            : await _api.PutAsync<ProductVariantResponse>($"api/products/{ProductId}/variants/{SelectedVariant.Id}",body);
        if(x is null){_dialog.Error("The variant could not be saved. Code must be unique.");return;}
        _dialog.Success("Variant saved successfully.", "Product Variants"); NewVariant(); await LoadAsync();
    }
    [RelayCommand] private async Task DeleteAsync()
    { if(SelectedVariant is null)return; if(!_dialog.Confirm("Variant will be deactivated. Continue?"))return; await _api.DeleteAsync($"api/products/{ProductId}/variants/{SelectedVariant.Id}"); await LoadAsync(); }
}
