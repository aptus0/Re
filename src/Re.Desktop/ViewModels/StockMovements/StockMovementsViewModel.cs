using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Xl = DocumentFormat.OpenXml.Spreadsheet;
using Re.Contracts.Inventory;
using Re.Desktop.Services;
using Re.Desktop.Views.StockMovements;
using System.Windows;

namespace Re.Desktop.ViewModels.StockMovements;

public partial class StockMovementsViewModel(ApiClient api, IDialogService dialog) : ObservableObject
{
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _selectedDirection = "All";
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
    [ObservableProperty] private WarehouseFilterItem _selectedWarehouse = WarehouseFilterItem.All;
    [ObservableProperty] private decimal _totalInventoryValue;
    [ObservableProperty] private int _criticalBalanceCount;
    [ObservableProperty] private int _trackedWarehouseCount;

    public ObservableCollection<StockMovementListItem> Movements { get; } = [];
    public ObservableCollection<WarehouseStockBalanceItem> WarehouseBalances { get; } = [];
    public ObservableCollection<WarehouseFilterItem> WarehouseFilters { get; } = [WarehouseFilterItem.All];
    public IReadOnlyList<string> Directions { get; } = ["All", "Receipt", "Issue"];
    public static IReadOnlyList<MovementTypeFilter> MovementTypes { get; } =
    [
        new(null, "All Movements"), new(1, "Purchase Receipt"), new(2, "Sales Shipment"),
        new(3, "Purchase Return"), new(4, "Sales Return"), new(5, "Warehouse Transfer"),
        new(6, "Inventory Count"), new(7, "Waste / Loss"), new(8, "Production Receipt"),
        new(9, "Production Consumption"), new(10, "Opening Balance")
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
            if (SelectedDirection != "All") parameters.Add($"direction={Uri.EscapeDataString(SelectedDirection)}");
            if (SelectedMovementType.Id.HasValue) parameters.Add($"movementType={SelectedMovementType.Id}");
            if (SelectedWarehouse.Id.HasValue) parameters.Add($"warehouseId={SelectedWarehouse.Id}");
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
            var balances = await api.GetAsync<IReadOnlyCollection<WarehouseStockBalanceItem>>(
                "api/stock-movements/balances");
            WarehouseBalances.Clear();
            foreach (var item in balances ?? []) WarehouseBalances.Add(item);
            TotalInventoryValue = WarehouseBalances.Sum(x => x.StockValue);
            CriticalBalanceCount = WarehouseBalances.Count(x => x.Status is "Low Stock" or "Out of Stock");
            TrackedWarehouseCount = WarehouseBalances.Select(x => x.WarehouseId).Distinct().Count();
            if (WarehouseFilters.Count == 1)
            {
                var warehouses = await api.GetAsync<IReadOnlyCollection<WarehouseLookupItem>>(
                    "api/stock-movements/warehouses");
                foreach (var item in warehouses ?? [])
                    WarehouseFilters.Add(new(item.Id, $"{item.Code} · {item.Name}"));
            }
        }
        catch (Exception ex) { dialog.Error($"Inventory movements could not be loaded.\n{ex.GetBaseException().Message}"); }
        finally { IsLoading = false; OnPropertyChanged(nameof(HasNoData)); }
    }

    [RelayCommand]
    private async Task NewOperationAsync()
    {
        var window = new InventoryOperationWindow
        {
            DataContext = new InventoryOperationViewModel(api, dialog)
        };
        window.Owner = Application.Current.Windows.OfType<Re.Desktop.Views.Shell.MainWindow>()
            .FirstOrDefault(x => x.IsVisible);
        window.ShowDialog();
        await LoadAsync();
    }

    [RelayCommand]
    private void ShowBalances()
    {
        var window = new WarehouseBalancesWindow { DataContext = this };
        window.Owner = Application.Current.Windows.OfType<Re.Desktop.Views.Shell.MainWindow>()
            .FirstOrDefault(x => x.IsVisible);
        window.ShowDialog();
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        SearchText = ""; SelectedDirection = "All"; SelectedMovementType = MovementTypes[0];
        SelectedWarehouse = WarehouseFilterItem.All;
        StartDate = DateTime.Today.AddDays(-30); EndDate = DateTime.Today;
        await LoadAsync();
    }

    [RelayCommand]
    private void Export()
    {
        if (Movements.Count == 0) { dialog.Warning("There are no stock movements to export."); return; }
        var save = new SaveFileDialog { Filter = "Excel Workbook (*.xlsx)|*.xlsx", FileName = $"stock-movements-{DateTime.Now:yyyyMMdd-HHmm}.xlsx" };
        if (save.ShowDialog() != true) return;
        using var document = SpreadsheetDocument.Create(save.FileName, SpreadsheetDocumentType.Workbook);
        var workbook = document.AddWorkbookPart(); workbook.Workbook = new Xl.Workbook();
        var worksheet = workbook.AddNewPart<WorksheetPart>(); var data = new Xl.SheetData(); worksheet.Worksheet = new Xl.Worksheet(data);
        workbook.Workbook.AppendChild(new Xl.Sheets()).Append(new Xl.Sheet { Id = workbook.GetIdOfPart(worksheet), SheetId = 1, Name = "Stock Movements" });
        AddRow(data, ["Date", "Type", "Direction", "Product Code", "Product", "Barcode", "Variant", "Warehouse", "Miktar", "Unit Cost", "Amount", "Bakiye", "Document", "User", "Description"]);
        foreach (var x in Movements) AddRow(data, [x.MovementDate.ToString("dd.MM.yyyy HH:mm"), x.MovementTypeName, x.Direction, x.ProductCode, x.ProductName, x.Barcode ?? "", x.VariantCode ?? "", x.WarehouseName, x.Quantity.ToString("0.####"), x.UnitCost.ToString("0.00"), x.TotalCost.ToString("0.00"), x.StockAfterMovement.ToString("0.####"), x.ReferenceDocumentType ?? "", x.UserName, x.Notes ?? ""]);
        workbook.Workbook.Save();
        dialog.Success($"{Movements.Count} movements were exported to Excel.", "Export Complete");
    }

    private static void AddRow(Xl.SheetData data, IEnumerable<string> values)
    {
        var row = new Xl.Row();
        foreach (var value in values) row.Append(new Xl.Cell { DataType = Xl.CellValues.InlineString, InlineString = new Xl.InlineString(new Xl.Text(value)) });
        data.Append(row);
    }
}

public sealed record MovementTypeFilter(int? Id, string Name);
public sealed record WarehouseFilterItem(Guid? Id, string Name)
{
    public static WarehouseFilterItem All { get; } = new(null, "All Warehouses");
}
