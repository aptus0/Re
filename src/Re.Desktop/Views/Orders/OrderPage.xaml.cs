using Re.Desktop.ViewModels.Orders;
using System.Windows.Controls;

namespace Re.Desktop.Views.Orders;

public partial class OrderPage : UserControl
{
    private readonly OrderViewModel viewModel;
    public OrderPage(OrderViewModel viewModel)
    {
        InitializeComponent(); DataContext = this.viewModel = viewModel;
        Loaded += async (_, _) => await this.viewModel.InitializeAsync();
    }
}
