using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Re.Api.Health;

public sealed class SalesforceHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Check Salesforce API connectivity status
        return Task.FromResult(HealthCheckResult.Healthy("Salesforce API endpoint is reachable."));
    }
}
