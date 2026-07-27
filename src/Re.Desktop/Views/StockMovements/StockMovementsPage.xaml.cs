using System.Windows.Controls;
using Re.Desktop.ViewModels.StockMovements;
namespace Re.Desktop.Views.StockMovements;
public partial class StockMovementsPage : UserControl
{
    public StockMovementsPage(StockMovementsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.InitializeAsync();
    }
}

