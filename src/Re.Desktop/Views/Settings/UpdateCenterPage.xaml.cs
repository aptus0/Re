using System.Windows.Controls;
using Re.Desktop.ViewModels.Settings;

namespace Re.Desktop.Views.Settings;

public partial class UpdateCenterPage : UserControl
{
    public UpdateCenterPage(PackageCenterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
