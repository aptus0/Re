using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Re.Contracts.Common;
using Re.Contracts.Dashboard;
using Re.Domain.Enums;
using Re.Persistence.Context;

namespace Re.Api.Controllers;

[ApiController, Route("api/[controller]"), Authorize]
public sealed class DashboardController(ReDbContext context) : ControllerBase
{
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")?.Value ?? Guid.Empty.ToString());

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryResponse>>> GetSummary(
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var currentMonth = new DateTime(now.Year, now.Month, 1);
        var previousMonth = currentMonth.AddMonths(-1);
        var trendStart = currentMonth.AddMonths(-5);
        var branchIds = await context.Branches
            .Where(x => x.CompanyId == CompanyId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var response = new DashboardSummaryResponse
        {
            TotalAccountCount = await context.Accounts.CountAsync(x => x.CompanyId == CompanyId, cancellationToken),
            TotalCashBalance = await context.CashRegisters
                .Where(x => branchIds.Contains(x.BranchId))
                .SumAsync(x => (decimal?)x.CurrentBalance, cancellationToken) ?? 0,
            TotalBankBalance = await context.BankAccounts
                .Where(x => branchIds.Contains(x.BranchId))
                .SumAsync(x => (decimal?)x.CurrentBalance, cancellationToken) ?? 0
        };

        var balances = await context.Accounts
            .Where(x => x.CompanyId == CompanyId)
            .Select(x => x.CurrentBalance)
            .ToListAsync(cancellationToken);
        response.TotalReceivables = balances.Where(x => x > 0).Sum();
        response.TotalPayables = Math.Abs(balances.Where(x => x < 0).Sum());

        var validInvoices = context.Invoices.Where(x =>
            x.CompanyId == CompanyId &&
            x.Status != DocumentStatus.Draft &&
            x.Status != DocumentStatus.Cancelled &&
            x.Status != DocumentStatus.Reversed);
        var currentSales = await validInvoices
            .Where(x => x.DocumentDate >= currentMonth)
            .Select(x => new { x.TotalAmount })
            .ToListAsync(cancellationToken);
        response.CurrentMonthSalesCount = currentSales.Count;
        response.CurrentMonthSalesTotal = currentSales.Sum(x => x.TotalAmount);
        response.PreviousMonthSalesTotal = await validInvoices
            .Where(x => x.DocumentDate >= previousMonth && x.DocumentDate < currentMonth)
            .SumAsync(x => (decimal?)x.TotalAmount, cancellationToken) ?? 0;
        response.SalesChangePercent = response.PreviousMonthSalesTotal == 0
            ? (response.CurrentMonthSalesTotal > 0 ? 100 : 0)
            : Math.Round((response.CurrentMonthSalesTotal - response.PreviousMonthSalesTotal)
                / response.PreviousMonthSalesTotal * 100, 1);

        response.DraftInvoiceCount = await context.Invoices.CountAsync(
            x => x.CompanyId == CompanyId && x.Status == DocumentStatus.Draft, cancellationToken);
        var overdue = await context.Invoices
            .Where(x => x.CompanyId == CompanyId && x.DueDate < today &&
                        x.Status != DocumentStatus.Draft &&
                        x.Status != DocumentStatus.Cancelled &&
                        x.Status != DocumentStatus.Reversed &&
                        x.Status != DocumentStatus.FullyPaid &&
                        x.TotalAmount > x.PaidAmount)
            .Select(x => x.TotalAmount - x.PaidAmount)
            .ToListAsync(cancellationToken);
        response.OverdueInvoiceCount = overdue.Count;
        response.OverdueInvoiceTotal = overdue.Sum();

        var todayCash = await context.CashRegisterMovements
            .Where(x => x.CompanyId == CompanyId && x.MovementDate >= today && x.MovementDate < tomorrow)
            .Select(x => new { x.Direction, Amount = x.Amount * x.ExchangeRate })
            .ToListAsync(cancellationToken);
        var todayBank = await context.BankAccountMovements
            .Where(x => x.CompanyId == CompanyId && x.MovementDate >= today && x.MovementDate < tomorrow)
            .Select(x => new { x.Direction, Amount = x.Amount * x.ExchangeRate })
            .ToListAsync(cancellationToken);
        response.TodayCollections = todayCash.Concat(todayBank)
            .Where(x => x.Direction == MovementDirection.Debit).Sum(x => x.Amount);
        response.TodayPayments = todayCash.Concat(todayBank)
            .Where(x => x.Direction == MovementDirection.Credit).Sum(x => x.Amount);

        var products = await context.Products
            .Where(x => x.CompanyId == CompanyId && x.IsActive && x.TrackStock)
            .Select(x => new { x.Id, x.MinStockLevel })
            .ToListAsync(cancellationToken);
        var productIds = products.Select(x => x.Id).ToList();
        var stocks = await context.StockMovements
            .Where(x => x.CompanyId == CompanyId && productIds.Contains(x.ProductId))
            .GroupBy(x => x.ProductId)
            .Select(x => new { ProductId = x.Key, Quantity = x.Sum(m => m.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity, cancellationToken);
        response.OutOfStockCount = products.Count(x => stocks.GetValueOrDefault(x.Id) <= 0);
        response.CriticalStockCount = products.Count(x =>
            stocks.GetValueOrDefault(x.Id) > 0 &&
            stocks.GetValueOrDefault(x.Id) <= x.MinStockLevel);

        var salesRows = await validInvoices
            .Where(x => x.DocumentDate >= trendStart)
            .Select(x => new { x.DocumentDate, x.TotalAmount })
            .ToListAsync(cancellationToken);
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        for (var month = trendStart; month <= currentMonth; month = month.AddMonths(1))
        {
            var end = month.AddMonths(1);
            response.SalesTrend.Add(new DashboardSalesPoint
            {
                Period = month.ToString("MMM yy", tr),
                Amount = salesRows.Where(x => x.DocumentDate >= month && x.DocumentDate < end)
                    .Sum(x => x.TotalAmount)
            });
        }

        var topProducts = await context.InvoiceLines
            .Where(x => x.Invoice.CompanyId == CompanyId &&
                        x.Invoice.DocumentDate >= currentMonth &&
                        x.Invoice.Status != DocumentStatus.Draft &&
                        x.Invoice.Status != DocumentStatus.Cancelled &&
                        x.Invoice.Status != DocumentStatus.Reversed)
            .GroupBy(x => new { x.ProductId, x.ProductCode, x.ProductName })
            .Select(g => new DashboardTopProductItem
            {
                ProductId = g.Key.ProductId,
                ProductCode = g.Key.ProductCode ?? string.Empty,
                ProductName = g.Key.ProductName,
                Quantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x =>
                    ((x.Quantity * x.UnitPrice) - x.DiscountAmount) *
                    (1 + (x.VatRate / 100)))
            })
            .ToListAsync(cancellationToken);
        response.TopProducts = topProducts.OrderByDescending(x => x.Revenue).Take(5).ToList();

        response.RecentTransactions = await GetRecentTransactions(cancellationToken);
        AddAlerts(response);
        return Ok(ApiResponse<DashboardSummaryResponse>.Ok(response));
    }

    private async Task<List<RecentTransactionItem>> GetRecentTransactions(CancellationToken cancellationToken)
    {
        var invoices = await context.Invoices
            .Where(x => x.CompanyId == CompanyId && x.Status != DocumentStatus.Draft &&
                        x.Status != DocumentStatus.Cancelled && x.Status != DocumentStatus.Reversed)
            .OrderByDescending(x => x.DocumentDate).Take(6)
            .Select(x => new RecentTransactionItem
            {
                Id = x.Id, Date = x.DocumentDate, TransactionType = "Sales Invoice",
                Description = x.DocumentNumber, Amount = x.TotalAmount, IsIncome = true
            }).ToListAsync(cancellationToken);
        var cash = await context.CashRegisterMovements
            .Where(x => x.CompanyId == CompanyId)
            .OrderByDescending(x => x.MovementDate).Take(6)
            .Select(x => new RecentTransactionItem
            {
                Id = x.Id, Date = x.MovementDate,
                TransactionType = x.Direction == MovementDirection.Debit ? "Cash Collection" : "Cash Payment",
                Description = x.Description ?? string.Empty, Amount = x.Amount,
                IsIncome = x.Direction == MovementDirection.Debit
            }).ToListAsync(cancellationToken);
        var bank = await context.BankAccountMovements
            .Where(x => x.CompanyId == CompanyId)
            .OrderByDescending(x => x.MovementDate).Take(6)
            .Select(x => new RecentTransactionItem
            {
                Id = x.Id, Date = x.MovementDate,
                TransactionType = x.Direction == MovementDirection.Debit ? "Bank Collection" : "Bank Payment",
                Description = x.Description ?? string.Empty, Amount = x.Amount,
                IsIncome = x.Direction == MovementDirection.Debit
            }).ToListAsync(cancellationToken);
        return invoices.Concat(cash).Concat(bank).OrderByDescending(x => x.Date).Take(10).ToList();
    }

    private static void AddAlerts(DashboardSummaryResponse response)
    {
        if (response.OverdueInvoiceCount > 0)
            response.Alerts.Add(new DashboardAlertItem
            {
                Severity = "Critical", Title = "Overdue invoices",
                Description = $"{response.OverdueInvoiceCount} invoices have an open balance of {response.OverdueInvoiceTotal:N2} ₺",
                Route = "Invoices"
            });
        if (response.OutOfStockCount > 0)
            response.Alerts.Add(new DashboardAlertItem
            {
                Severity = "Critical", Title = "Out-of-stock products",
                Description = $"{response.OutOfStockCount} products have no available inventory",
                Route = "ProductDashboard"
            });
        if (response.CriticalStockCount > 0)
            response.Alerts.Add(new DashboardAlertItem
            {
                Severity = "Warning", Title = "Critical stock",
                Description = $"{response.CriticalStockCount} products are at or below minimum stock",
                Route = "ProductDashboard"
            });
        if (response.DraftInvoiceCount > 0)
            response.Alerts.Add(new DashboardAlertItem
            {
                Severity = "Info", Title = "Invoices awaiting approval",
                Description = $"{response.DraftInvoiceCount} draft invoices require action",
                Route = "Invoices"
            });
        if (response.Alerts.Count == 0)
            response.Alerts.Add(new DashboardAlertItem
            {
                Severity = "Success", Title = "Operations are healthy",
                Description = "No critical records require action",
                Route = "Dashboard"
            });
    }
}
