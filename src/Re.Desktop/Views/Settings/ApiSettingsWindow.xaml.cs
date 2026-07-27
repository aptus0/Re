using System.Windows;

namespace Re.Desktop.Views.Settings;

public partial class ApiSettingsWindow : Window
{
    public ApiSettingsWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
