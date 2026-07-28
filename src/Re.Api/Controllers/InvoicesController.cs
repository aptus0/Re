using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Re.Contracts.Common;
using Re.Contracts.Sales;
using Re.Domain.Entities.Sales;
using Re.Persistence.Context;

namespace Re.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly ReDbContext _db;

    public InvoicesController(ReDbContext db)
    {
        _db = db;
    }

    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")?.Value ?? Guid.Empty.ToString());
    private Guid CurrentUserId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    /// <summary>Faturaları listele (sayfalı)</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<InvoiceListResponse>>>> GetInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20)
    {
        var query = _db.Invoices
            .Include(i => i.Lines)
            .Where(i => i.CompanyId == CompanyId);

        var total = await query.CountAsync();
        
        // Asıl müşteri ismini Account üzerinden çekmek gerekirse:
        // Join with Accounts, but for now we'll fetch them separately or if EF Core supports it.
        // Let's use left join with Accounts:
        
        var invoices = await query
            .OrderByDescending(i => i.DocumentDate)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        var customerIds = invoices.Where(i => i.CustomerId.HasValue).Select(i => i.CustomerId!.Value).Distinct().ToList();
        var customers = await _db.Accounts.Where(a => customerIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.Name);

        var items = invoices.Select(i => new InvoiceListResponse(
            i.Id,
            i.DocumentNumber,
            i.DocumentDate,
            i.CustomerId,
            i.CustomerId.HasValue && customers.ContainsKey(i.CustomerId.Value) ? customers[i.CustomerId.Value] : null,
            i.TotalAmount,
            i.PaidAmount,
            i.Status.ToString(), i.DueDate, i.RemainingAmount, i.Currency, i.EInvoiceStatus
        )).ToList();

        return Ok(ApiResponse<PagedResponse<InvoiceListResponse>>.Ok(new PagedResponse<InvoiceListResponse>
        {
            Items = items, TotalCount = total, Page = page, PageSize = size
        }));
    }

    /// <summary>Tekil fatura getir</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> GetInvoice(Guid id)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == CompanyId);

        if (invoice is null) return NotFound(ApiResponse<InvoiceResponse>.Fail("Invoice not found."));

        string? customerName = null;
        if (invoice.CustomerId.HasValue)
        {
            var customer = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == invoice.CustomerId.Value);
            customerName = customer?.Name;
        }

        var lines = invoice.Lines.OrderBy(l => l.SortOrder).Select(l => new InvoiceLineResponse(
            l.Id, l.ProductId, l.ProductVariantId, l.UnitId, l.ProductName, l.ProductCode,
            l.Quantity, l.UnitPrice, l.DiscountPercent, l.DiscountAmount, l.VatRate,
            l.LineTotal, l.TaxAmount, l.LineTotalWithTax, l.SortOrder, l.Notes
        )).ToList();

        var res = new InvoiceResponse(
            invoice.Id, invoice.BranchId, invoice.CustomerId, customerName, invoice.WarehouseId,
            invoice.DocumentNumber, invoice.Status.ToString(), invoice.DocumentDate, invoice.DueDate,
            invoice.SubTotal, invoice.DiscountAmount, invoice.DiscountPercent, invoice.TaxAmount,
            invoice.TotalAmount, invoice.PaidAmount, invoice.RemainingAmount, invoice.Currency,
            invoice.ExchangeRate, invoice.Notes, invoice.CreatedAt, lines
        );

        return Ok(ApiResponse<InvoiceResponse>.Ok(res));
    }

    /// <summary>Yeni fatura oluştur</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> CreateInvoice([FromBody] CreateInvoiceRequest req)
    {
        if (req.Lines.Count == 0)
            return BadRequest(ApiResponse<InvoiceResponse>.Fail("Invoice must contain at least one product line."));
        if (req.CustomerId.HasValue && !await _db.Accounts.AnyAsync(x =>
                x.Id == req.CustomerId.Value && x.CompanyId == CompanyId && x.IsActive))
            return BadRequest(ApiResponse<InvoiceResponse>.Fail("Select an active account belonging to the company."));
        if (!req.WarehouseId.HasValue || !await _db.Warehouses.AnyAsync(x =>
                x.Id == req.WarehouseId.Value && x.Branch.CompanyId == CompanyId && x.IsActive))
            return BadRequest(ApiResponse<InvoiceResponse>.Fail("Select an active warehouse belonging to the company."));
        var requestedProductIds = req.Lines.Select(x => x.ProductId).Distinct().ToList();
        var validProductCount = await _db.Products.CountAsync(x =>
            requestedProductIds.Contains(x.Id) && x.CompanyId == CompanyId && x.IsActive);
        if (validProductCount != requestedProductIds.Count)
            return BadRequest(ApiResponse<InvoiceResponse>.Fail("Invoice lines contain an invalid or inactive product."));

        // Temel şube kontrolü (Yoksa ilk şubeyi al)
        var branchId = req.BranchId;
        if (branchId == Guid.Empty)
        {
            var branch = await _db.Branches.FirstOrDefaultAsync(b => b.CompanyId == CompanyId);
            if (branch == null) return BadRequest(ApiResponse<InvoiceResponse>.Fail("No branch was found for the company."));
            branchId = branch.Id;
        }

        var invoice = Invoice.Create(CompanyId, branchId, req.DocumentNumber, req.DocumentDate, req.CustomerId, req.WarehouseId);
        invoice.UpdateBaseInfo(req.DocumentNumber, req.DocumentDate, req.CustomerId, req.WarehouseId, req.Notes);
        invoice.SetCommercialTerms(req.DueDate, req.Currency, req.ExchangeRate, ParsePaymentType(req.PaymentType));

        foreach (var l in req.Lines)
        {
            var line = new InvoiceLine
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                ProductId = l.ProductId,
                ProductVariantId = l.ProductVariantId,
                UnitId = l.UnitId,
                ProductName = l.ProductName,
                ProductCode = l.ProductCode,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                DiscountPercent = l.DiscountPercent,
                DiscountAmount = l.DiscountAmount,
                VatRate = l.VatRate,
                SortOrder = l.SortOrder,
                Notes = l.Notes
            };
            invoice.AddLine(line);
        }

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        return await GetInvoice(invoice.Id);
    }

    /// <summary>Faturayı güncelle (Sadece Draft)</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> UpdateInvoice(Guid id, [FromBody] UpdateInvoiceRequest req)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == CompanyId);

        if (invoice is null) return NotFound(ApiResponse<InvoiceResponse>.Fail("Invoice not found."));
        if (invoice.Status != Re.Domain.Enums.DocumentStatus.Draft)
            return BadRequest(ApiResponse<InvoiceResponse>.Fail("Only draft invoices can be updated."));

        invoice.UpdateBaseInfo(req.DocumentNumber, req.DocumentDate, req.CustomerId, req.WarehouseId, req.Notes);
        invoice.SetCommercialTerms(req.DueDate, req.Currency, req.ExchangeRate, ParsePaymentType(req.PaymentType));

        // Var olan satırları sil
        var existingLineIds = invoice.Lines.Select(l => l.Id).ToList();
        foreach (var lineId in existingLineIds)
        {
            invoice.RemoveLine(lineId);
        }

        // Yenileri ekle
        foreach (var l in req.Lines)
        {
            var line = new InvoiceLine
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                ProductId = l.ProductId,
                ProductVariantId = l.ProductVariantId,
                UnitId = l.UnitId,
                ProductName = l.ProductName,
                ProductCode = l.ProductCode,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                DiscountPercent = l.DiscountPercent,
                DiscountAmount = l.DiscountAmount,
                VatRate = l.VatRate,
                SortOrder = l.SortOrder,
                Notes = l.Notes
            };
            invoice.AddLine(line);
        }

        await _db.SaveChangesAsync();
        return await GetInvoice(invoice.Id);
    }

    /// <summary>Faturayı onayla ve stok/cari işlemlerini yap</summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> ApproveInvoice(Guid id)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == CompanyId);

        if (invoice is null) return NotFound(ApiResponse<InvoiceResponse>.Fail("Invoice not found."));
        
        if (invoice.Status != Re.Domain.Enums.DocumentStatus.Draft)
            return BadRequest(ApiResponse<InvoiceResponse>.Fail("Sadece taslak (Draft) durumundaki faturalar onaylanabilir."));

        if (!invoice.WarehouseId.HasValue)
            return BadRequest(ApiResponse<InvoiceResponse>.Fail("Select a warehouse before approving the invoice."));

        await using var transaction = await _db.Database.BeginTransactionAsync();

        // Cari hesap (Müşteri)
        if (invoice.CustomerId.HasValue)
        {
            var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == invoice.CustomerId.Value);
            if (account != null)
            {
                // Müşteriye borç yazılır (Debit)
                var accountMovement = new Re.Domain.Entities.Accounting.AccountMovement
                {
                    Id = Guid.NewGuid(),
                    CompanyId = CompanyId,
                    AccountId = account.Id,
                    Direction = Re.Domain.Enums.MovementDirection.Debit,
                    Amount = invoice.TotalAmount,
                    Currency = invoice.Currency,
                    ExchangeRate = invoice.ExchangeRate,
                    Description = $"{invoice.DocumentNumber} invoice amount",
                    MovementDate = DateTime.UtcNow,
                    ReferenceDocumentType = "Invoice",
                    ReferenceDocumentId = invoice.Id,
                    RunningBalance = account.CurrentBalance + invoice.TotalAmount
                };
                
                account.UpdateBalance(invoice.TotalAmount); // Hesabın güncel bakiyesini artır (Borç bakiye)
                _db.AccountMovements.Add(accountMovement);
            }
        }

        // Stok Issueları
        var sourceOrder = await _db.Orders.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.InvoiceId == invoice.Id && x.CompanyId == CompanyId);

        foreach (var line in invoice.Lines)
        {
            var alreadyFulfilled = sourceOrder?.Lines
                .FirstOrDefault(x => x.ProductId == line.ProductId &&
                    x.ProductVariantId == line.ProductVariantId)?.FulfilledQuantity ?? 0;
            var quantityToIssue = Math.Max(0, line.Quantity - alreadyFulfilled);
            if (quantityToIssue == 0) continue;
            var currentStock = await _db.StockMovements.Where(x =>
                x.ProductId == line.ProductId && x.WarehouseId == invoice.WarehouseId.Value &&
                x.ProductVariantId == line.ProductVariantId).SumAsync(x => x.Quantity);
            if (currentStock < quantityToIssue)
                return BadRequest(ApiResponse<InvoiceResponse>.Fail(
                    $"Insufficient stock for {line.ProductName}. Available: {currentStock:N2}, required: {quantityToIssue:N2}."));
            // Mevcut stoku hesaplamak gerek (Basitleştirilmiş: 0 geçiyoruz)
            var movement = Re.Domain.Entities.Inventory.StockMovement.Create(
                companyId: CompanyId,
                productId: line.ProductId,
                warehouseId: invoice.WarehouseId.Value,
                movementType: Re.Domain.Enums.StockMovementType.SalesShipment,
                quantity: -quantityToIssue,
                unitCost: line.UnitPrice, // Maliyet olarak satış fiyatı baz alınıyor (İleride FIFO vs. eklenebilir)
                stockAfterMovement: currentStock - quantityToIssue,
                referenceDocumentType: "Invoice",
                referenceDocumentId: invoice.Id,
                notes: $"Sales invoice issue: {invoice.DocumentNumber}"
            );
            _db.StockMovements.Add(movement);
        }

        invoice.Approve(CurrentUserId);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetInvoice(invoice.Id);
    }

    /// <summary>Faturayı iptal et</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> CancelInvoice(Guid id)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == CompanyId);
        if (invoice is null) return NotFound(ApiResponse<object>.Fail("Invoice not found."));

        invoice.Cancel(CurrentUserId, "Cancelled by the user.");
        
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPost("{id:guid}/reverse")]
    public async Task<ActionResult<ApiResponse<object>>> ReverseInvoice(Guid id, ReverseInvoiceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(ApiResponse<object>.Fail("A reversal reason is required."));
        var invoice = await _db.Invoices.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == CompanyId);
        if (invoice is null) return NotFound(ApiResponse<object>.Fail("Invoice not found."));
        if (invoice.Status is not (Re.Domain.Enums.DocumentStatus.Approved or
            Re.Domain.Enums.DocumentStatus.PartiallyPaid or Re.Domain.Enums.DocumentStatus.FullyPaid))
            return BadRequest(ApiResponse<object>.Fail("Only posted invoices can be reversed."));

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var reversalId = Guid.NewGuid();
        if (invoice.CustomerId.HasValue)
        {
            var account = await _db.Accounts.FirstAsync(x => x.Id == invoice.CustomerId.Value);
            account.UpdateBalance(-invoice.TotalAmount * invoice.ExchangeRate);
            _db.AccountMovements.Add(new Re.Domain.Entities.Accounting.AccountMovement
            {
                Id = Guid.NewGuid(), CompanyId = CompanyId, AccountId = account.Id,
                Direction = Re.Domain.Enums.MovementDirection.Credit, Amount = invoice.TotalAmount,
                Currency = invoice.Currency, ExchangeRate = invoice.ExchangeRate,
                MovementDate = DateTime.UtcNow, DueDate = invoice.DueDate,
                Description = $"Reversal of {invoice.DocumentNumber}: {request.Reason}",
                ReferenceDocumentType = "InvoiceReversal", ReferenceDocumentId = reversalId,
                RunningBalance = account.CurrentBalance
            });
        }
        if (invoice.WarehouseId.HasValue)
        {
            foreach (var line in invoice.Lines)
            {
                var issued = await _db.StockMovements.Where(x => x.ReferenceDocumentType == "Invoice" &&
                    x.ReferenceDocumentId == invoice.Id && x.ProductId == line.ProductId &&
                    x.ProductVariantId == line.ProductVariantId).SumAsync(x => x.Quantity);
                if (issued >= 0) continue;
                var current = await _db.StockMovements.Where(x => x.ProductId == line.ProductId &&
                    x.WarehouseId == invoice.WarehouseId.Value &&
                    x.ProductVariantId == line.ProductVariantId).SumAsync(x => x.Quantity);
                _db.StockMovements.Add(Re.Domain.Entities.Inventory.StockMovement.Create(
                    CompanyId, line.ProductId, invoice.WarehouseId.Value,
                    Re.Domain.Enums.StockMovementType.SalesReturn, Math.Abs(issued), line.UnitPrice,
                    current + Math.Abs(issued), "InvoiceReversal", reversalId,
                    $"Reversal of {invoice.DocumentNumber}: {request.Reason}", line.ProductVariantId));
            }
        }
        invoice.SetCancelledBy(reversalId, request.Reason);
        await _db.SaveChangesAsync(); await transaction.CommitAsync();
        return Ok(ApiResponse<object>.Ok(new { ReversalId = reversalId }));
    }

    [HttpPost("{id:guid}/prepare-electronic-document")]
    public async Task<ActionResult<ApiResponse<ElectronicDocumentPreparationResponse>>> PrepareElectronicDocument(Guid id)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == CompanyId);
        if (invoice is null) return NotFound(ApiResponse<ElectronicDocumentPreparationResponse>.Fail("Invoice not found."));
        if (!invoice.CustomerId.HasValue)
            return BadRequest(ApiResponse<ElectronicDocumentPreparationResponse>.Fail("An account is required for electronic invoicing."));
        var account = await _db.Accounts.FirstAsync(x => x.Id == invoice.CustomerId.Value);
        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(account.TaxNumber) && string.IsNullOrWhiteSpace(account.TcKimlik))
            warnings.Add("Customer tax or national identity number is missing.");
        if (string.IsNullOrWhiteSpace(account.AddressLine1) || string.IsNullOrWhiteSpace(account.City))
            warnings.Add("Customer billing address is incomplete.");
        if (warnings.Count > 0)
            return BadRequest(ApiResponse<ElectronicDocumentPreparationResponse>.Fail(warnings));
        var uuid = invoice.EInvoiceUuid ?? Guid.NewGuid().ToString();
        var documentType = account.IsEInvoicePayer ? "E-Invoice" : "E-Archive";
        invoice.MarkEInvoicePrepared(uuid, "Prepared");
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<ElectronicDocumentPreparationResponse>.Ok(new(invoice.Id,
            invoice.DocumentNumber, documentType, uuid, "Prepared", account.IsEInvoicePayer,
            account.EInvoiceAlias, warnings)));
    }

    private static Re.Domain.Enums.PaymentType? ParsePaymentType(string? value) =>
        Enum.TryParse<Re.Domain.Enums.PaymentType>(value, true, out var parsed) ? parsed : null;
}
