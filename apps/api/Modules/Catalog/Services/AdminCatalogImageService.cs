using System.Runtime.ExceptionServices;
using LineCom.Api.Infrastructure.Storage;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Repositories;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Services;

public sealed class AdminCatalogImageService : IAdminCatalogImageService
{
    private const string ProductImagePurpose = "product_image";

    private readonly IAdminCatalogStaffGuard _staffGuard;
    private readonly IAdminCatalogImageRepository _repository;
    private readonly ILocalStoredFileWriter _fileWriter;

    public AdminCatalogImageService(
        IAdminCatalogStaffGuard staffGuard,
        IAdminCatalogImageRepository repository,
        ILocalStoredFileWriter fileWriter)
    {
        _staffGuard = staffGuard;
        _repository = repository;
        _fileWriter = fileWriter;
    }

    public async Task<AdminProductImagesResponse> GetProductImagesAsync(
        HttpContext httpContext,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        if (!await _repository.ProductExistsAsync(productId, cancellationToken))
        {
            throw AdminCatalogErrors.ProductNotFound();
        }

        var images = await _repository.GetProductImagesAsync(productId, cancellationToken);
        return ToResponse(images);
    }

    public async Task<AdminProductImagesResponse> UploadProductImagesAsync(
        HttpContext httpContext,
        Guid productId,
        IReadOnlyList<IFormFile> files,
        CancellationToken cancellationToken = default)
    {
        var user = await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        if (files is null || files.Count == 0)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }

        var productName = await _repository.GetProductNameAsync(productId, cancellationToken);
        if (productName is null)
        {
            throw AdminCatalogErrors.ProductNotFound();
        }

        var drafts = new List<LocalStoredFileDraft>(files.Count);
        try
        {
            foreach (var file in files)
            {
                var draft = await _fileWriter.SaveAsync(
                    file,
                    Guid.NewGuid(),
                    ProductImagePurpose,
                    $"products/admin/{productId:N}",
                    user.Id,
                    cancellationToken);
                drafts.Add(draft);
            }
        }
        catch (InvalidLocalStoredFileException exception)
        {
            await DeleteDraftsBestEffortAsync(drafts);
            throw MapInvalidStoredFileException(exception);
        }

        IReadOnlyList<AdminProductImageRecord> images;
        try
        {
            images = await _repository.AddProductImagesAsync(
                productId,
                drafts,
                productName,
                cancellationToken);
        }
        catch (Exception exception)
        {
            await DeleteDraftsBestEffortAsync(drafts);

            if (exception is ApiException)
            {
                throw;
            }

            var mappedException = MapRepositoryException(exception);
            if (mappedException is not null)
            {
                throw mappedException;
            }

            ExceptionDispatchInfo.Capture(exception).Throw();
            throw;
        }

        if (images.Count == 0 && drafts.Count > 0)
        {
            await DeleteDraftsBestEffortAsync(drafts);
            throw AdminCatalogErrors.ProductNotFound();
        }

        return ToResponse(images);
    }

    public async Task<AdminProductImageDto> UpdateProductImageAsync(
        HttpContext httpContext,
        Guid productId,
        Guid imageId,
        UpdateAdminProductImageCommand command,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        if (command is null)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }

        var update = new AdminProductImageMetadataUpdate(
            AdminCatalogInput.RequireText(command.Alt),
            AdminCatalogInput.NormalizeText(command.Title));
        var image = await _repository.UpdateProductImageAsync(productId, imageId, update, cancellationToken);
        if (image is null)
        {
            throw AdminCatalogErrors.ImageNotFound();
        }

        return ToDto(image);
    }

    public async Task<AdminProductImagesResponse> UpdateProductImageOrderAsync(
        HttpContext httpContext,
        Guid productId,
        UpdateAdminProductImageOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        if (command is null || command.ImageIds is null || command.ImageIds.Count == 0)
        {
            throw AdminCatalogErrors.InvalidRequest();
        }

        try
        {
            var images = await _repository.UpdateProductImageOrderAsync(
                productId,
                command.ImageIds,
                cancellationToken);
            return ToResponse(images);
        }
        catch (AdminProductImageOrderMismatchException)
        {
            throw AdminCatalogErrors.ImageOrderMismatch();
        }
    }

    public async Task<AdminProductImageDto> SetMainProductImageAsync(
        HttpContext httpContext,
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        var image = await _repository.SetMainProductImageAsync(productId, imageId, cancellationToken);
        if (image is null)
        {
            throw AdminCatalogErrors.ImageNotFound();
        }

        return ToDto(image);
    }

    public async Task DeleteProductImageAsync(
        HttpContext httpContext,
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);

        if (!await _repository.DeleteProductImageAsync(productId, imageId, cancellationToken))
        {
            throw AdminCatalogErrors.ImageNotFound();
        }
    }

    private async Task DeleteDraftsBestEffortAsync(IReadOnlyList<LocalStoredFileDraft> drafts)
    {
        foreach (var draft in drafts)
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
    }

    private static ApiException MapInvalidStoredFileException(InvalidLocalStoredFileException exception)
    {
        return exception.Message.Contains("size", StringComparison.OrdinalIgnoreCase)
            ? AdminCatalogErrors.ImageTooLarge()
            : AdminCatalogErrors.InvalidImageType();
    }

    private static ApiException? MapRepositoryException(Exception exception)
    {
        return exception switch
        {
            AdminProductImageOrderMismatchException => AdminCatalogErrors.ImageOrderMismatch(),
            AdminProductImageNotFoundException => AdminCatalogErrors.ImageNotFound(),
            _ => null
        };
    }

    private static AdminProductImagesResponse ToResponse(IReadOnlyList<AdminProductImageRecord> records)
    {
        return new AdminProductImagesResponse(records.Select(ToDto).ToArray());
    }

    private static AdminProductImageDto ToDto(AdminProductImageRecord record)
    {
        return new AdminProductImageDto(
            record.Id,
            record.StoredFileId,
            record.Url,
            record.OriginalFileName,
            record.ContentType,
            record.SizeBytes,
            record.Checksum,
            record.Alt,
            record.Title,
            record.SortOrder,
            record.IsMain,
            ToUtcDateTimeOffset(record.CreatedAt));
    }

    private static DateTimeOffset ToUtcDateTimeOffset(DateTime value)
    {
        var utcValue = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return new DateTimeOffset(utcValue);
    }
}
