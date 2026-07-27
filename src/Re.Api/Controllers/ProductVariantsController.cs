using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Re.Contracts.Common;
using Re.Contracts.Products;
using Re.Domain.Entities.Inventory;
using Re.Persistence.Context;

namespace Re.Api.Controllers;

[ApiController, Route("api/products/{productId:guid}/variants"), Authorize]
public sealed class ProductVariantsController(ReDbContext db) : ControllerBase
{
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")?.Value ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ProductVariantResponse>>>> List(Guid productId)
    {
        if (!await db.Products.AnyAsync(x => x.Id == productId && x.CompanyId == CompanyId)) return NotFound();
        var items = await db.ProductVariants.Where(x => x.ProductId == productId).OrderBy(x => x.Code)
            .Select(x => new ProductVariantResponse(x.Id, x.ProductId, x.Code, x.Color, x.Size, x.Attribute1, x.Attribute2, x.SalePrice, x.IsActive))
            .ToListAsync();
        return Ok(ApiResponse<IReadOnlyCollection<ProductVariantResponse>>.Ok(items));
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid productId, SaveProductVariantRequest request)
    {
        if (!await db.Products.AnyAsync(x => x.Id == productId && x.CompanyId == CompanyId)) return NotFound();
        if (await db.ProductVariants.AnyAsync(x => x.ProductId == productId && x.Code == request.Code))
            return Conflict(ApiResponse<object>.Fail("Bu varyant kodu üründe zaten kullanılıyor."));
        var x = new ProductVariant { ProductId = productId, Code = request.Code.Trim().ToUpperInvariant(), Color = request.Color, Size = request.Size, Attribute1 = request.Attribute1, Attribute2 = request.Attribute2, SalePrice = request.SalePrice, IsActive = request.IsActive };
        db.Add(x); await db.SaveChangesAsync();
        return Ok(ApiResponse<ProductVariantResponse>.Ok(Map(x)));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid productId, Guid id, SaveProductVariantRequest request)
    {
        var x = await db.ProductVariants.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == id && x.ProductId == productId && x.Product.CompanyId == CompanyId);
        if (x is null) return NotFound();
        x.Code = request.Code.Trim().ToUpperInvariant(); x.Color = request.Color; x.Size = request.Size;
        x.Attribute1 = request.Attribute1; x.Attribute2 = request.Attribute2; x.SalePrice = request.SalePrice; x.IsActive = request.IsActive;
        await db.SaveChangesAsync(); return Ok(ApiResponse<ProductVariantResponse>.Ok(Map(x)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid productId, Guid id)
    {
        var x = await db.ProductVariants.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == id && x.ProductId == productId && x.Product.CompanyId == CompanyId);
        if (x is null) return NotFound(); x.IsActive = false; await db.SaveChangesAsync(); return Ok();
    }
    private static ProductVariantResponse Map(ProductVariant x) => new(x.Id, x.ProductId, x.Code, x.Color, x.Size, x.Attribute1, x.Attribute2, x.SalePrice, x.IsActive);
}
