namespace Re.Contracts.Orders;

public sealed record SaveOrderLineRequest(Guid ProductId, Guid? ProductVariantId, decimal Quantity,
    decimal UnitPrice, decimal DiscountPercent, decimal VatRate, string? Notes);
public sealed record CreateOrderRequest(Guid AccountId, Guid WarehouseId, string OrderNumber,
    string Type, DateTime OrderDate, DateTime? RequestedDeliveryDate, string Currency,
    decimal ExchangeRate, string? CustomerReference, string? Notes, IReadOnlyList<SaveOrderLineRequest> Lines);
public sealed record FulfilOrderLineRequest(Guid LineId, decimal Quantity);
public sealed record OrderListResponse(Guid Id, string OrderNumber, string Type, string Status,
    DateTime OrderDate, DateTime? RequestedDeliveryDate, string AccountName, string WarehouseName,
    decimal TotalAmount, string Currency, int LineCount, decimal FulfilmentPercent);
public sealed record OrderLineResponse(Guid Id, Guid ProductId, Guid? ProductVariantId,
    string ProductCode, string ProductName, decimal Quantity, decimal FulfilledQuantity,
    decimal RemainingQuantity, decimal UnitPrice, decimal DiscountPercent, decimal VatRate,
    decimal NetAmount, decimal TaxAmount, string? Notes);
public sealed record OrderResponse(Guid Id, Guid AccountId, Guid WarehouseId, string OrderNumber,
    string Type, string Status, DateTime OrderDate, DateTime? RequestedDeliveryDate,
    string Currency, decimal ExchangeRate, decimal SubTotal, decimal TaxAmount, decimal TotalAmount,
    string? CustomerReference, string? Notes, Guid? InvoiceId, IReadOnlyList<OrderLineResponse> Lines);
