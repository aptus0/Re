namespace Re.Application.Interfaces;

public record BulkJobResult(bool Success, string JobId, string ObjectName, string State, int RecordsProcessed, int RecordsFailed, DateTime CreatedAt);

public interface ISalesforceBulkService
{
    Task<BulkJobResult> CreateIngestJobAsync(string objectName, string operation, string externalIdFieldName = "ReSoft_External_Id__c", CancellationToken cancellationToken = default);
    Task<BulkJobResult> UploadCsvDataAndStartJobAsync(string jobId, string csvData, CancellationToken cancellationToken = default);
    Task<BulkJobResult> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default);
}
