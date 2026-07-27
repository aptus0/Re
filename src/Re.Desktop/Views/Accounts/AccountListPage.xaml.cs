using System.Windows.Controls;
using Re.Desktop.ViewModels.Accounts;
namespace Re.Desktop.Views.Accounts;
public partial class AccountListPage : UserControl
{
    public AccountListPage(AccountListViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

