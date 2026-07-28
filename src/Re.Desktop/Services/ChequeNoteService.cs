using Re.Contracts.Finance;

namespace Re.Desktop.Services;

public enum ChequeNoteType { CustomerCheque, CustomerNote, SupplierCheque, SupplierNote }
public enum ChequeNoteStatus { Portfolio, Endorsed, Collected, Paid, Bounced, Cancelled }

public sealed class ChequeNoteItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; } = "";
    public ChequeNoteType Type { get; set; }
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal ExchangeRate { get; set; } = 1;
    public DateTime DueDate { get; set; }
    public DateTime IssueDate { get; set; } = DateTime.Today;
    public ChequeNoteStatus Status { get; set; } = ChequeNoteStatus.Portfolio;
    public string BankName { get; set; } = "";
    public string BranchName { get; set; } = "";
    public string Drawer { get; set; } = "";
    public string Description { get; set; } = "";
    public Guid? SettlementAccountId { get; set; }
    public DateTime? SettledAt { get; set; }
    public string TypeDisplay => Type switch
    {
        ChequeNoteType.CustomerCheque => "Customer Cheque",
        ChequeNoteType.CustomerNote => "Customer Promissory Note",
        ChequeNoteType.SupplierCheque => "Our Cheque",
        _ => "Our Promissory Note"
    };
    public string StatusDisplay => Status switch
    {
        ChequeNoteStatus.Portfolio => "In Portfolio",
        ChequeNoteStatus.Endorsed => "Endorsed",
        ChequeNoteStatus.Collected => "Collected",
        ChequeNoteStatus.Paid => "Paid",
        ChequeNoteStatus.Bounced => "Bounced / Unpaid",
        _ => "Cancelled"
    };
    public bool IsReceivable => Type is ChequeNoteType.CustomerCheque or ChequeNoteType.CustomerNote;
    public bool IsPayable => !IsReceivable;
    public bool IsOverdue => DueDate.Date < DateTime.Today && Status == ChequeNoteStatus.Portfolio;
}

public interface IChequeNoteService
{
    Task<List<ChequeNoteItem>> GetAllAsync();
    Task SaveAsync(ChequeNoteItem item);
    Task DeleteAsync(Guid id);
    Task<ChequeNoteItem?> GetByIdAsync(Guid id);
    Task SeedDefaultDataAsync();
    Task<bool> ChangeStatusAsync(Guid id, ChequeNoteStatus status,
        Guid? cashRegisterId = null, Guid? bankAccountId = null);
}

public sealed class ChequeNoteService(ApiClient api) : IChequeNoteService
{
    public async Task<List<ChequeNoteItem>> GetAllAsync()
    {
        var rows = await api.GetAsync<IReadOnlyCollection<ChequeNoteResponse>>("api/finance/cheque-notes");
        return rows?.Select(Map).ToList() ?? [];
    }

    public async Task SaveAsync(ChequeNoteItem item)
    {
        var existing = (await GetAllAsync()).Any(x => x.Id == item.Id);
        if (existing)
        {
            await ChangeStatusAsync(item.Id, item.Status);
            return;
        }
        var created = await api.PostAsync<ChequeNoteResponse>("api/finance/cheque-notes",
            new SaveChequeNoteRequest(item.AccountId, item.Number, item.Type.ToString(),
                item.Amount, item.Currency, item.ExchangeRate, item.IssueDate, item.DueDate,
                item.BankName, item.BranchName, item.Drawer, item.Description));
        if (created is not null) item.Id = created.Id;
    }

    public async Task DeleteAsync(Guid id) => await api.DeleteAsync($"api/finance/cheque-notes/{id}");
    public async Task<ChequeNoteItem?> GetByIdAsync(Guid id) => (await GetAllAsync()).Find(x => x.Id == id);
    public Task SeedDefaultDataAsync() => Task.CompletedTask;

    public async Task<bool> ChangeStatusAsync(Guid id, ChequeNoteStatus status,
        Guid? cashRegisterId = null, Guid? bankAccountId = null)
        => await api.PostAsync<object>($"api/finance/cheque-notes/{id}/status",
            new ChangeChequeNoteStatusRequest(status.ToString(), cashRegisterId, bankAccountId)) is not null;

    private static ChequeNoteItem Map(ChequeNoteResponse x) => new()
    {
        Id = x.Id, AccountId = x.AccountId, AccountName = x.AccountName, Number = x.Number,
        Type = Enum.Parse<ChequeNoteType>(x.Type), Status = Enum.Parse<ChequeNoteStatus>(x.Status),
        Amount = x.Amount, Currency = x.Currency, ExchangeRate = x.ExchangeRate,
        IssueDate = x.IssueDate, DueDate = x.DueDate, BankName = x.BankName ?? "",
        BranchName = x.BranchName ?? "", Drawer = x.Drawer ?? "", Description = x.Description ?? "",
        SettlementAccountId = x.SettlementAccountId, SettledAt = x.SettledAt
    };
}
