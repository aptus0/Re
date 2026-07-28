using Re.Desktop.Services;
using Re.Desktop.ViewModels.Auth;
using Re.Desktop.Views.Shell;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;

namespace Re.Desktop.Views.Auth;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;

    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        // Receipt başarılı olduğunda ana ekrana geç
        _vm.LoginSucceeded += OnLoginSucceeded;

        // Varsayılan şifreyi UI'a yansıt
        PasswordBox.Password = _vm.Password;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _vm.Password = PasswordBox.Password;
    }

    private void OnLoginSucceeded(object? sender, EventArgs e)
    {
        var mainWindow = App.Services.GetRequiredService<MainWindow>();
        // Login penceresi kapandıktan sonra modal ekranların Owner'ı geçerli ana pencere olmalı.
        Application.Current.MainWindow = mainWindow;
        mainWindow.Show();
        Close();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new Re.Desktop.Views.Settings.ApiSettingsWindow();
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            this.DragMove();
        }
    }
}

