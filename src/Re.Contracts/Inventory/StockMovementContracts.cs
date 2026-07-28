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

public sealed record WarehouseLookupItem(Guid Id, string Code, string Name, bool IsActive);

public sealed record CreateStockAdjustmentRequest(
    Guid ProductId, Guid WarehouseId, decimal Quantity, decimal UnitCost,
    string Reason, string? ReferenceNumber = null);

public sealed record StockAdjustmentResponse(
    Guid MovementId, decimal PreviousStock, decimal AdjustmentQuantity,
    decimal CurrentStock, DateTime MovementDate);

public sealed record InventoryOperationRequest(
    string OperationType, Guid ProductId, Guid SourceWarehouseId,
    Guid? DestinationWarehouseId, decimal Quantity, decimal UnitCost,
    Guid? ProductVariantId, string? LotNumber, string? SerialNumber,
    DateTime? ExpiryDate, string Reason, string? ReferenceNumber);

public sealed record InventoryOperationResponse(
    string OperationNumber, IReadOnlyCollection<Guid> MovementIds,
    decimal SourceBalance, decimal? DestinationBalance);

public sealed record WarehouseStockBalanceItem(
    Guid ProductId, string ProductCode, string ProductName, string? Barcode,
    Guid WarehouseId, string WarehouseCode, string WarehouseName,
    decimal OnHand, decimal MinStockLevel, decimal StockValue,
    string Status);
