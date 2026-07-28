using Re.Domain.Entities.Common;

namespace Re.Domain.Entities.Company;

/// <summary>
/// Lisans sahibi firma bilgileri. Çok şubeli yapıda her şube buraya bağlıdır.
/// </summary>
public class Company : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? TradeName { get; private set; }
    public string? TaxNumber { get; private set; }
    public string? TaxOffice { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Website { get; private set; }
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public string? District { get; private set; }
    public string? PostalCode { get; private set; }
    public string Country { get; private set; } = "Turkey";
    public string? LogoPath { get; private set; }
    public string BaseCurrency { get; private set; } = "TRY";
    public int FiscalYearStartMonth { get; private set; } = 1;
    public bool IsActive { get; private set; } = true;

    // Navigation
    public ICollection<Branch> Branches { get; private set; } = new List<Branch>();

    private Company() { }

    public static Company Create(string name, string? taxNumber = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name is required.", nameof(name));

        return new Company
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            TaxNumber = taxNumber?.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string? tradeName, string? taxNumber, string? taxOffice,
        string? phone, string? email, string? website,
        string? addressLine1, string? addressLine2, string? city, string? district,
        string? postalCode, string? country)
    {
        Name = name.Trim();
        TradeName = tradeName?.Trim();
        TaxNumber = taxNumber?.Trim();
        TaxOffice = taxOffice?.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim();
        Website = website?.Trim();
        AddressLine1 = addressLine1?.Trim();
        AddressLine2 = addressLine2?.Trim();
        City = city?.Trim();
        District = district?.Trim();
        PostalCode = postalCode?.Trim();
        Country = country?.Trim() ?? "Turkey";
        UpdatedAt = DateTime.UtcNow;
    }
}

