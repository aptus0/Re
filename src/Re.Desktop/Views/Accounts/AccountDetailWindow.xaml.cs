using System;
using System.Windows;
using Re.Desktop.ViewModels.Accounts;
using Re.Desktop.Services;

namespace Re.Desktop.Views.Accounts;

public partial class AccountDetailWindow : Window
{
    public AccountDetailWindow(Guid accountId, ApiClient api, IDialogService dialog)
    {
        InitializeComponent();
        DataContext = new AccountDetailViewModel(accountId, api, dialog);
    }

    private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            this.DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
