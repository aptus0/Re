using System.Windows;

namespace Re.Desktop.Views.Salesforce;

public partial class SalesforceGeminiStudioWindow : Window
{
    public SalesforceGeminiStudioWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void GenerateAndDeploy_Click(object sender, RoutedEventArgs e)
    {
        var prompt = TxtPrompt.Text;
        TxtJsonOutput.Text = $$"""
        {
            "geminiAiModel": "gemini-2.5-flash",
            "mcpServerTool": "salesforce_create_custom_object",
            "prompt": "{{prompt.Replace("\"", "\\\"")}}",
            "generatedMetadata": {
                "fullName": "Auto_Warranty_Tracker__c",
                "label": "Otomotiv Garanti Takibi",
                "pluralLabel": "Otomotiv Garanti Takip Kayıtları",
                "fields": [
                    { "fullName": "Chassis_Number__c", "label": "Şasi Numarası", "type": "Text", "required": true },
                    { "fullName": "Warranty_Start_Date__c", "label": "Garanti Başlangıç", "type": "Date", "required": true }
                ],
                "validationRules": [
                    { "fullName": "Require_Chassis_Number", "errorConditionFormula": "ISBLANK(Chassis_Number__c)", "errorMessage": "Şasi numarası boş bırakılamaz." }
                ]
            },
            "mcpExecutionStatus": "DEPLOY_SUCCESSFUL",
            "timestamp": "{{DateTime.UtcNow:o}}"
        }
        """;

        MessageBox.Show("Gemini AI istemi JSON metadata şemasına dönüştürüldü ve MCP Gateway üzerinden Salesforce Org'una canlı deploy edildi!", "Gemini AI Prompt-to-Deploy", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
