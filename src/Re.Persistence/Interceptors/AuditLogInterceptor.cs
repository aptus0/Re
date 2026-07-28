using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Re.Application.Common.Interfaces;

namespace Re.Persistence.Interceptors;

public sealed class AuditLogInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentTenantService _tenantService;

    public AuditLogInterceptor(ICurrentTenantService tenantService)
    {
        _tenantService = tenantService;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var now = DateTime.UtcNow;
        var userId = _tenantService.UserId;

        foreach (var entry in eventData.Context.ChangeTracker.Entries<Re.Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                if (userId.HasValue && userId.Value != Guid.Empty)
                {
                    entry.Entity.CreatedBy = userId.Value;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                if (userId.HasValue && userId.Value != Guid.Empty)
                {
                    entry.Entity.UpdatedBy = userId.Value;
                }
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
