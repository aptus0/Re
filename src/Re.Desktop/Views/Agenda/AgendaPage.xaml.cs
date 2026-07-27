using System.Windows.Controls;
using Re.Desktop.ViewModels.Agenda;
using Microsoft.Extensions.DependencyInjection;

namespace Re.Desktop.Views.Agenda;

public partial class AgendaPage : UserControl
{
    public AgendaPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<AgendaViewModel>();
    }
}
