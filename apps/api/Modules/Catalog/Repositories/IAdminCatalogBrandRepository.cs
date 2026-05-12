using LineCom.Api.Infrastructure.Storage;

namespace LineCom.Api.Modules.Catalog.Repositories;

public sealed record AdminBrandReadListQuery(int Page, int PageSize, string? Search, bool? IsActive);

public sealed record AdminBrandListRecordResponse(IReadOnlyList<AdminBrandRecord> Items, int TotalItems);

public sealed record AdminBrandRecord(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? SeoTitle,
    string? SeoDescription,
    Guid? LogoFileId,
    bool IsActive,
    int ProductsCount);

public sealed record AdminBrandUpsert(
    string Name,
    string Slug,
    string? Description,
    string? SeoTitle,
    string? SeoDescription,
    Guid? LogoFileId,
    bool IsActive);

public sealed record AdminBrandQuickCreate(string Name, string Slug);

public sealed record AdminBrandLogoRecord(
    Guid StoredFileId,
    string Url,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Checksum);

internal sealed class AdminBrandSlugAlreadyExistsException : Exception
{
    public AdminBrandSlugAlreadyExistsException(Exception? innerException = null)
        : base("Brand slug already exists.", innerException)
    {
    }
}

internal sealed class InvalidAdminBrandLogoException : Exception
{
    public InvalidAdminBrandLogoException(Exception? innerException = null)
        : base("Brand logo is invalid.", innerException)
    {
    }
}

internal sealed class AdminBrandInUseException : Exception
{
    public AdminBrandInUseException(Exception? innerException = null)
        : base("Brand is in use.", innerException)
    {
    }
}

public interface IAdminCatalogBrandRepository
{
    Task<AdminBrandListRecordResponse> GetBrandsAsync(AdminBrandReadListQuery query, CancellationToken cancellationToken = default);

    Task<AdminBrandRecord?> GetBrandAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AdminBrandRecord> CreateBrandAsync(AdminBrandUpsert command, CancellationToken cancellationToken = default);

    Task<AdminBrandRecord?> UpdateBrandAsync(Guid id, AdminBrandUpsert command, CancellationToken cancellationToken = default);

    Task<AdminBrandRecord> QuickCreateBrandAsync(AdminBrandQuickCreate command, CancellationToken cancellationToken = default);

    Task<bool> DeleteBrandAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AdminBrandLogoRecord?> UpdateBrandLogoAsync(
        Guid brandId,
        LocalStoredFileDraft file,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteBrandLogoAsync(Guid brandId, CancellationToken cancellationToken = default);
}
