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
    public decimal PreviousMonthSalesTotal { get; set; }
    public decimal SalesChangePercent { get; set; }
    public decimal TodayCollections { get; set; }
    public decimal TodayPayments { get; set; }
    public int DraftInvoiceCount { get; set; }
    public int OverdueInvoiceCount { get; set; }
    public decimal OverdueInvoiceTotal { get; set; }
    public int CriticalStockCount { get; set; }
    public int OutOfStockCount { get; set; }

    public List<RecentTransactionItem> RecentTransactions { get; set; } = new();
    public List<DashboardSalesPoint> SalesTrend { get; set; } = new();
    public List<DashboardTopProductItem> TopProducts { get; set; } = new();
    public List<DashboardAlertItem> Alerts { get; set; } = new();
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

public class DashboardSalesPoint
{
    public string Period { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class DashboardTopProductItem
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Revenue { get; set; }
}

public class DashboardAlertItem
{
    public string Severity { get; set; } = "Info";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
}
