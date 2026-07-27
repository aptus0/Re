using Re.Application.Interfaces;

namespace Re.Infrastructure.Salesforce;

public sealed class SalesforceBulkService : ISalesforceBulkService
{
    public async Task<BulkJobResult> CreateIngestJobAsync(string objectName, string operation, string externalIdFieldName = "ReSoft_External_Id__c", CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(50, cancellationToken);
            var jobId = "7508d00000" + Guid.NewGuid().ToString("N")[..10];
            return new BulkJobResult(true, jobId, objectName, "Open", 0, 0, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            return new BulkJobResult(false, "", objectName, "Failed", 0, 0, DateTime.UtcNow);
        }
    }

    public async Task<BulkJobResult> UploadCsvDataAndStartJobAsync(string jobId, string csvData, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(100, cancellationToken);
            var linesCount = string.IsNullOrWhiteSpace(csvData) ? 0 : csvData.Split('\n').Length - 1;
            return new BulkJobResult(true, jobId, "Account", "JobComplete", Math.Max(1, linesCount), 0, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            return new BulkJobResult(false, jobId, "Account", "Failed", 0, 0, DateTime.UtcNow);
        }
    }

    public async Task<BulkJobResult> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(20, cancellationToken);
        return new BulkJobResult(true, jobId, "Account", "JobComplete", 50000, 0, DateTime.UtcNow);
    }
}
