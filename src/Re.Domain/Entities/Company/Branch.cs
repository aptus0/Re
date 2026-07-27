using Re.Domain.Entities.Common;

namespace Re.Domain.Entities.Company;

/// <summary>
/// Şube – Bir firmanın fiziksel lokasyonu.
/// </summary>
public class Branch : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? AddressLine1 { get; private set; }
    public string? City { get; private set; }
    public string? District { get; private set; }
    public bool IsHeadquarters { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation
    public Company Company { get; private set; } = null!;
    public ICollection<Warehouse> Warehouses { get; private set; } = new List<Warehouse>();

    private Branch() { }

    public static Branch Create(Guid companyId, string code, string name, bool isHeadquarters = false)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Şube kodu boş olamaz.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Şube adı boş olamaz.");

        return new Branch
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            IsHeadquarters = isHeadquarters,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string? phone, string? email,
        string? addressLine1, string? city, string? district)
    {
        Name = name.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim();
        AddressLine1 = addressLine1?.Trim();
        City = city?.Trim();
        District = district?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}



