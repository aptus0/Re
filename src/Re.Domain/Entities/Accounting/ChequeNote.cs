using Re.Domain.Entities.Common;
using Re.Domain.Enums;
using Re.Domain.Exceptions;

namespace Re.Domain.Entities.Accounting;

public sealed class ChequeNote : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public Guid AccountId { get; private set; }
    public string Number { get; private set; } = "";
    public ChequeNoteType Type { get; private set; }
    public ChequeNoteStatus Status { get; private set; } = ChequeNoteStatus.Portfolio;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public decimal ExchangeRate { get; private set; } = 1;
    public DateTime IssueDate { get; private set; }
    public DateTime DueDate { get; private set; }
    public string? BankName { get; private set; }
    public string? BranchName { get; private set; }
    public string? Drawer { get; private set; }
    public string? Description { get; private set; }
    public Guid? SettlementAccountId { get; private set; }
    public DateTime? SettledAt { get; private set; }

    private ChequeNote() { }

    public static ChequeNote Create(Guid companyId, Guid accountId, string number,
        ChequeNoteType type, decimal amount, string currency, decimal exchangeRate,
        DateTime issueDate, DateTime dueDate, string? bankName, string? branchName,
        string? drawer, string? description)
    {
        if (string.IsNullOrWhiteSpace(number)) throw new DomainException("Document number is required.");
        if (amount <= 0 || exchangeRate <= 0) throw new DomainException("Amount and exchange rate must be positive.");
        if (dueDate.Date < issueDate.Date) throw new DomainException("Due date cannot precede issue date.");
        return new ChequeNote
        {
            Id = Guid.NewGuid(), CompanyId = companyId, AccountId = accountId,
            Number = number.Trim().ToUpperInvariant(), Type = type, Amount = amount,
            Currency = currency.Trim().ToUpperInvariant(), ExchangeRate = exchangeRate,
            IssueDate = issueDate, DueDate = dueDate, BankName = bankName,
            BranchName = branchName, Drawer = drawer, Description = description,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void ChangeStatus(ChequeNoteStatus status, Guid? settlementAccountId = null)
    {
        if (Status is ChequeNoteStatus.Collected or ChequeNoteStatus.Paid or ChequeNoteStatus.Cancelled)
            throw new DomainException("A closed cheque/note cannot change status.");
        Status = status;
        SettlementAccountId = settlementAccountId;
        SettledAt = status is ChequeNoteStatus.Collected or ChequeNoteStatus.Paid ? DateTime.UtcNow : null;
        UpdatedAt = DateTime.UtcNow;
    }
}
