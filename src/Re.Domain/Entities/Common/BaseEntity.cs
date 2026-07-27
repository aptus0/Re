namespace Re.Domain.Entities.Common;

/// <summary>
/// Tüm entity'lerin türediği temel sınıf.
/// Audit bilgileri, soft delete ve concurrency token içerir.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    /// <summary>
    /// EF Core concurrency token – SQL Server rowversion ile eşlenir.
    /// Aynı satırı iki kişi aynı anda değiştirmeye çalışırsa hata fırlatır.
    /// </summary>
    public byte[]? RowVersion { get; set; }
}

