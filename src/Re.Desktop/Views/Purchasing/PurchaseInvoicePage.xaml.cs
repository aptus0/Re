using System.Windows.Controls;
using Re.Desktop.ViewModels.Purchasing;

namespace Re.Desktop.Views.Purchasing;

public partial class PurchaseInvoicePage : UserControl
{
    public PurchaseInvoicePage(PurchaseInvoiceViewModel viewModel)
    {
        InitializeComponent(); DataContext = viewModel; _ = viewModel.InitializeAsync();
    }
}
