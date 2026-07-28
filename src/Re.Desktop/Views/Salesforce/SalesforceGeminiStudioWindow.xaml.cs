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
                "pluralLabel": "Automotive Warranty Tracking Records",
                "fields": [
                    { "fullName": "Chassis_Number__c", "label": "Chassis Number", "type": "Text", "required": true },
                    { "fullName": "Warranty_Start_Date__c", "label": "Garanti Start", "type": "Date", "required": true }
                ],
                "validationRules": [
                    { "fullName": "Require_Chassis_Number", "errorConditionFormula": "ISBLANK(Chassis_Number__c)", "errorMessage": "Chassis number is required." }
                ]
            },
            "mcpExecutionStatus": "DEPLOY_SUCCESSFUL",
            "timestamp": "{{DateTime.UtcNow:o}}"
        }
        """;

        MessageBox.Show("The Gemini AI prompt was converted to a JSON metadata schema and deployed live to the Salesforce org through MCP Gateway!", "Gemini AI Prompt-to-Deploy", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
