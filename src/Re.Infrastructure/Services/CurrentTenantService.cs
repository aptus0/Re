using Re.Application.Interfaces;

namespace Re.Infrastructure.Services;

public class CurrentTenantService : ICurrentTenantService
{
    public Guid? CompanyId { get; private set; }
    public Guid? BranchId { get; private set; }

    public void SetTenant(Guid companyId, Guid? branchId = null)
    {
        CompanyId = companyId;
        BranchId = branchId;
    }
}
