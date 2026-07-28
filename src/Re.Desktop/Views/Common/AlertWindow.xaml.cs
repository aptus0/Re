using System.Windows;
using Re.Desktop.Services;

namespace Re.Desktop.Views.Common;

public partial class AlertWindow : Window
{
    public AlertWindow(
        string title,
        string message,
        NotificationKind kind,
        bool confirmation,
        IUiLocalizationService localization)
    {
        InitializeComponent();
        var (symbol, accent, iconBackground) = kind switch
        {
            NotificationKind.Success => ("✓", "#0176D3", "#EFF6FF"),
            NotificationKind.Warning => ("!", "#F97316", "#FFF7ED"),
            NotificationKind.Error => ("×", "#475569", "#F1F5F9"),
            NotificationKind.Confirmation => ("?", "#F97316", "#FFF7ED"),
            _ => ("i", "#0176D3", "#EFF6FF")
        };
        DataContext = new
        {
            Title = title,
            Message = message,
            Symbol = symbol,
            AccentBrush = accent,
            IconBackground = iconBackground,
            AcceptText = localization.Translate(confirmation ? "Dialog.Yes" : "Dialog.OK"),
            CancelText = localization.Translate("Dialog.No")
        };
        CancelButton.Visibility = confirmation ? Visibility.Visible : Visibility.Collapsed;
        Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
    }

    private void Accept_Click(object sender, RoutedEventArgs e) { DialogResult = true; Close(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
