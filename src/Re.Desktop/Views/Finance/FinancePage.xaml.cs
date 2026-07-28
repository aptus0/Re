using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Re.Desktop.ViewModels.Finance;
using Re.Desktop.Services;

namespace Re.Desktop.Views.Finance;

public partial class FinancePage : UserControl
{
    public FinancePage()
    {
        InitializeComponent();

        // DI container'dan ViewModel'i alıp DataContext'e atıyoruz
        var vm = App.Services?.GetService<FinanceViewModel>();
        if (vm != null)
        {
            DataContext = vm;
        }
    }

    private void DataGridRow_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is DataGridRow row && row.DataContext is ChequeNoteItem item)
        {
            if (DataContext is FinanceViewModel vm)
            {
                vm.ViewChequeDetailsCommand.Execute(item);
            }
        }
    }
}
