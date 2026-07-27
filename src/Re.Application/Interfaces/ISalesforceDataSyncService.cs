namespace Re.Application.Interfaces;

public record SalesforceSyncResult(bool Success, string Message, int SyncedCount, DateTime SyncTime);

public interface ISalesforceDataSyncService
{
    Task<SalesforceSyncResult> SyncCustomersToSalesforceAsync(CancellationToken cancellationToken = default);
    Task<SalesforceSyncResult> SyncProductsToSalesforceAsync(CancellationToken cancellationToken = default);
    Task<SalesforceSyncResult> SyncInvoicesToSalesforceAsync(CancellationToken cancellationToken = default);
    Task<SalesforceSyncResult> DeployReadyMadePackageAsync(string targetOrgAlias, CancellationToken cancellationToken = default);
}
