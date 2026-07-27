using System.Windows.Controls;
using Re.Desktop.ViewModels.Shell;

namespace Re.Desktop.Views.Shell;

public partial class DashboardPage : UserControl
{
    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void DashboardPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
            await vm.LoadDashboardCommand.ExecuteAsync(null);
    }
}
