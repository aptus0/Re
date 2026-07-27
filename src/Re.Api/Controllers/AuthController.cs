using Re.Application.Common.Interfaces;
using Re.Contracts.Auth;
using Re.Contracts.Common;
using Re.Persistence.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Re.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ReDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;

    public AuthController(ReDbContext db, IPasswordHasher hasher, IJwtTokenService jwt)
    {
        _db = db; _hasher = hasher; _jwt = jwt;
    }

    /// <summary>
    /// Kullanıcı girişi – access + refresh token döner.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        var user = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u =>
                u.Username == request.Username.ToLowerInvariant() &&
                !u.IsDeleted && u.IsActive);

        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
        {
            if (user is not null) user.RecordFailedLogin();
            await _db.SaveChangesAsync();
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Kullanıcı adı veya şifre hatalı."));
        }

        if (user.IsLocked)
            return Unauthorized(ApiResponse<AuthResponse>.Fail(
                $"Hesabınız kilitli. {user.LockedUntil:HH:mm}'e kadar bekleyin."));

        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        if (user.IsSystemAdmin)
            permissions.Add("System.Admin");

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        user.RecordLogin(clientIp);

        var accessToken  = _jwt.GenerateAccessToken(user.Id, user.CompanyId, user.BranchId,
                               user.Username, user.Email, permissions);
        var refreshToken = _jwt.GenerateRefreshToken();

        _db.RefreshTokens.Add(new Domain.Entities.Identity.RefreshToken
        {
            Id          = Guid.NewGuid(),
            UserId      = user.Id,
            Token       = refreshToken,
            ExpiresAt   = DateTime.UtcNow.AddDays(30),
            CreatedByIp = clientIp,
            CreatedAt   = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var response = new AuthResponse(
            AccessToken:  accessToken,
            RefreshToken: refreshToken,
            ExpiresAt:    DateTime.UtcNow.AddHours(1),
            User: new UserInfo(
                user.Id, user.CompanyId, user.BranchId,
                user.Username, user.Email, user.FullName, permissions));

        return Ok(ApiResponse<AuthResponse>.Ok(response, "Giriş başarılı."));
    }

    /// <summary>
    /// Refresh token ile yeni access token al.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var token = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken && !t.IsRevoked);

        if (token is null || !token.IsActive)
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Geçersiz veya süresi dolmuş yenileme tokeni."));

        // Eski token'ı iptal et, yeni oluştur
        token.IsRevoked  = true;
        token.RevokedAt  = DateTime.UtcNow;
        token.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var newAccessToken  = _jwt.GenerateAccessToken(
            token.User.Id, token.User.CompanyId, token.User.BranchId,
            token.User.Username, token.User.Email, []);
        var newRefreshToken = _jwt.GenerateRefreshToken();

        _db.RefreshTokens.Add(new Domain.Entities.Identity.RefreshToken
        {
            Id        = Guid.NewGuid(),
            UserId    = token.User.Id,
            Token     = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var response = new AuthResponse(
            newAccessToken, newRefreshToken, DateTime.UtcNow.AddHours(1),
            new UserInfo(token.User.Id, token.User.CompanyId, token.User.BranchId,
                token.User.Username, token.User.Email, token.User.FullName, []));

        return Ok(ApiResponse<AuthResponse>.Ok(response));
    }

    /// <summary>
    /// Çıkış – refresh token iptal et.
    /// </summary>
    [HttpPost("logout")]
    public async Task<ActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (token is not null)
        {
            token.IsRevoked  = true;
            token.RevokedAt  = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return Ok(new { success = true, message = "Çıkış başarılı." });
    }
}

