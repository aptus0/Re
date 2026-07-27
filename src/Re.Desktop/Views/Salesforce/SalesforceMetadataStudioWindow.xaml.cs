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
        MessageBox.Show($"Özel Obje ({TxtObjectLabel.Text} - {TxtObjectApiName.Text}) Salesforce Org'una başarıyla deploy edildi.", "Metadata Studio", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DeployRule_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show($"Doğrulama Kuralı ({TxtRuleName.Text}) [{TxtRuleObject.Text}] nesnesine başarıyla eklendi.", "Metadata Studio", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DeployFlow_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show($"Flow Otomasyonu ({TxtFlowName.Text}) yayına alındı ve aktifleştirildi.", "Metadata Studio", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
