namespace Re.Domain.Entities.Common;

/// <summary>
/// Bu arayüzü uygulayan her varlık (entity) belirli bir şirkete (Company) aittir.
/// Veritabanında Multi-Tenant izolasyonu (Global Query Filter) bu arayüz üzerinden yapılır.
/// </summary>
public interface IMustHaveCompany
{
    Guid CompanyId { get; set; }
}
