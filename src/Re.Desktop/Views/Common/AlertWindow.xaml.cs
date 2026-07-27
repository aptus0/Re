using System.Windows;

namespace Re.Desktop.Views.Common;

public partial class AlertWindow : Window
{
    public AlertWindow(string title, string message, bool confirmation, bool isError = false)
    {
        InitializeComponent();
        DataContext = new
        {
            Title = title,
            Message = message,
            Symbol = isError ? "!" : confirmation ? "?" : "i",
            AcceptText = confirmation ? "Evet" : "Tamam"
        };
        CancelButton.Visibility = confirmation ? Visibility.Visible : Visibility.Collapsed;
        Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
    }

    private void Accept_Click(object sender, RoutedEventArgs e) { DialogResult = true; Close(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
