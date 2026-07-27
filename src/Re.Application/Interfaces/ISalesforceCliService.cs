namespace Re.Application.Interfaces;

public record SalesforceCliStatus(
    bool IsInstalled, string? Version, string ProjectPath, bool ProjectExists,
    int AuthorizedOrgCount, string? Error);
public record SalesforceCliOrg(
    string? Alias, string? Username, string? OrgId, string? InstanceUrl, bool IsScratchOrg, string? Status);
public record SalesforceCliCommandResult(bool Success, string Message);

public interface ISalesforceCliService
{
    Task<SalesforceCliStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SalesforceCliOrg>> ListOrgsAsync(CancellationToken cancellationToken = default);
    SalesforceCliCommandResult StartWebLogin(string alias, bool sandbox);
    Task<string?> GetOrgLoginUrlAsync(string alias, CancellationToken cancellationToken = default);
}
