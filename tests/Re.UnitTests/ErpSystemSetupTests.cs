using Re.Domain.Entities.Company;
using Re.Domain.Exceptions;

namespace Re.UnitTests;

public class ErpSystemSetupTests
{
    [Fact]
    public void DocumentSeries_ResetsSequenceForANewYear()
    {
        var series = DocumentSeries.Create(Guid.NewGuid(), null, "SALES_INVOICE", "SF", 6);

        Assert.Equal("SF2026000001", series.Next(new DateTime(2026, 1, 10)));
        Assert.Equal("SF2026000002", series.Next(new DateTime(2026, 2, 10)));
        Assert.Equal("SF2027000001", series.Next(new DateTime(2027, 1, 2)));
    }

    [Fact]
    public void FiscalPeriod_RejectsAnInvalidDateRange()
    {
        Assert.Throws<DomainException>(() => FiscalPeriod.Create(
            Guid.NewGuid(), "2026", 2026,
            new DateTime(2026, 12, 31), new DateTime(2026, 1, 1)));
    }

    [Fact]
    public void ClosedFiscalPeriod_CannotBeReopenedDirectly()
    {
        var period = FiscalPeriod.Create(Guid.NewGuid(), "2026", 2026,
            new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        period.ChangeStatus(FiscalPeriodStatus.Closed, Guid.NewGuid());

        Assert.Throws<DomainException>(() =>
            period.ChangeStatus(FiscalPeriodStatus.Open, Guid.NewGuid()));
    }
}
