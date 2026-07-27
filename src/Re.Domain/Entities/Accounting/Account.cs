using Re.Domain.Entities.Common;
using Re.Domain.Enums;

namespace Re.Domain.Entities.Accounting;

/// <summary>
/// Cari hesap – müşteri, tedarikçi veya her ikisi olabilir.
/// Borç/alacak hareketleri AccountMovement entity'si üzerinden takip edilir.
/// </summary>
public class Account : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public AccountType AccountType { get; private set; }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? TaxNumber { get; private set; }
    public string? TaxOffice { get; private set; }
    public string? TcKimlik { get; private set; }

    public string? Phone { get; private set; }
    public string? MobilePhone { get; private set; }
    public string? Phone2 { get; private set; }
    public string? Email { get; private set; }
    public string? Website { get; private set; }

    public string? AddressLine1 { get; private set; }
    public string? City { get; private set; }
    public string? District { get; private set; }
    public string? PostalCode { get; private set; }

    public string? ContactPerson { get; private set; }
    public string? ContactPhone { get; private set; }

    // Risk ve vade
    public decimal CreditLimit { get; private set; } = 0;
    public int DefaultPaymentTermDays { get; private set; } = 30;
    public string Currency { get; private set; } = "TRY";
    public string? BankAccount { get; private set; }
    public string? PriceListId { get; private set; }

    // CRM
    public string? Representative { get; private set; }

    // E-Dönüşüm
    public bool IsEInvoicePayer { get; private set; } = false;
    public string? EInvoiceAlias { get; private set; }

    // Bakiye (gerçek değer AccountMovement'lardan hesaplanır – bu snapshot)
    public decimal CurrentBalance { get; private set; } = 0;
    public bool IsActive { get; private set; } = true;

    public string? Notes { get; private set; }

    public ICollection<AccountMovement> Movements { get; private set; } = new List<AccountMovement>();

    private Account() { }

    public static Account Create(Guid companyId, string code, string name, AccountType accountType)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Cari kodu boş olamaz.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Cari adı boş olamaz.");

        return new Account
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            AccountType = accountType,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateBalance(decimal delta)
    {
        CurrentBalance += delta;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(
        string? taxNumber, string? taxOffice, string? tcKimlik,
        string? phone, string? mobilePhone, string? phone2, string? email, string? website,
        string? addressLine1, string? city, string? district, string? postalCode,
        decimal creditLimit, int defaultPaymentTermDays, string currency,
        string? bankAccount, string? representative, bool isEInvoicePayer, string? eInvoiceAlias)
    {
        TaxNumber = taxNumber;
        TaxOffice = taxOffice;
        TcKimlik = tcKimlik;
        Phone = phone;
        MobilePhone = mobilePhone;
        Phone2 = phone2;
        Email = email;
        Website = website;
        AddressLine1 = addressLine1;
        City = city;
        District = district;
        PostalCode = postalCode;
        CreditLimit = creditLimit;
        DefaultPaymentTermDays = defaultPaymentTermDays;
        Currency = currency ?? "TRY";
        BankAccount = bankAccount;
        Representative = representative;
        IsEInvoicePayer = isEInvoicePayer;
        EInvoiceAlias = eInvoiceAlias;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateBaseInfo(string name, AccountType accountType, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Cari adı boş olamaz.");
        Name = name.Trim();
        AccountType = accountType;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Cari hesap hareketi – alacak, borç, tahsilat, ödeme.
/// Geçmiş hareketler silinemez; iptal için ters kayıt oluşturulur.
/// </summary>
public class AccountMovement : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public Guid AccountId { get; set; }
    public MovementDirection Direction { get; set; }   // Borç / Alacak
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal ExchangeRate { get; set; } = 1;
    public decimal AmountTRY => Amount * ExchangeRate;

    public string? Description { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }

    public string? ReferenceDocumentType { get; set; }
    public Guid? ReferenceDocumentId { get; set; }

    public decimal RunningBalance { get; set; }  // Hareket sonrası bakiye

    public Account Account { get; set; } = null!;
}

/// <summary>
/// Kasa – nakit işlemlerin tutulduğu hesap.
/// </summary>
public class CashRegister : BaseEntity
{
    public Guid BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
    public decimal CurrentBalance { get; private set; } = 0;
    public bool IsActive { get; private set; } = true;

    public void UpdateBalance(decimal delta)
    {
        CurrentBalance += delta;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Banka hesabı.
/// </summary>
public class BankAccount : BaseEntity
{
    public Guid BranchId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? Iban { get; set; }
    public string? AccountNumber { get; set; }
    public string? BranchCode { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal CurrentBalance { get; private set; } = 0;
    public bool IsActive { get; private set; } = true;

    public void UpdateBalance(decimal delta)
    {
        CurrentBalance += delta;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Kasa hareketleri
/// </summary>
public class CashRegisterMovement : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public Guid CashRegisterId { get; set; }
    public MovementDirection Direction { get; set; } // Borç / Alacak (Kasa için Giriş / Çıkış)
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal ExchangeRate { get; set; } = 1;
    public decimal AmountTRY => Amount * ExchangeRate;

    public string? Description { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    
    public string? ReferenceDocumentType { get; set; } // "Collection", "Payment" vb.
    public Guid? ReferenceDocumentId { get; set; }
    
    public decimal RunningBalance { get; set; }

    public CashRegister CashRegister { get; set; } = null!;
}

/// <summary>
/// Banka hareketleri
/// </summary>
public class BankAccountMovement : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public Guid BankAccountId { get; set; }
    public MovementDirection Direction { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal ExchangeRate { get; set; } = 1;
    public decimal AmountTRY => Amount * ExchangeRate;

    public string? Description { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    
    public string? ReferenceDocumentType { get; set; }
    public Guid? ReferenceDocumentId { get; set; }
    
    public decimal RunningBalance { get; set; }

    public BankAccount BankAccount { get; set; } = null!;
}



