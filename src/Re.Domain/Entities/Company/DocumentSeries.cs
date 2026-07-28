using Re.Domain.Entities.Common;
using Re.Domain.Exceptions;

namespace Re.Domain.Entities.Company;

public sealed class DocumentSeries : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; private set; }
    public string DocumentType { get; private set; } = "";
    public string Prefix { get; private set; } = "";
    public int CurrentYear { get; private set; }
    public long CurrentNumber { get; private set; }
    public int NumberLength { get; private set; } = 7;
    public bool ResetAnnually { get; private set; } = true;
    public bool IsActive { get; private set; } = true;

    private DocumentSeries() { }

    public static DocumentSeries Create(Guid companyId, Guid? branchId, string documentType,
        string prefix, int numberLength = 7, bool resetAnnually = true)
    {
        if (string.IsNullOrWhiteSpace(documentType)) throw new DomainException("Document type is required.");
        if (string.IsNullOrWhiteSpace(prefix)) throw new DomainException("Document series prefix is required.");
        if (numberLength is < 4 or > 14) throw new DomainException("Document number length must be between 4 and 14.");
        return new DocumentSeries
        {
            CompanyId = companyId, BranchId = branchId,
            DocumentType = documentType.Trim().ToUpperInvariant(),
            Prefix = prefix.Trim().ToUpperInvariant(), NumberLength = numberLength,
            ResetAnnually = resetAnnually
        };
    }

    public string Next(DateTime documentDate)
    {
        var year = documentDate.Year;
        if (ResetAnnually && CurrentYear != year) CurrentNumber = 0;
        CurrentYear = year;
        CurrentNumber++;
        UpdatedAt = DateTime.UtcNow;
        return $"{Prefix}{year}{CurrentNumber.ToString().PadLeft(NumberLength, '0')}";
    }
}
