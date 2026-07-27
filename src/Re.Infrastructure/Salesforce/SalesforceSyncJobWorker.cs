using Re.Application.Interfaces;

namespace Re.Infrastructure.Salesforce;

public record SalesforceJobStep(string JobName, string Status, string LogSummary, DateTime CompletedAt);
public record SalesforceJobQueueResult(bool Success, string QueueId, string TotalStatus, List<SalesforceJobStep> Steps);

public sealed class SalesforceSyncJobWorker(
    ISalesforceMetadataService metadataService,
    ISalesforceDataSyncService syncService,
    ISalesforceMcpServerService mcpService)
{
    public async Task<SalesforceJobQueueResult> RunFullAutoSyncJobQueueAsync(string targetOrg, CancellationToken cancellationToken = default)
    {
        var steps = new List<SalesforceJobStep>();
        var queueId = "job_q_" + Guid.NewGuid().ToString("N")[..8];

        // Step 1: Auto Zero-Touch Package Deploy
        var metaRes = await metadataService.DeployPackageAsync("ReSalesforceCore", targetOrg, checkOnly: false, cancellationToken);
        steps.Add(new SalesforceJobStep("Zero-Touch Package Deploy", metaRes.Status, metaRes.Message, DateTime.UtcNow));

        // Step 2: Gemini AI & MCP Server Object Provisioning
        var mcpRes = await mcpService.GenerateMetadataWithGeminiAiAsync("ERP Auto Sync Object Generation", "CustomObject", cancellationToken);
        steps.Add(new SalesforceJobStep("Gemini AI MCP Schema Generation", "Completed", mcpRes.LogSummary, DateTime.UtcNow));

        // Step 3: Fetch Customers
        var custRes = await syncService.SyncCustomersToSalesforceAsync(cancellationToken);
        steps.Add(new SalesforceJobStep("Sync Customers (Accounts)", "Completed", custRes.Message, DateTime.UtcNow));

        // Step 4: Fetch Products
        var prodRes = await syncService.SyncProductsToSalesforceAsync(cancellationToken);
        steps.Add(new SalesforceJobStep("Sync Products (Product2)", "Completed", prodRes.Message, DateTime.UtcNow));

        // Step 5: Fetch Invoices
        var invRes = await syncService.SyncInvoicesToSalesforceAsync(cancellationToken);
        steps.Add(new SalesforceJobStep("Sync Invoices (Opportunities)", "Completed", invRes.Message, DateTime.UtcNow));

        return new SalesforceJobQueueResult(true, queueId, "CompletedAllSteps", steps);
    }
}
