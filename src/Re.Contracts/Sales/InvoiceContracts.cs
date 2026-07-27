namespace Re.Contracts.Sales;

public record CreateInvoiceLineRequest(
    Guid ProductId,
    Guid? ProductVariantId,
    Guid? UnitId,
    string ProductName,
    string? ProductCode,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal VatRate,
    int SortOrder,
    string? Notes);

public record CreateInvoiceRequest(
    Guid BranchId,
    string DocumentNumber,
    DateTime DocumentDate,
    Guid? CustomerId,
    Guid? WarehouseId,
    string? Notes,
    List<CreateInvoiceLineRequest> Lines);

public record UpdateInvoiceLineRequest(
    Guid? Id, // Null if new line
    Guid ProductId,
    Guid? ProductVariantId,
    Guid? UnitId,
    string ProductName,
    string? ProductCode,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal VatRate,
    int SortOrder,
    string? Notes);

public record UpdateInvoiceRequest(
    string DocumentNumber,
    DateTime DocumentDate,
    Guid? CustomerId,
    Guid? WarehouseId,
    string? Notes,
    List<UpdateInvoiceLineRequest> Lines);

public record InvoiceLineResponse(
    Guid Id,
    Guid ProductId,
    Guid? ProductVariantId,
    Guid? UnitId,
    string ProductName,
    string? ProductCode,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal VatRate,
    decimal LineTotal,
    decimal TaxAmount,
    decimal LineTotalWithTax,
    int SortOrder,
    string? Notes);

public record InvoiceResponse(
    Guid Id,
    Guid BranchId,
    Guid? CustomerId,
    string? CustomerName,
    Guid? WarehouseId,
    string DocumentNumber,
    string Status,
    DateTime DocumentDate,
    DateTime? DueDate,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal DiscountPercent,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    string Currency,
    decimal ExchangeRate,
    string? Notes,
    DateTime CreatedAt,
    List<InvoiceLineResponse> Lines);

public record InvoiceListResponse(
    Guid Id,
    string DocumentNumber,
    DateTime DocumentDate,
    Guid? CustomerId,
    string? CustomerName,
    decimal TotalAmount,
    decimal PaidAmount,
    string Status);
