using Re.Contracts.Auth;

namespace Re.Desktop.Services;

/// <summary>
/// Oturum bilgilerini (token, kullanıcı) tüm uygulama boyunca tutar.
/// </summary>
public interface ISessionService
{
    string? AccessToken { get; }
    string? RefreshToken { get; }
    UserInfo? CurrentUser { get; }
    bool IsAuthenticated { get; }

    void SetSession(AuthResponse response);
    void ClearSession();
    bool HasPermission(string permissionCode);
}

public class SessionService : ISessionService
{
    private AuthResponse? _session;

    public string?   AccessToken  => _session?.AccessToken;
    public string?   RefreshToken => _session?.RefreshToken;
    public UserInfo? CurrentUser  => _session?.User;
    public bool      IsAuthenticated => _session is not null
                                     && _session.ExpiresAt > DateTime.UtcNow;

    public void SetSession(AuthResponse response) => _session = response;

    public void ClearSession() => _session = null;

    public bool HasPermission(string permissionCode) =>
        _session?.User.Permissions.Contains(permissionCode) ?? false;
}

