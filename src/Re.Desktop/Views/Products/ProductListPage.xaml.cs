using System.Windows.Controls;
using Re.Desktop.ViewModels.Products;

namespace Re.Desktop.Views.Products;

public partial class ProductListPage : UserControl
{
    public ProductListPage(ProductListViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}

