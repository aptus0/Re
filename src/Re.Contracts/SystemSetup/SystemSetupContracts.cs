namespace Re.Contracts.SystemSetup;

public record CreateFiscalPeriodRequest(string Name, int FiscalYear, DateTime StartDate, DateTime EndDate);
public record ChangeFiscalPeriodStatusRequest(string Status);
public record FiscalPeriodResponse(Guid Id, string Name, int FiscalYear, DateTime StartDate,
    DateTime EndDate, string Status, DateTime? ClosedAt);

public record CreateDocumentSeriesRequest(Guid? BranchId, string DocumentType, string Prefix,
    int NumberLength = 7, bool ResetAnnually = true);
public record GenerateDocumentNumberRequest(Guid? BranchId, string DocumentType, DateTime DocumentDate);
public record DocumentSeriesResponse(Guid Id, Guid? BranchId, string DocumentType, string Prefix,
    int CurrentYear, long CurrentNumber, int NumberLength, bool ResetAnnually, bool IsActive);
public record GeneratedDocumentNumberResponse(string DocumentNumber, Guid SeriesId, Guid FiscalPeriodId);
