namespace Re.Application.Interfaces;

public interface ICurrentTenantService
{
    Guid? CompanyId { get; }
    Guid? BranchId { get; }
    void SetTenant(Guid companyId, Guid? branchId = null);
}
