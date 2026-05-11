namespace LineCom.Api.Modules.Catalog.Repositories;

public sealed record AdminCategoryReadListQuery(int Page, int PageSize, Guid? ParentId, string? Search, bool? IsActive);

public sealed record AdminCategoryListRecordResponse(IReadOnlyList<AdminCategoryRecord> Items, int TotalItems);

public sealed record AdminCategoryRecord(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Slug,
    string? Description,
    string? SeoTitle,
    string? SeoDescription,
    string? H1,
    int SortOrder,
    bool IsActive,
    bool IsVisibleInMenu,
    int ProductsCount,
    int ChildrenCount);

public sealed record AdminCategoryUpsert(
    Guid? ParentId,
    string Name,
    string Slug,
    string? Description,
    string? SeoTitle,
    string? SeoDescription,
    string? H1,
    int SortOrder,
    bool IsActive,
    bool IsVisibleInMenu);

internal sealed class AdminCategorySlugAlreadyExistsException : Exception
{
    public AdminCategorySlugAlreadyExistsException(Exception? innerException = null)
        : base("Category slug already exists.", innerException)
    {
    }
}

internal sealed class InvalidAdminCategoryParentException : Exception
{
    public InvalidAdminCategoryParentException(Exception? innerException = null)
        : base("Category parent is invalid.", innerException)
    {
    }
}

internal sealed class AdminCategoryInUseException : Exception
{
    public AdminCategoryInUseException(Exception? innerException = null)
        : base("Category is in use.", innerException)
    {
    }
}

public interface IAdminCatalogCategoryRepository
{
    Task<AdminCategoryListRecordResponse> GetCategoriesAsync(AdminCategoryReadListQuery query, CancellationToken cancellationToken = default);

    Task<AdminCategoryRecord?> GetCategoryAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AdminCategoryRecord> CreateCategoryAsync(AdminCategoryUpsert command, CancellationToken cancellationToken = default);

    Task<AdminCategoryRecord?> UpdateCategoryAsync(Guid id, AdminCategoryUpsert command, CancellationToken cancellationToken = default);

    Task<AdminCategoryRecord?> MoveCategoryAsync(Guid id, Guid? parentId, CancellationToken cancellationToken = default);

    Task<AdminCategoryRecord?> SortCategoryAsync(Guid id, int sortOrder, CancellationToken cancellationToken = default);

    Task<int> CountCategoryUsageAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);
}
