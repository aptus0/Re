using Re.Contracts.Auth;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Re.Desktop.Services;

/// <summary>
/// Re API ile iletişim kuran HTTP istemcisi.
/// Tüm VM'ler bu servis üzerinden API'ye erişir.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly ISessionService _session;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(HttpClient http, ISessionService session)
    {
        _http    = http;
        _session = session;
    }

    // ── Auth & Health ──────────────────────────────────────────────────

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var response = await _http.GetAsync("/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }


    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", request);
        if (!response.IsSuccessStatusCode) return null;

        var result = await response.Content.ReadFromJsonAsync<ApiResult<AuthResponse>>(JsonOptions);
        return result?.Data;
    }

    public async Task<AuthResponse?> RefreshTokenAsync(string refreshToken)
    {
        var response = await _http.PostAsJsonAsync("api/auth/refresh",
            new RefreshTokenRequest(refreshToken));
        if (!response.IsSuccessStatusCode) return null;

        var result = await response.Content.ReadFromJsonAsync<ApiResult<AuthResponse>>(JsonOptions);
        return result?.Data;
    }

    // ── Generic helpers ───────────────────────────────────────────────

    public async Task<T?> GetAsync<T>(string url)
    {
        SetAuthHeader();
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return default;
        var result = await response.Content.ReadFromJsonAsync<ApiResult<T>>(JsonOptions);
        return result is { Success: true } ? result.Data : default;
    }

    public async Task<T?> PostAsync<T>(string url, object body)
    {
        SetAuthHeader();
        var response = await _http.PostAsJsonAsync(url, body);
        if (!response.IsSuccessStatusCode) return default;
        var result = await response.Content.ReadFromJsonAsync<ApiResult<T>>(JsonOptions);
        return result is { Success: true } ? result.Data : default;
    }

    public async Task<T?> PutAsync<T>(string url, object body)
    {
        SetAuthHeader();
        var response = await _http.PutAsJsonAsync(url, body);
        if (!response.IsSuccessStatusCode) return default;
        var result = await response.Content.ReadFromJsonAsync<ApiResult<T>>(JsonOptions);
        return result is { Success: true } ? result.Data : default;
    }

    public async Task<bool> DeleteAsync(string url)
    {
        SetAuthHeader();
        var response = await _http.DeleteAsync(url);
        return response.IsSuccessStatusCode;
    }

    private void SetAuthHeader()
    {
        if (_session.AccessToken is { } token)
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
    }
}

/// <summary>
/// API standart yanıt wrapper (Contracts katmanındaki ApiResponse ile eşleşir).
/// </summary>
internal record ApiResult<T>(bool Success, T? Data, string? Message, List<string>? Errors);

