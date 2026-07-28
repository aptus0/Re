namespace Re.Contracts.Accounts;

public record CreateAccountRequest(
    string Code,
    string Name,
    string AccountType,
    string? TaxNumber = null,
    string? TaxOffice = null,
    string? TcKimlik = null,
    string? Phone = null,
    string? MobilePhone = null,
    string? Phone2 = null,
    string? Email = null,
    string? Website = null,
    string? AddressLine1 = null,
    string? City = null,
    string? District = null,
    string? PostalCode = null,
    decimal CreditLimit = 0,
    int DefaultPaymentTermDays = 0,
    string Currency = "TRY",
    string? BankAccount = null,
    string? Representative = null,
    bool IsEInvoicePayer = false,
    string? EInvoiceAlias = null);

public record UpdateAccountRequest(
    string Name,
    string AccountType,
    string? TaxNumber,
    string? TaxOffice,
    string? TcKimlik,
    string? Phone,
    string? MobilePhone,
    string? Phone2,
    string? Email,
    string? Website,
    string? AddressLine1,
    string? City,
    string? District,
    string? PostalCode,
    decimal CreditLimit,
    int DefaultPaymentTermDays,
    string? Currency,
    string? BankAccount,
    string? Representative,
    bool IsEInvoicePayer,
    string? EInvoiceAlias,
    bool IsActive
);

public record AccountResponse(
    Guid Id,
    string Code,
    string Name,
    string AccountType,
    string? TaxNumber,
    string? TaxOffice,
    string? TcKimlik,
    string? Phone,
    string? MobilePhone,
    string? Email,
    string? Website,
    string? AddressLine1,
    string? City,
    string? District,
    decimal CreditLimit,
    int DefaultPaymentTermDays,
    string Currency,
    string? BankAccount,
    string? Representative,
    bool IsEInvoicePayer,
    string? EInvoiceAlias,
    decimal CurrentBalance,
    bool IsActive,
    DateTime CreatedAt);

public record AccountListResponse(
    Guid Id,
    string Code,
    string Name,
    string AccountType,
    string? Phone,
    string? TaxNumber,
    decimal CurrentBalance,
    bool IsActive);

public record AccountActivityResponse(
    Guid Id, DateTime Date, string Type, string Description, decimal Amount,
    decimal RunningBalance, string? ReferenceType, Guid? ReferenceId);

public record AccountInvoiceSummaryResponse(
    int InvoiceCount, decimal TotalInvoiced, decimal TotalPaid, decimal OpenBalance,
    IReadOnlyCollection<AccountActivityResponse> RecentActivities,
    decimal UnitsSold,
    int StockDocumentCount,
    IReadOnlyCollection<AccountProductSummaryResponse> TopProducts,
    IReadOnlyCollection<AccountInvoiceLinkResponse> RecentInvoices,
    AccountAgingSummaryResponse Aging,
    string CustomerSegment,
    int RiskScore,
    string RiskLevel);

public record AccountAgingSummaryResponse(
    decimal Current, decimal Days1To30, decimal Days31To60,
    decimal Days61To90, decimal Over90, decimal TotalOverdue,
    int OverdueInvoiceCount, int MaximumDaysOverdue);

public record AccountProductSummaryResponse(
    Guid ProductId, string ProductCode, string ProductName,
    decimal Quantity, decimal NetAmount);

public record AccountInvoiceLinkResponse(
    Guid InvoiceId, string DocumentNumber, DateTime DocumentDate,
    string Status, decimal TotalAmount, decimal PaidAmount, int LineCount);

public record CreateAccountOperationRequest(
    string OperationType, decimal Amount, string Currency, decimal ExchangeRate,
    DateTime MovementDate, DateTime? DueDate, string Description,
    string? ReferenceNumber);

public record AccountOperationResponse(
    Guid MovementId, string OperationNumber, decimal PreviousBalance,
    decimal Amount, decimal CurrentBalance, string Direction);
