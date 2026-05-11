using LineCom.Api.Infrastructure.Storage;
using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Repositories;
using LineCom.Api.Modules.Catalog.Services;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminCatalogImageServiceTests
{
    private static readonly Guid ProductId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ImageId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid StoredFileId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task UploadProductImagesAsync_UsesProductNameAsDefaultAltAndStoresFilesUnderProductDirectory()
    {
        var repository = new CapturingAdminCatalogImageRepository { ProductName = "Cable UTP" };
        var writer = new CapturingLocalStoredFileWriter();
        var service = CreateService(repository, writer);

        await service.UploadProductImagesAsync(
            new DefaultHttpContext(),
            ProductId,
            [FormFile("cable.jpg", "image/jpeg")],
            CancellationToken.None);

        Assert.Equal("products/admin/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", writer.LastStorageDirectory);
        Assert.Equal("product_image", writer.LastPurpose);
        Assert.Equal(UserId, writer.CreatedByUserId);
        Assert.Equal("Cable UTP", repository.LastDefaultAlt);
    }

    [Fact]
    public async Task UploadProductImagesAsync_ProductMissing_ThrowsProductNotFound()
    {
        var repository = new CapturingAdminCatalogImageRepository { ProductName = null };
        var service = CreateService(repository, new CapturingLocalStoredFileWriter());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UploadProductImagesAsync(
                new DefaultHttpContext(),
                ProductId,
                [FormFile("cable.jpg", "image/jpeg")],
                CancellationToken.None));

        Assert.Equal("admin_catalog.product_not_found", exception.Code);
    }

    [Fact]
    public async Task UpdateProductImageOrderAsync_OrderMismatch_ThrowsImageOrderMismatch()
    {
        var repository = new CapturingAdminCatalogImageRepository
        {
            OrderException = new AdminProductImageOrderMismatchException()
        };
        var service = CreateService(repository, new CapturingLocalStoredFileWriter());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UpdateProductImageOrderAsync(
                new DefaultHttpContext(),
                ProductId,
                new UpdateAdminProductImageOrderCommand([ImageId]),
                CancellationToken.None));

        Assert.Equal("admin_catalog.image_order_mismatch", exception.Code);
    }

    [Fact]
    public async Task UploadProductImagesAsync_RepositoryFailureAfterDraftWrite_DeletesWrittenDraft()
    {
        var repository = new CapturingAdminCatalogImageRepository
        {
            AddException = new InvalidOperationException("database failed")
        };
        var writer = new CapturingLocalStoredFileWriter();
        var service = CreateService(repository, writer);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadProductImagesAsync(
                new DefaultHttpContext(),
                ProductId,
                [FormFile("cable.jpg", "image/jpeg")],
                CancellationToken.None));

        Assert.Equal("storage/products/admin/captured-1.jpg", Assert.Single(writer.DeletedStorageKeys));
    }

    [Fact]
    public async Task UploadProductImagesAsync_WriterFailureAfterDrafts_CleansWrittenDraftsAndMapsError()
    {
        var writer = new CapturingLocalStoredFileWriter
        {
            ThrowOnSaveCallNumber = 2,
            SaveException = new InvalidLocalStoredFileException("Invalid image content type.")
        };
        var service = CreateService(new CapturingAdminCatalogImageRepository(), writer);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UploadProductImagesAsync(
                new DefaultHttpContext(),
                ProductId,
                [
                    FormFile("first.jpg", "image/jpeg"),
                    FormFile("second.txt", "text/plain")
                ],
                CancellationToken.None));

        Assert.Equal("admin_catalog.invalid_image_type", exception.Code);
        Assert.Equal("storage/products/admin/captured-1.jpg", Assert.Single(writer.DeletedStorageKeys));
    }

    [Fact]
    public async Task UploadProductImagesAsync_ProductDisappearsDuringRepositoryAdd_CleansDraftsAndThrowsProductNotFound()
    {
        var repository = new CapturingAdminCatalogImageRepository
        {
            AddReturnsEmpty = true
        };
        var writer = new CapturingLocalStoredFileWriter();
        var service = CreateService(repository, writer);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UploadProductImagesAsync(
                new DefaultHttpContext(),
                ProductId,
                [FormFile("cable.jpg", "image/jpeg")],
                CancellationToken.None));

        Assert.Equal("admin_catalog.product_not_found", exception.Code);
        Assert.Equal("storage/products/admin/captured-1.jpg", Assert.Single(writer.DeletedStorageKeys));
    }

    [Fact]
    public async Task UpdateProductImageAsync_NullCommand_ThrowsInvalidRequest()
    {
        var service = CreateService(new CapturingAdminCatalogImageRepository(), new CapturingLocalStoredFileWriter());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UpdateProductImageAsync(
                new DefaultHttpContext(),
                ProductId,
                ImageId,
                null!,
                CancellationToken.None));

        Assert.Equal("admin_catalog.invalid_request", exception.Code);
    }

    [Fact]
    public async Task UpdateProductImageOrderAsync_NullCommand_ThrowsInvalidRequest()
    {
        var service = CreateService(new CapturingAdminCatalogImageRepository(), new CapturingLocalStoredFileWriter());

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UpdateProductImageOrderAsync(
                new DefaultHttpContext(),
                ProductId,
                null!,
                CancellationToken.None));

        Assert.Equal("admin_catalog.invalid_request", exception.Code);
    }

    [Fact]
    public async Task UploadProductImagesAsync_CleanupFailure_DoesNotMaskRepositoryExceptionAndAttemptsRemainingDeletes()
    {
        var repositoryException = new InvalidOperationException("database failed");
        var repository = new CapturingAdminCatalogImageRepository
        {
            AddException = repositoryException
        };
        var writer = new CapturingLocalStoredFileWriter
        {
            ThrowOnDeleteCallNumber = 1
        };
        var service = CreateService(repository, writer);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadProductImagesAsync(
                new DefaultHttpContext(),
                ProductId,
                [
                    FormFile("first.jpg", "image/jpeg"),
                    FormFile("second.jpg", "image/jpeg")
                ],
                CancellationToken.None));

        Assert.Same(repositoryException, exception);
        Assert.Equal(
            [
                "storage/products/admin/captured-1.jpg",
                "storage/products/admin/captured-2.jpg"
            ],
            writer.DeletedStorageKeys);
    }

    [Fact]
    public async Task UploadProductImagesAsync_WriterSizeFailure_ThrowsImageTooLarge()
    {
        var writer = new CapturingLocalStoredFileWriter
        {
            SaveException = new InvalidLocalStoredFileException("Invalid image size.")
        };
        var service = CreateService(new CapturingAdminCatalogImageRepository(), writer);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UploadProductImagesAsync(
                new DefaultHttpContext(),
                ProductId,
                [FormFile("cable.jpg", "image/jpeg")],
                CancellationToken.None));

        Assert.Equal("admin_catalog.image_too_large", exception.Code);
    }

    [Fact]
    public async Task UploadProductImagesAsync_WriterTypeFailure_ThrowsInvalidImageType()
    {
        var writer = new CapturingLocalStoredFileWriter
        {
            SaveException = new InvalidLocalStoredFileException("Invalid image content type.")
        };
        var service = CreateService(new CapturingAdminCatalogImageRepository(), writer);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UploadProductImagesAsync(
                new DefaultHttpContext(),
                ProductId,
                [FormFile("cable.jpg", "image/jpeg")],
                CancellationToken.None));

        Assert.Equal("admin_catalog.invalid_image_type", exception.Code);
    }

    private static AdminCatalogImageService CreateService(
        CapturingAdminCatalogImageRepository repository,
        CapturingLocalStoredFileWriter writer)
    {
        return new AdminCatalogImageService(
            new RoleAdminCatalogStaffGuard("seller"),
            repository,
            writer);
    }

    private static IFormFile FormFile(string fileName, string contentType)
    {
        var bytes = "image-bytes"u8.ToArray();
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static AdminProductImageRecord ImageRecord()
    {
        return new AdminProductImageRecord(
            ImageId,
            StoredFileId,
            "/storage/products/admin/captured.jpg",
            "cable.jpg",
            "image/jpeg",
            11,
            "checksum",
            "Cable UTP",
            "Title",
            10,
            true,
            DateTimeOffset.Parse("2026-05-11T10:00:00Z"));
    }

    private sealed class RoleAdminCatalogStaffGuard : IAdminCatalogStaffGuard
    {
        private readonly string _role;

        public RoleAdminCatalogStaffGuard(string role)
        {
            _role = role;
        }

        public Task<CurrentUserDto> RequireStaffAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken = default)
        {
            if (_role is not ("seller" or "admin"))
            {
                throw AuthErrors.Forbidden();
            }

            return Task.FromResult(new CurrentUserDto(
                UserId,
                "Staff User",
                "staff@example.com",
                null,
                _role));
        }
    }

    private sealed class CapturingLocalStoredFileWriter : ILocalStoredFileWriter
    {
        private int _deleteCallCount;
        private int _saveCallCount;

        public string? LastStorageDirectory { get; private set; }
        public string? LastPurpose { get; private set; }
        public Guid? CreatedByUserId { get; private set; }
        public Exception? SaveException { get; init; }
        public int? ThrowOnSaveCallNumber { get; init; }
        public int? ThrowOnDeleteCallNumber { get; init; }
        public List<string> DeletedStorageKeys { get; } = [];

        public Task<LocalStoredFileDraft> SaveAsync(
            IFormFile file,
            Guid fileId,
            string purpose,
            string storageDirectory,
            Guid createdByUserId,
            CancellationToken cancellationToken = default)
        {
            LastPurpose = purpose;
            LastStorageDirectory = storageDirectory;
            CreatedByUserId = createdByUserId;
            _saveCallCount++;

            if (SaveException is not null
                && (ThrowOnSaveCallNumber is null || ThrowOnSaveCallNumber == _saveCallCount))
            {
                throw SaveException;
            }

            return Task.FromResult(new LocalStoredFileDraft(
                fileId,
                $"storage/products/admin/captured-{_saveCallCount}.jpg",
                file.FileName,
                file.ContentType,
                file.Length,
                "checksum",
                purpose,
                createdByUserId));
        }

        public Task DeletePhysicalFileIfExistsAsync(
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            DeletedStorageKeys.Add(storageKey);
            _deleteCallCount++;
            if (ThrowOnDeleteCallNumber == _deleteCallCount)
            {
                throw new IOException("cleanup failed");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class CapturingAdminCatalogImageRepository : IAdminCatalogImageRepository
    {
        public string? LastDefaultAlt { get; private set; }
        public string? ProductName { get; init; } = "Cable";
        public Exception? AddException { get; init; }
        public Exception? OrderException { get; init; }
        public bool AddReturnsEmpty { get; init; }

        public Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ProductName is not null);
        }

        public Task<string?> GetProductNameAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ProductName);
        }

        public Task<IReadOnlyList<AdminProductImageRecord>> GetProductImagesAsync(
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AdminProductImageRecord>>([ImageRecord()]);
        }

        public Task<IReadOnlyList<AdminProductImageRecord>> AddProductImagesAsync(
            Guid productId,
            IReadOnlyList<LocalStoredFileDraft> files,
            string defaultAlt,
            CancellationToken cancellationToken = default)
        {
            LastDefaultAlt = defaultAlt;
            if (AddException is not null)
            {
                throw AddException;
            }

            if (AddReturnsEmpty)
            {
                return Task.FromResult<IReadOnlyList<AdminProductImageRecord>>([]);
            }

            return Task.FromResult<IReadOnlyList<AdminProductImageRecord>>([ImageRecord()]);
        }

        public Task<AdminProductImageRecord?> UpdateProductImageAsync(
            Guid productId,
            Guid imageId,
            AdminProductImageMetadataUpdate command,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AdminProductImageRecord?>(ImageRecord());
        }

        public Task<IReadOnlyList<AdminProductImageRecord>> UpdateProductImageOrderAsync(
            Guid productId,
            IReadOnlyList<Guid> imageIds,
            CancellationToken cancellationToken = default)
        {
            if (OrderException is not null)
            {
                throw OrderException;
            }

            return Task.FromResult<IReadOnlyList<AdminProductImageRecord>>([ImageRecord()]);
        }

        public Task<AdminProductImageRecord?> SetMainProductImageAsync(
            Guid productId,
            Guid imageId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AdminProductImageRecord?>(ImageRecord());
        }

        public Task<bool> DeleteProductImageAsync(
            Guid productId,
            Guid imageId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}
