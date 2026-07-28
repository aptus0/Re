using System.Windows.Controls;
using Re.Desktop.ViewModels.Settings;

namespace Re.Desktop.Views.Settings;

public partial class PackageCenterPage : UserControl
{
    public PackageCenterPage(PackageCenterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
