using Re.Contracts.Common;
using Re.Contracts.Products;
using Re.Domain.Entities.Inventory;
using Re.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Re.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly ReDbContext _db;

    public ProductsController(ReDbContext db) => _db = db;

    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")?.Value ?? Guid.Empty.ToString());

    /// <summary>Ürün listesi (sayfalı, arama destekli)</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<ProductListResponse>>>> GetProducts(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int size = 25)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.CompanyId == CompanyId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.Name.Contains(search) || p.Code.Contains(search));

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId);

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        var total = await query.CountAsync();
        var pageItems = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(p => new
            {
                p.Id, p.Code, p.Name, p.PurchasePrice, p.SalePrice, p.DealerPrice,
                p.VatRate, p.MinStockLevel, p.MaxStockLevel, p.Barcode1,
                p.CategoryId, CategoryName = p.Category != null ? p.Category.Name : null,
                p.BrandId, BrandName = p.Brand != null ? p.Brand.Name : null,
                p.Warehouse, p.ImagePath, p.CreatedAt, p.UpdatedAt, p.IsActive
            })
            .ToListAsync();
        var productIds = pageItems.Select(x => x.Id).ToList();
        var stocks = await _db.StockMovements
            .Where(x => productIds.Contains(x.ProductId))
            .GroupBy(x => x.ProductId)
            .Select(g => new { ProductId = g.Key, Stock = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Stock);
        var items = pageItems.Select(p => new ProductListResponse(
            p.Id, p.Code, p.Name, p.PurchasePrice, p.SalePrice, p.DealerPrice,
            p.VatRate, p.MinStockLevel, p.MaxStockLevel, p.Barcode1,
            p.CategoryName, p.CategoryId, p.BrandName, p.BrandId, p.Warehouse,
            stocks.GetValueOrDefault(p.Id), p.ImagePath, p.UpdatedAt ?? p.CreatedAt,
            p.IsActive)).ToList();

        return Ok(ApiResponse<PagedResponse<ProductListResponse>>.Ok(new PagedResponse<ProductListResponse>
        {
            Items = items, TotalCount = total, Page = page, PageSize = size
        }));
    }

    /// <summary>Ürün detayı</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> GetProduct(Guid id)
    {
        var p = await _db.Products
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.Unit)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == CompanyId);

        if (p is null) return NotFound(ApiResponse<ProductResponse>.Fail("Ürün bulunamadı."));

        return Ok(ApiResponse<ProductResponse>.Ok(new ProductResponse(
            p.Id, p.Code, p.Name, p.ShortName, p.SalePrice, p.PurchasePrice, p.DealerPrice, p.VatRate,
            p.MinStockLevel, p.MaxStockLevel, p.Barcode1, p.Barcode2, p.Warehouse, p.SupplierName,
            p.LeadTimeDays, p.Color, p.Size, p.IsPublishedEcommerce, p.SeoTitle, p.PurchaseAccountCode,
            p.SalesAccountCode, p.Category?.Name, p.Brand?.Name, p.Unit?.Name, p.IsActive, p.CreatedAt)));
    }

    /// <summary>Yeni ürün oluştur</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> CreateProduct([FromBody] CreateProductRequest req)
    {
        if (!req.CategoryId.HasValue || !await _db.Categories.AnyAsync(x =>
                x.Id == req.CategoryId && x.CompanyId == CompanyId && x.IsActive))
            return BadRequest(ApiResponse<ProductResponse>.Fail("Aktif bir kategori seçilmelidir."));
        if (!req.BrandId.HasValue || !await _db.Brands.AnyAsync(x =>
                x.Id == req.BrandId && x.CompanyId == CompanyId && x.IsActive))
            return BadRequest(ApiResponse<ProductResponse>.Fail("Aktif bir marka seçilmelidir."));

        if (await _db.Products.AnyAsync(p => p.Code == req.Code && p.CompanyId == CompanyId))
            return BadRequest(ApiResponse<ProductResponse>.Fail($"'{req.Code}' kodlu ürün zaten mevcut."));

        var barcode = string.IsNullOrWhiteSpace(req.Barcode1)
            ? await GenerateUniqueEan13Async()
            : req.Barcode1.Trim();
        if (await _db.Products.AnyAsync(p => p.CompanyId == CompanyId &&
                (p.Barcode1 == barcode || p.Barcode2 == barcode)))
            return BadRequest(ApiResponse<ProductResponse>.Fail($"'{barcode}' barkodu zaten kullanılıyor."));

        var product = Product.Create(CompanyId, req.Code, req.Name, req.SalePrice, req.VatRate,
            req.CategoryId, req.BrandId);
        
        product.UpdatePrices(req.PurchasePrice, req.SalePrice, req.DealerPrice, req.VatRate);
        
        product.UpdateDetails(
            req.MinStockLevel, req.MaxStockLevel,
            barcode, req.Barcode2,
            req.Warehouse, req.SupplierName, req.LeadTimeDays,
            req.Color, req.Size,
            req.IsPublishedEcommerce, req.SeoTitle,
            req.PurchaseAccountCode, req.SalesAccountCode);
        product.SetImagePath(req.ImagePath);
        foreach (var variant in req.Variants ?? [])
        {
            var code = variant.Code.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(ApiResponse<ProductResponse>.Fail("Varyant kodu boş bırakılamaz."));
            if (product.Variants.Any(x => x.Code == code))
                return BadRequest(ApiResponse<ProductResponse>.Fail($"'{code}' varyant kodu birden fazla kez girildi."));
            product.Variants.Add(new ProductVariant
            {
                Code = code, Color = variant.Color, Size = variant.Size,
                Attribute1 = variant.Attribute1, Attribute2 = variant.Attribute2,
                SalePrice = variant.SalePrice, IsActive = variant.IsActive
            });
        }

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id },
            ApiResponse<ProductResponse>.Ok(new ProductResponse(
                product.Id, product.Code, product.Name, product.ShortName, product.SalePrice, product.PurchasePrice, product.DealerPrice, product.VatRate,
                product.MinStockLevel, product.MaxStockLevel, product.Barcode1, product.Barcode2, product.Warehouse, product.SupplierName,
                product.LeadTimeDays, product.Color, product.Size, product.IsPublishedEcommerce, product.SeoTitle, product.PurchaseAccountCode,
                product.SalesAccountCode, null, null, null, product.IsActive, product.CreatedAt, product.ImagePath)));
    }

    /// <summary>Ürüne barkod ekle</summary>
    [HttpPost("{id:guid}/barcodes")]
    public async Task<ActionResult> AddBarcode(Guid id, [FromBody] AddBarcodeRequest req)
    {
        var product = await _db.Products
            .Include(p => p.Barcodes)
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == CompanyId);

        if (product is null) return NotFound(ApiResponse<object>.Fail("Ürün bulunamadı."));

        product.AddBarcode(req.Value, req.BarcodeType);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, message = "Barkod eklendi." });
    }

    /// <summary>Barkod ile ürün ara (POS için)</summary>
    [HttpGet("byBarcode/{barcode}")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> GetByBarcode(string barcode)
    {
        var product = await _db.Products
            .Include(p => p.Barcodes)
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .FirstOrDefaultAsync(p =>
                p.CompanyId == CompanyId &&
                (p.Barcode1 == barcode || p.Barcode2 == barcode ||
                 p.Barcodes.Any(b => b.Value == barcode)));

        if (product is null)
            return NotFound(ApiResponse<ProductResponse>.Fail($"'{barcode}' barkodlu ürün bulunamadı."));

        return Ok(ApiResponse<ProductResponse>.Ok(new ProductResponse(
            product.Id, product.Code, product.Name, product.ShortName, product.SalePrice, product.PurchasePrice, product.DealerPrice, product.VatRate,
            product.MinStockLevel, product.MaxStockLevel, product.Barcode1, product.Barcode2, product.Warehouse, product.SupplierName,
            product.LeadTimeDays, product.Color, product.Size, product.IsPublishedEcommerce, product.SeoTitle, product.PurchaseAccountCode,
            product.SalesAccountCode, product.Category?.Name, null, product.Unit?.Name,
            product.IsActive, product.CreatedAt)));
    }

    /// <summary>Ürünü güncelle</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> UpdateProduct(Guid id, [FromBody] UpdateProductRequest req)
    {
        if (!req.CategoryId.HasValue || !await _db.Categories.AnyAsync(x =>
                x.Id == req.CategoryId && x.CompanyId == CompanyId && x.IsActive))
            return BadRequest(ApiResponse<ProductResponse>.Fail("Aktif bir kategori seçilmelidir."));
        if (!req.BrandId.HasValue || !await _db.Brands.AnyAsync(x =>
                x.Id == req.BrandId && x.CompanyId == CompanyId && x.IsActive))
            return BadRequest(ApiResponse<ProductResponse>.Fail("Aktif bir marka seçilmelidir."));

        var product = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == CompanyId);

        if (product is null) return NotFound(ApiResponse<ProductResponse>.Fail("Ürün bulunamadı."));

        product.UpdateBaseInfo(req.Name, req.ShortName, req.Description, req.CategoryId, req.BrandId, req.UnitId, req.IsActive);
        product.UpdatePrices(req.PurchasePrice, req.SalePrice, req.DealerPrice, req.VatRate);
        product.UpdateDetails(
            req.MinStockLevel, req.MaxStockLevel,
            req.Barcode1, req.Barcode2,
            req.Warehouse, req.SupplierName, req.LeadTimeDays,
            req.Color, req.Size,
            req.IsPublishedEcommerce, req.SeoTitle,
            req.PurchaseAccountCode, req.SalesAccountCode);
        product.SetImagePath(req.ImagePath);

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<ProductResponse>.Ok(new ProductResponse(
            product.Id, product.Code, product.Name, product.ShortName, product.SalePrice, product.PurchasePrice, product.DealerPrice, product.VatRate,
            product.MinStockLevel, product.MaxStockLevel, product.Barcode1, product.Barcode2, product.Warehouse, product.SupplierName,
            product.LeadTimeDays, product.Color, product.Size, product.IsPublishedEcommerce, product.SeoTitle, product.PurchaseAccountCode,
            product.SalesAccountCode, product.Category?.Name, product.Brand?.Name, product.Unit?.Name,
            product.IsActive, product.CreatedAt, product.ImagePath)));
    }

    /// <summary>Ürünü sil (Soft delete)</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteProduct(Guid id)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == CompanyId);
        if (product is null) return NotFound(ApiResponse<object>.Fail("Ürün bulunamadı."));

        product.Deactivate(); // Soft delete işlemi, IsActive = false yapar
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null));
    }

    private async Task<string> GenerateUniqueEan13Async()
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var body = "869" + Random.Shared.NextInt64(0, 1_000_000_000).ToString("D9");
            var sum = 0;
            for (var i = 0; i < body.Length; i++)
                sum += (body[i] - '0') * (i % 2 == 0 ? 1 : 3);
            var value = body + ((10 - sum % 10) % 10);
            if (!await _db.Products.AnyAsync(x => x.CompanyId == CompanyId &&
                    (x.Barcode1 == value || x.Barcode2 == value)))
                return value;
        }
        throw new InvalidOperationException("Benzersiz barkod üretilemedi.");
    }
}

