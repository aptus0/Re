namespace Re.Application.Interfaces;

public record MetadataDeployResult(bool Success, string DeploymentId, string Status, string Message, int ComponentsDeployed, DateTime CompletedAt);

public interface ISalesforceMetadataService
{
    Task<MetadataDeployResult> ValidateDeploymentAsync(string manifestPath, string targetOrg, CancellationToken cancellationToken = default);
    Task<MetadataDeployResult> DeployPackageAsync(string zipOrFolderPath, string targetOrg, bool checkOnly = false, CancellationToken cancellationToken = default);
    Task<MetadataDeployResult> DeployCustomObjectAsync(string objectName, string label, string targetOrg, CancellationToken cancellationToken = default);
}
