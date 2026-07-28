using Re.Domain.Entities.Common;
using Re.Domain.Enums;
using Re.Domain.Exceptions;

namespace Re.Domain.Entities.Purchasing;

public class PurchaseInvoice : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public string DocumentNumber { get; private set; } = "";
    public string? SupplierDocumentNumber { get; private set; }
    public DateTime DocumentDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DocumentStatus Status { get; private set; } = DocumentStatus.Draft;
    public decimal SubTotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public decimal ExchangeRate { get; private set; } = 1;
    public string? Notes { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public ICollection<PurchaseInvoiceLine> Lines { get; private set; } = [];

    private PurchaseInvoice() { }

    public static PurchaseInvoice Create(Guid companyId, Guid branchId, Guid supplierId,
        Guid warehouseId, string documentNumber, DateTime documentDate)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
            throw new DomainException("Purchase invoice number is required.");
        return new()
        {
            Id = Guid.NewGuid(), CompanyId = companyId, BranchId = branchId,
            SupplierId = supplierId, WarehouseId = warehouseId,
            DocumentNumber = documentNumber.Trim().ToUpperInvariant(),
            DocumentDate = documentDate, CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateHeader(string documentNumber, string? supplierDocumentNumber,
        DateTime documentDate, DateTime? dueDate, string currency, decimal exchangeRate, string? notes)
    {
        EnsureDraft();
        if (string.IsNullOrWhiteSpace(documentNumber) || exchangeRate <= 0)
            throw new DomainException("Document number and positive exchange rate are required.");
        DocumentNumber = documentNumber.Trim().ToUpperInvariant();
        SupplierDocumentNumber = supplierDocumentNumber?.Trim();
        DocumentDate = documentDate; DueDate = dueDate;
        Currency = string.IsNullOrWhiteSpace(currency) ? "TRY" : currency.Trim().ToUpperInvariant();
        ExchangeRate = exchangeRate; Notes = notes; UpdatedAt = DateTime.UtcNow;
    }

    public void ReplaceLines(IEnumerable<PurchaseInvoiceLine> lines)
    {
        EnsureDraft(); Lines.Clear();
        foreach (var line in lines) Lines.Add(line);
        Recalculate();
    }

    public void Approve(Guid userId)
    {
        EnsureDraft();
        if (Lines.Count == 0) throw new DomainException("Purchase invoice requires at least one line.");
        Status = DocumentStatus.Approved; ApprovedBy = userId;
        ApprovedAt = DateTime.UtcNow; UpdatedAt = DateTime.UtcNow;
    }

    private void Recalculate()
    {
        SubTotal = Lines.Sum(x => x.NetAmount);
        TaxAmount = Lines.Sum(x => x.TaxAmount);
        TotalAmount = SubTotal + TaxAmount;
    }
    private void EnsureDraft()
    {
        if (Status != DocumentStatus.Draft)
            throw new DocumentLockedException("PurchaseInvoice", Id);
    }
}

public class PurchaseInvoiceLine : BaseEntity
{
    public Guid PurchaseInvoiceId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal VatRate { get; set; }
    public decimal NetAmount => Quantity * UnitPrice * (1 - DiscountPercent / 100m);
    public decimal TaxAmount => NetAmount * VatRate / 100m;
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public PurchaseInvoice PurchaseInvoice { get; set; } = null!;
}
