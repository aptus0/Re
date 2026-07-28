using System.Windows;
using Re.Desktop.ViewModels.Accounts;

namespace Re.Desktop.Views.Accounts;

public partial class AccountOperationWindow : Window
{
    public AccountOperationWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is AccountOperationViewModel vm)
                vm.Saved += () => { DialogResult = true; Close(); };
        };
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
