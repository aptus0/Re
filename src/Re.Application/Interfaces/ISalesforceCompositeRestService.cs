namespace Re.Application.Interfaces;

public record CompositeSubRequest(string Method, string Url, string ReferenceId, object Body);
public record CompositeResult(bool Success, int ExecutedRequestsCount, string Message, DateTime ExecutedAt);

public interface ISalesforceCompositeRestService
{
    Task<CompositeResult> ExecuteCompositePayloadAsync(List<CompositeSubRequest> requests, bool allOrNone = true, CancellationToken cancellationToken = default);
    Task<CompositeResult> UpsertAccountWithContactAndOpportunityAsync(string externalId, string accountName, string contactLastName, string opportunityName, decimal amount, CancellationToken cancellationToken = default);
}
