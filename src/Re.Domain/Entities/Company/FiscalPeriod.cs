using Re.Domain.Entities.Common;
using Re.Domain.Exceptions;

namespace Re.Domain.Entities.Company;

public enum FiscalPeriodStatus
{
    Open = 1,
    TemporarilyClosed = 2,
    Closed = 3,
    Archived = 4
}

public sealed class FiscalPeriod : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public string Name { get; private set; } = "";
    public int FiscalYear { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public FiscalPeriodStatus Status { get; private set; } = FiscalPeriodStatus.Open;
    public DateTime? ClosedAt { get; private set; }
    public Guid? ClosedBy { get; private set; }

    private FiscalPeriod() { }

    public static FiscalPeriod Create(Guid companyId, string name, int fiscalYear,
        DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Fiscal period name is required.");
        if (endDate.Date < startDate.Date) throw new DomainException("Fiscal period end date cannot precede its start date.");
        if (fiscalYear < 2000 || fiscalYear > 2200) throw new DomainException("Fiscal year is invalid.");
        return new FiscalPeriod
        {
            CompanyId = companyId, Name = name.Trim(), FiscalYear = fiscalYear,
            StartDate = startDate.Date, EndDate = endDate.Date
        };
    }

    public bool Contains(DateTime date) => date.Date >= StartDate && date.Date <= EndDate;

    public void ChangeStatus(FiscalPeriodStatus status, Guid? userId)
    {
        if (Status == FiscalPeriodStatus.Archived)
            throw new DomainException("An archived fiscal period cannot be changed.");
        if (Status == FiscalPeriodStatus.Closed && status == FiscalPeriodStatus.Open)
            throw new DomainException("A closed fiscal period cannot be reopened directly.");
        Status = status;
        ClosedAt = status is FiscalPeriodStatus.Closed or FiscalPeriodStatus.Archived ? DateTime.UtcNow : null;
        ClosedBy = ClosedAt.HasValue ? userId : null;
        UpdatedAt = DateTime.UtcNow;
    }
}
