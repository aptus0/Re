using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Re.Application.Interfaces;
using Re.Contracts.Common;
using Re.Contracts.SystemSetup;
using Re.Domain.Entities.Company;
using Re.Persistence.Context;

namespace Re.Api.Controllers;

[ApiController]
[Route("api/system-setup")]
[Authorize]
public sealed class SystemSetupController(ReDbContext db, ICurrentTenantService tenant) : ControllerBase
{
    [HttpGet("fiscal-periods")]
    public async Task<IActionResult> GetFiscalPeriods()
    {
        var companyId = RequireCompany();
        var rows = await db.FiscalPeriods.OrderByDescending(x => x.StartDate).ToListAsync();
        return Ok(ApiResponse<IReadOnlyCollection<FiscalPeriodResponse>>.Ok(rows.Select(Map).ToList()));
    }

    [HttpPost("fiscal-periods")]
    public async Task<IActionResult> CreateFiscalPeriod(CreateFiscalPeriodRequest request)
    {
        var companyId = RequireCompany();
        var overlaps = await db.FiscalPeriods.AnyAsync(x =>
            request.StartDate.Date <= x.EndDate && request.EndDate.Date >= x.StartDate);
        if (overlaps) return Conflict(ApiResponse<object>.Fail("Fiscal period dates overlap an existing period."));
        var period = FiscalPeriod.Create(companyId, request.Name, request.FiscalYear,
            request.StartDate, request.EndDate);
        db.FiscalPeriods.Add(period);
        await db.SaveChangesAsync();
        return Ok(ApiResponse<FiscalPeriodResponse>.Ok(Map(period)));
    }

    [HttpPost("fiscal-periods/{id:guid}/status")]
    public async Task<IActionResult> ChangeFiscalPeriodStatus(Guid id, ChangeFiscalPeriodStatusRequest request)
    {
        if (!Enum.TryParse<FiscalPeriodStatus>(request.Status, true, out var status))
            return BadRequest(ApiResponse<object>.Fail("Fiscal period status is invalid."));
        var period = await db.FiscalPeriods.FirstOrDefaultAsync(x => x.Id == id);
        if (period is null) return NotFound(ApiResponse<object>.Fail("Fiscal period was not found."));
        Guid? userId = Guid.TryParse(User.FindFirst("sub")?.Value, out var parsed) ? parsed : null;
        period.ChangeStatus(status, userId);
        await db.SaveChangesAsync();
        return Ok(ApiResponse<FiscalPeriodResponse>.Ok(Map(period)));
    }

    [HttpGet("document-series")]
    public async Task<IActionResult> GetDocumentSeries()
    {
        RequireCompany();
        var rows = await db.DocumentSeries.OrderBy(x => x.DocumentType).ThenBy(x => x.Prefix).ToListAsync();
        return Ok(ApiResponse<IReadOnlyCollection<DocumentSeriesResponse>>.Ok(rows.Select(Map).ToList()));
    }

    [HttpPost("document-series")]
    public async Task<IActionResult> CreateDocumentSeries(CreateDocumentSeriesRequest request)
    {
        var companyId = RequireCompany();
        if (request.BranchId.HasValue &&
            !await db.Branches.AnyAsync(x => x.Id == request.BranchId && x.CompanyId == companyId && x.IsActive))
            return BadRequest(ApiResponse<object>.Fail("Branch is invalid or inactive."));
        var documentType = request.DocumentType.Trim().ToUpperInvariant();
        if (await db.DocumentSeries.AnyAsync(x => x.BranchId == request.BranchId &&
            x.DocumentType == documentType && x.IsActive))
            return Conflict(ApiResponse<object>.Fail("An active series already exists for this document type and branch."));
        var series = DocumentSeries.Create(companyId, request.BranchId, documentType,
            request.Prefix, request.NumberLength, request.ResetAnnually);
        db.DocumentSeries.Add(series);
        await db.SaveChangesAsync();
        return Ok(ApiResponse<DocumentSeriesResponse>.Ok(Map(series)));
    }

    [HttpPost("document-series/next")]
    public async Task<IActionResult> GenerateDocumentNumber(GenerateDocumentNumberRequest request)
    {
        var companyId = RequireCompany();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var period = await db.FiscalPeriods.FirstOrDefaultAsync(x =>
            x.Status == FiscalPeriodStatus.Open &&
            request.DocumentDate.Date >= x.StartDate && request.DocumentDate.Date <= x.EndDate);
        if (period is null)
            return BadRequest(ApiResponse<object>.Fail("No open fiscal period covers the document date."));
        var type = request.DocumentType.Trim().ToUpperInvariant();
        var series = await db.DocumentSeries.FirstOrDefaultAsync(x =>
            x.BranchId == request.BranchId && x.DocumentType == type && x.IsActive);
        if (series is null && request.BranchId.HasValue)
            series = await db.DocumentSeries.FirstOrDefaultAsync(x =>
                x.BranchId == null && x.DocumentType == type && x.IsActive);
        if (series is null)
            return BadRequest(ApiResponse<object>.Fail("No active document series is configured."));
        var number = series.Next(request.DocumentDate);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(ApiResponse<GeneratedDocumentNumberResponse>.Ok(
            new(number, series.Id, period.Id)));
    }

    private Guid RequireCompany() =>
        tenant.CompanyId is { } id && id != Guid.Empty
            ? id
            : throw new UnauthorizedAccessException("Company context is required.");

    private static FiscalPeriodResponse Map(FiscalPeriod x) =>
        new(x.Id, x.Name, x.FiscalYear, x.StartDate, x.EndDate, x.Status.ToString(), x.ClosedAt);
    private static DocumentSeriesResponse Map(DocumentSeries x) =>
        new(x.Id, x.BranchId, x.DocumentType, x.Prefix, x.CurrentYear, x.CurrentNumber,
            x.NumberLength, x.ResetAnnually, x.IsActive);
}
