using System.Windows.Controls;
using Re.Desktop.ViewModels.Sales;

namespace Re.Desktop.Views.Sales;

public partial class InvoicePage : UserControl
{
    public InvoicePage(InvoiceListViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
