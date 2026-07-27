using Re.Domain.Entities.Common;
using Re.Domain.Enums;
using Re.Domain.Exceptions;

namespace Re.Domain.Entities.Inventory;

/// <summary>
/// Stok hareketi – her türlü stok değişimi bu entity ile kaydedilir.
/// Geçmiş hareketler fiziksel olarak silinemez; iptal için ters hareket oluşturulur.
/// </summary>
public class StockMovement : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid? ProductVariantId { get; private set; }

    public StockMovementType MovementType { get; private set; }
    public decimal Quantity { get; private set; }       // Pozitif = giriş, Negatif = çıkış
    public decimal UnitCost { get; private set; }
    public decimal TotalCost => Math.Abs(Quantity) * UnitCost;

    public string? LotNumber { get; private set; }
    public string? SerialNumber { get; private set; }
    public DateTime? ExpiryDate { get; private set; }

    public string? ReferenceDocumentType { get; private set; }  // "Invoice", "PurchaseInvoice" vb.
    public Guid? ReferenceDocumentId { get; private set; }

    public string? Notes { get; private set; }
    public DateTime MovementDate { get; private set; } = DateTime.UtcNow;

    // Stok bakiyesi anlık görüntü (hareket anındaki depo stoğu)
    public decimal StockAfterMovement { get; private set; }

    // Navigation
    public Product Product { get; private set; } = null!;

    private StockMovement() { }

    public static StockMovement Create(
        Guid companyId,
        Guid productId,
        Guid warehouseId,
        StockMovementType movementType,
        decimal quantity,
        decimal unitCost,
        decimal stockAfterMovement,
        string? referenceDocumentType = null,
        Guid? referenceDocumentId = null,
        string? notes = null)
    {
        if (quantity == 0)
            throw new DomainException("Stok hareketi miktarı sıfır olamaz.");

        return new StockMovement
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProductId = productId,
            WarehouseId = warehouseId,
            MovementType = movementType,
            Quantity = quantity,
            UnitCost = unitCost,
            StockAfterMovement = stockAfterMovement,
            ReferenceDocumentType = referenceDocumentType,
            ReferenceDocumentId = referenceDocumentId,
            Notes = notes,
            MovementDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Stok hareketi fiziksel olarak silinemez; hata durumunda ters hareket oluşturulur.
    /// </summary>
    public StockMovement CreateReversal(decimal currentStock, string reason)
    {
        return new StockMovement
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            ProductId = ProductId,
            WarehouseId = WarehouseId,
            MovementType = MovementType,
            Quantity = -Quantity,   // Ters miktar
            UnitCost = UnitCost,
            StockAfterMovement = currentStock - Quantity,
            ReferenceDocumentType = "StockMovementReversal",
            ReferenceDocumentId = Id,
            Notes = $"İptal: {reason}",
            MovementDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Ürün kategorisi – hiyerarşik yapı desteklenir.
/// </summary>
public class Category : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    public Category? ParentCategory { get; set; }
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
}

/// <summary>
/// Marka.
/// </summary>
public class Brand : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ProductCollection : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Season { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Ölçü birimi.
/// </summary>
public class Unit : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Abbreviation { get; set; }
    public bool IsActive { get; set; } = true;
}



