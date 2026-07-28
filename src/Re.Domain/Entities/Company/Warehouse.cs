using Re.Domain.Entities.Common;

namespace Re.Domain.Entities.Company;

/// <summary>
/// Depo – Bir şubenin stok tuttuğu fiziksel veya sanal yer.
/// </summary>
public class Warehouse : BaseEntity
{
    public Guid BranchId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation
    public Branch Branch { get; private set; } = null!;

    private Warehouse() { }

    public static Warehouse Create(Guid branchId, string code, string name, bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Warehouse code is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Warehouse name is required.");

        return new Warehouse
        {
            Id = Guid.NewGuid(),
            BranchId = branchId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow
        };
    }
}

