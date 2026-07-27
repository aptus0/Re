using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Re.Persistence.Context;
using Re.Contracts.Dashboard;
using Re.Contracts.Common;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Re.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ReDbContext _context;

    public DashboardController(ReDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryResponse>>> GetSummary()
    {
        var response = new DashboardSummaryResponse();

        // 1. Toplam Cari Sayısı
        response.TotalAccountCount = await _context.Accounts.CountAsync();

        // 2. Toplam Alacak ve Borç (Bakiyesi > 0 olanlar Alacak, < 0 olanlar Borç veya tam tersi)
        // Cari bakiyeleri hesaplamak için Account objesinde Balance var.
        // Bizdeki Balance mantığı: Pozitif = Müşterinin bize borcu (Bizim alacağımız), Negatif = Bizim borcumuz
        var accounts = await _context.Accounts.Select(a => a.CurrentBalance).ToListAsync();
        response.TotalReceivables = accounts.Where(b => b > 0).Sum();
        response.TotalPayables = Math.Abs(accounts.Where(b => b < 0).Sum());

        // 3. Toplam Kasa ve Banka
        // Nakit kasaların bakiyesi (Cash türü)
        response.TotalCashBalance = await _context.CashRegisters.SumAsync(x => (decimal?)x.CurrentBalance) ?? 0;

        // Banka bakiyesi (Bank türü)
        response.TotalBankBalance = await _context.BankAccounts.SumAsync(x => (decimal?)x.CurrentBalance) ?? 0;

        // 4. Bu Ayki Satışlar (Approved durumunda olan Invoices)
        var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var monthlySales = await _context.Invoices
            .Where(i => i.Status == Re.Domain.Enums.DocumentStatus.Approved && i.DocumentDate >= startOfMonth)
            .ToListAsync();
            
        response.CurrentMonthSalesCount = monthlySales.Count;
        response.CurrentMonthSalesTotal = monthlySales.Sum(i => i.TotalAmount);

        // 5. Son İşlemler (Karma liste: Son faturalar, son tahsilatlar, son ödemeler)
        var recentInvoices = await _context.Invoices
            .Where(i => i.Status == Re.Domain.Enums.DocumentStatus.Approved)
            .OrderByDescending(i => i.DocumentDate)
            .Take(5)
            .Select(i => new RecentTransactionItem
            {
                Id = i.Id,
                Date = i.DocumentDate,
                TransactionType = "Satış Faturası",
                Description = i.DocumentNumber,
                Amount = i.TotalAmount,
                IsIncome = true
            })
            .ToListAsync();

        var recentCash = await _context.CashRegisterMovements
            .OrderByDescending(c => c.MovementDate)
            .Take(5)
            .Select(c => new RecentTransactionItem
            {
                Id = c.Id,
                Date = c.MovementDate,
                TransactionType = c.Direction == Re.Domain.Enums.MovementDirection.Debit ? "Kasa Tahsilatı" : "Kasa Ödemesi",
                Description = c.Description,
                Amount = c.Amount,
                IsIncome = c.Direction == Re.Domain.Enums.MovementDirection.Debit
            })
            .ToListAsync();

        var recentBank = await _context.BankAccountMovements
            .OrderByDescending(b => b.MovementDate)
            .Take(5)
            .Select(b => new RecentTransactionItem
            {
                Id = b.Id,
                Date = b.MovementDate,
                TransactionType = b.Direction == Re.Domain.Enums.MovementDirection.Debit ? "Banka Gelen" : "Banka Giden",
                Description = b.Description,
                Amount = b.Amount,
                IsIncome = b.Direction == Re.Domain.Enums.MovementDirection.Debit
            })
            .ToListAsync();

        //epsini birleştirip tarihe göre sırala, ilk 10'u al
        var allRecent = recentInvoices.Concat(recentCash).Concat(recentBank)
            .OrderByDescending(r => r.Date)
            .Take(10)
            .ToList();

        response.RecentTransactions = allRecent;

        return Ok(new ApiResponse<DashboardSummaryResponse> { Data = response });
    }
}
