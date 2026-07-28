using Re.Domain.Entities.Common;
using Re.Domain.Enums;
using Re.Domain.Exceptions;

namespace Re.Domain.Entities.Inventory;

/// <summary>
/// Ürün kartı – ERP'nin temel stok birimi.
/// Varyant, barkod, fiyat listesi ve birim dönüşümü destekler.
/// </summary>
public class Product : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public Guid? CategoryId { get; private set; }
    public Guid? BrandId { get; private set; }
    public Guid? UnitId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? ShortName { get; private set; }
    public string? Description { get; private set; }
    public string? ImagePath { get; private set; }

    // Fiyatlar
    public decimal PurchasePrice { get; private set; }
    public decimal SalePrice { get; private set; }
    public decimal DealerPrice { get; private set; }
    public decimal VatRate { get; private set; } = 20; // %20 standart KDV

    // Stok kontrol
    public decimal MinStockLevel { get; private set; } = 0;
    public decimal MaxStockLevel { get; private set; } = 0;
    public bool TrackStock { get; private set; } = true;
    public bool AllowNegativeStock { get; private set; } = false;

    // Barkodlar (Basit Kullanım)
    public string? Barcode1 { get; private set; }
    public string? Barcode2 { get; private set; }

    // Depo ve Tedarik (Basit Kullanım)
    public string? Warehouse { get; private set; }
    public string? SupplierName { get; private set; }
    public int LeadTimeDays { get; private set; }

    // Varyantlar (Basit Kullanım)
    public string? Color { get; private set; }
    public string? Size { get; private set; }

    // E-Ticaret
    public bool IsPublishedEcommerce { get; private set; }
    public string? SeoTitle { get; private set; }

    // Muhasebe
    public string? PurchaseAccountCode { get; private set; }
    public string? SalesAccountCode { get; private set; }

    // Özellikler
    public bool HasVariants { get; private set; } = false;
    public bool HasSerialNumber { get; private set; } = false;
    public bool HasLotNumber { get; private set; } = false;
    public bool HasExpiryDate { get; private set; } = false;
    public decimal Weight { get; private set; } = 0;
    public bool IsActive { get; private set; } = true;

    // Navigation
    public Category? Category { get; private set; }
    public Brand? Brand { get; private set; }
    public Unit? Unit { get; private set; }
    public ICollection<ProductBarcode> Barcodes { get; private set; } = new List<ProductBarcode>();
    public ICollection<ProductVariant> Variants { get; private set; } = new List<ProductVariant>();
    public ICollection<StockMovement> StockMovements { get; private set; } = new List<StockMovement>();

    private Product() { }

    public static Product Create(Guid companyId, string code, string name,
        decimal salePrice, decimal vatRate = 20, Guid? categoryId = null, Guid? brandId = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Product code is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Product name is required.");
        if (salePrice < 0) throw new ArgumentException("Sales price cannot be negative.");

        return new Product
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            SalePrice = salePrice,
            VatRate = vatRate,
            CategoryId = categoryId,
            BrandId = brandId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdatePrices(decimal purchasePrice, decimal salePrice, decimal dealerPrice, decimal vatRate)
    {
        if (salePrice < 0) throw new DomainException("Sales price cannot be negative.");
        PurchasePrice = purchasePrice;
        SalePrice = salePrice;
        DealerPrice = dealerPrice;
        VatRate = vatRate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(
        decimal minStockLevel, decimal maxStockLevel,
        string? barcode1, string? barcode2,
        string? warehouse, string? supplierName, int leadTimeDays,
        string? color, string? size,
        bool isPublishedEcommerce, string? seoTitle,
        string? purchaseAccountCode, string? salesAccountCode)
    {
        MinStockLevel = minStockLevel;
        MaxStockLevel = maxStockLevel;
        Barcode1 = barcode1;
        Barcode2 = barcode2;
        Warehouse = warehouse;
        SupplierName = supplierName;
        LeadTimeDays = leadTimeDays;
        Color = color;
        Size = size;
        IsPublishedEcommerce = isPublishedEcommerce;
        SeoTitle = seoTitle;
        PurchaseAccountCode = purchaseAccountCode;
        SalesAccountCode = salesAccountCode;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddBarcode(string barcodeValue, string barcodeType = "EAN13")
    {
        if (Barcodes.Any(b => b.Value == barcodeValue))
            throw new DomainException($"'{barcodeValue}' barcode is already defined.");
        Barcodes.Add(new ProductBarcode
        {
            Id = Guid.NewGuid(),
            ProductId = Id,
            Value = barcodeValue.Trim(),
            BarcodeType = barcodeType,
            CreatedAt = DateTime.UtcNow
        });
    }

    public void UpdateBaseInfo(string name, string? shortName, string? description, Guid? categoryId, Guid? brandId, Guid? unitId, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Product name is required.");
        Name = name.Trim();
        ShortName = shortName;
        Description = description;
        CategoryId = categoryId;
        BrandId = brandId;
        UnitId = unitId;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetImagePath(string? imagePath)
    {
        ImagePath = string.IsNullOrWhiteSpace(imagePath) ? null : imagePath.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Ürüne ait barkodlar. Bir ürünün birden fazla barkodu olabilir.
/// </summary>
public class ProductBarcode : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string BarcodeType { get; set; } = "EAN13"; // EAN13, QR, CODE128 vb.
    public Guid? UnitId { get; set; }                  // Farklı birimler için farklı barkod
    public decimal? UnitQuantity { get; set; }          // 1 koli = 12 adet

    public Product Product { get; set; } = null!;
}

/// <summary>
/// Ürün varyantı – renk, beden gibi kombinasyonlar.
/// </summary>
public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Size { get; set; }
    public string? Attribute1 { get; set; }
    public string? Attribute2 { get; set; }
    public decimal SalePrice { get; set; }
    public bool IsActive { get; set; } = true;

    public Product Product { get; set; } = null!;
    public ICollection<ProductBarcode> Barcodes { get; set; } = new List<ProductBarcode>();
}



