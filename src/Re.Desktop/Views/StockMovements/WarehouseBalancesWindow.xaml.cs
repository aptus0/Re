using System.Windows;

namespace Re.Desktop.Views.StockMovements;

public partial class WarehouseBalancesWindow : Window
{
    public WarehouseBalancesWindow() => InitializeComponent();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
