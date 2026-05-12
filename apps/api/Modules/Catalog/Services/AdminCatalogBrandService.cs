using System.Runtime.ExceptionServices;
using LineCom.Api.Infrastructure.Storage;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Repositories;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Services;

public sealed class AdminCatalogBrandService : IAdminCatalogBrandService
{
    private const string BrandInUseMessage = "\u0411\u0440\u0435\u043d\u0434 \u0438\u0441\u043f\u043e\u043b\u044c\u0437\u0443\u0435\u0442\u0441\u044f \u0442\u043e\u0432\u0430\u0440\u0430\u043c\u0438.";
    private const string BrandLogoPurpose = "brand_logo";

    private readonly IAdminCatalogStaffGuard _staffGuard;
    private readonly IAdminCatalogBrandRepository _repository;
    private readonly ILocalStoredFileWriter _fileWriter;

    public AdminCatalogBrandService(
        IAdminCatalogStaffGuard staffGuard,
        IAdminCatalogBrandRepository repository,
        ILocalStoredFileWriter fileWriter)
    {
        _staffGuard = staffGuard;
        _repository = repository;
        _fileWriter = fileWriter;
    }

    public async Task<AdminBrandListResponse> GetBrandsAsync(
        HttpContext httpContext,
        AdminBrandListQuery query,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        var page = AdminCatalogInput.NormalizePage(query.Page);
        var pageSize = AdminCatalogInput.NormalizePageSize(query.PageSize);
        var result = await _repository.GetBrandsAsync(
            new AdminBrandReadListQuery(
                page,
                pageSize,
                AdminCatalogInput.NormalizeText(query.Search),
                query.IsActive),
            cancellationToken);
        var totalPages = result.TotalItems == 0
            ? 0
            : (int)Math.Ceiling(result.TotalItems / (double)pageSize);

        return new AdminBrandListResponse(
            result.Items.Select(ToListItemDto).ToArray(),
            page,
            pageSize,
            result.TotalItems,
            totalPages);
    }

    public async Task<AdminBrandDetailDto> GetBrandAsync(
        HttpContext httpContext,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        var record = await _repository.GetBrandAsync(id, cancellationToken);
        if (record is null)
        {
            throw AdminCatalogErrors.BrandNotFound();
        }

        return ToDetailDto(record);
    }

    public async Task<AdminBrandDetailDto> CreateBrandAsync(
        HttpContext httpContext,
        UpsertAdminBrandCommand command,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        try
        {
            var record = await _repository.CreateBrandAsync(ToUpsert(command), cancellationToken);

            return ToDetailDto(record);
        }
        catch (AdminBrandSlugAlreadyExistsException)
        {
            throw AdminCatalogErrors.SlugAlreadyExists();
        }
        catch (InvalidAdminBrandLogoException)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }
    }

    public async Task<AdminBrandDetailDto> UpdateBrandAsync(
        HttpContext httpContext,
        Guid id,
        UpsertAdminBrandCommand command,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        AdminBrandRecord? record;
        try
        {
            record = await _repository.UpdateBrandAsync(id, ToUpsert(command), cancellationToken);
        }
        catch (AdminBrandSlugAlreadyExistsException)
        {
            throw AdminCatalogErrors.SlugAlreadyExists();
        }
        catch (InvalidAdminBrandLogoException)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }

        if (record is null)
        {
            throw AdminCatalogErrors.BrandNotFound();
        }

        return ToDetailDto(record);
    }

    public async Task<AdminBrandDetailDto> QuickCreateBrandAsync(
        HttpContext httpContext,
        QuickCreateAdminBrandCommand command,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        try
        {
            var record = await _repository.QuickCreateBrandAsync(
                new AdminBrandQuickCreate(
                    AdminCatalogInput.RequireText(command.Name),
                    CreateFallbackSlug()),
                cancellationToken);

            return ToDetailDto(record);
        }
        catch (AdminBrandSlugAlreadyExistsException)
        {
            throw AdminCatalogErrors.SlugAlreadyExists();
        }
    }

    public async Task DeleteBrandAsync(
        HttpContext httpContext,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        var record = await _repository.GetBrandAsync(id, cancellationToken);
        if (record is null)
        {
            throw AdminCatalogErrors.BrandNotFound();
        }

        if (record.ProductsCount > 0)
        {
            throw AdminCatalogErrors.EntityInUse(BrandInUseMessage);
        }

        bool deleted;
        try
        {
            deleted = await _repository.DeleteBrandAsync(id, cancellationToken);
        }
        catch (AdminBrandInUseException)
        {
            throw AdminCatalogErrors.EntityInUse(BrandInUseMessage);
        }

        if (!deleted)
        {
            var latestRecord = await _repository.GetBrandAsync(id, cancellationToken);
            if (latestRecord?.ProductsCount > 0)
            {
                throw AdminCatalogErrors.EntityInUse(BrandInUseMessage);
            }

            throw AdminCatalogErrors.BrandNotFound();
        }
    }

    public async Task<AdminBrandLogoDto> UploadLogoAsync(
        HttpContext httpContext,
        Guid brandId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var user = await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        if (await _repository.GetBrandAsync(brandId, cancellationToken) is null)
        {
            throw AdminCatalogErrors.BrandNotFound();
        }

        LocalStoredFileDraft draft;
        try
        {
            draft = await _fileWriter.SaveAsync(
                file,
                Guid.NewGuid(),
                BrandLogoPurpose,
                $"brands/admin/{brandId:N}",
                user.Id,
                cancellationToken);
        }
        catch (InvalidLocalStoredFileException exception)
        {
            throw MapInvalidStoredFileException(exception);
        }

        AdminBrandLogoRecord? logo;
        try
        {
            logo = await _repository.UpdateBrandLogoAsync(brandId, draft, cancellationToken);
        }
        catch (Exception exception)
        {
            await DeleteDraftBestEffortAsync(draft);

            if (exception is ApiException)
            {
                throw;
            }

            ExceptionDispatchInfo.Capture(exception).Throw();
            throw;
        }

        if (logo is null)
        {
            await DeleteDraftBestEffortAsync(draft);
            throw AdminCatalogErrors.BrandNotFound();
        }

        return ToLogoDto(logo);
    }

    public async Task DeleteLogoAsync(
        HttpContext httpContext,
        Guid brandId,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        if (!await _repository.DeleteBrandLogoAsync(brandId, cancellationToken))
        {
            throw AdminCatalogErrors.BrandNotFound();
        }
    }

    private static AdminBrandUpsert ToUpsert(UpsertAdminBrandCommand command)
    {
        return new AdminBrandUpsert(
            AdminCatalogInput.RequireText(command.Name),
            AdminCatalogInput.RequireText(command.Slug),
            AdminCatalogInput.NormalizeText(command.Description),
            AdminCatalogInput.NormalizeText(command.SeoTitle),
            AdminCatalogInput.NormalizeText(command.SeoDescription),
            command.LogoFileId,
            command.IsActive ?? true);
    }

    private static string CreateFallbackSlug()
    {
        return $"brand-{Guid.NewGuid():N}";
    }

    private static AdminBrandListItemDto ToListItemDto(AdminBrandRecord record)
    {
        return new AdminBrandListItemDto(
            record.Id,
            record.Name,
            record.Slug,
            record.IsActive,
            record.ProductsCount);
    }

    private static AdminBrandDetailDto ToDetailDto(AdminBrandRecord record)
    {
        return new AdminBrandDetailDto(
            record.Id,
            record.Name,
            record.Slug,
            record.Description,
            record.SeoTitle,
            record.SeoDescription,
            record.LogoFileId,
            record.IsActive,
            record.ProductsCount);
    }

    private async Task DeleteDraftBestEffortAsync(LocalStoredFileDraft draft)
    {
        try
        {
            await _fileWriter.DeletePhysicalFileIfExistsAsync(
                draft.StorageKey,
                CancellationToken.None);
        }
        catch
        {
            // Best-effort cleanup must not mask the original upload error.
        }
    }

    private static ApiException MapInvalidStoredFileException(InvalidLocalStoredFileException exception)
    {
        return exception.Message.Contains("size", StringComparison.OrdinalIgnoreCase)
            ? AdminCatalogErrors.ImageTooLarge()
            : AdminCatalogErrors.InvalidImageType();
    }

    private static AdminBrandLogoDto ToLogoDto(AdminBrandLogoRecord record)
    {
        return new AdminBrandLogoDto(
            record.StoredFileId,
            record.Url,
            record.OriginalFileName,
            record.ContentType,
            record.SizeBytes,
            record.Checksum);
    }
}
