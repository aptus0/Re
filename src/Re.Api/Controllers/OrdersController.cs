using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Re.Contracts.Common;
using Re.Contracts.Orders;
using Re.Domain.Entities.Inventory;
using Re.Domain.Entities.Orders;
using Re.Domain.Entities.Sales;
using Re.Domain.Enums;
using Re.Persistence.Context;
using System.Security.Claims;

namespace Re.Api.Controllers;

[ApiController, Route("api/[controller]"), Authorize]
public sealed class OrdersController(ReDbContext db) : ControllerBase
{
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")?.Value ?? Guid.Empty.ToString());
    private Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<OrderListResponse>>>> List(
        [FromQuery] string? type, [FromQuery] string? status, int page = 1, int size = 100)
    {
        var query = db.Orders.Include(x => x.Lines).Where(x => x.CompanyId == CompanyId);
        if (Enum.TryParse<OrderType>(type, true, out var parsedType)) query = query.Where(x => x.Type == parsedType);
        if (Enum.TryParse<OrderStatus>(status, true, out var parsedStatus)) query = query.Where(x => x.Status == parsedStatus);
        var total = await query.CountAsync();
        var rows = await query.OrderByDescending(x => x.OrderDate).Skip((page - 1) * size).Take(size).ToListAsync();
        var accountIds = rows.Select(x => x.AccountId).Distinct().ToList();
        var warehouseIds = rows.Select(x => x.WarehouseId).Distinct().ToList();
        var accounts = await db.Accounts.Where(x => accountIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name);
        var warehouses = await db.Warehouses.Where(x => warehouseIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name);
        var result = rows.Select(x => new OrderListResponse(x.Id, x.OrderNumber, x.Type.ToString(),
            x.Status.ToString(), x.OrderDate, x.RequestedDeliveryDate,
            accounts.GetValueOrDefault(x.AccountId, "-"), warehouses.GetValueOrDefault(x.WarehouseId, "-"),
            x.TotalAmount, x.Currency, x.Lines.Count,
            x.Lines.Sum(l => l.Quantity) == 0 ? 0 :
                Math.Round(x.Lines.Sum(l => l.FulfilledQuantity) / x.Lines.Sum(l => l.Quantity) * 100, 1))).ToList();
        return Ok(ApiResponse<PagedResponse<OrderListResponse>>.Ok(new()
            { Items = result, TotalCount = total, Page = page, PageSize = size }));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> Get(Guid id)
    {
        var order = await db.Orders.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == CompanyId);
        return order is null
            ? NotFound(ApiResponse<OrderResponse>.Fail("Order not found."))
            : Ok(ApiResponse<OrderResponse>.Ok(Map(order)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> Create(CreateOrderRequest request)
    {
        if (!Enum.TryParse<OrderType>(request.Type, true, out var type))
            return BadRequest(ApiResponse<OrderResponse>.Fail("Order type must be Sales or Purchase."));
        if (request.Lines.Count == 0) return BadRequest(ApiResponse<OrderResponse>.Fail("At least one line is required."));
        var account = await db.Accounts.FirstOrDefaultAsync(x => x.Id == request.AccountId && x.CompanyId == CompanyId && x.IsActive);
        var warehouse = await db.Warehouses.Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == request.WarehouseId && x.Branch.CompanyId == CompanyId && x.IsActive);
        if (account is null || warehouse is null)
            return BadRequest(ApiResponse<OrderResponse>.Fail("Select a valid account and warehouse."));
        if (await db.Orders.AnyAsync(x => x.CompanyId == CompanyId && x.OrderNumber == request.OrderNumber))
            return Conflict(ApiResponse<OrderResponse>.Fail("Order number already exists."));
        var productIds = request.Lines.Select(x => x.ProductId).Distinct().ToList();
        var products = await db.Products.Where(x => productIds.Contains(x.Id) && x.CompanyId == CompanyId && x.IsActive)
            .ToDictionaryAsync(x => x.Id);
        if (products.Count != productIds.Count) return BadRequest(ApiResponse<OrderResponse>.Fail("An order line contains an invalid product."));

        var order = Order.Create(CompanyId, warehouse.BranchId, account.Id, warehouse.Id,
            request.OrderNumber, type, request.OrderDate, request.RequestedDeliveryDate,
            request.Currency, request.ExchangeRate, request.CustomerReference, request.Notes);
        foreach (var item in request.Lines)
        {
            var product = products[item.ProductId];
            order.AddLine(new OrderLine
            {
                Id = Guid.NewGuid(), OrderId = order.Id, ProductId = item.ProductId,
                ProductVariantId = item.ProductVariantId, ProductCode = product.Code,
                ProductName = product.Name, Quantity = item.Quantity, UnitPrice = item.UnitPrice,
                DiscountPercent = item.DiscountPercent, VatRate = item.VatRate, Notes = item.Notes
            });
        }
        db.Orders.Add(order); await db.SaveChangesAsync();
        return Ok(ApiResponse<OrderResponse>.Ok(Map(order)));
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> Confirm(Guid id)
    {
        var order = await Find(id); if (order is null) return NotFound(ApiResponse<OrderResponse>.Fail("Order not found."));
        order.Confirm(UserId); await db.SaveChangesAsync();
        return Ok(ApiResponse<OrderResponse>.Ok(Map(order)));
    }

    [HttpPost("{id:guid}/fulfil")]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> Fulfil(Guid id, IReadOnlyList<FulfilOrderLineRequest> requests)
    {
        var order = await Find(id); if (order is null) return NotFound(ApiResponse<OrderResponse>.Fail("Order not found."));
        if (requests.Count == 0) return BadRequest(ApiResponse<OrderResponse>.Fail("Enter at least one fulfilment quantity."));
        await using var transaction = await db.Database.BeginTransactionAsync();
        foreach (var request in requests)
        {
            var line = order.Lines.SingleOrDefault(x => x.Id == request.LineId);
            if (line is null) return BadRequest(ApiResponse<OrderResponse>.Fail("Order line not found."));
            var current = await db.StockMovements.Where(x => x.ProductId == line.ProductId &&
                x.WarehouseId == order.WarehouseId && x.ProductVariantId == line.ProductVariantId).SumAsync(x => x.Quantity);
            var signedQuantity = order.Type == OrderType.Sales ? -request.Quantity : request.Quantity;
            if (order.Type == OrderType.Sales && current < request.Quantity)
                return BadRequest(ApiResponse<OrderResponse>.Fail($"Insufficient stock for {line.ProductName}. Available: {current:N2}."));
            order.Fulfil(line.Id, request.Quantity);
            db.StockMovements.Add(StockMovement.Create(CompanyId, line.ProductId, order.WarehouseId,
                order.Type == OrderType.Sales ? StockMovementType.SalesShipment : StockMovementType.PurchaseReceipt,
                signedQuantity, line.UnitPrice, current + signedQuantity, "Order", order.Id,
                $"{order.Type} order fulfilment: {order.OrderNumber}", line.ProductVariantId));
        }
        await db.SaveChangesAsync(); await transaction.CommitAsync();
        return Ok(ApiResponse<OrderResponse>.Ok(Map(order)));
    }

    [HttpPost("{id:guid}/create-invoice")]
    public async Task<ActionResult<ApiResponse<object>>> CreateInvoice(Guid id)
    {
        var order = await Find(id); if (order is null) return NotFound(ApiResponse<object>.Fail("Order not found."));
        if (order.Type != OrderType.Sales) return BadRequest(ApiResponse<object>.Fail("Purchase order invoicing will be handled by the purchase invoice workflow."));
        if (order.InvoiceId.HasValue) return Conflict(ApiResponse<object>.Fail("An invoice has already been created."));
        var invoice = Invoice.Create(CompanyId, order.BranchId, $"INV-{order.OrderNumber}", DateTime.Today,
            order.AccountId, order.WarehouseId);
        invoice.UpdateBaseInfo(invoice.DocumentNumber, DateTime.Today, order.AccountId, order.WarehouseId,
            $"Created from sales order {order.OrderNumber}");
        var sort = 0;
        foreach (var line in order.Lines)
            invoice.AddLine(new InvoiceLine
            {
                Id = Guid.NewGuid(), InvoiceId = invoice.Id, ProductId = line.ProductId,
                ProductVariantId = line.ProductVariantId, ProductCode = line.ProductCode,
                ProductName = line.ProductName, Quantity = line.Quantity, UnitPrice = line.UnitPrice,
                DiscountPercent = line.DiscountPercent,
                DiscountAmount = line.Quantity * line.UnitPrice * line.DiscountPercent / 100m,
                VatRate = line.VatRate, SortOrder = sort++, Notes = line.Notes
            });
        db.Invoices.Add(invoice); order.MarkInvoiced(invoice.Id); await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { invoice.Id, invoice.DocumentNumber }));
    }

    private Task<Order?> Find(Guid id) => db.Orders.Include(x => x.Lines)
        .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == CompanyId);
    private static OrderResponse Map(Order x) => new(x.Id, x.AccountId, x.WarehouseId,
        x.OrderNumber, x.Type.ToString(), x.Status.ToString(), x.OrderDate, x.RequestedDeliveryDate,
        x.Currency, x.ExchangeRate, x.SubTotal, x.TaxAmount, x.TotalAmount, x.CustomerReference,
        x.Notes, x.InvoiceId, x.Lines.Select(l => new OrderLineResponse(l.Id, l.ProductId,
            l.ProductVariantId, l.ProductCode, l.ProductName, l.Quantity, l.FulfilledQuantity,
            l.RemainingQuantity, l.UnitPrice, l.DiscountPercent, l.VatRate, l.NetAmount,
            l.TaxAmount, l.Notes)).ToList());
}
