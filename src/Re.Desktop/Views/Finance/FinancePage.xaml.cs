using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Re.Desktop.ViewModels.Finance;

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
}
