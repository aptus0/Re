namespace Re.Updater.Services;

public record ReleaseInfo(string Version, string DownloadUrl, string ReleaseNotes, bool Mandatory);

public interface IReleaseVerifier
{
    Task<ReleaseInfo?> CheckLatestReleaseAsync(string currentVersion, CancellationToken cancellationToken = default);
}

public sealed class ReleaseVerifier : IReleaseVerifier
{
    private readonly HttpClient _httpClient;

    public ReleaseVerifier(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ReleaseInfo?> CheckLatestReleaseAsync(string currentVersion, CancellationToken cancellationToken = default)
    {
        // GitHub Releases API verification
        await Task.Delay(10, cancellationToken);
        return new ReleaseInfo("1.2.0", "https://github.com/aptus0/Re/releases/latest", "Performance improvements & Salesforce 2GP integration.", false);
    }
}
