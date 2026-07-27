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
                x.Direction == MovementDirection.Debit ? "Giriş" : "Çıkış", x.Amount, x.Currency,
                x.Amount * x.ExchangeRate, x.RunningBalance, x.Description, x.ReferenceDocumentType)).ToListAsync();
        var bankMovements = await _context.BankAccountMovements.Include(x => x.BankAccount)
            .Where(x => x.CompanyId == companyId.Value).OrderByDescending(x => x.MovementDate).Take(100)
            .Select(x => new TreasuryMovementResponse(x.Id, x.MovementDate, x.BankAccount.BankName + " / " + x.BankAccount.AccountName,
                x.Direction == MovementDirection.Debit ? "Giriş" : "Çıkış", x.Amount, x.Currency,
                x.Amount * x.ExchangeRate, x.RunningBalance, x.Description, x.ReferenceDocumentType)).ToListAsync();
        var today = DateTime.Today;
        var response = new TreasuryDashboardResponse(cash, banks, cashMovements, bankMovements,
            cash.Where(x => x.Currency == "TRY").Sum(x => x.CurrentBalance),
            banks.Where(x => x.Currency == "TRY").Sum(x => x.CurrentBalance),
            cashMovements.Where(x => x.Date >= today && x.Direction == "Giriş").Sum(x => x.AmountTRY),
            cashMovements.Where(x => x.Date >= today && x.Direction == "Çıkış").Sum(x => x.AmountTRY),
            bankMovements.Where(x => x.Date >= today && x.Direction == "Giriş").Sum(x => x.AmountTRY),
            bankMovements.Where(x => x.Date >= today && x.Direction == "Çıkış").Sum(x => x.AmountTRY));
        return Ok(ApiResponse<TreasuryDashboardResponse>.Ok(response));
    }

    [HttpPost("collections")]
    public async Task<IActionResult> AddCollection([FromBody] CollectionRequest request)
    {
        var companyId = _tenantService.CompanyId;
        if (!companyId.HasValue || companyId == Guid.Empty) return Unauthorized();

        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId && a.CompanyId == companyId);
        if (account == null) return NotFound("Cari hesap bulunamadı.");

        // Cari hesap için Alacak kaydı (Borcunu düşürür)
        var accountMovement = new AccountMovement
        {
            CompanyId = companyId.Value,
            AccountId = request.AccountId,
            Direction = MovementDirection.Credit, // Tahsilat: Alacak
            Amount = request.Amount,
            Currency = request.Currency,
            ExchangeRate = request.ExchangeRate,
            Description = request.Description ?? "Tahsilat İşlemi",
            MovementDate = request.Date,
            RunningBalance = account.CurrentBalance - request.Amount,
            ReferenceDocumentType = "Collection"
        };

        account.UpdateBalance(-request.Amount); // Müşteriden para alırsak borcu (bakiye) azalır
        _context.AccountMovements.Add(accountMovement);

        if (request.CashRegisterId.HasValue)
        {
            var cash = await _context.CashRegisters.FirstOrDefaultAsync(c => c.Id == request.CashRegisterId.Value);
            if (cash == null) return NotFound("Kasa bulunamadı.");
            
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
            if (bank == null) return NotFound("Banka hesabı bulunamadı.");
            
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

        return Ok(ApiResponse<FinanceTransactionResponse>.Ok(
            new FinanceTransactionResponse(accountMovement.Id, "Collection", request.Amount, request.Date)));
    }

    [HttpPost("payments")]
    public async Task<IActionResult> AddPayment([FromBody] PaymentRequest request)
    {
        var companyId = _tenantService.CompanyId;
        if (!companyId.HasValue || companyId == Guid.Empty) return Unauthorized();

        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId && a.CompanyId == companyId);
        if (account == null) return NotFound("Cari hesap bulunamadı.");

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
            Description = request.Description ?? "Ödeme İşlemi",
            MovementDate = request.Date,
            RunningBalance = account.CurrentBalance + request.Amount,
            ReferenceDocumentType = "Payment"
        };

        account.UpdateBalance(request.Amount); 
        _context.AccountMovements.Add(accountMovement);

        if (request.CashRegisterId.HasValue)
        {
            var cash = await _context.CashRegisters.FirstOrDefaultAsync(c => c.Id == request.CashRegisterId.Value);
            if (cash == null) return NotFound("Kasa bulunamadı.");
            
            var cashMovement = new CashRegisterMovement
            {
                CompanyId = companyId.Value,
                CashRegisterId = request.CashRegisterId.Value,
                Direction = MovementDirection.Credit, // Kasadan para çıkışı
                Amount = request.Amount,
                Currency = request.Currency,
                ExchangeRate = request.ExchangeRate,
                Description = request.Description ?? "Ödeme (Cari)",
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
            if (bank == null) return NotFound("Banka hesabı bulunamadı.");
            
            var bankMovement = new BankAccountMovement
            {
                CompanyId = companyId.Value,
                BankAccountId = request.BankAccountId.Value,
                Direction = MovementDirection.Credit, // Bankadan para çıkışı
                Amount = request.Amount,
                Currency = request.Currency,
                ExchangeRate = request.ExchangeRate,
                Description = request.Description ?? "Ödeme (Cari)",
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

        return Ok(ApiResponse<FinanceTransactionResponse>.Ok(
            new FinanceTransactionResponse(accountMovement.Id, "Payment", request.Amount, request.Date)));
    }
}
