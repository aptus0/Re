using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;

namespace Re.Desktop.ViewModels.Invoices;

public record InvoiceRowDto(
    Guid Id, string DocumentNumber, DateTime DocumentDate,
    string CustomerName, decimal TotalAmount, decimal RemainingAmount, string Status);

public partial class InvoiceListViewModel : ObservableObject
{
    private readonly List<InvoiceRowDto> _allInvoices = [];
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _selectedStatus = "All";
    [ObservableProperty] private DateTime? _startDate = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime? _endDate = DateTime.Today;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private decimal _totalInvoiceAmount;
    [ObservableProperty] private decimal _totalCollectedAmount;
    [ObservableProperty] private decimal _totalRemainingAmount;
    [ObservableProperty] private decimal _collectionRate;
    [ObservableProperty] private InvoiceRowDto? _selectedInvoice;

    public ObservableCollection<InvoiceRowDto> Invoices { get; } = [];
    public List<string> StatusFilters { get; } = ["All", "Draft", "Approved", "Partially Paid", "Fully Paid", "Cancel"];

    public InvoiceListViewModel() => ApplyFilter();

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedStatusChanged(string value) => ApplyFilter();
    partial void OnStartDateChanged(DateTime? value) => ApplyFilter();
    partial void OnEndDateChanged(DateTime? value) => ApplyFilter();

    private void ApplyFilter()
    {
        var query = _allInvoices.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(i =>
                i.DocumentNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var statusCode = SelectedStatus switch
        {
            "Draft" => "Draft",
            "Approved" => "Approved",
            "Partially Paid" => "PartiallyPaid",
            "Fully Paid" => "FullyPaid",
            "Cancel" => "Cancelled",
            _ => null
        };
        if (statusCode is not null)
            query = query.Where(i => i.Status == statusCode);
        if (StartDate.HasValue)
            query = query.Where(i => i.DocumentDate.Date >= StartDate.Value.Date);
        if (EndDate.HasValue)
            query = query.Where(i => i.DocumentDate.Date <= EndDate.Value.Date);

        var filtered = query.OrderByDescending(i => i.DocumentDate).ToList();
        Invoices.Clear();
        foreach (var invoice in filtered)
            Invoices.Add(invoice);

        TotalCount = filtered.Count;
        TotalInvoiceAmount = filtered.Sum(i => i.TotalAmount);
        TotalRemainingAmount = filtered.Sum(i => i.RemainingAmount);
        TotalCollectedAmount = TotalInvoiceAmount - TotalRemainingAmount;
        CollectionRate = TotalInvoiceAmount == 0 ? 0 : TotalCollectedAmount / TotalInvoiceAmount * 100;
    }

    [RelayCommand]
    private void NewInvoice() =>
        MessageBox.Show("New invoice entry is handled in the detailed Sales Invoices form.",
            "New Invoice", MessageBoxButton.OK, MessageBoxImage.Information);

    [RelayCommand]
    private void ViewInvoice(InvoiceRowDto? invoice)
    {
        if (invoice is null) return;
        MessageBox.Show(
            $"Document: {invoice.DocumentNumber}\nAccounts: {invoice.CustomerName}\nTotal: {invoice.TotalAmount:N2} ₺\nRemaining: {invoice.RemainingAmount:N2} ₺",
            "Invoice Summary", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    [RelayCommand]
    private void ApproveInvoice(InvoiceRowDto? invoice)
    {
        if (invoice is null || invoice.Status != "Draft") return;
        var index = _allInvoices.FindIndex(i => i.Id == invoice.Id);
        if (index >= 0)
            _allInvoices[index] = invoice with { Status = "Approved" };
        ApplyFilter();
    }
    [RelayCommand]
    private void Export()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export invoice list",
            Filter = "CSV File (*.csv)|*.csv",
            FileName = $"fatura-listesi-{DateTime.Now:yyyyMMdd-HHmm}.csv"
        };
        if (dialog.ShowDialog() != true) return;

        static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
        var lines = new List<string> { "Document No;Date;Account;Total;Remaining;Status" };
        lines.AddRange(Invoices.Select(i => string.Join(";",
            Csv(i.DocumentNumber), i.DocumentDate.ToString("yyyy-MM-dd"), Csv(i.CustomerName),
            i.TotalAmount.ToString("0.00"), i.RemainingAmount.ToString("0.00"), Csv(i.Status))));
        File.WriteAllLines(dialog.FileName, lines, new UTF8Encoding(true));
    }
    [RelayCommand] private void ClearFilters()
    {
        SearchText = ""; SelectedStatus = "All";
        StartDate = DateTime.Today.AddMonths(-1); EndDate = DateTime.Today;
    }
}

