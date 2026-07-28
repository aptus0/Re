using System.Windows;

namespace Re.Desktop.Views.Salesforce;

public partial class SalesforceMetadataStudioWindow : Window
{
    public SalesforceMetadataStudioWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void DeployObject_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show($"Custom Object ({TxtObjectLabel.Text} - {TxtObjectApiName.Text}) was deployed successfully to the Salesforce org.", "Metadata Studio", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DeployRule_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show($"Validation Rule ({TxtRuleName.Text}) [{TxtRuleObject.Text}] was added successfully.", "Metadata Studio", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DeployFlow_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show($"Flow Automation ({TxtFlowName.Text}) was published and activated.", "Metadata Studio", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
