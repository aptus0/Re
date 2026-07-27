namespace Re.Contracts.Products;

public record CatalogItemResponse(Guid Id, string Code, string Name, string? Detail, bool IsActive);
public record SaveCategoryRequest(string Code, string Name, string? Description, Guid? ParentCategoryId, bool IsActive = true);
public record SaveBrandRequest(string Code, string Name, string? LogoPath, bool IsActive = true);
public record SaveCollectionRequest(string Code, string Name, string? Season, DateTime? StartDate, DateTime? EndDate, bool IsActive = true);
