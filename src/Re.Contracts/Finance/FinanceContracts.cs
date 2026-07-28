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

public sealed record SaveChequeNoteRequest(Guid AccountId, string Number, string Type,
    decimal Amount, string Currency, decimal ExchangeRate, DateTime IssueDate, DateTime DueDate,
    string? BankName, string? BranchName, string? Drawer, string? Description);
public sealed record ChangeChequeNoteStatusRequest(string Status, Guid? CashRegisterId, Guid? BankAccountId);
public sealed record ChequeNoteResponse(Guid Id, Guid AccountId, string AccountName, string Number,
    string Type, string Status, decimal Amount, string Currency, decimal ExchangeRate,
    DateTime IssueDate, DateTime DueDate, string? BankName, string? BranchName, string? Drawer,
    string? Description, Guid? SettlementAccountId, DateTime? SettledAt);
