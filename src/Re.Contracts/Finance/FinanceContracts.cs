using System;

namespace Re.Contracts.Finance;

public record CollectionRequest(
    Guid AccountId,
    Guid? CashRegisterId,
    Guid? BankAccountId,
    decimal Amount,
    string Currency,
    decimal ExchangeRate,
    string? Description,
    DateTime Date
);

public record PaymentRequest(
    Guid AccountId,
    Guid? CashRegisterId,
    Guid? BankAccountId,
    decimal Amount,
    string Currency,
    decimal ExchangeRate,
    string? Description,
    DateTime Date
);

public record FinanceTransactionResponse(
    Guid Id,
    string TransactionType,
    decimal Amount,
    DateTime Date
);

public sealed record CashRegisterResponse(Guid Id, string Code, string Name, string Currency, decimal CurrentBalance, bool IsActive);
public sealed record BankAccountResponse(Guid Id, string BankName, string AccountName, string? Iban, string Currency, decimal CurrentBalance, bool IsActive);
public sealed record TreasuryMovementResponse(
    Guid Id, DateTime Date, string AccountName, string Direction, decimal Amount,
    string Currency, decimal AmountTRY, decimal RunningBalance, string? Description, string? ReferenceType);
public sealed record TreasuryDashboardResponse(
    IReadOnlyCollection<CashRegisterResponse> CashRegisters,
    IReadOnlyCollection<BankAccountResponse> BankAccounts,
    IReadOnlyCollection<TreasuryMovementResponse> CashMovements,
    IReadOnlyCollection<TreasuryMovementResponse> BankMovements,
    decimal TotalCashTRY, decimal TotalBankTRY, decimal TodayCashIn, decimal TodayCashOut,
    decimal TodayBankIn, decimal TodayBankOut);
