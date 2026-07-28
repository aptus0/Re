using System.Windows;

namespace Re.Desktop.Views.StockMovements;

public partial class StockAdjustmentWindow : Window
{
    public StockAdjustmentWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is ViewModels.StockMovements.StockAdjustmentViewModel vm)
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
