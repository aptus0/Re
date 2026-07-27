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
            i.Status.ToString()
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

        if (invoice is null) return NotFound(ApiResponse<InvoiceResponse>.Fail("Fatura bulunamadı."));

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
        // Temel şube kontrolü (Yoksa ilk şubeyi al)
        var branchId = req.BranchId;
        if (branchId == Guid.Empty)
        {
            var branch = await _db.Branches.FirstOrDefaultAsync(b => b.CompanyId == CompanyId);
            if (branch == null) return BadRequest(ApiResponse<InvoiceResponse>.Fail("Şirkete ait şube bulunamadı."));
            branchId = branch.Id;
        }

        var invoice = Invoice.Create(CompanyId, branchId, req.DocumentNumber, req.DocumentDate, req.CustomerId, req.WarehouseId);
        invoice.UpdateBaseInfo(req.DocumentNumber, req.DocumentDate, req.CustomerId, req.WarehouseId, req.Notes);

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

        if (invoice is null) return NotFound(ApiResponse<InvoiceResponse>.Fail("Fatura bulunamadı."));
        if (invoice.Status != Re.Domain.Enums.DocumentStatus.Draft)
            return BadRequest(ApiResponse<InvoiceResponse>.Fail("Sadece taslak (Draft) durumundaki faturalar güncellenebilir."));

        invoice.UpdateBaseInfo(req.DocumentNumber, req.DocumentDate, req.CustomerId, req.WarehouseId, req.Notes);

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

        if (invoice is null) return NotFound(ApiResponse<InvoiceResponse>.Fail("Fatura bulunamadı."));
        
        if (invoice.Status != Re.Domain.Enums.DocumentStatus.Draft)
            return BadRequest(ApiResponse<InvoiceResponse>.Fail("Sadece taslak (Draft) durumundaki faturalar onaylanabilir."));

        if (!invoice.WarehouseId.HasValue)
            return BadRequest(ApiResponse<InvoiceResponse>.Fail("Faturayı onaylamak için depo (Warehouse) seçimi yapılmalıdır."));

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
                    Description = $"{invoice.DocumentNumber} numaralı fatura tutarı",
                    MovementDate = DateTime.UtcNow,
                    ReferenceDocumentType = "Invoice",
                    ReferenceDocumentId = invoice.Id,
                    RunningBalance = account.CurrentBalance + invoice.TotalAmount
                };
                
                account.UpdateBalance(invoice.TotalAmount); // Hesabın güncel bakiyesini artır (Borç bakiye)
                _db.AccountMovements.Add(accountMovement);
            }
        }

        // Stok Çıkışları
        foreach (var line in invoice.Lines)
        {
            // Mevcut stoku hesaplamak gerek (Basitleştirilmiş: 0 geçiyoruz)
            var movement = Re.Domain.Entities.Inventory.StockMovement.Create(
                companyId: CompanyId,
                productId: line.ProductId,
                warehouseId: invoice.WarehouseId.Value,
                movementType: Re.Domain.Enums.StockMovementType.SalesShipment,
                quantity: -line.Quantity, // Çıkış olduğu için negatif
                unitCost: line.UnitPrice, // Maliyet olarak satış fiyatı baz alınıyor (İleride FIFO vs. eklenebilir)
                stockAfterMovement: 0,
                referenceDocumentType: "Invoice",
                referenceDocumentId: invoice.Id,
                notes: $"{invoice.DocumentNumber} nolu satış faturası çıkışı"
            );
            _db.StockMovements.Add(movement);
        }

        invoice.Approve(CurrentUserId);
        await _db.SaveChangesAsync();

        return await GetInvoice(invoice.Id);
    }

    /// <summary>Faturayı iptal et</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> CancelInvoice(Guid id)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == CompanyId);
        if (invoice is null) return NotFound(ApiResponse<object>.Fail("Fatura bulunamadı."));

        invoice.Cancel(CurrentUserId, "Kullanıcı tarafından silindi/iptal edildi."); 
        
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null));
    }
}
