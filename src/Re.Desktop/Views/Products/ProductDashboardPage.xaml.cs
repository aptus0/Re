using System.Windows.Controls;
using Re.Desktop.ViewModels.Products;

namespace Re.Desktop.Views.Products;

public partial class ProductDashboardPage : UserControl
{
    public ProductDashboardPage(ProductDashboardViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
    }
}
