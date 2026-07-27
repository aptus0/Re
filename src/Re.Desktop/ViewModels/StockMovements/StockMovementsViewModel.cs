using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Xl = DocumentFormat.OpenXml.Spreadsheet;
using Re.Contracts.Inventory;
using Re.Desktop.Services;

namespace Re.Desktop.ViewModels.StockMovements;

public partial class StockMovementsViewModel(ApiClient api, IDialogService dialog) : ObservableObject
{
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _selectedDirection = "Tümü";
    [ObservableProperty] private MovementTypeFilter _selectedMovementType = MovementTypes[0];
    [ObservableProperty] private DateTime? _startDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime? _endDate = DateTime.Today;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private decimal _todayInbound;
    [ObservableProperty] private decimal _todayOutbound;
    [ObservableProperty] private int _todayMovementCount;
    [ObservableProperty] private decimal _periodInbound;
    [ObservableProperty] private decimal _periodOutbound;
    [ObservableProperty] private decimal _periodNet;
    [ObservableProperty] private StockMovementListItem? _selectedMovement;

    public ObservableCollection<StockMovementListItem> Movements { get; } = [];
    public IReadOnlyList<string> Directions { get; } = ["Tümü", "Giriş", "Çıkış"];
    public static IReadOnlyList<MovementTypeFilter> MovementTypes { get; } =
    [
        new(null, "Tüm Hareketler"), new(1, "Alış Mal Kabul"), new(2, "Satış Sevkiyat"),
        new(3, "Alış İade"), new(4, "Satış İade"), new(5, "Depo Transferi"),
        new(6, "Stok Sayımı"), new(7, "Fire / Zayiat"), new(8, "Üretim Girişi"),
        new(9, "Üretim Tüketimi"), new(10, "Açılış Bakiyesi")
    ];
    public bool HasNoData => !IsLoading && Movements.Count == 0;

    public async Task InitializeAsync() => await LoadAsync();
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(HasNoData));

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var parameters = new List<string>
            {
                $"from={StartDate:yyyy-MM-dd}", $"to={EndDate:yyyy-MM-dd}", "size=500"
            };
            if (!string.IsNullOrWhiteSpace(SearchText)) parameters.Add($"search={Uri.EscapeDataString(SearchText.Trim())}");
            if (SelectedDirection != "Tümü") parameters.Add($"direction={Uri.EscapeDataString(SelectedDirection)}");
            if (SelectedMovementType.Id.HasValue) parameters.Add($"movementType={SelectedMovementType.Id}");
            var result = await api.GetAsync<StockMovementListResult>($"api/stock-movements?{string.Join("&", parameters)}");
            Movements.Clear();
            foreach (var item in result?.Items ?? []) Movements.Add(item);
            TotalCount = result?.TotalCount ?? 0;
            TodayInbound = result?.Summary.TodayInbound ?? 0;
            TodayOutbound = result?.Summary.TodayOutbound ?? 0;
            TodayMovementCount = result?.Summary.TodayMovementCount ?? 0;
            PeriodInbound = result?.Summary.PeriodInbound ?? 0;
            PeriodOutbound = result?.Summary.PeriodOutbound ?? 0;
            PeriodNet = result?.Summary.PeriodNet ?? 0;
        }
        catch (Exception ex) { dialog.Error($"Stok hareketleri alınamadı.\n{ex.GetBaseException().Message}"); }
        finally { IsLoading = false; OnPropertyChanged(nameof(HasNoData)); }
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        SearchText = ""; SelectedDirection = "Tümü"; SelectedMovementType = MovementTypes[0];
        StartDate = DateTime.Today.AddDays(-30); EndDate = DateTime.Today;
        await LoadAsync();
    }

    [RelayCommand]
    private void Export()
    {
        if (Movements.Count == 0) { dialog.Info("Dışa aktarılacak stok hareketi bulunamadı."); return; }
        var save = new SaveFileDialog { Filter = "Excel Çalışma Kitabı (*.xlsx)|*.xlsx", FileName = $"stok-hareketleri-{DateTime.Now:yyyyMMdd-HHmm}.xlsx" };
        if (save.ShowDialog() != true) return;
        using var document = SpreadsheetDocument.Create(save.FileName, SpreadsheetDocumentType.Workbook);
        var workbook = document.AddWorkbookPart(); workbook.Workbook = new Xl.Workbook();
        var worksheet = workbook.AddNewPart<WorksheetPart>(); var data = new Xl.SheetData(); worksheet.Worksheet = new Xl.Worksheet(data);
        workbook.Workbook.AppendChild(new Xl.Sheets()).Append(new Xl.Sheet { Id = workbook.GetIdOfPart(worksheet), SheetId = 1, Name = "Stok Hareketleri" });
        AddRow(data, ["Tarih", "Tür", "Yön", "Ürün Kodu", "Ürün", "Barkod", "Varyant", "Depo", "Miktar", "Birim Maliyet", "Tutar", "Bakiye", "Belge", "Kullanıcı", "Açıklama"]);
        foreach (var x in Movements) AddRow(data, [x.MovementDate.ToString("dd.MM.yyyy HH:mm"), x.MovementTypeName, x.Direction, x.ProductCode, x.ProductName, x.Barcode ?? "", x.VariantCode ?? "", x.WarehouseName, x.Quantity.ToString("0.####"), x.UnitCost.ToString("0.00"), x.TotalCost.ToString("0.00"), x.StockAfterMovement.ToString("0.####"), x.ReferenceDocumentType ?? "", x.UserName, x.Notes ?? ""]);
        workbook.Workbook.Save();
        dialog.Info($"{Movements.Count} hareket Excel dosyasına aktarıldı.", "Aktarım Tamamlandı");
    }

    private static void AddRow(Xl.SheetData data, IEnumerable<string> values)
    {
        var row = new Xl.Row();
        foreach (var value in values) row.Append(new Xl.Cell { DataType = Xl.CellValues.InlineString, InlineString = new Xl.InlineString(new Xl.Text(value)) });
        data.Append(row);
    }
}

public sealed record MovementTypeFilter(int? Id, string Name);
