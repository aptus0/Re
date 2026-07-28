using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Re.Contracts.Common;
using Re.Contracts.Inventory;
using Re.Domain.Enums;
using Re.Persistence.Context;
using Re.Domain.Entities.Inventory;

namespace Re.Api.Controllers;

[ApiController, Route("api/stock-movements"), Authorize]
public sealed class StockMovementsController(ReDbContext db) : ControllerBase
{
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")?.Value ?? Guid.Empty.ToString());

    [HttpGet("warehouses")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<WarehouseLookupItem>>>> Warehouses()
    {
        var items = await db.Warehouses
            .Where(x => x.Branch.CompanyId == CompanyId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new WarehouseLookupItem(x.Id, x.Code, x.Name, x.IsActive))
            .ToListAsync();
        return Ok(ApiResponse<IReadOnlyCollection<WarehouseLookupItem>>.Ok(items));
    }

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
        if (direction == "Receipt") query = query.Where(x => x.Quantity > 0);
        if (direction == "Issue") query = query.Where(x => x.Quantity < 0);
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
            x.Quantity >= 0 ? "Receipt" : "Issue", x.ReferenceDocumentType, x.ReferenceDocumentId,
            x.Code, x.Name, x.Barcode1, warehouses.GetValueOrDefault(x.WarehouseId, "Unknown Warehouse"),
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

    [HttpPost("adjustments")]
    public async Task<ActionResult<ApiResponse<StockAdjustmentResponse>>> CreateAdjustment(
        CreateStockAdjustmentRequest request)
    {
        if (request.Quantity == 0)
            return BadRequest(ApiResponse<StockAdjustmentResponse>.Fail("Adjustment quantity cannot be zero."));
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(ApiResponse<StockAdjustmentResponse>.Fail("Adjustment reason is required."));

        var productExists = await db.Products.AnyAsync(x =>
            x.Id == request.ProductId && x.CompanyId == CompanyId && x.IsActive);
        var warehouseExists = await db.Warehouses.AnyAsync(x =>
            x.Id == request.WarehouseId && x.Branch.CompanyId == CompanyId && x.IsActive);
        if (!productExists || !warehouseExists)
            return BadRequest(ApiResponse<StockAdjustmentResponse>.Fail("Active product and warehouse are required."));

        var previous = await db.StockMovements
            .Where(x => x.CompanyId == CompanyId && x.ProductId == request.ProductId &&
                        x.WarehouseId == request.WarehouseId)
            .SumAsync(x => x.Quantity);
        var current = previous + request.Quantity;
        if (current < 0)
            return BadRequest(ApiResponse<StockAdjustmentResponse>.Fail(
                $"Adjustment would create negative stock ({current:0.###})."));

        var movement = StockMovement.Create(
            CompanyId, request.ProductId, request.WarehouseId,
            StockMovementType.Counting, request.Quantity, request.UnitCost, current,
            "StockAdjustment", null,
            $"{request.ReferenceNumber?.Trim()} · {request.Reason.Trim()}".Trim(' ', '·'));
        db.StockMovements.Add(movement);
        await db.SaveChangesAsync();

        return Ok(ApiResponse<StockAdjustmentResponse>.Ok(new(
            movement.Id, previous, request.Quantity, current, movement.MovementDate)));
    }

    [HttpGet("balances")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<WarehouseStockBalanceItem>>>> Balances()
    {
        var raw = await db.StockMovements
            .Where(x => x.CompanyId == CompanyId)
            .GroupBy(x => new { x.ProductId, x.WarehouseId })
            .Select(g => new { g.Key.ProductId, g.Key.WarehouseId, OnHand = g.Sum(x => x.Quantity) })
            .ToListAsync();
        var productIds = raw.Select(x => x.ProductId).Distinct().ToList();
        var warehouseIds = raw.Select(x => x.WarehouseId).Distinct().ToList();
        var products = await db.Products.Where(x => productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
        var warehouses = await db.Warehouses.Where(x => warehouseIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
        var items = raw.Select(x =>
        {
            var p = products[x.ProductId]; var w = warehouses[x.WarehouseId];
            var status = x.OnHand <= 0 ? "Out of Stock" :
                x.OnHand <= p.MinStockLevel ? "Low Stock" : "Available";
            return new WarehouseStockBalanceItem(p.Id, p.Code, p.Name, p.Barcode1,
                w.Id, w.Code, w.Name, x.OnHand, p.MinStockLevel,
                x.OnHand * p.PurchasePrice, status);
        }).OrderBy(x => x.ProductName).ThenBy(x => x.WarehouseName).ToList();
        return Ok(ApiResponse<IReadOnlyCollection<WarehouseStockBalanceItem>>.Ok(items));
    }

    [HttpPost("operations")]
    public async Task<ActionResult<ApiResponse<InventoryOperationResponse>>> CreateOperation(
        InventoryOperationRequest request)
    {
        var operation = request.OperationType.Trim();
        if (request.Quantity <= 0 || string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(ApiResponse<InventoryOperationResponse>.Fail("Positive quantity and reason are required."));
        if (operation is not ("Receipt" or "Issue" or "Transfer" or "Count" or "Waste"))
            return BadRequest(ApiResponse<InventoryOperationResponse>.Fail("Unsupported inventory operation."));

        var product = await db.Products.FirstOrDefaultAsync(x =>
            x.Id == request.ProductId && x.CompanyId == CompanyId && x.IsActive);
        if (product is null) return BadRequest(ApiResponse<InventoryOperationResponse>.Fail("Active product is required."));
        var sourceExists = await db.Warehouses.AnyAsync(x =>
            x.Id == request.SourceWarehouseId && x.Branch.CompanyId == CompanyId && x.IsActive);
        if (!sourceExists) return BadRequest(ApiResponse<InventoryOperationResponse>.Fail("Source warehouse is invalid."));
        if (request.ProductVariantId.HasValue && !await db.ProductVariants.AnyAsync(x =>
                x.Id == request.ProductVariantId && x.ProductId == request.ProductId && x.IsActive))
            return BadRequest(ApiResponse<InventoryOperationResponse>.Fail("Variant does not belong to this product."));
        if (!string.IsNullOrWhiteSpace(request.SerialNumber) && request.Quantity != 1)
            return BadRequest(ApiResponse<InventoryOperationResponse>.Fail("Serial-number operations must have quantity 1."));

        var sourceBalance = await Balance(request.ProductId, request.SourceWarehouseId);
        var operationNumber = string.IsNullOrWhiteSpace(request.ReferenceNumber)
            ? $"STK-{DateTime.UtcNow:yyyyMMddHHmmss}" : request.ReferenceNumber.Trim();
        var ids = new List<Guid>();
        decimal? destinationBalance = null;

        await using var transaction = await db.Database.BeginTransactionAsync();
        if (operation == "Transfer")
        {
            if (!request.DestinationWarehouseId.HasValue ||
                request.DestinationWarehouseId == request.SourceWarehouseId)
                return BadRequest(ApiResponse<InventoryOperationResponse>.Fail("A different destination warehouse is required."));
            if (!await db.Warehouses.AnyAsync(x => x.Id == request.DestinationWarehouseId &&
                    x.Branch.CompanyId == CompanyId && x.IsActive))
                return BadRequest(ApiResponse<InventoryOperationResponse>.Fail("Destination warehouse is invalid."));
            if (sourceBalance < request.Quantity)
                return Conflict(ApiResponse<InventoryOperationResponse>.Fail("Insufficient source warehouse stock."));
            destinationBalance = await Balance(request.ProductId, request.DestinationWarehouseId.Value);
            var issue = Create(-request.Quantity, request.SourceWarehouseId, sourceBalance - request.Quantity);
            var receipt = Create(request.Quantity, request.DestinationWarehouseId.Value, destinationBalance.Value + request.Quantity);
            db.AddRange(issue, receipt); ids.Add(issue.Id); ids.Add(receipt.Id);
            sourceBalance -= request.Quantity; destinationBalance += request.Quantity;
        }
        else
        {
            var signedQuantity = operation is "Issue" or "Waste" ? -request.Quantity :
                operation == "Count" ? request.Quantity - sourceBalance : request.Quantity;
            if (signedQuantity == 0)
                return BadRequest(ApiResponse<InventoryOperationResponse>.Fail("Count already matches the current balance."));
            if (sourceBalance + signedQuantity < 0)
                return Conflict(ApiResponse<InventoryOperationResponse>.Fail("Operation would create negative stock."));
            var movement = Create(signedQuantity, request.SourceWarehouseId, sourceBalance + signedQuantity);
            db.Add(movement); ids.Add(movement.Id); sourceBalance += signedQuantity;
        }
        await db.SaveChangesAsync(); await transaction.CommitAsync();
        return Ok(ApiResponse<InventoryOperationResponse>.Ok(
            new(operationNumber, ids, sourceBalance, destinationBalance)));

        async Task<decimal> Balance(Guid productId, Guid warehouseId) =>
            await db.StockMovements.Where(x => x.CompanyId == CompanyId &&
                x.ProductId == productId && x.WarehouseId == warehouseId).SumAsync(x => x.Quantity);

        StockMovement Create(decimal quantity, Guid warehouseId, decimal after) =>
            StockMovement.Create(CompanyId, request.ProductId, warehouseId,
                operation switch
                {
                    "Transfer" => StockMovementType.WarehouseTransfer,
                    "Count" => StockMovementType.Counting,
                    "Waste" => StockMovementType.Waste,
                    "Issue" => StockMovementType.SalesShipment,
                    _ => StockMovementType.PurchaseReceipt
                }, quantity, request.UnitCost, after, operationNumber, null, request.Reason,
                request.ProductVariantId, request.LotNumber, request.SerialNumber, request.ExpiryDate);
    }

    private static string TypeName(StockMovementType type) => type switch
    {
        StockMovementType.PurchaseReceipt => "Purchase Receipt",
        StockMovementType.SalesShipment => "Sales Shipment",
        StockMovementType.PurchaseReturn => "Purchase Return",
        StockMovementType.SalesReturn => "Sales Return",
        StockMovementType.WarehouseTransfer => "Depo Transferi",
        StockMovementType.Counting => "Stock Count",
        StockMovementType.Waste => "Fire / Zayiat",
        StockMovementType.Production => "Production Receipt",
        StockMovementType.ProductionConsumption => "Production Consumption",
        StockMovementType.Opening => "Opening Balance",
        _ => type.ToString()
    };
}
