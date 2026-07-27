namespace Re.Contracts.Salesforce;

public record SalesforceConnectionRequest(
    string DisplayName, string SalesforceOrgId, string InstanceUrl, string Edition,
    string ApiVersion, string ConnectedUserId, string CredentialReference, string Environment);

public record SalesforceTenantResponse(
    Guid Id, string DisplayName, string SalesforceOrgId, string InstanceUrl, string Edition,
    string ApiVersion, string ConnectionStatus, string Environment, string NamespaceStatus,
    DateTime? LastHealthCheckAt);

public record SalesforceDiscoveryResponse(
    Guid Id, Guid TenantId, string Status, bool HasApiAccess, bool HasModifyAllData,
    bool SupportsNamedCredentials, bool SupportsPlatformEvents, bool SupportsMcp,
    int ConflictingFields, int ConflictingFlows, int MissingPermissions, int EstimatedMinutes,
    DateTime? CompletedAt);

public record CreateSalesforceBlueprintRequest(
    string Name, string Version, string Sector, IReadOnlyCollection<string> Modules,
    IReadOnlyDictionary<string, bool> Features, bool Publish);

public record SalesforceBlueprintResponse(
    Guid Id, string Name, string Version, string Sector, IReadOnlyCollection<string> Modules,
    IReadOnlyDictionary<string, bool> Features, string Status);

public record CreateSalesforceDeploymentRequest(Guid TenantId, Guid BlueprintId, string TargetEnvironment);
public record ApproveSalesforceDeploymentRequest(string ApprovalNote);
public record SalesforceDeploymentStepResponse(
    Guid Id, int Sequence, string Stage, string Status, int RetryCount, string? LogSummary,
    DateTime? StartedAt, DateTime? CompletedAt);
public record SalesforceDeploymentResponse(
    Guid Id, Guid CorrelationId, Guid TenantId, string TenantName, Guid BlueprintId,
    string BlueprintName, string TargetEnvironment, string Status, string CurrentStage,
    int ProgressPercent, int RetryCount, string? ErrorMessage, DateTime? StartedAt,
    DateTime? CompletedAt, IReadOnlyCollection<SalesforceDeploymentStepResponse> Steps);
public record SalesforceOverviewResponse(
    int ConnectedOrgs, int HealthyOrgs, int ActiveDeployments, int FailedDeployments,
    int PublishedBlueprints, IReadOnlyCollection<SalesforceTenantResponse> Tenants,
    IReadOnlyCollection<SalesforceDeploymentResponse> RecentDeployments);
public record SalesforceCliStatusResponse(
    bool IsInstalled, string? Version, string ProjectPath, bool ProjectExists,
    int AuthorizedOrgCount, string? Error);
public record SalesforceCliOrgResponse(
    string? Alias, string? Username, string? OrgId, string? InstanceUrl, bool IsScratchOrg, string? Status);
public record SalesforceCliLoginRequest(string Alias, bool Sandbox);
