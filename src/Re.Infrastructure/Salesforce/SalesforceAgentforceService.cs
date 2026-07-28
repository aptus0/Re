using Re.Application.Interfaces;

namespace Re.Infrastructure.Salesforce;

public sealed class SalesforceAgentforceService : ISalesforceAgentforceService
{
    public async Task<AgentforceSessionResponse> StartAgentSessionAsync(string agentName = "ReSoft_ERP_Proposal_Agent", CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        var sessionId = "ag_sess_" + Guid.NewGuid().ToString("N")[..12];
        return new AgentforceSessionResponse(sessionId, agentName, "Active", DateTime.UtcNow);
    }

    public async Task<AgentforceRecommendationResult> GetProposalRecommendationAsync(string sessionId, string accountId, decimal requestedAmount, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);
        return new AgentforceRecommendationResult(
            true,
            "Agentforce Recommendation: Account risk is low. A 5% customer discount and 30-day payment term are appropriate. The automatic approval flow can be started.",
            "LOW_RISK",
            5.0m,
            DateTime.UtcNow
        );
    }
}
