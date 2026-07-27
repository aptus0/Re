namespace Re.Application.Interfaces;

public record AgentforceSessionResponse(string SessionId, string AgentName, string Status, DateTime StartedAt);
public record AgentforceRecommendationResult(bool Success, string ActionRecommendation, string RiskAssessment, decimal SuggestedDiscountPercent, DateTime CreatedAt);

public interface ISalesforceAgentforceService
{
    Task<AgentforceSessionResponse> StartAgentSessionAsync(string agentName = "ReSoft_ERP_Proposal_Agent", CancellationToken cancellationToken = default);
    Task<AgentforceRecommendationResult> GetProposalRecommendationAsync(string sessionId, string accountId, decimal requestedAmount, CancellationToken cancellationToken = default);
}
