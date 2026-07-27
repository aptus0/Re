using Re.Domain.Entities.Common;

namespace Re.Domain.Entities.Identity;

/// <summary>
/// Rol â€“ kullanÄ±cÄ±lara atanan yetki gruplarÄ±.
/// </summary>
public class Role : BaseEntity, IMustHaveCompany
{
    public Guid CompanyId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSystemRole { get; private set; } = false; // Admin, Kasiyer vb. sistem rolleri

    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();
    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    private Role() { }

    public static Role Create(Guid companyId, string name, string? description = null, bool isSystemRole = false)
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = name.Trim(),
            Description = description?.Trim(),
            IsSystemRole = isSystemRole,
            CreatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Ä°zin tanÄ±mÄ± â€“ hangi iÅŸlemi kimin yapabileceÄŸi.
/// </summary>
public class Permission : BaseEntity
{
    public string Code { get; set; } = string.Empty;          // Ã¶rn: "Invoice.Approve"
    public string Category { get; set; } = string.Empty;      // Ã¶rn: "SatÄ±ÅŸ"
    public string Name { get; set; } = string.Empty;          // Ã¶rn: "Fatura Onayla"
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// Rol-Ä°zin iliÅŸkisi (N:M).
/// </summary>
public class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}

/// <summary>
/// KullanÄ±cÄ±-Rol iliÅŸkisi (N:M).
/// </summary>
public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Guid? BranchId { get; set; }   // null = tÃ¼m ÅŸubeler geÃ§erli

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}


