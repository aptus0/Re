using Re.Desktop.Services;
using Re.Desktop.ViewModels.Shell;
using System.Windows;
using System.Windows.Input;

namespace Re.Desktop.Views.Shell;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly INavigationService _navigation;

    public MainWindow(MainViewModel vm, INavigationService navigation)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        _navigation = navigation;

        // Initial loading and tabs are handled by MainViewModel.
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Çıkmak istiyor musunuz?", "Re ERP",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            Application.Current.Shutdown();
    }
}

