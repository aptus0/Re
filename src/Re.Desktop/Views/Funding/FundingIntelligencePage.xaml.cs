using System.Windows.Controls;
using Re.Desktop.ViewModels.Funding;

namespace Re.Desktop.Views.Funding;

public partial class FundingIntelligencePage : UserControl
{
    public FundingIntelligencePage(FundingIntelligenceViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
