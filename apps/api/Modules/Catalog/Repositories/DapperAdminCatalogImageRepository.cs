using Dapper;
using LineCom.Api.Infrastructure.Database;
using LineCom.Api.Infrastructure.Storage;

namespace LineCom.Api.Modules.Catalog.Repositories;

public sealed class DapperAdminCatalogImageRepository : IAdminCatalogImageRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperAdminCatalogImageRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleAsync<bool>(
            new CommandDefinition(
                AdminCatalogImageSql.ProductExists,
                new { ProductId = productId },
                cancellationToken: cancellationToken));
    }

    public async Task<string?> GetProductNameAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition(
                AdminCatalogImageSql.GetProductName,
                new { ProductId = productId },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<AdminProductImageRecord>> GetProductImagesAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<AdminProductImageRecord>(
            new CommandDefinition(
                AdminCatalogImageSql.ListProductImages,
                new { ProductId = productId },
                cancellationToken: cancellationToken));

        return rows.ToArray();
    }

    public async Task<IReadOnlyList<AdminProductImageRecord>> AddProductImagesAsync(
        Guid productId,
        IReadOnlyList<LocalStoredFileDraft> files,
        string defaultAlt,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        IReadOnlyList<AdminProductImageRecord> images;

        try
        {
            var lockedProductId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                new CommandDefinition(
                    AdminCatalogImageSql.LockProductForImageUpdate,
                    new { ProductId = productId },
                    transaction,
                    cancellationToken: cancellationToken));
            if (lockedProductId is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Array.Empty<AdminProductImageRecord>();
            }

            foreach (var file in files)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    AdminCatalogImageSql.InsertStoredFile,
                    file,
                    transaction,
                    cancellationToken: cancellationToken));

                await connection.QuerySingleAsync<Guid>(new CommandDefinition(
                    AdminCatalogImageSql.InsertProductImage,
                    new { ProductId = productId, StoredFileId = file.Id, Alt = defaultAlt },
                    transaction,
                    cancellationToken: cancellationToken));
            }

            images = (await connection.QueryAsync<AdminProductImageRecord>(
                new CommandDefinition(
                    AdminCatalogImageSql.ListProductImages,
                    new { ProductId = productId },
                    transaction,
                    cancellationToken: cancellationToken))).ToArray();

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return images;
    }

    public async Task<AdminProductImageRecord?> UpdateProductImageAsync(
        Guid productId,
        Guid imageId,
        AdminProductImageMetadataUpdate command,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var updatedId = await connection.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(
                AdminCatalogImageSql.UpdateProductImage,
                new { ProductId = productId, ImageId = imageId, command.Alt, command.Title },
                cancellationToken: cancellationToken));
        if (updatedId is null)
        {
            return null;
        }

        return await GetProductImageAsync(productId, imageId, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminProductImageRecord>> UpdateProductImageOrderAsync(
        Guid productId,
        IReadOnlyList<Guid> imageIds,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var existingImageIds = (await connection.QueryAsync<Guid>(
                new CommandDefinition(
                    AdminCatalogImageSql.GetProductImageIds,
                    new { ProductId = productId },
                    transaction,
                    cancellationToken: cancellationToken))).ToArray();

            if (!HasSameIds(existingImageIds, imageIds))
            {
                throw new AdminProductImageOrderMismatchException();
            }

            for (var index = 0; index < imageIds.Count; index++)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    AdminCatalogImageSql.UpdateProductImageSortOrder,
                    new { ProductId = productId, ImageId = imageIds[index], SortOrder = (index + 1) * 10 },
                    transaction,
                    cancellationToken: cancellationToken));
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return await GetProductImagesAsync(productId, cancellationToken);
    }

    public async Task<AdminProductImageRecord?> SetMainProductImageAsync(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                AdminCatalogImageSql.ClearProductMainImages,
                new { ProductId = productId },
                transaction,
                cancellationToken: cancellationToken));

            var updatedId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                new CommandDefinition(
                    AdminCatalogImageSql.SetProductMainImage,
                    new { ProductId = productId, ImageId = imageId },
                    transaction,
                    cancellationToken: cancellationToken));
            if (updatedId is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return await GetProductImageAsync(productId, imageId, cancellationToken);
    }

    public async Task<bool> DeleteProductImageAsync(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var image = await connection.QuerySingleOrDefaultAsync<ProductImageForDelete?>(
                new CommandDefinition(
                    AdminCatalogImageSql.GetProductImageForDelete,
                    new { ProductId = productId, ImageId = imageId },
                    transaction,
                    cancellationToken: cancellationToken));
            if (image is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await connection.ExecuteAsync(new CommandDefinition(
                AdminCatalogImageSql.DeleteProductImage,
                new { ProductId = productId, ImageId = imageId },
                transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                AdminCatalogImageSql.MarkStoredFileDeletedIfUnreferenced,
                new { image.StoredFileId },
                transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                AdminCatalogImageSql.PromoteFirstRemainingProductImage,
                new { ProductId = productId },
                transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return true;
    }

    private async Task<AdminProductImageRecord?> GetProductImageAsync(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var images = await GetProductImagesAsync(productId, cancellationToken);

        return images.FirstOrDefault(image => image.Id == imageId);
    }

    private static bool HasSameIds(IReadOnlyList<Guid> existingImageIds, IReadOnlyList<Guid> submittedImageIds)
    {
        if (existingImageIds.Count != submittedImageIds.Count)
        {
            return false;
        }

        var existing = existingImageIds.ToHashSet();
        if (existing.Count != existingImageIds.Count)
        {
            return false;
        }

        var submitted = submittedImageIds.ToHashSet();
        return submitted.Count == submittedImageIds.Count && existing.SetEquals(submitted);
    }

    private sealed record ProductImageForDelete(Guid Id, Guid StoredFileId, bool IsMain);
}
