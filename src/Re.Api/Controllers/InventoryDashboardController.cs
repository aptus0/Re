using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Re.Contracts.Common;
using Re.Contracts.Inventory;
using Re.Domain.Enums;
using Re.Persistence.Context;

namespace Re.Api.Controllers;

[ApiController, Route("api/inventory-dashboard"), Authorize]
public sealed class InventoryDashboardController(ReDbContext db) : ControllerBase
{
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")?.Value ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<ActionResult<ApiResponse<InventoryDashboardResponse>>> Get()
    {
        var products = await db.Products
            .Where(x => x.CompanyId == CompanyId)
            .Select(x => new
            {
                x.Id, x.Code, x.Name, x.ImagePath, x.IsActive, x.MinStockLevel, x.PurchasePrice,
                CategoryName = x.Category != null ? x.Category.Name : null,
                BrandName = x.Brand != null ? x.Brand.Name : null
            }).ToListAsync();
        var productIds = products.Select(x => x.Id).ToList();
        var stockRows = await db.StockMovements.Where(x => productIds.Contains(x.ProductId))
            .GroupBy(x => x.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Stock = g.Sum(x => x.Quantity),
                LastMovement = g.Max(x => x.MovementDate)
            }).ToDictionaryAsync(x => x.ProductId);
        var critical = products.Select(x =>
        {
            var stock = stockRows.GetValueOrDefault(x.Id)?.Stock ?? 0;
            return new { Product = x, Stock = stock };
        }).Where(x => x.Product.IsActive && x.Stock <= x.Product.MinStockLevel)
          .OrderBy(x => x.Stock).ThenBy(x => x.Product.Name).ToList();
        var now = DateTime.UtcNow;
        var today = DateTime.Today;
        var todayValues = await db.StockMovements.Where(x => x.CompanyId == CompanyId && x.MovementDate >= today)
            .GroupBy(_ => 1).Select(g => new
            {
                Inbound = g.Where(x => x.Quantity > 0).Sum(x => x.Quantity),
                Outbound = g.Where(x => x.Quantity < 0).Sum(x => x.Quantity)
            }).FirstOrDefaultAsync();
        var recentRaw = await db.StockMovements.Include(x => x.Product)
            .Where(x => x.CompanyId == CompanyId).OrderByDescending(x => x.MovementDate).Take(12)
            .Select(x => new
            {
                x.Id, x.MovementDate, x.MovementType, x.Product.Code, x.Product.Name,
                x.Product.Barcode1, x.WarehouseId, x.Quantity, x.StockAfterMovement, x.ReferenceDocumentType
            }).ToListAsync();
        var warehouseIds = recentRaw.Select(x => x.WarehouseId).Distinct().ToList();
        var warehouses = await db.Warehouses.Where(x => warehouseIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name);
        var response = new InventoryDashboardResponse(
            products.Count, products.Count(x => x.IsActive),
            products.Sum(x => stockRows.GetValueOrDefault(x.Id)?.Stock ?? 0),
            products.Sum(x => (stockRows.GetValueOrDefault(x.Id)?.Stock ?? 0) * x.PurchasePrice),
            critical.Count(x => x.Stock > 0), critical.Count(x => x.Stock == 0),
            products.Count(x => (stockRows.GetValueOrDefault(x.Id)?.Stock ?? 0) < 0),
            products.Count(x => !stockRows.TryGetValue(x.Id, out var s) || s.LastMovement < now.AddDays(-30)),
            todayValues?.Inbound ?? 0, Math.Abs(todayValues?.Outbound ?? 0), now,
            critical.Take(15).Select(x => new CriticalStockItem(
                x.Product.Id, x.Product.Code, x.Product.Name, x.Product.ImagePath,
                x.Product.CategoryName, x.Product.BrandName, x.Stock, x.Product.MinStockLevel,
                Math.Max(0, x.Product.MinStockLevel - x.Stock),
                x.Stock < 0 ? "Negatif" : x.Stock == 0 ? "Tükendi" : "Kritik")).ToList(),
            recentRaw.Select(x => new RecentStockMovementItem(
                x.Id, x.MovementDate, TypeName(x.MovementType), x.Quantity >= 0 ? "Giriş" : "Çıkış",
                x.Code, x.Name, x.Barcode1, warehouses.GetValueOrDefault(x.WarehouseId, "Tanımsız Depo"),
                x.Quantity, x.StockAfterMovement, x.ReferenceDocumentType)).ToList());
        return Ok(ApiResponse<InventoryDashboardResponse>.Ok(response));
    }

    private static string TypeName(StockMovementType type) => type switch
    {
        StockMovementType.PurchaseReceipt => "Alış Kabul", StockMovementType.SalesShipment => "Satış Sevkiyat",
        StockMovementType.PurchaseReturn => "Alış İade", StockMovementType.SalesReturn => "Satış İade",
        StockMovementType.WarehouseTransfer => "Transfer", StockMovementType.Counting => "Sayım",
        StockMovementType.Waste => "Fire", StockMovementType.Production => "Üretim Girişi",
        StockMovementType.ProductionConsumption => "Üretim Tüketimi", StockMovementType.Opening => "Açılış",
        _ => type.ToString()
    };
}
