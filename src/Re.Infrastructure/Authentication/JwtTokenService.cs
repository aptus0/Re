using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Re.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Re.Infrastructure.Authentication;

/// <summary>
/// JWT erişim ve yenileme token'ı oluşturma/doğrulama servisi.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
        => _configuration = configuration;

    public string GenerateAccessToken(Guid userId, Guid companyId, Guid? branchId,
        string username, string email, IEnumerable<string> permissions)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new("companyId", companyId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (branchId.HasValue)
            claims.Add(new("branchId", branchId.Value.ToString()));

        foreach (var permission in permissions)
            claims.Add(new("permission", permission));

        var expireMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public Guid? ValidateAndGetUserId(string token)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            handler.ValidateToken(token, parameters, out var validatedToken);
            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
            return Guid.Parse(userId);
        }
        catch
        {
            return null;
        }
    }
}

