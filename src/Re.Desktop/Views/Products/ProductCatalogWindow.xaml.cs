using System.Windows;
using Re.Desktop.ViewModels.Products;

namespace Re.Desktop.Views.Products;
public partial class ProductCatalogWindow : Window
{
    public ProductCatalogWindow(ProductCatalogViewModel viewModel) { InitializeComponent(); DataContext = viewModel; }
}
