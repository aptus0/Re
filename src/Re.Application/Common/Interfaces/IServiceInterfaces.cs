namespace Re.Application.Common.Interfaces;

/// <summary>
/// Kimliği doğrulanmış mevcut kullanıcının bilgileri.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }
    Guid CompanyId { get; }
    Guid? BranchId { get; }
    string Username { get; }
    string Email { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permissionCode);
}

/// <summary>
/// UTC saat servisi – test edilebilirlik için soyutlama.
/// </summary>
public interface IDateTimeService
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}

/// <summary>
/// JWT token servisi.
/// </summary>
public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, Guid companyId, Guid? branchId,
        string username, string email, IEnumerable<string> permissions);
    string GenerateRefreshToken();
    Guid? ValidateAndGetUserId(string token);
}

/// <summary>
/// Şifre hashleme servisi.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string plainText);
    bool Verify(string plainText, string hash);
}

/// <summary>
/// ERP veritabanına erişim arayüzü (Application katmanından EF Core'u soyutlar).
/// </summary>
public interface IApplicationDbContext
{
    // Bu interface Persistence katmanında ReDbContext tarafından implement edilir.
    // Burada DbSet'ler değil Save metodu tanımlanır; Entity'lere erişim
    // için MediatR Handler'ları IApplicationDbContext yerine ReDbContext'i kullanır.
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

