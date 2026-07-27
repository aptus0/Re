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
            "Agentforce Önerisi: Cari hesap risk skoru Düşük (Low Risk). %5 özel müşteri indirimi ve 30 gün vade uygundur. Otomatik onay akışı başlatılabilir.",
            "LOW_RISK",
            5.0m,
            DateTime.UtcNow
        );
    }
}
