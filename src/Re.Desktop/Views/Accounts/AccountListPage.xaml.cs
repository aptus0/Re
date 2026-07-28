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

    private void DataGridRow_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is DataGridRow row && row.DataContext is AccountItem item)
        {
            if (DataContext is AccountListViewModel vm)
            {
                vm.ShowAccountDetailWindowCommand.Execute(item);
            }
        }
    }
}

