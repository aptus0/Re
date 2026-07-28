using System.Windows;
using Re.Desktop.ViewModels.StockMovements;

namespace Re.Desktop.Views.StockMovements;

public partial class InventoryOperationWindow : Window
{
    public InventoryOperationWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is InventoryOperationViewModel vm)
                vm.Saved += () => { DialogResult = true; Close(); };
        };
    }

    private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            this.DragMove();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
