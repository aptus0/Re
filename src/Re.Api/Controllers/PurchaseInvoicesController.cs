using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Re.Contracts.Common;
using Re.Contracts.Purchasing;
using Re.Domain.Entities.Accounting;
using Re.Domain.Entities.Inventory;
using Re.Domain.Entities.Purchasing;
using Re.Domain.Enums;
using Re.Persistence.Context;

namespace Re.Api.Controllers;

[ApiController, Route("api/purchase-invoices"), Authorize]
public sealed class PurchaseInvoicesController(ReDbContext db) : ControllerBase
{
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")?.Value ?? Guid.Empty.ToString());
    private Guid UserId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<PurchaseInvoiceListResponse>>>> List(
        string? search, int page = 1, int size = 100)
    {
        var query = db.PurchaseInvoices.Where(x => x.CompanyId == CompanyId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.DocumentNumber.Contains(search) ||
                (x.SupplierDocumentNumber != null && x.SupplierDocumentNumber.Contains(search)));
        var total = await query.CountAsync();
        var raw = await query.OrderByDescending(x => x.DocumentDate)
            .Skip((Math.Max(page, 1) - 1) * Math.Clamp(size, 1, 500))
            .Take(Math.Clamp(size, 1, 500)).ToListAsync();
        var supplierIds = raw.Select(x => x.SupplierId).Distinct().ToList();
        var warehouseIds = raw.Select(x => x.WarehouseId).Distinct().ToList();
        var suppliers = await db.Accounts.Where(x => supplierIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name);
        var warehouses = await db.Warehouses.Where(x => warehouseIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name);
        var items = raw.Select(x => new PurchaseInvoiceListResponse(x.Id, x.DocumentNumber,
            x.SupplierDocumentNumber, x.DocumentDate, suppliers.GetValueOrDefault(x.SupplierId, "-"),
            warehouses.GetValueOrDefault(x.WarehouseId, "-"), x.TotalAmount, x.Currency, x.Status.ToString())).ToList();
        return Ok(ApiResponse<PagedResponse<PurchaseInvoiceListResponse>>.Ok(new()
        { Items = items, TotalCount = total, Page = page, PageSize = size }));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PurchaseInvoiceResponse>>> Get(Guid id)
    {
        var invoice = await db.PurchaseInvoices.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == CompanyId);
        if (invoice is null) return NotFound(ApiResponse<PurchaseInvoiceResponse>.Fail("Purchase invoice not found."));
        var supplier = await db.Accounts.Where(x => x.Id == invoice.SupplierId).Select(x => x.Name).FirstAsync();
        var warehouse = await db.Warehouses.Where(x => x.Id == invoice.WarehouseId).Select(x => x.Name).FirstAsync();
        return Ok(ApiResponse<PurchaseInvoiceResponse>.Ok(Map(invoice, supplier, warehouse)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PurchaseInvoiceResponse>>> Create(CreatePurchaseInvoiceRequest request)
    {
        if (request.Lines.Count == 0)
            return BadRequest(ApiResponse<PurchaseInvoiceResponse>.Fail("At least one purchase invoice line is required."));
        var supplier = await db.Accounts.FirstOrDefaultAsync(x =>
            x.Id == request.SupplierId && x.CompanyId == CompanyId && x.IsActive &&
            (x.AccountType == AccountType.Supplier || x.AccountType == AccountType.CustomerSupplier));
        var warehouse = await db.Warehouses.Include(x => x.Branch).FirstOrDefaultAsync(x =>
            x.Id == request.WarehouseId && x.Branch.CompanyId == CompanyId && x.IsActive);
        if (supplier is null || warehouse is null)
            return BadRequest(ApiResponse<PurchaseInvoiceResponse>.Fail("Active supplier and warehouse are required."));
        if (await db.PurchaseInvoices.AnyAsync(x => x.CompanyId == CompanyId &&
                x.DocumentNumber == request.DocumentNumber))
            return Conflict(ApiResponse<PurchaseInvoiceResponse>.Fail("Purchase invoice number already exists."));
        var productIds = request.Lines.Select(x => x.ProductId).Distinct().ToList();
        var products = await db.Products.Where(x => productIds.Contains(x.Id) && x.CompanyId == CompanyId && x.IsActive)
            .ToDictionaryAsync(x => x.Id);
        if (products.Count != productIds.Count)
            return BadRequest(ApiResponse<PurchaseInvoiceResponse>.Fail("All lines require active products."));

        var invoice = PurchaseInvoice.Create(CompanyId, warehouse.BranchId, supplier.Id,
            warehouse.Id, request.DocumentNumber, request.DocumentDate);
        invoice.UpdateHeader(request.DocumentNumber, request.SupplierDocumentNumber,
            request.DocumentDate, request.DueDate, request.Currency, request.ExchangeRate, request.Notes);
        invoice.ReplaceLines(request.Lines.Select(x =>
        {
            var p = products[x.ProductId];
            return new PurchaseInvoiceLine
            {
                Id = Guid.NewGuid(), ProductId = p.Id, ProductVariantId = x.ProductVariantId,
                ProductCode = p.Code, ProductName = p.Name, Quantity = x.Quantity,
                UnitPrice = x.UnitPrice, DiscountPercent = x.DiscountPercent, VatRate = x.VatRate,
                LotNumber = x.LotNumber, SerialNumber = x.SerialNumber, ExpiryDate = x.ExpiryDate,
                CreatedAt = DateTime.UtcNow
            };
        }));
        db.Add(invoice); await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = invoice.Id },
            ApiResponse<PurchaseInvoiceResponse>.Ok(Map(invoice, supplier.Name, warehouse.Name)));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<PurchaseInvoiceResponse>>> Approve(Guid id)
    {
        var invoice = await db.PurchaseInvoices.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == CompanyId);
        if (invoice is null) return NotFound(ApiResponse<PurchaseInvoiceResponse>.Fail("Purchase invoice not found."));
        if (invoice.Status != DocumentStatus.Draft)
            return Conflict(ApiResponse<PurchaseInvoiceResponse>.Fail("Only draft purchase invoices can be approved."));
        var supplier = await db.Accounts.FirstAsync(x => x.Id == invoice.SupplierId);
        await using var transaction = await db.Database.BeginTransactionAsync();
        invoice.Approve(UserId);
        supplier.UpdateBalance(-invoice.TotalAmount * invoice.ExchangeRate);
        db.AccountMovements.Add(new AccountMovement
        {
            Id = Guid.NewGuid(), CompanyId = CompanyId, AccountId = supplier.Id,
            Direction = MovementDirection.Credit, Amount = invoice.TotalAmount,
            Currency = invoice.Currency, ExchangeRate = invoice.ExchangeRate,
            Description = $"Purchase invoice {invoice.DocumentNumber}",
            MovementDate = invoice.DocumentDate, DueDate = invoice.DueDate,
            ReferenceDocumentType = "PurchaseInvoice", ReferenceDocumentId = invoice.Id,
            RunningBalance = supplier.CurrentBalance, CreatedAt = DateTime.UtcNow
        });
        foreach (var line in invoice.Lines)
        {
            if (line.Quantity <= 0) return BadRequest(ApiResponse<PurchaseInvoiceResponse>.Fail("Line quantity must be positive."));
            var current = await db.StockMovements.Where(x => x.CompanyId == CompanyId &&
                x.ProductId == line.ProductId && x.WarehouseId == invoice.WarehouseId).SumAsync(x => x.Quantity);
            db.StockMovements.Add(StockMovement.Create(CompanyId, line.ProductId, invoice.WarehouseId,
                StockMovementType.PurchaseReceipt, line.Quantity, line.UnitPrice, current + line.Quantity,
                "PurchaseInvoice", invoice.Id, $"Purchase invoice {invoice.DocumentNumber}",
                line.ProductVariantId, line.LotNumber, line.SerialNumber, line.ExpiryDate));
        }
        await db.SaveChangesAsync(); await transaction.CommitAsync();
        var warehouseName = await db.Warehouses.Where(x => x.Id == invoice.WarehouseId).Select(x => x.Name).FirstAsync();
        return Ok(ApiResponse<PurchaseInvoiceResponse>.Ok(Map(invoice, supplier.Name, warehouseName)));
    }

    private static PurchaseInvoiceResponse Map(PurchaseInvoice x, string supplier, string warehouse) =>
        new(x.Id, x.DocumentNumber, x.SupplierDocumentNumber, x.DocumentDate, x.DueDate,
            x.SupplierId, supplier, x.WarehouseId, warehouse, x.SubTotal, x.TaxAmount,
            x.TotalAmount, x.Currency, x.ExchangeRate, x.Status.ToString(), x.Notes,
            x.Lines.Select(l => new PurchaseInvoiceLineResponse(l.Id, l.ProductId,
                l.ProductVariantId, l.ProductCode, l.ProductName, l.Quantity, l.UnitPrice,
                l.DiscountPercent, l.VatRate, l.NetAmount, l.TaxAmount,
                l.LotNumber, l.SerialNumber, l.ExpiryDate)).ToList());
}
