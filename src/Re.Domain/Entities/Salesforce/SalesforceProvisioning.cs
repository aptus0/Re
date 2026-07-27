using Re.Domain.Entities.Common;

namespace Re.Domain.Entities.Salesforce;

public enum SalesforceEnvironment { Sandbox, Production }
public enum SalesforceConnectionStatus { PendingAuthorization, Connected, AttentionRequired, Disconnected }
public enum SalesforceDiscoveryStatus { Pending, Running, Completed, Failed }
public enum SalesforceBlueprintStatus { Draft, Published, Retired }
public enum SalesforceDeploymentStatus { Pending, Running, WaitingForApproval, Failed, Completed, Cancelled }
public enum SalesforceStepStatus { Pending, Running, Failed, Completed, Skipped }
public enum SalesforceDeploymentStage
{
    OrgDiscovery, Precheck, PackageInstall, MetadataValidation, MetadataDeployment,
    PostInstallConfiguration, PermissionAssignment, IntegrationSetup, DataMigration,
    Testing, UserAcceptance, Completed
}

public sealed class SalesforceTenant : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public string DisplayName { get; set; } = "";
    public string SalesforceOrgId { get; set; } = "";
    public string InstanceUrl { get; set; } = "";
    public string Edition { get; set; } = "";
    public string ApiVersion { get; set; } = "v65.0";
    public string ConnectedUserId { get; set; } = "";
    public string? CredentialReference { get; set; }
    public SalesforceConnectionStatus ConnectionStatus { get; set; } = SalesforceConnectionStatus.PendingAuthorization;
    public SalesforceEnvironment EnvironmentType { get; set; }
    public string NamespaceStatus { get; set; } = "NotInstalled";
    public DateTime? LastHealthCheckAt { get; set; }
    public ICollection<SalesforceOrgDiscovery> Discoveries { get; set; } = [];
    public ICollection<SalesforceDeploymentJob> Deployments { get; set; } = [];
}

public sealed class SalesforceOrgDiscovery : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public Guid TenantId { get; set; }
    public SalesforceTenant Tenant { get; set; } = null!;
    public SalesforceDiscoveryStatus Status { get; set; } = SalesforceDiscoveryStatus.Pending;
    public bool HasApiAccess { get; set; }
    public bool HasModifyAllData { get; set; }
    public bool SupportsNamedCredentials { get; set; }
    public bool SupportsPlatformEvents { get; set; }
    public bool SupportsMcp { get; set; }
    public int ConflictingFields { get; set; }
    public int ConflictingFlows { get; set; }
    public int MissingPermissions { get; set; }
    public int EstimatedMinutes { get; set; }
    public string FindingsJson { get; set; } = "{}";
    public DateTime? CompletedAt { get; set; }
}

public sealed class SalesforceBlueprint : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Sector { get; set; } = "";
    public string ModulesJson { get; set; } = "[]";
    public string FeaturesJson { get; set; } = "{}";
    public SalesforceBlueprintStatus Status { get; set; } = SalesforceBlueprintStatus.Draft;
    public ICollection<SalesforceDeploymentJob> Deployments { get; set; } = [];
}

public sealed class SalesforceDeploymentJob : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public Guid TenantId { get; set; }
    public SalesforceTenant Tenant { get; set; } = null!;
    public Guid BlueprintId { get; set; }
    public SalesforceBlueprint Blueprint { get; set; } = null!;
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public SalesforceEnvironment TargetEnvironment { get; set; }
    public SalesforceDeploymentStatus Status { get; set; } = SalesforceDeploymentStatus.Pending;
    public SalesforceDeploymentStage CurrentStage { get; set; } = SalesforceDeploymentStage.OrgDiscovery;
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ICollection<SalesforceDeploymentStep> Steps { get; set; } = [];
}

public sealed class SalesforceDeploymentStep : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public Guid DeploymentJobId { get; set; }
    public SalesforceDeploymentJob DeploymentJob { get; set; } = null!;
    public int Sequence { get; set; }
    public SalesforceDeploymentStage Stage { get; set; }
    public SalesforceStepStatus Status { get; set; } = SalesforceStepStatus.Pending;
    public int RetryCount { get; set; }
    public string? LogSummary { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
