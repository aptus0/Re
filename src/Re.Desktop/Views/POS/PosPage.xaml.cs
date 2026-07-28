using System.Windows.Controls;
using Re.Desktop.ViewModels.POS;
namespace Re.Desktop.Views.POS;
public partial class PosPage : UserControl
{
    public PosPage(PosViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

