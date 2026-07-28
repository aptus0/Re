using System.Windows;

namespace Re.Desktop.Views.Products;

public partial class CatalogItemQuickWindow : Window
{
    public string ItemCodeText { get; private set; } = string.Empty;
    public string ItemNameText { get; private set; } = string.Empty;

    public CatalogItemQuickWindow(string title)
    {
        InitializeComponent();
        TitleLabel.Text = title.ToUpperInvariant();
        Title = title;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var code = ItemCode.Text.Trim();
        var name = ItemName.Text.Trim();

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
        {
            MessageBox.Show("Please enter both a Code and a Name.", "Required Fields", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ItemCodeText = code;
        ItemNameText = name;
        DialogResult = true;
        Close();
    }
}
