using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Re.Contracts.Common;
using Re.Contracts.Products;
using Re.Domain.Entities.Inventory;
using Re.Persistence.Context;

namespace Re.Api.Controllers;

[ApiController, Route("api/product-catalog"), Authorize]
public sealed class ProductCatalogController(ReDbContext db) : ControllerBase
{
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")?.Value ?? Guid.Empty.ToString());

    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CatalogItemResponse>>>> Categories() =>
        Ok(ApiResponse<IReadOnlyCollection<CatalogItemResponse>>.Ok(await db.Categories.Where(x => x.CompanyId == CompanyId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name).Select(x => new CatalogItemResponse(x.Id, x.Code, x.Name, x.Description, x.IsActive)).ToListAsync()));
    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(SaveCategoryRequest r)
    {
        if (await db.Categories.AnyAsync(x => x.CompanyId == CompanyId && x.Code == r.Code)) return Conflict(ApiResponse<object>.Fail("Kategori kodu zaten kullanılıyor."));
        var x = new Category { CompanyId = CompanyId, Code = r.Code.Trim().ToUpperInvariant(), Name = r.Name.Trim(), Description = r.Description, ParentCategoryId = r.ParentCategoryId, IsActive = r.IsActive };
        db.Add(x); await db.SaveChangesAsync(); return Ok(ApiResponse<CatalogItemResponse>.Ok(new(x.Id, x.Code, x.Name, x.Description, x.IsActive)));
    }
    [HttpPut("categories/{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, SaveCategoryRequest r)
    {
        var x = await db.Categories.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == CompanyId); if (x is null) return NotFound();
        x.Code = r.Code.Trim().ToUpperInvariant(); x.Name = r.Name.Trim(); x.Description = r.Description; x.ParentCategoryId = r.ParentCategoryId; x.IsActive = r.IsActive;
        await db.SaveChangesAsync(); return Ok(ApiResponse<CatalogItemResponse>.Ok(new(x.Id, x.Code, x.Name, x.Description, x.IsActive)));
    }
    [HttpDelete("categories/{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id) { var x = await db.Categories.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == CompanyId); if (x is null) return NotFound(); x.IsActive = false; await db.SaveChangesAsync(); return Ok(); }

    [HttpGet("brands")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CatalogItemResponse>>>> Brands() =>
        Ok(ApiResponse<IReadOnlyCollection<CatalogItemResponse>>.Ok(await db.Brands.Where(x => x.CompanyId == CompanyId).OrderBy(x => x.Name)
            .Select(x => new CatalogItemResponse(x.Id, x.Code ?? "", x.Name, x.LogoPath, x.IsActive)).ToListAsync()));
    [HttpPost("brands")]
    public async Task<IActionResult> CreateBrand(SaveBrandRequest r) { var code = r.Code.Trim().ToUpperInvariant(); if (await db.Brands.AnyAsync(x => x.CompanyId == CompanyId && x.Code == code)) return Conflict(ApiResponse<object>.Fail("Marka kodu zaten kullanılıyor.")); var x = new Brand { CompanyId = CompanyId, Code = code, Name = r.Name.Trim(), LogoPath = r.LogoPath, IsActive = r.IsActive }; db.Add(x); await db.SaveChangesAsync(); return Ok(ApiResponse<CatalogItemResponse>.Ok(new(x.Id, x.Code ?? code, x.Name, x.LogoPath, x.IsActive))); }
    [HttpPut("brands/{id:guid}")]
    public async Task<IActionResult> UpdateBrand(Guid id, SaveBrandRequest r) { var x = await db.Brands.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == CompanyId); if (x is null) return NotFound(); x.Code = r.Code.Trim().ToUpperInvariant(); x.Name = r.Name.Trim(); x.LogoPath = r.LogoPath; x.IsActive = r.IsActive; await db.SaveChangesAsync(); return Ok(ApiResponse<CatalogItemResponse>.Ok(new(x.Id, x.Code ?? "", x.Name, x.LogoPath, x.IsActive))); }
    [HttpDelete("brands/{id:guid}")]
    public async Task<IActionResult> DeleteBrand(Guid id) { var x = await db.Brands.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == CompanyId); if (x is null) return NotFound(); x.IsActive = false; await db.SaveChangesAsync(); return Ok(); }

    [HttpGet("collections")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CatalogItemResponse>>>> Collections() =>
        Ok(ApiResponse<IReadOnlyCollection<CatalogItemResponse>>.Ok(await db.ProductCollections.OrderBy(x => x.Name)
            .Select(x => new CatalogItemResponse(x.Id, x.Code, x.Name, x.Season, x.IsActive)).ToListAsync()));
    [HttpPost("collections")]
    public async Task<IActionResult> CreateCollection(SaveCollectionRequest r) { var x = new ProductCollection { CompanyId = CompanyId, Code = r.Code.Trim().ToUpperInvariant(), Name = r.Name.Trim(), Season = r.Season, StartDate = r.StartDate, EndDate = r.EndDate, IsActive = r.IsActive }; db.Add(x); await db.SaveChangesAsync(); return Ok(ApiResponse<CatalogItemResponse>.Ok(new(x.Id, x.Code, x.Name, x.Season, x.IsActive))); }
    [HttpPut("collections/{id:guid}")]
    public async Task<IActionResult> UpdateCollection(Guid id, SaveCollectionRequest r) { var x = await db.ProductCollections.FindAsync(id); if (x is null) return NotFound(); x.Code = r.Code.Trim().ToUpperInvariant(); x.Name = r.Name.Trim(); x.Season = r.Season; x.StartDate = r.StartDate; x.EndDate = r.EndDate; x.IsActive = r.IsActive; await db.SaveChangesAsync(); return Ok(ApiResponse<CatalogItemResponse>.Ok(new(x.Id, x.Code, x.Name, x.Season, x.IsActive))); }
    [HttpDelete("collections/{id:guid}")]
    public async Task<IActionResult> DeleteCollection(Guid id) { var x = await db.ProductCollections.FindAsync(id); if (x is null) return NotFound(); x.IsActive = false; await db.SaveChangesAsync(); return Ok(); }
}
