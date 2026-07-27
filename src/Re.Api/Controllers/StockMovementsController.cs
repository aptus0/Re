using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Re.Contracts.Common;
using Re.Contracts.Inventory;
using Re.Domain.Enums;
using Re.Persistence.Context;

namespace Re.Api.Controllers;

[ApiController, Route("api/stock-movements"), Authorize]
public sealed class StockMovementsController(ReDbContext db) : ControllerBase
{
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")?.Value ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<ActionResult<ApiResponse<StockMovementListResult>>> List(
        string? search, string? direction, int? movementType,
        Guid? warehouseId, DateTime? from, DateTime? to, int page = 1, int size = 100)
    {
        page = Math.Max(1, page); size = Math.Clamp(size, 1, 500);
        var start = (from ?? DateTime.Today.AddDays(-30)).Date;
        var endExclusive = (to ?? DateTime.Today).Date.AddDays(1);
        var query = db.StockMovements
            .Include(x => x.Product)
            .Where(x => x.CompanyId == CompanyId && x.MovementDate >= start && x.MovementDate < endExclusive);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Product.Code.Contains(search) || x.Product.Name.Contains(search) ||
                (x.Product.Barcode1 != null && x.Product.Barcode1.Contains(search)) ||
                (x.ReferenceDocumentType != null && x.ReferenceDocumentType.Contains(search)));
        if (direction == "Giriş") query = query.Where(x => x.Quantity > 0);
        if (direction == "Çıkış") query = query.Where(x => x.Quantity < 0);
        if (movementType.HasValue) query = query.Where(x => (int)x.MovementType == movementType);
        if (warehouseId.HasValue) query = query.Where(x => x.WarehouseId == warehouseId);

        var total = await query.CountAsync();
        var raw = await query.OrderByDescending(x => x.MovementDate)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new
            {
                x.Id, x.MovementDate, x.MovementType, x.ReferenceDocumentType, x.ReferenceDocumentId,
                x.Product.Code, x.Product.Name, x.Product.Barcode1, x.WarehouseId, x.Quantity,
                x.UnitCost, x.StockAfterMovement, x.ProductVariantId, x.LotNumber, x.SerialNumber,
                x.ExpiryDate, x.Notes, x.CreatedBy
            }).ToListAsync();
        var warehouseIds = raw.Select(x => x.WarehouseId).Distinct().ToList();
        var warehouses = await db.Warehouses.Where(x => warehouseIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name);
        var variantIds = raw.Where(x => x.ProductVariantId.HasValue).Select(x => x.ProductVariantId!.Value).Distinct().ToList();
        var variants = await db.ProductVariants.Where(x => variantIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Code);
        var userIds = raw.Where(x => x.CreatedBy.HasValue).Select(x => x.CreatedBy!.Value).Distinct().ToList();
        var users = await db.Users.IgnoreQueryFilters().Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Username);
        var items = raw.Select(x => new StockMovementListItem(
            x.Id, x.MovementDate, x.MovementType.ToString(), TypeName(x.MovementType),
            x.Quantity >= 0 ? "Giriş" : "Çıkış", x.ReferenceDocumentType, x.ReferenceDocumentId,
            x.Code, x.Name, x.Barcode1, warehouses.GetValueOrDefault(x.WarehouseId, "Tanımsız Depo"),
            x.Quantity, x.UnitCost, Math.Abs(x.Quantity) * x.UnitCost, x.StockAfterMovement,
            x.ProductVariantId.HasValue ? variants.GetValueOrDefault(x.ProductVariantId.Value) : null,
            x.LotNumber, x.SerialNumber, x.ExpiryDate, x.Notes,
            x.CreatedBy.HasValue ? users.GetValueOrDefault(x.CreatedBy.Value, "Sistem") : "Sistem")).ToList();

        var today = DateTime.Today;
        var all = await query.Select(x => new { x.Quantity, x.MovementDate }).ToListAsync();
        var summary = new StockMovementSummary(
            all.Where(x => x.MovementDate >= today && x.Quantity > 0).Sum(x => x.Quantity),
            Math.Abs(all.Where(x => x.MovementDate >= today && x.Quantity < 0).Sum(x => x.Quantity)),
            all.Count(x => x.MovementDate >= today),
            all.Where(x => x.Quantity > 0).Sum(x => x.Quantity),
            Math.Abs(all.Where(x => x.Quantity < 0).Sum(x => x.Quantity)),
            all.Sum(x => x.Quantity), total);
        return Ok(ApiResponse<StockMovementListResult>.Ok(new(items, summary, total, page, size)));
    }

    private static string TypeName(StockMovementType type) => type switch
    {
        StockMovementType.PurchaseReceipt => "Alış Mal Kabul",
        StockMovementType.SalesShipment => "Satış Sevkiyat",
        StockMovementType.PurchaseReturn => "Alış İade",
        StockMovementType.SalesReturn => "Satış İade",
        StockMovementType.WarehouseTransfer => "Depo Transferi",
        StockMovementType.Counting => "Stok Sayımı",
        StockMovementType.Waste => "Fire / Zayiat",
        StockMovementType.Production => "Üretim Girişi",
        StockMovementType.ProductionConsumption => "Üretim Tüketimi",
        StockMovementType.Opening => "Açılış Bakiyesi",
        _ => type.ToString()
    };
}
