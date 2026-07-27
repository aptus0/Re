namespace Re.Application.Interfaces;

public record ToolingInspectionResult(bool Success, int CodeCoveragePercent, int ActiveFlowsCount, int InstalledPackagesCount, string SecurityRating, DateTime InspectedAt);

public interface ISalesforceToolingService
{
    Task<ToolingInspectionResult> InspectOrgCodeCoverageAndFlowsAsync(CancellationToken cancellationToken = default);
}
