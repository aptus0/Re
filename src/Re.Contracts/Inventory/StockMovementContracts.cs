namespace Re.Contracts.Inventory;

public sealed record StockMovementListItem(
    Guid Id, DateTime MovementDate, string MovementType, string MovementTypeName,
    string Direction, string? ReferenceDocumentType, Guid? ReferenceDocumentId,
    string ProductCode, string ProductName, string? Barcode, string WarehouseName,
    decimal Quantity, decimal UnitCost, decimal TotalCost, decimal StockAfterMovement,
    string? VariantCode, string? LotNumber, string? SerialNumber, DateTime? ExpiryDate,
    string? Notes, string UserName);

public sealed record StockMovementSummary(
    decimal TodayInbound, decimal TodayOutbound, int TodayMovementCount,
    decimal PeriodInbound, decimal PeriodOutbound, decimal PeriodNet, int PeriodMovementCount);

public sealed record StockMovementListResult(
    IReadOnlyCollection<StockMovementListItem> Items, StockMovementSummary Summary,
    int TotalCount, int Page, int PageSize);
