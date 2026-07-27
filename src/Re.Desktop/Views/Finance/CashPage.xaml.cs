using System.Windows.Controls;
using Re.Desktop.ViewModels.Finance;
namespace Re.Desktop.Views.Finance;
public partial class CashPage : UserControl
{
    public CashPage(TreasuryViewModel vm) { InitializeComponent(); DataContext = vm; Loaded += async (_, _) => await vm.LoadAsync(); }
}

