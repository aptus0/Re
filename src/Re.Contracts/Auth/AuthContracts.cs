namespace Re.Contracts.Auth;

public record LoginRequest(string Username, string Password, string? BranchCode = null, string? StoreCode = null, string? DeviceId = null);

public record RefreshTokenRequest(string RefreshToken);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfo User);

public record UserInfo(
    Guid Id,
    Guid CompanyId,
    Guid? BranchId,
    string Username,
    string Email,
    string FullName,
    List<string> Permissions);

