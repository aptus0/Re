using System;
using System.Collections.Generic;

namespace Re.Contracts.Dashboard;

public class DashboardSummaryResponse
{
    public int TotalAccountCount { get; set; }
    public decimal TotalReceivables { get; set; }
    public decimal TotalPayables { get; set; }
    public decimal TotalCashBalance { get; set; }
    public decimal TotalBankBalance { get; set; }
    public decimal CurrentMonthSalesTotal { get; set; }
    public int CurrentMonthSalesCount { get; set; }
    
    public List<RecentTransactionItem> RecentTransactions { get; set; } = new();
}

public class RecentTransactionItem
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string TransactionType { get; set; } = string.Empty; // e.g. "Satış Faturası", "Tahsilat", "Ödeme"
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsIncome { get; set; } 
}
