namespace Re.Contracts.Inventory;

public sealed record InventoryDashboardResponse(
    int TotalProducts, int ActiveProducts, decimal TotalStockQuantity, decimal TotalStockValue,
    int CriticalStockCount, int OutOfStockCount, int NegativeStockCount, int Inactive30DaysCount,
    decimal TodayInbound, decimal TodayOutbound, DateTime RefreshedAt,
    IReadOnlyCollection<CriticalStockItem> CriticalProducts,
    IReadOnlyCollection<RecentStockMovementItem> RecentMovements);

public sealed record CriticalStockItem(
    Guid ProductId, string Code, string Name, string? ImagePath,
    string? CategoryName, string? BrandName, decimal Stock, decimal MinStock,
    decimal Shortage, string Status);

public sealed record RecentStockMovementItem(
    Guid Id, DateTime MovementDate, string TypeName, string Direction,
    string ProductCode, string ProductName, string? Barcode, string WarehouseName,
    decimal Quantity, decimal StockAfterMovement, string? ReferenceDocumentType);
