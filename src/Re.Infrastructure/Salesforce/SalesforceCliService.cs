using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Re.Application.Interfaces;

namespace Re.Infrastructure.Salesforce;

public sealed class SalesforceCliService(IConfiguration configuration) : ISalesforceCliService
{
    private string ProjectPath => ResolveProjectPath();

    public async Task<SalesforceCliStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var version = await RunAsync(["--version"], null, cancellationToken);
        if (!version.Success)
            return new(false, null, ProjectPath, File.Exists(Path.Combine(ProjectPath, "sfdx-project.json")), 0, version.Error);
        var orgs = await ListOrgsAsync(cancellationToken);
        return new(true, version.Output.Trim(), ProjectPath,
            File.Exists(Path.Combine(ProjectPath, "sfdx-project.json")), orgs.Count, null);
    }

    public async Task<IReadOnlyCollection<SalesforceCliOrg>> ListOrgsAsync(CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(["org", "list", "auth", "--json"], ProjectPath, cancellationToken);
        if (!result.Success) return [];
        using var document = JsonDocument.Parse(result.Output);
        if (!document.RootElement.TryGetProperty("result", out var items) || items.ValueKind != JsonValueKind.Array)
            return [];
        return items.EnumerateArray().Select(x => new SalesforceCliOrg(
            Text(x, "alias"), Text(x, "username"), Text(x, "orgId"), Text(x, "instanceUrl"),
            Bool(x, "isScratchOrg"), Text(x, "oauthMethod") ?? "Authorized")).ToList();
    }

    public SalesforceCliCommandResult StartWebLogin(string alias, bool sandbox)
    {
        if (string.IsNullOrWhiteSpace(alias) ||
            alias.Any(c => !char.IsLetterOrDigit(c) && c is not '-' and not '_'))
            return new(false, "Alias may contain letters, numbers, hyphens, and underscores only.");
        if (!File.Exists(Path.Combine(ProjectPath, "sfdx-project.json")))
            return new(false, "Salesforce DX project file was not found.");
        try
        {
            var instanceUrl = sandbox ? "https://test.salesforce.com" : "https://login.salesforce.com";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = FindExecutable(),
                    Arguments = $"org login web --alias {alias} --instance-url {instanceUrl} --set-default",
                    WorkingDirectory = ProjectPath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            return new(true, "The secure Salesforce sign-in page was opened. Refresh the org list after signing in.");
        }
        catch (Exception ex) { return new(false, $"Salesforce CLI could not be started: {ex.Message}"); }
    }

    public async Task<string?> GetOrgLoginUrlAsync(string alias, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(alias)) return null;
        var result = await RunAsync(["org", "open", "--target-org", alias, "--url-only", "--json"], ProjectPath, cancellationToken);
        if (!result.Success) return null;
        try
        {
            using var document = JsonDocument.Parse(result.Output);
            if (document.RootElement.TryGetProperty("result", out var res) && res.TryGetProperty("url", out var urlElement))
            {
                return urlElement.GetString();
            }
        }
        catch { }
        return null;
    }

    private async Task<(bool Success, string Output, string? Error)> RunAsync(
        IReadOnlyCollection<string> arguments, string? workingDirectory, CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = FindExecutable(),
                    WorkingDirectory = workingDirectory ?? AppContext.BaseDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            foreach (var argument in arguments) process.StartInfo.ArgumentList.Add(argument);
            process.Start();
            var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            return (process.ExitCode == 0, stdout, process.ExitCode == 0 ? null : stderr);
        }
        catch (Exception ex) { return (false, "", ex.Message); }
    }

    private string ResolveProjectPath()
    {
        var configured = configuration["SalesforceCli:ProjectPath"];
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "salesforce", "ReSalesforceCore");
            if (File.Exists(Path.Combine(candidate, "sfdx-project.json"))) return candidate;
            directory = directory.Parent;
        }
        return Path.Combine(AppContext.BaseDirectory, "salesforce", "ReSalesforceCore");
    }

    private static string FindExecutable() =>
        OperatingSystem.IsWindows() ? "sf.cmd" : "sf";
    private static string? Text(JsonElement x, string name) =>
        x.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static bool Bool(JsonElement x, string name) =>
        x.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False && value.GetBoolean();
}
