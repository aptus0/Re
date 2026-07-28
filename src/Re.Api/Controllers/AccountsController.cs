using Re.Contracts.Accounts;
using Re.Contracts.Common;
using Re.Domain.Entities.Accounting;
using Re.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Re.Domain.Enums;

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
                p.Phone, p.TaxNumber, p.CurrentBalance, p.IsActive, p.Currency))
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

        if (p is null) return NotFound(ApiResponse<AccountResponse>.Fail("Account not found."));

        return Ok(ApiResponse<AccountResponse>.Ok(new AccountResponse(
            p.Id, p.Code, p.Name, p.AccountType.ToString(),
            p.TaxNumber, p.TaxOffice, p.TcKimlik,
            p.Phone, p.MobilePhone, p.Email, p.Website,
            p.AddressLine1, p.City, p.District,
            p.CreditLimit, p.DefaultPaymentTermDays, p.Currency,
            p.BankAccount, p.Representative, p.IsEInvoicePayer, p.EInvoiceAlias,
            p.CurrentBalance, p.IsActive, p.CreatedAt)));
    }

    [HttpGet("{id:guid}/360")]
    public async Task<ActionResult<ApiResponse<AccountInvoiceSummaryResponse>>> GetAccount360(Guid id)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == CompanyId);
        if (account is null)
            return NotFound(ApiResponse<AccountInvoiceSummaryResponse>.Fail("Account not found."));

        var invoices = await _db.Invoices
            .Include(x => x.Lines)
            .Where(x => x.CompanyId == CompanyId && x.CustomerId == id && !x.IsDeleted)
            .ToListAsync();
        var activities = await _db.AccountMovements
            .Where(x => x.CompanyId == CompanyId && x.AccountId == id)
            .OrderByDescending(x => x.MovementDate).Take(12)
            .Select(x => new AccountActivityResponse(
                x.Id, x.MovementDate, x.Direction.ToString(), x.Description ?? "Cari hareket",
                x.Amount, x.RunningBalance, x.ReferenceDocumentType, x.ReferenceDocumentId))
            .ToListAsync();

        var invoiceIds = invoices.Select(x => x.Id).ToList();
        var stockDocumentCount = await _db.StockMovements.CountAsync(x =>
            x.CompanyId == CompanyId && x.ReferenceDocumentId.HasValue &&
            invoiceIds.Contains(x.ReferenceDocumentId.Value));
        var productIds = invoices.SelectMany(x => x.Lines).Select(x => x.ProductId).Distinct().ToList();
        var productCodes = await _db.Products.Where(x => productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Code);
        var topProducts = invoices.SelectMany(x => x.Lines)
            .GroupBy(x => new { x.ProductId, x.ProductName })
            .Select(g => new AccountProductSummaryResponse(g.Key.ProductId,
                productCodes.GetValueOrDefault(g.Key.ProductId, "-"), g.Key.ProductName,
                g.Sum(x => x.Quantity), g.Sum(x => x.LineTotal)))
            .OrderByDescending(x => x.NetAmount).Take(8).ToList();
        var recentInvoices = invoices.OrderByDescending(x => x.DocumentDate).Take(10)
            .Select(x => new AccountInvoiceLinkResponse(x.Id, x.DocumentNumber, x.DocumentDate,
                x.Status.ToString(), x.TotalAmount, x.PaidAmount, x.Lines.Count)).ToList();
        var today = DateTime.Today;
        var openInvoices = invoices.Where(x => x.TotalAmount > x.PaidAmount)
            .Select(x => new
            {
                Remaining = x.TotalAmount - x.PaidAmount,
                DueDate = (x.DueDate ?? x.DocumentDate.AddDays(account.DefaultPaymentTermDays)).Date
            }).ToList();
        var aging = new AccountAgingSummaryResponse(
            openInvoices.Where(x => x.DueDate >= today).Sum(x => x.Remaining),
            openInvoices.Where(x => (today - x.DueDate).Days is >= 1 and <= 30).Sum(x => x.Remaining),
            openInvoices.Where(x => (today - x.DueDate).Days is >= 31 and <= 60).Sum(x => x.Remaining),
            openInvoices.Where(x => (today - x.DueDate).Days is >= 61 and <= 90).Sum(x => x.Remaining),
            openInvoices.Where(x => (today - x.DueDate).Days > 90).Sum(x => x.Remaining),
            openInvoices.Where(x => x.DueDate < today).Sum(x => x.Remaining),
            openInvoices.Count(x => x.DueDate < today),
            openInvoices.Where(x => x.DueDate < today).Select(x => (today - x.DueDate).Days).DefaultIfEmpty(0).Max());
        var totalInvoiced = invoices.Sum(x => x.TotalAmount);
        var customerSegment = totalInvoiced >= 5_000_000 ? "Strategic" :
            totalInvoiced >= 1_000_000 ? "Enterprise" :
            totalInvoiced >= 250_000 ? "Growth" : "Standard";
        var utilization = account.CreditLimit > 0
            ? Math.Clamp((double)(Math.Max(0, account.CurrentBalance) / account.CreditLimit), 0, 1.5) : 0;
        var overdueRatio = totalInvoiced > 0
            ? Math.Clamp((double)(aging.TotalOverdue / totalInvoiced), 0, 1) : 0;
        var riskScore = Math.Clamp((int)Math.Round(utilization * 55 + overdueRatio * 35 +
            Math.Min(aging.MaximumDaysOverdue, 180) / 180d * 10), 0, 100);
        var riskLevel = riskScore >= 75 ? "Critical" : riskScore >= 50 ? "High" :
            riskScore >= 25 ? "Moderate" : "Low";

        return Ok(ApiResponse<AccountInvoiceSummaryResponse>.Ok(new(
            invoices.Count, invoices.Sum(x => x.TotalAmount), invoices.Sum(x => x.PaidAmount),
            invoices.Sum(x => x.TotalAmount - x.PaidAmount), activities,
            invoices.SelectMany(x => x.Lines).Sum(x => x.Quantity), stockDocumentCount,
            topProducts, recentInvoices, aging, customerSegment, riskScore, riskLevel)));
    }

    [HttpPost("{id:guid}/operations")]
    public async Task<ActionResult<ApiResponse<AccountOperationResponse>>> CreateOperation(
        Guid id, CreateAccountOperationRequest request)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(x =>
            x.Id == id && x.CompanyId == CompanyId && x.IsActive);
        if (account is null)
            return NotFound(ApiResponse<AccountOperationResponse>.Fail("Active account not found."));
        if (request.Amount <= 0 || request.ExchangeRate <= 0 || string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(ApiResponse<AccountOperationResponse>.Fail(
                "Positive amount, exchange rate and description are required."));
        var operation = request.OperationType.Trim();
        if (operation is not ("Debit" or "Credit" or "Collection" or "Payment"))
            return BadRequest(ApiResponse<AccountOperationResponse>.Fail("Unsupported account operation."));

        var direction = operation is "Debit" or "Payment"
            ? MovementDirection.Debit : MovementDirection.Credit;
        var amountTry = request.Amount * request.ExchangeRate;
        var previous = account.CurrentBalance;
        var delta = direction == MovementDirection.Debit ? amountTry : -amountTry;
        account.UpdateBalance(delta);
        var operationNumber = string.IsNullOrWhiteSpace(request.ReferenceNumber)
            ? $"CAR-{DateTime.UtcNow:yyyyMMddHHmmss}" : request.ReferenceNumber.Trim();
        var movement = new AccountMovement
        {
            Id = Guid.NewGuid(), CompanyId = CompanyId, AccountId = account.Id,
            Direction = direction, Amount = request.Amount,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant(),
            ExchangeRate = request.ExchangeRate, MovementDate = request.MovementDate,
            DueDate = request.DueDate, Description = request.Description.Trim(),
            ReferenceDocumentType = operationNumber, RunningBalance = account.CurrentBalance,
            CreatedAt = DateTime.UtcNow
        };
        _db.AccountMovements.Add(movement);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<AccountOperationResponse>.Ok(new(
            movement.Id, operationNumber, previous, request.Amount,
            account.CurrentBalance, direction.ToString())));
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

        if (account is null) return NotFound(ApiResponse<AccountResponse>.Fail("Account not found."));

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
        if (account is null) return NotFound(ApiResponse<object>.Fail("Account not found."));

        account.Deactivate(); // Soft delete
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { }));
    }
}
