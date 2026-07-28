namespace Re.Contracts.Purchasing;

public record SavePurchaseInvoiceLineRequest(
    Guid ProductId, Guid? ProductVariantId, decimal Quantity, decimal UnitPrice,
    decimal DiscountPercent, decimal VatRate, string? LotNumber,
    string? SerialNumber, DateTime? ExpiryDate);

public record CreatePurchaseInvoiceRequest(
    Guid SupplierId, Guid WarehouseId, string DocumentNumber,
    string? SupplierDocumentNumber, DateTime DocumentDate, DateTime? DueDate,
    string Currency, decimal ExchangeRate, string? Notes,
    IReadOnlyCollection<SavePurchaseInvoiceLineRequest> Lines);

public record PurchaseInvoiceListResponse(
    Guid Id, string DocumentNumber, string? SupplierDocumentNumber,
    DateTime DocumentDate, string SupplierName, string WarehouseName,
    decimal TotalAmount, string Currency, string Status);

public record PurchaseInvoiceResponse(
    Guid Id, string DocumentNumber, string? SupplierDocumentNumber,
    DateTime DocumentDate, DateTime? DueDate, Guid SupplierId, string SupplierName,
    Guid WarehouseId, string WarehouseName, decimal SubTotal, decimal TaxAmount,
    decimal TotalAmount, string Currency, decimal ExchangeRate, string Status,
    string? Notes, IReadOnlyCollection<PurchaseInvoiceLineResponse> Lines);

public record PurchaseInvoiceLineResponse(
    Guid Id, Guid ProductId, Guid? ProductVariantId, string ProductCode,
    string ProductName, decimal Quantity, decimal UnitPrice,
    decimal DiscountPercent, decimal VatRate, decimal NetAmount,
    decimal TaxAmount, string? LotNumber, string? SerialNumber, DateTime? ExpiryDate);
