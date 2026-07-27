using Re.Contracts.Accounts;
using Re.Contracts.Common;
using Re.Domain.Entities.Accounting;
using Re.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Re.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly ReDbContext _db;

    public AccountsController(ReDbContext db) => _db = db;

    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")?.Value ?? Guid.Empty.ToString());

    /// <summary>Cari listesi (sayfalı, arama destekli)</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<AccountListResponse>>>> GetAccounts(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int size = 25)
    {
        var query = _db.Accounts.Where(p => p.CompanyId == CompanyId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.Name.Contains(search) || p.Code.Contains(search) || (p.TaxNumber != null && p.TaxNumber.Contains(search)));

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(p => new AccountListResponse(
                p.Id, p.Code, p.Name, p.AccountType.ToString(),
                p.Phone, p.TaxNumber, p.CurrentBalance, p.IsActive))
            .ToListAsync();

        return Ok(ApiResponse<PagedResponse<AccountListResponse>>.Ok(new PagedResponse<AccountListResponse>
        {
            Items = items, TotalCount = total, Page = page, PageSize = size
        }));
    }

    /// <summary>Cari detayı</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AccountResponse>>> GetAccount(Guid id)
    {
        var p = await _db.Accounts
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == CompanyId);

        if (p is null) return NotFound(ApiResponse<AccountResponse>.Fail("Cari bulunamadı."));

        return Ok(ApiResponse<AccountResponse>.Ok(new AccountResponse(
            p.Id, p.Code, p.Name, p.AccountType.ToString(),
            p.TaxNumber, p.TaxOffice, p.TcKimlik,
            p.Phone, p.MobilePhone, p.Email, p.Website,
            p.AddressLine1, p.City, p.District,
            p.CreditLimit, p.DefaultPaymentTermDays, p.Currency,
            p.BankAccount, p.Representative, p.IsEInvoicePayer, p.EInvoiceAlias,
            p.CurrentBalance, p.IsActive, p.CreatedAt)));
    }

    /// <summary>Yeni cari oluştur</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AccountResponse>>> CreateAccount([FromBody] CreateAccountRequest req)
    {
        if (await _db.Accounts.AnyAsync(p => p.Code == req.Code && p.CompanyId == CompanyId))
            return BadRequest(ApiResponse<AccountResponse>.Fail($"'{req.Code}' kodlu cari zaten mevcut."));

        if (!Enum.TryParse<Re.Domain.Enums.AccountType>(req.AccountType, out var parsedAccountType))
            parsedAccountType = Re.Domain.Enums.AccountType.Customer;

        var account = Account.Create(CompanyId, req.Code, req.Name, parsedAccountType);
        
        account.UpdateDetails(
            req.TaxNumber, req.TaxOffice, req.TcKimlik,
            req.Phone, req.MobilePhone, req.Phone2, req.Email, req.Website,
            req.AddressLine1, req.City, req.District, req.PostalCode,
            req.CreditLimit, req.DefaultPaymentTermDays, req.Currency,
            req.BankAccount, req.Representative, req.IsEInvoicePayer, req.EInvoiceAlias);

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAccount), new { id = account.Id },
            ApiResponse<AccountResponse>.Ok(new AccountResponse(
                account.Id, account.Code, account.Name, account.AccountType.ToString(),
                account.TaxNumber, account.TaxOffice, account.TcKimlik,
                account.Phone, account.MobilePhone, account.Email, account.Website,
                account.AddressLine1, account.City, account.District,
                account.CreditLimit, account.DefaultPaymentTermDays, account.Currency,
                account.BankAccount, account.Representative, account.IsEInvoicePayer, account.EInvoiceAlias,
                account.CurrentBalance, account.IsActive, account.CreatedAt)));
    }

    /// <summary>Cari hesabı güncelle</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AccountResponse>>> UpdateAccount(Guid id, [FromBody] UpdateAccountRequest req)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == CompanyId);

        if (account is null) return NotFound(ApiResponse<AccountResponse>.Fail("Cari hesap bulunamadı."));

        if (!Enum.TryParse<Re.Domain.Enums.AccountType>(req.AccountType, out var parsedAccountType))
            parsedAccountType = Re.Domain.Enums.AccountType.Customer;

        account.UpdateBaseInfo(req.Name, parsedAccountType, req.IsActive);
        
        account.UpdateDetails(
            req.TaxNumber, req.TaxOffice, req.TcKimlik,
            req.Phone, req.MobilePhone, req.Phone2, req.Email, req.Website,
            req.AddressLine1, req.City, req.District, req.PostalCode,
            req.CreditLimit, req.DefaultPaymentTermDays, req.Currency ?? "TRY",
            req.BankAccount, req.Representative, req.IsEInvoicePayer, req.EInvoiceAlias);

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<AccountResponse>.Ok(new AccountResponse(
            account.Id, account.Code, account.Name, account.AccountType.ToString(),
            account.TaxNumber, account.TaxOffice, account.TcKimlik,
            account.Phone, account.MobilePhone, account.Email, account.Website,
            account.AddressLine1, account.City, account.District,
            account.CreditLimit, account.DefaultPaymentTermDays, account.Currency,
            account.BankAccount, account.Representative, account.IsEInvoicePayer, account.EInvoiceAlias,
            account.CurrentBalance, account.IsActive, account.CreatedAt)));
    }

    /// <summary>Cari hesabı sil (Soft delete)</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteAccount(Guid id)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == CompanyId);
        if (account is null) return NotFound(ApiResponse<object>.Fail("Cari hesap bulunamadı."));

        account.Deactivate(); // Soft delete
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null));
    }
}
