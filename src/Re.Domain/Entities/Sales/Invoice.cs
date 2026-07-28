using Re.Domain.Entities.Common;
using Re.Domain.Enums;
using Re.Domain.Exceptions;

namespace Re.Domain.Entities.Sales;

/// <summary>
/// Satış faturası – ERP'nin merkezi satış belgesi.
/// Onaylandıktan sonra değiştirilemez; hata için iptal + ters kayıt uygulanır.
/// </summary>
public class Invoice : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; private set; }
    public Guid? CustomerId { get; private set; }    // Cari hesap
    public Guid? WarehouseId { get; private set; }

    public string DocumentNumber { get; private set; } = string.Empty;
    public DocumentStatus Status { get; private set; } = DocumentStatus.Draft;
    public DateTime DocumentDate { get; private set; }
    public DateTime? DueDate { get; private set; }

    // Tutarlar
    public decimal SubTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RemainingAmount => TotalAmount - PaidAmount;

    public string Currency { get; private set; } = "TRY";
    public decimal ExchangeRate { get; private set; } = 1;

    public PaymentType? PaymentType { get; private set; }
    public string? Notes { get; private set; }

    // e-Belge
    public string? EInvoiceUuid { get; private set; }
    public DateTime? EInvoiceSentAt { get; private set; }
    public string? EInvoiceStatus { get; private set; }

    // İptal / ters kayıt
    public Guid? CancelledByInvoiceId { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancelReason { get; private set; }

    // Audit
    public Guid? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }

    // Navigation
    public ICollection<InvoiceLine> Lines { get; private set; } = new List<InvoiceLine>();

    private Invoice() { }

    public static Invoice Create(Guid companyId, Guid branchId, string documentNumber,
        DateTime documentDate, Guid? customerId = null, Guid? warehouseId = null)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
            throw new ArgumentException("Document number is required.");

        return new Invoice
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            DocumentNumber = documentNumber,
            DocumentDate = documentDate,
            CustomerId = customerId,
            WarehouseId = warehouseId,
            Status = DocumentStatus.Draft,
            Currency = "TRY",
            ExchangeRate = 1,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateBaseInfo(string documentNumber, DateTime documentDate, Guid? customerId, Guid? warehouseId, string? notes)
    {
        EnsureIsDraft();
        if (string.IsNullOrWhiteSpace(documentNumber))
            throw new ArgumentException("Document number is required.");

        DocumentNumber = documentNumber;
        DocumentDate = documentDate;
        CustomerId = customerId;
        WarehouseId = warehouseId;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCommercialTerms(DateTime? dueDate, string currency, decimal exchangeRate,
        PaymentType? paymentType)
    {
        EnsureIsDraft();
        if (dueDate.HasValue && dueDate.Value.Date < DocumentDate.Date)
            throw new DomainException("Due date cannot be earlier than invoice date.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
            throw new DomainException("Currency must be a three-letter code.");
        if (exchangeRate <= 0) throw new DomainException("Exchange rate must be positive.");
        DueDate = dueDate;
        Currency = currency.Trim().ToUpperInvariant();
        ExchangeRate = exchangeRate;
        PaymentType = paymentType;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkEInvoicePrepared(string uuid, string status)
    {
        if (Status is not (DocumentStatus.Approved or DocumentStatus.PartiallyPaid or DocumentStatus.FullyPaid))
            throw new DomainException("Only approved invoices can be prepared as an electronic document.");
        if (string.IsNullOrWhiteSpace(uuid)) throw new DomainException("Electronic document UUID is required.");
        EInvoiceUuid = uuid;
        EInvoiceStatus = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddLine(InvoiceLine line)
    {
        EnsureIsDraft();
        Lines.Add(line);
        RecalculateTotals();
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureIsDraft();
        var line = Lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new EntityNotFoundException("InvoiceLine", lineId);
        Lines.Remove(line);
        RecalculateTotals();
    }

    public void Approve(Guid approvedBy)
    {
        EnsureIsDraft();
        if (!Lines.Any())
            throw new DomainException("An invoice without lines cannot be approved.");

        Status = DocumentStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(Guid cancelledBy, string reason)
    {
        if (Status == DocumentStatus.Cancelled)
            throw new DomainException("Invoice is already cancelled.");
        if (Status == DocumentStatus.Draft)
        {
            Status = DocumentStatus.Cancelled;
            CancelReason = reason;
            CancelledAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            return;
        }
        // Onaylanmış fatura direkt iptal edilemez, ters kayıt gerekir
        throw new DocumentLockedException("Invoice", Id);
    }

    public void SetCancelledBy(Guid reversalInvoiceId, string reason)
    {
        // Sadece Infrastructure/Application katmanı çağırabilir
        Status = DocumentStatus.Cancelled;
        CancelledByInvoiceId = reversalInvoiceId;
        CancelReason = reason;
        CancelledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordPayment(decimal amount)
    {
        if (amount <= 0) throw new DomainException("Payment amount must be positive.");
        PaidAmount += amount;
        Status = PaidAmount >= TotalAmount
            ? DocumentStatus.FullyPaid
            : DocumentStatus.PartiallyPaid;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureIsDraft()
    {
        if (Status != DocumentStatus.Draft)
            throw new DocumentLockedException("Invoice", Id);
    }

    private void RecalculateTotals()
    {
        SubTotal = Lines.Sum(l => l.LineTotal);
        TaxAmount = Lines.Sum(l => l.TaxAmount);
        TotalAmount = SubTotal + TaxAmount - DiscountAmount;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Fatura satırı.
/// </summary>
public class InvoiceLine : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public Guid? UnitId { get; set; }

    public string ProductName { get; set; } = string.Empty;  // Snapshot
    public string? ProductCode { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatRate { get; set; }
    public decimal LineTotal => (Quantity * UnitPrice) - DiscountAmount;
    public decimal TaxAmount => LineTotal * VatRate / 100;
    public decimal LineTotalWithTax => LineTotal + TaxAmount;
    public int SortOrder { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Invoice Invoice { get; set; } = null!;
}



