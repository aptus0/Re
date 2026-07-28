using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Re.Application.Interfaces;
using Re.Contracts.Finance;
using Re.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Re.Domain.Entities.Accounting;
using Re.Domain.Entities.Common;
using Re.Domain.Enums;
using Re.Persistence.Context;
using System;
using System.Threading.Tasks;

namespace Re.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FinanceController : ControllerBase
{
    private readonly ReDbContext _context;
    private readonly ICurrentTenantService _tenantService;

    public FinanceController(ReDbContext context, ICurrentTenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    [HttpGet("cashregisters")]
    public async Task<IActionResult> GetCashRegisters()
    {
        var companyId = _tenantService.CompanyId;
        if (!companyId.HasValue || companyId == Guid.Empty) return Unauthorized();

        var branchIds = _context.Branches.Where(x => x.CompanyId == companyId.Value).Select(x => x.Id);
        var list = await _context.CashRegisters.Where(x => branchIds.Contains(x.BranchId))
            .Select(c => new CashRegisterResponse(c.Id, c.Code, c.Name, c.Currency, c.CurrentBalance, c.IsActive))
            .ToListAsync();
        return Ok(ApiResponse<IReadOnlyCollection<CashRegisterResponse>>.Ok(list));
    }

    [HttpGet("bankaccounts")]
    public async Task<IActionResult> GetBankAccounts()
    {
        var companyId = _tenantService.CompanyId;
        if (!companyId.HasValue || companyId == Guid.Empty) return Unauthorized();

        var branchIds = _context.Branches.Where(x => x.CompanyId == companyId.Value).Select(x => x.Id);
        var list = await _context.BankAccounts.Where(x => branchIds.Contains(x.BranchId))
            .Select(b => new BankAccountResponse(b.Id, b.BankName, b.AccountName, b.Iban, b.Currency, b.CurrentBalance, b.IsActive))
            .ToListAsync();
        return Ok(ApiResponse<IReadOnlyCollection<BankAccountResponse>>.Ok(list));
    }

    [HttpGet("treasury-dashboard")]
    public async Task<IActionResult> TreasuryDashboard()
    {
        var companyId = _tenantService.CompanyId;
        if (!companyId.HasValue || companyId == Guid.Empty) return Unauthorized();
        var branchIds = await _context.Branches.Where(x => x.CompanyId == companyId.Value).Select(x => x.Id).ToListAsync();
        var cash = await _context.CashRegisters.Where(x => branchIds.Contains(x.BranchId))
            .Select(x => new CashRegisterResponse(x.Id, x.Code, x.Name, x.Currency, x.CurrentBalance, x.IsActive)).ToListAsync();
        var banks = await _context.BankAccounts.Where(x => branchIds.Contains(x.BranchId))
            .Select(x => new BankAccountResponse(x.Id, x.BankName, x.AccountName, x.Iban, x.Currency, x.CurrentBalance, x.IsActive)).ToListAsync();
        var cashMovements = await _context.CashRegisterMovements.Include(x => x.CashRegister)
            .Where(x => x.CompanyId == companyId.Value).OrderByDescending(x => x.MovementDate).Take(100)
            .Select(x => new TreasuryMovementResponse(x.Id, x.MovementDate, x.CashRegister.Name,
                x.Direction == MovementDirection.Debit ? "Receipt" : "Issue", x.Amount, x.Currency,
                x.Amount * x.ExchangeRate, x.RunningBalance, x.Description, x.ReferenceDocumentType)).ToListAsync();
        var bankMovements = await _context.BankAccountMovements.Include(x => x.BankAccount)
            .Where(x => x.CompanyId == companyId.Value).OrderByDescending(x => x.MovementDate).Take(100)
            .Select(x => new TreasuryMovementResponse(x.Id, x.MovementDate, x.BankAccount.BankName + " / " + x.BankAccount.AccountName,
                x.Direction == MovementDirection.Debit ? "Receipt" : "Issue", x.Amount, x.Currency,
                x.Amount * x.ExchangeRate, x.RunningBalance, x.Description, x.ReferenceDocumentType)).ToListAsync();
        var today = DateTime.Today;
        var response = new TreasuryDashboardResponse(cash, banks, cashMovements, bankMovements,
            cash.Where(x => x.Currency == "TRY").Sum(x => x.CurrentBalance),
            banks.Where(x => x.Currency == "TRY").Sum(x => x.CurrentBalance),
            cashMovements.Where(x => x.Date >= today && x.Direction == "Receipt").Sum(x => x.AmountTRY),
            cashMovements.Where(x => x.Date >= today && x.Direction == "Issue").Sum(x => x.AmountTRY),
            bankMovements.Where(x => x.Date >= today && x.Direction == "Receipt").Sum(x => x.AmountTRY),
            bankMovements.Where(x => x.Date >= today && x.Direction == "Issue").Sum(x => x.AmountTRY));
        return Ok(ApiResponse<TreasuryDashboardResponse>.Ok(response));
    }

    [HttpPost("collections")]
    public async Task<IActionResult> AddCollection([FromBody] CollectionRequest request)
    {
        var companyId = _tenantService.CompanyId;
        if (!companyId.HasValue || companyId == Guid.Empty) return Unauthorized();
        var validation = await ValidateTransactionAsync(companyId.Value, request.CashRegisterId,
            request.BankAccountId, request.Amount, request.Currency, request.ExchangeRate, false);
        if (validation is not null) return BadRequest(ApiResponse<object>.Fail(validation));

        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId && a.CompanyId == companyId);
        if (account == null) return NotFound("Account not found.");
        if (!string.Equals(account.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<object>.Fail("Transaction currency must match the account currency."));
        await using var transaction = await _context.Database.BeginTransactionAsync();

        // Cari hesap için Alacak kaydı (Borcunu düşürür)
        var accountMovement = new AccountMovement
        {
            CompanyId = companyId.Value,
            AccountId = request.AccountId,
            Direction = MovementDirection.Credit, // Tahsilat: Alacak
            Amount = request.Amount,
            Currency = request.Currency,
            ExchangeRate = request.ExchangeRate,
            Description = request.Description ?? "Collection Transaction",
            MovementDate = request.Date,
            RunningBalance = account.CurrentBalance - request.Amount,
            ReferenceDocumentType = "Collection"
        };

        account.UpdateBalance(-request.Amount); // Müşteriden para alırsak borcu (bakiye) azalır
        _context.AccountMovements.Add(accountMovement);

        if (request.CashRegisterId.HasValue)
        {
            var cash = await _context.CashRegisters.FirstOrDefaultAsync(c => c.Id == request.CashRegisterId.Value);
            if (cash == null) return NotFound("Cash register not found.");
            
            var cashMovement = new CashRegisterMovement
            {
                CompanyId = companyId.Value,
                CashRegisterId = request.CashRegisterId.Value,
                Direction = MovementDirection.Debit, // Kasaya para girişi
                Amount = request.Amount,
                Currency = request.Currency,
                ExchangeRate = request.ExchangeRate,
                Description = request.Description ?? "Tahsilat (Cari)",
                MovementDate = request.Date,
                RunningBalance = cash.CurrentBalance + request.Amount,
                ReferenceDocumentType = "Collection"
            };
            
            cash.UpdateBalance(request.Amount);
            _context.CashRegisterMovements.Add(cashMovement);
        }
        else if (request.BankAccountId.HasValue)
        {
            var bank = await _context.BankAccounts.FirstOrDefaultAsync(b => b.Id == request.BankAccountId.Value);
            if (bank == null) return NotFound("Bank account not found.");
            
            var bankMovement = new BankAccountMovement
            {
                CompanyId = companyId.Value,
                BankAccountId = request.BankAccountId.Value,
                Direction = MovementDirection.Debit, // Bankaya para girişi
                Amount = request.Amount,
                Currency = request.Currency,
                ExchangeRate = request.ExchangeRate,
                Description = request.Description ?? "Tahsilat (Cari)",
                MovementDate = request.Date,
                RunningBalance = bank.CurrentBalance + request.Amount,
                ReferenceDocumentType = "Collection"
            };
            
            bank.UpdateBalance(request.Amount);
            _context.BankAccountMovements.Add(bankMovement);
        }
        else
        {
            return BadRequest("Kasa veya Banka belirtilmelidir.");
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(ApiResponse<FinanceTransactionResponse>.Ok(
            new FinanceTransactionResponse(accountMovement.Id, "Collection", request.Amount, request.Date)));
    }

    [HttpPost("payments")]
    public async Task<IActionResult> AddPayment([FromBody] PaymentRequest request)
    {
        var companyId = _tenantService.CompanyId;
        if (!companyId.HasValue || companyId == Guid.Empty) return Unauthorized();
        var validation = await ValidateTransactionAsync(companyId.Value, request.CashRegisterId,
            request.BankAccountId, request.Amount, request.Currency, request.ExchangeRate, true);
        if (validation is not null) return BadRequest(ApiResponse<object>.Fail(validation));

        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId && a.CompanyId == companyId);
        if (account == null) return NotFound("Account not found.");
        if (!string.Equals(account.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<object>.Fail("Transaction currency must match the account currency."));
        await using var transaction = await _context.Database.BeginTransactionAsync();

        // Cari hesap için Borç kaydı (Tedarikçiye ödeme yaptık, bizim alacağımız arttı veya borcumuz azaldı, 
        // ancak pozitif bakiye = borçlu diyoruz, bu yüzden ödeme yaptığımızda borcu artar (Debit) şeklinde kurgulanabilir.)
        // Veya "Tedarikçi bize alacaklı", biz ödeme yaparsak borç kaydedilir.
        var accountMovement = new AccountMovement
        {
            CompanyId = companyId.Value,
            AccountId = request.AccountId,
            Direction = MovementDirection.Debit, // Ödeme: Borç
            Amount = request.Amount,
            Currency = request.Currency,
            ExchangeRate = request.ExchangeRate,
            Description = request.Description ?? "Payment Transaction",
            MovementDate = request.Date,
            RunningBalance = account.CurrentBalance + request.Amount,
            ReferenceDocumentType = "Payment"
        };

        account.UpdateBalance(request.Amount); 
        _context.AccountMovements.Add(accountMovement);

        if (request.CashRegisterId.HasValue)
        {
            var cash = await _context.CashRegisters.FirstOrDefaultAsync(c => c.Id == request.CashRegisterId.Value);
            if (cash == null) return NotFound("Cash register not found.");
            
            var cashMovement = new CashRegisterMovement
            {
                CompanyId = companyId.Value,
                CashRegisterId = request.CashRegisterId.Value,
                Direction = MovementDirection.Credit, // Kasadan para çıkışı
                Amount = request.Amount,
                Currency = request.Currency,
                ExchangeRate = request.ExchangeRate,
                Description = request.Description ?? "Account Payment",
                MovementDate = request.Date,
                RunningBalance = cash.CurrentBalance - request.Amount,
                ReferenceDocumentType = "Payment"
            };
            
            cash.UpdateBalance(-request.Amount);
            _context.CashRegisterMovements.Add(cashMovement);
        }
        else if (request.BankAccountId.HasValue)
        {
            var bank = await _context.BankAccounts.FirstOrDefaultAsync(b => b.Id == request.BankAccountId.Value);
            if (bank == null) return NotFound("Bank account not found.");
            
            var bankMovement = new BankAccountMovement
            {
                CompanyId = companyId.Value,
                BankAccountId = request.BankAccountId.Value,
                Direction = MovementDirection.Credit, // Bankadan para çıkışı
                Amount = request.Amount,
                Currency = request.Currency,
                ExchangeRate = request.ExchangeRate,
                Description = request.Description ?? "Account Payment",
                MovementDate = request.Date,
                RunningBalance = bank.CurrentBalance - request.Amount,
                ReferenceDocumentType = "Payment"
            };
            
            bank.UpdateBalance(-request.Amount);
            _context.BankAccountMovements.Add(bankMovement);
        }
        else
        {
            return BadRequest("Kasa veya Banka belirtilmelidir.");
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(ApiResponse<FinanceTransactionResponse>.Ok(
            new FinanceTransactionResponse(accountMovement.Id, "Payment", request.Amount, request.Date)));
    }

    [HttpGet("cheque-notes")]
    public async Task<IActionResult> GetChequeNotes([FromQuery] string? status = null)
    {
        var companyId = _tenantService.CompanyId;
        if (!companyId.HasValue) return Unauthorized();
        var query = _context.ChequeNotes.Where(x => x.CompanyId == companyId.Value);
        if (Enum.TryParse<ChequeNoteStatus>(status, true, out var parsed)) query = query.Where(x => x.Status == parsed);
        var rows = await query.OrderBy(x => x.DueDate).ToListAsync();
        var accountIds = rows.Select(x => x.AccountId).Distinct().ToList();
        var names = await _context.Accounts.Where(x => accountIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name);
        return Ok(ApiResponse<IReadOnlyCollection<ChequeNoteResponse>>.Ok(rows.Select(x => MapCheque(x,
            names.GetValueOrDefault(x.AccountId, "-"))).ToList()));
    }

    [HttpPost("cheque-notes")]
    public async Task<IActionResult> CreateChequeNote(SaveChequeNoteRequest request)
    {
        var companyId = _tenantService.CompanyId;
        if (!companyId.HasValue) return Unauthorized();
        if (!Enum.TryParse<ChequeNoteType>(request.Type, true, out var type))
            return BadRequest(ApiResponse<object>.Fail("Invalid cheque/note type."));
        var account = await _context.Accounts.FirstOrDefaultAsync(x =>
            x.Id == request.AccountId && x.CompanyId == companyId.Value && x.IsActive);
        if (account is null) return BadRequest(ApiResponse<object>.Fail("Select an active account."));
        if (!string.Equals(account.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<object>.Fail("Document and account currencies must match."));
        if (await _context.ChequeNotes.AnyAsync(x => x.CompanyId == companyId.Value && x.Number == request.Number))
            return Conflict(ApiResponse<object>.Fail("Document number already exists."));
        var item = ChequeNote.Create(companyId.Value, account.Id, request.Number, type,
            request.Amount, request.Currency, request.ExchangeRate, request.IssueDate,
            request.DueDate, request.BankName, request.BranchName, request.Drawer, request.Description);
        _context.ChequeNotes.Add(item); await _context.SaveChangesAsync();
        return Ok(ApiResponse<ChequeNoteResponse>.Ok(MapCheque(item, account.Name)));
    }

    [HttpPost("cheque-notes/{id:guid}/status")]
    public async Task<IActionResult> ChangeChequeNoteStatus(Guid id, ChangeChequeNoteStatusRequest request)
    {
        var companyId = _tenantService.CompanyId;
        if (!companyId.HasValue) return Unauthorized();
        if (!Enum.TryParse<ChequeNoteStatus>(request.Status, true, out var status))
            return BadRequest(ApiResponse<object>.Fail("Invalid document status."));
        var item = await _context.ChequeNotes.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId.Value);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Cheque/note not found."));
        var isSettlement = status is ChequeNoteStatus.Collected or ChequeNoteStatus.Paid;
        if (status == ChequeNoteStatus.Collected &&
            item.Type is not (ChequeNoteType.CustomerCheque or ChequeNoteType.CustomerNote))
            return BadRequest(ApiResponse<object>.Fail("Only customer documents can be collected."));
        if (status == ChequeNoteStatus.Paid &&
            item.Type is not (ChequeNoteType.SupplierCheque or ChequeNoteType.SupplierNote))
            return BadRequest(ApiResponse<object>.Fail("Only supplier documents can be paid."));
        if (isSettlement)
        {
            var validation = await ValidateTransactionAsync(companyId.Value, request.CashRegisterId,
                request.BankAccountId, item.Amount, item.Currency, item.ExchangeRate,
                status == ChequeNoteStatus.Paid);
            if (validation is not null) return BadRequest(ApiResponse<object>.Fail(validation));
        }
        else if (request.CashRegisterId.HasValue || request.BankAccountId.HasValue)
            return BadRequest(ApiResponse<object>.Fail("A treasury account is only valid when collecting or paying."));

        Guid? settlementId = request.CashRegisterId ?? request.BankAccountId;
        await using var transaction = await _context.Database.BeginTransactionAsync();
        if (isSettlement)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(x =>
                x.Id == item.AccountId && x.CompanyId == companyId.Value);
            if (account is null) return BadRequest(ApiResponse<object>.Fail("Related account was not found."));
            if (!string.Equals(account.Currency, item.Currency, StringComparison.OrdinalIgnoreCase))
                return BadRequest(ApiResponse<object>.Fail("Document and account currencies must match."));

            var isCollection = status == ChequeNoteStatus.Collected;
            var accountDelta = isCollection ? -item.Amount : item.Amount;
            _context.AccountMovements.Add(new AccountMovement
            {
                CompanyId = companyId.Value, AccountId = account.Id,
                Direction = isCollection ? MovementDirection.Credit : MovementDirection.Debit,
                Amount = item.Amount, Currency = item.Currency, ExchangeRate = item.ExchangeRate,
                Description = $"{item.Number} cheque/note settlement",
                MovementDate = DateTime.Now, RunningBalance = account.CurrentBalance + accountDelta,
                ReferenceDocumentType = "ChequeNote", ReferenceDocumentId = item.Id
            });
            account.UpdateBalance(accountDelta);

            if (request.CashRegisterId.HasValue)
            {
                var cash = await _context.CashRegisters.FindAsync(request.CashRegisterId.Value);
                if (cash is null) return BadRequest(ApiResponse<object>.Fail("Cash register was not found."));
                var treasuryDelta = isCollection ? item.Amount : -item.Amount;
                _context.CashRegisterMovements.Add(new CashRegisterMovement
                {
                    CompanyId = companyId.Value, CashRegisterId = cash.Id,
                    Direction = isCollection ? MovementDirection.Debit : MovementDirection.Credit,
                    Amount = item.Amount, Currency = item.Currency, ExchangeRate = item.ExchangeRate,
                    Description = $"{item.Number} cheque/note settlement",
                    MovementDate = DateTime.Now, RunningBalance = cash.CurrentBalance + treasuryDelta,
                    ReferenceDocumentType = "ChequeNote", ReferenceDocumentId = item.Id
                });
                cash.UpdateBalance(treasuryDelta);
            }
            else
            {
                var bank = await _context.BankAccounts.FindAsync(request.BankAccountId!.Value);
                if (bank is null) return BadRequest(ApiResponse<object>.Fail("Bank account was not found."));
                var treasuryDelta = isCollection ? item.Amount : -item.Amount;
                _context.BankAccountMovements.Add(new BankAccountMovement
                {
                    CompanyId = companyId.Value, BankAccountId = bank.Id,
                    Direction = isCollection ? MovementDirection.Debit : MovementDirection.Credit,
                    Amount = item.Amount, Currency = item.Currency, ExchangeRate = item.ExchangeRate,
                    Description = $"{item.Number} cheque/note settlement",
                    MovementDate = DateTime.Now, RunningBalance = bank.CurrentBalance + treasuryDelta,
                    ReferenceDocumentType = "ChequeNote", ReferenceDocumentId = item.Id
                });
                bank.UpdateBalance(treasuryDelta);
            }
        }
        item.ChangeStatus(status, settlementId);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(ApiResponse<object>.Ok(new { item.Id, Status = item.Status.ToString() }));
    }

    [HttpDelete("cheque-notes/{id:guid}")]
    public async Task<IActionResult> DeleteChequeNote(Guid id)
    {
        var companyId = _tenantService.CompanyId;
        if (!companyId.HasValue) return Unauthorized();
        var item = await _context.ChequeNotes.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId.Value);
        if (item is null) return NotFound();
        if (item.Status != ChequeNoteStatus.Portfolio)
            return BadRequest(ApiResponse<object>.Fail("Only portfolio documents can be deleted."));
        item.IsDeleted = true; item.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { item.Id }));
    }

    private async Task<string?> ValidateTransactionAsync(Guid companyId, Guid? cashId, Guid? bankId,
        decimal amount, string currency, decimal exchangeRate, bool requireFunds)
    {
        if (amount <= 0 || exchangeRate <= 0) return "Amount and exchange rate must be positive.";
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3) return "Use a valid currency code.";
        if (cashId.HasValue == bankId.HasValue) return "Select exactly one cash register or bank account.";
        var branchIds = _context.Branches.Where(x => x.CompanyId == companyId).Select(x => x.Id);
        if (cashId.HasValue)
        {
            var cash = await _context.CashRegisters.FirstOrDefaultAsync(x => x.Id == cashId && branchIds.Contains(x.BranchId) && x.IsActive);
            if (cash is null) return "Cash register is invalid or inactive.";
            if (!string.Equals(cash.Currency, currency, StringComparison.OrdinalIgnoreCase)) return "Cash register currency does not match.";
            if (requireFunds && cash.CurrentBalance < amount) return "Cash register balance is insufficient.";
        }
        else
        {
            var bank = await _context.BankAccounts.FirstOrDefaultAsync(x => x.Id == bankId && branchIds.Contains(x.BranchId) && x.IsActive);
            if (bank is null) return "Bank account is invalid or inactive.";
            if (!string.Equals(bank.Currency, currency, StringComparison.OrdinalIgnoreCase)) return "Bank account currency does not match.";
            if (requireFunds && bank.CurrentBalance < amount) return "Bank balance is insufficient.";
        }
        return null;
    }

    private static ChequeNoteResponse MapCheque(ChequeNote x, string accountName) =>
        new(x.Id, x.AccountId, accountName, x.Number, x.Type.ToString(), x.Status.ToString(),
            x.Amount, x.Currency, x.ExchangeRate, x.IssueDate, x.DueDate, x.BankName,
            x.BranchName, x.Drawer, x.Description, x.SettlementAccountId, x.SettledAt);
}
