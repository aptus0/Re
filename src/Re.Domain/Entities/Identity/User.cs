using Re.Domain.Entities.Common;

namespace Re.Domain.Entities.Identity;

/// <summary>
/// Sistem kullanıcısı. Şirketle ilişkili; birden fazla role sahip olabilir.
/// </summary>
public class User : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; private set; }     // null = tüm şubeler
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string? Phone { get; private set; }
    public string? AvatarPath { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsSystemAdmin { get; private set; } = false;
    public DateTime? LastLoginAt { get; private set; }
    public string? LastLoginIp { get; private set; }
    public int FailedLoginCount { get; private set; } = 0;
    public DateTime? LockedUntil { get; private set; }

    // Navigation
    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    private User() { }

    public static User Create(Guid companyId, string username, string email,
        string firstName, string lastName, string passwordHash, Guid? branchId = null)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username is required.");
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.");

        return new User
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            Username = username.Trim().ToLowerInvariant(),
            Email = email.Trim().ToLowerInvariant(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void RecordLogin(string ipAddress)
    {
        LastLoginAt = DateTime.UtcNow;
        LastLoginIp = ipAddress;
        FailedLoginCount = 0;
        LockedUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordFailedLogin()
    {
        FailedLoginCount++;
        if (FailedLoginCount >= 5)
            LockedUntil = DateTime.UtcNow.AddMinutes(30);
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsLocked => LockedUntil.HasValue && LockedUntil > DateTime.UtcNow;

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive(bool active)
    {
        IsActive = active;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Kullanıcının JWT refresh token'ları.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string? RevokedByIp { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? CreatedByIp { get; set; }

    public User User { get; set; } = null!;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}



