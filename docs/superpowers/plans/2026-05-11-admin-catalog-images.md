# Admin Catalog Images Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Реализовать backend API для загрузки, просмотра, обновления, сортировки, выбора главного изображения товара и загрузки, замены, удаления логотипа бренда через Local FileStorage.

**Architecture:** Срез добавляет reusable инфраструктуру записи локальных файлов поверх существующей таблицы `stored_files`, затем использует ее в сервисах админского каталога. Контроллеры остаются тонкими, бизнес-правила и транзакции живут в сервисах/репозиториях, SQL остается в Dapper repository-слое. Физическое удаление файлов не выполняется при бизнес-удалении: связь отвязывается, `stored_files.status` переводится в `deleted`, а очистка диска остается отдельной служебной операцией.

**Tech Stack:** ASP.NET Core Web API, cookie auth + CSRF for mutations, multipart form upload, PostgreSQL, Npgsql, Dapper, DbUp SQL migrations, Local FileStorage, xUnit.

---

## Scope

Входит в план:

- `POST /api/admin/catalog/products/{id}/images` - множественная загрузка изображений товара;
- `GET /api/admin/catalog/products/{id}/images` - список изображений товара;
- `PUT /api/admin/catalog/products/{id}/images/order` - сохранение порядка изображений;
- `PUT /api/admin/catalog/products/{id}/images/{imageId}` - обновление `alt` и `title`;
- `PUT /api/admin/catalog/products/{id}/images/{imageId}/main` - выбор главного изображения;
- `DELETE /api/admin/catalog/products/{id}/images/{imageId}` - отвязка изображения от товара и перевод файла в `deleted`, если файл больше нигде не используется;
- `PUT /api/admin/catalog/brands/{id}/logo` - загрузка или замена логотипа бренда;
- `DELETE /api/admin/catalog/brands/{id}/logo` - удаление логотипа бренда;
- Local FileStorage запись на диск и регистрация в `stored_files`;
- тесты API, сервисов, SQL-контрактов, файлового writer и opt-in PostgreSQL behavior tests.

Не входит в план:

- frontend UI;
- homepage mutation endpoints;
- import/export;
- audit log;
- LLM duplicate checking;
- общая медиатека;
- crop/resize изображений;
- физическая background-очистка старых `deleted`/`orphaned` файлов.

## Source Of Truth And Existing Patterns

Перед реализацией прочитать:

- `docs/superpowers/specs/2026-05-11-admin-catalog-homepage-design.md`;
- `docs/superpowers/plans/2026-05-11-admin-catalog-foundation.md`;
- `docs/superpowers/plans/2026-05-11-admin-catalog-crud.md`;
- `vault/Человекочитаемое/Архитектура backend и БД.md`;
- `vault/Человекочитаемое/Сквозные требования.md`;
- `vault/Человекочитаемое/Продуктовая модель.md`;
- `vault/Человекочитаемое/Catalog database foundation.md`;
- `apps/api/Modules/Catalog/Controllers/AdminCatalogProductsController.cs`;
- `apps/api/Modules/Catalog/Controllers/AdminCatalogBrandsController.cs`;
- `apps/api/Modules/Catalog/Services/AdminCatalogProductService.cs`;
- `apps/api/Modules/Catalog/Services/AdminCatalogBrandService.cs`;
- `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogProductRepository.cs`;
- `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogBrandRepository.cs`;
- `apps/api/Infrastructure/Hosting/LocalStorageStaticFilesExtensions.cs`;
- `apps/dbmigrator/Migrations/002_catalog_foundation.sql`;
- `apps/dbmigrator/Migrations/005_product_image_shared_files.sql`.

Следовать текущим паттернам:

- admin endpoints под `/api/admin`;
- `[Authorize]` на admin controllers;
- `[RequireCsrfToken]` на `POST`, `PUT`, `DELETE`;
- staff access через `IAdminCatalogStaffGuard`;
- контроллеры не содержат SQL и предметную валидацию;
- Dapper repository использует `IDbConnectionFactory`, `CommandDefinition`, явные транзакции для связанных изменений;
- ошибки идут через `AdminCatalogErrors` и `ApiException`;
- `seller` и `admin` имеют доступ, `customer` получает `auth.forbidden`;
- `stored_files.storage_key` хранит публичный путь с префиксом `storage/...`, поэтому публичный URL строится как `"/" + storage_key`;
- физический путь под `Storage:RootPath` строится без ведущего сегмента `storage/`, как уже делает catalog-import: `storage/products/...` в БД соответствует файлу `{Storage:RootPath}/products/...`.

## Existing Data Model Contract

Использовать существующие таблицы:

```text
stored_files
- id
- storage_key
- original_file_name
- content_type
- size_bytes
- checksum
- purpose: product_image | brand_logo | import_source | export_result | temp
- status: active | deleted | orphaned
- created_by_user_id
- created_at
```

```text
product_images
- id
- product_id
- stored_file_id
- alt
- title
- sort_order
- is_main
- created_at
- updated_at
```

```text
brands.logo_file_id -> stored_files.id
```

Существующие constraints уже обязательны для этого плана:

- `trg_product_images_validate_file` запрещает файл не с `purpose = 'product_image'`;
- `trg_brands_validate_logo_file` запрещает файл не с `purpose = 'brand_logo'`;
- `ux_product_images_single_main` гарантирует одно главное изображение на товар;
- `ux_product_images_product_id_stored_file_id` разрешает один файл у разных товаров, но не дублирует файл внутри одного товара.

## API Contracts

Точные endpoints:

```text
GET    /api/admin/catalog/products/{id}/images
POST   /api/admin/catalog/products/{id}/images
PUT    /api/admin/catalog/products/{id}/images/order
PUT    /api/admin/catalog/products/{id}/images/{imageId}
PUT    /api/admin/catalog/products/{id}/images/{imageId}/main
DELETE /api/admin/catalog/products/{id}/images/{imageId}

PUT    /api/admin/catalog/brands/{id}/logo
DELETE /api/admin/catalog/brands/{id}/logo
```

Multipart upload forms:

```text
POST /products/{id}/images
- files: one or more image files

PUT /brands/{id}/logo
- file: exactly one image file
```

Supported file validation:

```csharp
internal static class AdminCatalogImageUploadPolicy
{
    public const long MaxImageSizeBytes = 10 * 1024 * 1024;

    public static readonly IReadOnlyDictionary<string, string> AllowedContentTypes = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };
}
```

Error codes to add to `AdminCatalogErrors`:

```csharp
public static ApiException ImageNotFound()
{
    return new ApiException(
        "admin_catalog.image_not_found",
        "Изображение не найдено.",
        StatusCodes.Status404NotFound);
}

public static ApiException InvalidImageType()
{
    return new ApiException(
        "admin_catalog.invalid_image_type",
        "Изображение имеет недопустимый тип.",
        StatusCodes.Status400BadRequest);
}

public static ApiException ImageTooLarge()
{
    return new ApiException(
        "admin_catalog.image_too_large",
        "Изображение превышает допустимый размер.",
        StatusCodes.Status400BadRequest);
}

public static ApiException ImageOrderMismatch()
{
    return new ApiException(
        "admin_catalog.image_order_mismatch",
        "Порядок изображений не соответствует изображениям товара.",
        StatusCodes.Status400BadRequest);
}
```

## File Structure

Create:

- `apps/api/Infrastructure/Storage/LocalStoredFileOptions.cs`
- `apps/api/Infrastructure/Storage/LocalStoredFileDraft.cs`
- `apps/api/Infrastructure/Storage/ILocalStoredFileWriter.cs`
- `apps/api/Infrastructure/Storage/LocalStoredFileWriter.cs`
- `apps/api/Modules/Catalog/DTOs/AdminCatalogImageDtos.cs`
- `apps/api/Modules/Catalog/Repositories/IAdminCatalogImageRepository.cs`
- `apps/api/Modules/Catalog/Repositories/AdminCatalogImageSql.cs`
- `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogImageRepository.cs`
- `apps/api/Modules/Catalog/Services/IAdminCatalogImageService.cs`
- `apps/api/Modules/Catalog/Services/AdminCatalogImageService.cs`
- `apps/api/Modules/Catalog/Controllers/AdminCatalogProductImagesController.cs`
- `tests/LineCom.Api.Tests/Infrastructure/Storage/LocalStoredFileWriterTests.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogImageSqlTests.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogImageServiceTests.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductImagesEndpointTests.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogBrandLogoEndpointTests.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogImagesDatabaseBehaviorTests.cs`

Modify:

- `apps/api/Infrastructure/Database/DatabaseServiceCollectionExtensions.cs`;
- `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs`;
- `apps/api/Modules/Catalog/Controllers/AdminCatalogBrandsController.cs`;
- `apps/api/Modules/Catalog/DTOs/AdminCatalogBrandDtos.cs` only if the endpoint response reuses brand DTOs;
- `apps/api/Modules/Catalog/Repositories/IAdminCatalogBrandRepository.cs`;
- `apps/api/Modules/Catalog/Repositories/AdminCatalogBrandSql.cs`;
- `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogBrandRepository.cs`;
- `apps/api/Modules/Catalog/Services/IAdminCatalogBrandService.cs`;
- `apps/api/Modules/Catalog/Services/AdminCatalogBrandService.cs`;
- `apps/api/Modules/Catalog/Services/AdminCatalogErrors.cs`;
- `tests/LineCom.Api.Tests/Modules/Catalog/CatalogModuleRegistrationTests.cs`;
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogBrandSqlTests.cs`;
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogBrandServiceTests.cs`.

Do not modify:

- frontend files;
- homepage APIs;
- import/export tools;
- `admin-catalog-homepage-slice.png`;
- DbUp migrations unless behavior tests prove an actual schema mismatch.

---

### Task 1: Local FileStorage Writer

**Files:**
- Create: `apps/api/Infrastructure/Storage/LocalStoredFileOptions.cs`
- Create: `apps/api/Infrastructure/Storage/LocalStoredFileDraft.cs`
- Create: `apps/api/Infrastructure/Storage/ILocalStoredFileWriter.cs`
- Create: `apps/api/Infrastructure/Storage/LocalStoredFileWriter.cs`
- Modify: `apps/api/Infrastructure/Database/DatabaseServiceCollectionExtensions.cs`
- Create: `tests/LineCom.Api.Tests/Infrastructure/Storage/LocalStoredFileWriterTests.cs`

- [ ] **Step 1: Write failing writer tests**

Create `tests/LineCom.Api.Tests/Infrastructure/Storage/LocalStoredFileWriterTests.cs`:

```csharp
using System.Security.Cryptography;
using LineCom.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LineCom.Api.Tests.Infrastructure.Storage;

public sealed class LocalStoredFileWriterTests
{
    [Fact]
    public async Task SaveAsync_WritesFileUnderStorageRootAndReturnsStoredFileDraft()
    {
        using var temp = new TemporaryDirectory();
        var writer = new LocalStoredFileWriter(Options.Create(new LocalStoredFileOptions
        {
            RootPath = temp.Path
        }));
        var bytes = "image-bytes"u8.ToArray();
        var file = FormFile("cable.JPG", "image/jpeg", bytes);
        var fileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var userId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var draft = await writer.SaveAsync(
            file,
            fileId,
            "product_image",
            "products/admin/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            userId,
            CancellationToken.None);

        Assert.Equal(fileId, draft.Id);
        Assert.Equal("storage/products/admin/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg", draft.StorageKey);
        Assert.Equal("cable.JPG", draft.OriginalFileName);
        Assert.Equal("image/jpeg", draft.ContentType);
        Assert.Equal(bytes.Length, draft.SizeBytes);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), draft.Checksum);
        Assert.Equal("product_image", draft.Purpose);
        Assert.Equal(userId, draft.CreatedByUserId);
        Assert.True(File.Exists(Path.Combine(temp.Path, "products", "admin", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg")));
    }

    [Fact]
    public async Task SaveAsync_RejectsUnsupportedContentType()
    {
        using var temp = new TemporaryDirectory();
        var writer = new LocalStoredFileWriter(Options.Create(new LocalStoredFileOptions { RootPath = temp.Path }));

        await Assert.ThrowsAsync<InvalidLocalStoredFileException>(() => writer.SaveAsync(
            FormFile("notes.txt", "text/plain", "not-image"u8.ToArray()),
            Guid.NewGuid(),
            "product_image",
            "products/admin/test",
            Guid.NewGuid(),
            CancellationToken.None));
    }

    [Fact]
    public async Task DeletePhysicalFileIfExistsAsync_RemovesOnlyPathInsideStorageRoot()
    {
        using var temp = new TemporaryDirectory();
        var writer = new LocalStoredFileWriter(Options.Create(new LocalStoredFileOptions { RootPath = temp.Path }));
        var path = Path.Combine(temp.Path, "products", "admin", "image.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "bytes");

        await writer.DeletePhysicalFileIfExistsAsync("storage/products/admin/image.jpg", CancellationToken.None);

        Assert.False(File.Exists(path));
    }

    private static IFormFile FormFile(string fileName, string contentType, byte[] bytes)
    {
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
```

- [ ] **Step 2: Run tests to verify failure**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter LocalStoredFileWriterTests
```

Expected: FAIL because `LocalStoredFileWriter` does not exist.

- [ ] **Step 3: Add storage types**

Create `apps/api/Infrastructure/Storage/LocalStoredFileOptions.cs`:

```csharp
namespace LineCom.Api.Infrastructure.Storage;

public sealed class LocalStoredFileOptions
{
    public string? RootPath { get; set; }
}
```

Create `apps/api/Infrastructure/Storage/LocalStoredFileDraft.cs`:

```csharp
namespace LineCom.Api.Infrastructure.Storage;

public sealed record LocalStoredFileDraft(
    Guid Id,
    string StorageKey,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Checksum,
    string Purpose,
    Guid CreatedByUserId);
```

Create `apps/api/Infrastructure/Storage/ILocalStoredFileWriter.cs`:

```csharp
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Infrastructure.Storage;

public interface ILocalStoredFileWriter
{
    Task<LocalStoredFileDraft> SaveAsync(
        IFormFile file,
        Guid fileId,
        string purpose,
        string storageDirectory,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);

    Task DeletePhysicalFileIfExistsAsync(string storageKey, CancellationToken cancellationToken = default);
}

public sealed class InvalidLocalStoredFileException : Exception
{
    public InvalidLocalStoredFileException(string message)
        : base(message)
    {
    }
}
```

Create `apps/api/Infrastructure/Storage/LocalStoredFileWriter.cs`:

```csharp
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LineCom.Api.Infrastructure.Storage;

public sealed class LocalStoredFileWriter : ILocalStoredFileWriter
{
    private const long MaxImageSizeBytes = 10 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string> AllowedImageExtensions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };

    private readonly string _rootPath;

    public LocalStoredFileWriter(IOptions<LocalStoredFileOptions> options)
    {
        _rootPath = ResolveRootPath(options.Value.RootPath);
    }

    public async Task<LocalStoredFileDraft> SaveAsync(
        IFormFile file,
        Guid fileId,
        string purpose,
        string storageDirectory,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (file.Length is <= 0 or > MaxImageSizeBytes)
        {
            throw new InvalidLocalStoredFileException("Invalid image size.");
        }

        if (!AllowedImageExtensions.TryGetValue(file.ContentType, out var extension))
        {
            throw new InvalidLocalStoredFileException("Invalid image content type.");
        }

        var safeDirectory = NormalizeStorageDirectory(storageDirectory);
        var storageKey = $"storage/{safeDirectory}/{fileId:N}{extension}";
        var physicalPath = ResolvePhysicalPath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);

        await using var input = file.OpenReadStream();
        await using var output = File.Create(physicalPath);
        using var sha256 = SHA256.Create();
        var buffer = new byte[81920];
        long totalBytes = 0;
        int read;
        while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            totalBytes += read;
            if (totalBytes > MaxImageSizeBytes)
            {
                output.Close();
                File.Delete(physicalPath);
                throw new InvalidLocalStoredFileException("Invalid image size.");
            }

            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            sha256.TransformBlock(buffer, 0, read, null, 0);
        }

        sha256.TransformFinalBlock([], 0, 0);

        return new LocalStoredFileDraft(
            fileId,
            storageKey,
            Path.GetFileName(file.FileName),
            file.ContentType,
            totalBytes,
            Convert.ToHexString(sha256.Hash!).ToLowerInvariant(),
            purpose,
            createdByUserId);
    }

    public Task DeletePhysicalFileIfExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var physicalPath = ResolvePhysicalPath(storageKey);
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        return Task.CompletedTask;
    }

    private string ResolvePhysicalPath(string storageKey)
    {
        var relative = storageKey.StartsWith("storage/", StringComparison.Ordinal)
            ? storageKey["storage/".Length..]
            : throw new InvalidLocalStoredFileException("Invalid storage key.");
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, relative.Replace('/', Path.DirectorySeparatorChar)));
        var rootFullPath = Path.GetFullPath(_rootPath);
        if (!fullPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidLocalStoredFileException("Storage key escapes storage root.");
        }

        return fullPath;
    }

    private static string NormalizeStorageDirectory(string storageDirectory)
    {
        var normalized = storageDirectory.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(normalized)
            || normalized.Contains("..", StringComparison.Ordinal)
            || normalized.StartsWith("storage/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidLocalStoredFileException("Invalid storage directory.");
        }

        return normalized;
    }

    private static string ResolveRootPath(string? configuredRootPath)
    {
        var rootPath = string.IsNullOrWhiteSpace(configuredRootPath)
            ? Path.Combine(AppContext.BaseDirectory, "storage")
            : configuredRootPath;

        Directory.CreateDirectory(rootPath);
        return Path.GetFullPath(rootPath);
    }
}
```

- [ ] **Step 4: Register writer**

Modify `apps/api/Infrastructure/Database/DatabaseServiceCollectionExtensions.cs`:

```csharp
using LineCom.Api.Infrastructure.Storage;

// inside AddDatabase(IServiceCollection services, IConfiguration configuration), after database registrations:
services.Configure<LocalStoredFileOptions>(configuration.GetSection("Storage"));
services.AddScoped<ILocalStoredFileWriter, LocalStoredFileWriter>();
```

- [ ] **Step 5: Run writer tests**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter LocalStoredFileWriterTests
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add apps/api/Infrastructure/Storage/LocalStoredFileOptions.cs apps/api/Infrastructure/Storage/LocalStoredFileDraft.cs apps/api/Infrastructure/Storage/ILocalStoredFileWriter.cs apps/api/Infrastructure/Storage/LocalStoredFileWriter.cs apps/api/Infrastructure/Database/DatabaseServiceCollectionExtensions.cs tests/LineCom.Api.Tests/Infrastructure/Storage/LocalStoredFileWriterTests.cs
git commit -m "feat: add local stored file writer"
```

### Task 2: Product Image DTOs, SQL, Repository

**Files:**
- Create: `apps/api/Modules/Catalog/DTOs/AdminCatalogImageDtos.cs`
- Create: `apps/api/Modules/Catalog/Repositories/IAdminCatalogImageRepository.cs`
- Create: `apps/api/Modules/Catalog/Repositories/AdminCatalogImageSql.cs`
- Create: `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogImageRepository.cs`
- Modify: `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs`
- Modify: `tests/LineCom.Api.Tests/Modules/Catalog/CatalogModuleRegistrationTests.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogImageSqlTests.cs`

- [ ] **Step 1: Write failing SQL and registration tests**

Create `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogImageSqlTests.cs`:

```csharp
using LineCom.Api.Modules.Catalog.Repositories;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminCatalogImageSqlTests
{
    [Fact]
    public void ListProductImages_SelectsActiveStoredFilesInDisplayOrder()
    {
        Assert.Contains("FROM product_images image", AdminCatalogImageSql.ListProductImages);
        Assert.Contains("stored_file.status = 'active'", AdminCatalogImageSql.ListProductImages);
        Assert.Contains("stored_file.purpose = 'product_image'", AdminCatalogImageSql.ListProductImages);
        Assert.Contains("'/' || stored_file.storage_key AS \"Url\"", AdminCatalogImageSql.ListProductImages);
        Assert.Contains("ORDER BY image.is_main DESC, image.sort_order, image.id", AdminCatalogImageSql.ListProductImages);
    }

    [Fact]
    public void InsertProductImage_RegistersStoredFileAndDefaultsFirstImageToMain()
    {
        Assert.Contains("INSERT INTO stored_files", AdminCatalogImageSql.InsertStoredFile);
        Assert.Contains("INSERT INTO product_images", AdminCatalogImageSql.InsertProductImage);
        Assert.Contains("COALESCE(MAX(sort_order), 0) + 10", AdminCatalogImageSql.InsertProductImage);
        Assert.Contains("NOT EXISTS", AdminCatalogImageSql.InsertProductImage);
    }

    [Fact]
    public void DeleteProductImage_MarksFileDeletedOnlyWhenUnreferenced()
    {
        Assert.Contains("DELETE FROM product_images", AdminCatalogImageSql.DeleteProductImage);
        Assert.Contains("UPDATE stored_files", AdminCatalogImageSql.MarkStoredFileDeletedIfUnreferenced);
        Assert.Contains("NOT EXISTS", AdminCatalogImageSql.MarkStoredFileDeletedIfUnreferenced);
        Assert.Contains("UPDATE product_images", AdminCatalogImageSql.PromoteFirstRemainingProductImage);
    }
}
```

Add to `CatalogModuleRegistrationTests.cs`:

```csharp
[Fact]
public void AddCatalogModule_RegistersAdminCatalogImageRepositoryAsScoped()
{
    var services = new ServiceCollection();
    services.AddCatalogModule();

    var descriptor = Assert.Single(services, service => service.ServiceType == typeof(IAdminCatalogImageRepository));

    Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    Assert.Equal(typeof(DapperAdminCatalogImageRepository), descriptor.ImplementationType);
}
```

- [ ] **Step 2: Run tests to verify failure**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalogImageSqlTests|CatalogModuleRegistrationTests"
```

Expected: FAIL because image repository types do not exist.

- [ ] **Step 3: Create DTOs**

Create `apps/api/Modules/Catalog/DTOs/AdminCatalogImageDtos.cs`:

```csharp
namespace LineCom.Api.Modules.Catalog.DTOs;

public sealed record AdminProductImagesResponse(IReadOnlyList<AdminProductImageDto> Items);

public sealed record AdminProductImageDto(
    Guid Id,
    Guid StoredFileId,
    string Url,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Checksum,
    string Alt,
    string? Title,
    int SortOrder,
    bool IsMain,
    DateTimeOffset CreatedAt);

public sealed record UpdateAdminProductImageCommand(string? Alt, string? Title);

public sealed record UpdateAdminProductImageOrderCommand(IReadOnlyList<Guid> ImageIds);

public sealed record AdminBrandLogoDto(
    Guid StoredFileId,
    string Url,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Checksum);
```

- [ ] **Step 4: Create repository contracts**

Create `apps/api/Modules/Catalog/Repositories/IAdminCatalogImageRepository.cs`:

```csharp
using LineCom.Api.Infrastructure.Storage;

namespace LineCom.Api.Modules.Catalog.Repositories;

public sealed record AdminProductImageRecord(
    Guid Id,
    Guid StoredFileId,
    string Url,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Checksum,
    string Alt,
    string? Title,
    int SortOrder,
    bool IsMain,
    DateTimeOffset CreatedAt);

public sealed record AdminProductImageMetadataUpdate(string Alt, string? Title);

internal sealed class AdminProductImageNotFoundException : Exception;
internal sealed class AdminProductImageOrderMismatchException : Exception;

public interface IAdminCatalogImageRepository
{
    Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<string?> GetProductNameAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminProductImageRecord>> GetProductImagesAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminProductImageRecord>> AddProductImagesAsync(
        Guid productId,
        IReadOnlyList<LocalStoredFileDraft> files,
        string defaultAlt,
        CancellationToken cancellationToken = default);

    Task<AdminProductImageRecord?> UpdateProductImageAsync(
        Guid productId,
        Guid imageId,
        AdminProductImageMetadataUpdate command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminProductImageRecord>> UpdateProductImageOrderAsync(
        Guid productId,
        IReadOnlyList<Guid> imageIds,
        CancellationToken cancellationToken = default);

    Task<AdminProductImageRecord?> SetMainProductImageAsync(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteProductImageAsync(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default);
}
```

- [ ] **Step 5: Create SQL**

Create `apps/api/Modules/Catalog/Repositories/AdminCatalogImageSql.cs`:

```csharp
namespace LineCom.Api.Modules.Catalog.Repositories;

internal static class AdminCatalogImageSql
{
    public const string ProductExists = """
        SELECT EXISTS (
            SELECT 1
            FROM products product
            WHERE product.id = @ProductId
        );
        """;

    public const string GetProductName = """
        SELECT product.name
        FROM products product
        WHERE product.id = @ProductId;
        """;

    public const string ListProductImages = """
        SELECT
            image.id AS "Id",
            stored_file.id AS "StoredFileId",
            '/' || stored_file.storage_key AS "Url",
            stored_file.original_file_name AS "OriginalFileName",
            stored_file.content_type AS "ContentType",
            stored_file.size_bytes AS "SizeBytes",
            stored_file.checksum AS "Checksum",
            image.alt AS "Alt",
            image.title AS "Title",
            image.sort_order AS "SortOrder",
            image.is_main AS "IsMain",
            image.created_at AS "CreatedAt"
        FROM product_images image
        INNER JOIN stored_files stored_file ON stored_file.id = image.stored_file_id
            AND stored_file.status = 'active'
            AND stored_file.purpose = 'product_image'
        WHERE image.product_id = @ProductId
        ORDER BY image.is_main DESC, image.sort_order, image.id;
        """;

    public const string InsertStoredFile = """
        INSERT INTO stored_files (
            id,
            storage_key,
            original_file_name,
            content_type,
            size_bytes,
            checksum,
            purpose,
            status,
            created_by_user_id
        )
        VALUES (
            @Id,
            @StorageKey,
            @OriginalFileName,
            @ContentType,
            @SizeBytes,
            @Checksum,
            @Purpose,
            'active',
            @CreatedByUserId
        );
        """;

    public const string InsertProductImage = """
        INSERT INTO product_images (
            product_id,
            stored_file_id,
            alt,
            title,
            sort_order,
            is_main
        )
        SELECT
            @ProductId,
            @StoredFileId,
            @Alt,
            NULL,
            COALESCE(MAX(sort_order), 0) + 10,
            NOT EXISTS (
                SELECT 1
                FROM product_images existing
                INNER JOIN stored_files existing_file ON existing_file.id = existing.stored_file_id
                    AND existing_file.status = 'active'
                WHERE existing.product_id = @ProductId
            )
        FROM product_images
        WHERE product_id = @ProductId
        RETURNING id;
        """;

    public const string UpdateProductImage = """
        UPDATE product_images image
        SET
            alt = @Alt,
            title = @Title
        WHERE image.id = @ImageId
            AND image.product_id = @ProductId
        RETURNING image.id;
        """;

    public const string GetProductImageIds = """
        SELECT image.id
        FROM product_images image
        INNER JOIN stored_files stored_file ON stored_file.id = image.stored_file_id
            AND stored_file.status = 'active'
            AND stored_file.purpose = 'product_image'
        WHERE image.product_id = @ProductId
        ORDER BY image.sort_order, image.id;
        """;

    public const string UpdateProductImageSortOrder = """
        UPDATE product_images
        SET sort_order = @SortOrder
        WHERE id = @ImageId
            AND product_id = @ProductId;
        """;

    public const string ClearProductMainImages = """
        UPDATE product_images
        SET is_main = FALSE
        WHERE product_id = @ProductId
            AND is_main = TRUE;
        """;

    public const string SetProductMainImage = """
        UPDATE product_images image
        SET is_main = TRUE
        FROM stored_files stored_file
        WHERE image.stored_file_id = stored_file.id
            AND stored_file.status = 'active'
            AND stored_file.purpose = 'product_image'
            AND image.id = @ImageId
            AND image.product_id = @ProductId
        RETURNING image.id;
        """;

    public const string GetProductImageForDelete = """
        SELECT
            image.id AS "Id",
            image.stored_file_id AS "StoredFileId",
            image.is_main AS "IsMain"
        FROM product_images image
        WHERE image.id = @ImageId
            AND image.product_id = @ProductId;
        """;

    public const string DeleteProductImage = """
        DELETE FROM product_images
        WHERE id = @ImageId
            AND product_id = @ProductId;
        """;

    public const string MarkStoredFileDeletedIfUnreferenced = """
        UPDATE stored_files stored_file
        SET status = 'deleted'
        WHERE stored_file.id = @StoredFileId
            AND NOT EXISTS (
                SELECT 1
                FROM product_images image
                WHERE image.stored_file_id = stored_file.id
            );
        """;

    public const string PromoteFirstRemainingProductImage = """
        UPDATE product_images image
        SET is_main = TRUE
        WHERE image.id = (
            SELECT remaining.id
            FROM product_images remaining
            INNER JOIN stored_files stored_file ON stored_file.id = remaining.stored_file_id
                AND stored_file.status = 'active'
                AND stored_file.purpose = 'product_image'
            WHERE remaining.product_id = @ProductId
            ORDER BY remaining.sort_order, remaining.id
            LIMIT 1
        )
            AND NOT EXISTS (
                SELECT 1
                FROM product_images main_image
                WHERE main_image.product_id = @ProductId
                    AND main_image.is_main = TRUE
            );
        """;
}
```

- [ ] **Step 6: Implement repository**

Create `DapperAdminCatalogImageRepository.cs` with these required transaction rules:

```csharp
public async Task<IReadOnlyList<AdminProductImageRecord>> AddProductImagesAsync(
    Guid productId,
    IReadOnlyList<LocalStoredFileDraft> files,
    string defaultAlt,
    CancellationToken cancellationToken = default)
{
    await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

    try
    {
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

        await transaction.CommitAsync(cancellationToken);
    }
    catch
    {
        await transaction.RollbackAsync(CancellationToken.None);
        throw;
    }

    return await GetProductImagesAsync(productId, cancellationToken);
}
```

For `UpdateProductImageOrderAsync`, load existing active image ids with `GetProductImageIds`; reject when the submitted ids are not exactly the same set and count by throwing `AdminProductImageOrderMismatchException`. Use `SortOrder = (index + 1) * 10`.

For `SetMainProductImageAsync`, run `ClearProductMainImages` and `SetProductMainImage` in one transaction. If target image is missing, rollback and return `null`.

For `DeleteProductImageAsync`, run `GetProductImageForDelete`, `DeleteProductImage`, `MarkStoredFileDeletedIfUnreferenced`, and `PromoteFirstRemainingProductImage` in one transaction.

- [ ] **Step 7: Register repository**

Modify `CatalogServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<IAdminCatalogImageRepository, DapperAdminCatalogImageRepository>();
```

- [ ] **Step 8: Run tests**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalogImageSqlTests|CatalogModuleRegistrationTests"
```

Expected: PASS.

- [ ] **Step 9: Commit**

```powershell
git add apps/api/Modules/Catalog/DTOs/AdminCatalogImageDtos.cs apps/api/Modules/Catalog/Repositories/IAdminCatalogImageRepository.cs apps/api/Modules/Catalog/Repositories/AdminCatalogImageSql.cs apps/api/Modules/Catalog/Repositories/DapperAdminCatalogImageRepository.cs apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogImageSqlTests.cs tests/LineCom.Api.Tests/Modules/Catalog/CatalogModuleRegistrationTests.cs
git commit -m "feat: add admin product image repository"
```

### Task 3: Product Image Service

**Files:**
- Create: `apps/api/Modules/Catalog/Services/IAdminCatalogImageService.cs`
- Create: `apps/api/Modules/Catalog/Services/AdminCatalogImageService.cs`
- Modify: `apps/api/Modules/Catalog/Services/AdminCatalogErrors.cs`
- Modify: `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs`
- Modify: `tests/LineCom.Api.Tests/Modules/Catalog/CatalogModuleRegistrationTests.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogImageServiceTests.cs`

- [ ] **Step 1: Write failing service tests**

Create tests covering:

```csharp
[Fact]
public async Task UploadProductImagesAsync_UsesProductNameAsDefaultAltAndStoresFilesUnderProductDirectory()
{
    var repository = new CapturingAdminCatalogImageRepository { ProductName = "Cable UTP" };
    var writer = new CapturingLocalStoredFileWriter();
    var service = CreateService("seller", repository, writer);

    await service.UploadProductImagesAsync(
        new DefaultHttpContext(),
        ProductId,
        [FormFile("cable.jpg", "image/jpeg")],
        CancellationToken.None);

    Assert.Equal("products/admin/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", writer.LastStorageDirectory);
    Assert.Equal("product_image", writer.LastPurpose);
    Assert.Equal("Cable UTP", repository.LastDefaultAlt);
}

[Fact]
public async Task UploadProductImagesAsync_ProductMissing_ThrowsProductNotFound()
{
    var service = CreateService("seller", new CapturingAdminCatalogImageRepository { ProductName = null }, new CapturingLocalStoredFileWriter());

    var exception = await Assert.ThrowsAsync<ApiException>(() =>
        service.UploadProductImagesAsync(new DefaultHttpContext(), ProductId, [FormFile("cable.jpg", "image/jpeg")], CancellationToken.None));

    Assert.Equal("admin_catalog.product_not_found", exception.Code);
}

[Fact]
public async Task UpdateProductImageOrderAsync_OrderMismatch_ThrowsImageOrderMismatch()
{
    var repository = new CapturingAdminCatalogImageRepository
    {
        OrderMismatch = true
    };
    var service = CreateService("admin", repository, new CapturingLocalStoredFileWriter());

    var exception = await Assert.ThrowsAsync<ApiException>(() =>
        service.UpdateProductImageOrderAsync(
            new DefaultHttpContext(),
            ProductId,
            new UpdateAdminProductImageOrderCommand([ImageId]),
            CancellationToken.None));

    Assert.Equal("admin_catalog.image_order_mismatch", exception.Code);
}
```

Use local capturing fakes with `IAdminCatalogStaffGuard`, `IAdminCatalogImageRepository`, and `ILocalStoredFileWriter`. Reuse the style from `AdminCatalogProductServiceTests.cs`.

- [ ] **Step 2: Run tests to verify failure**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalogImageServiceTests|CatalogModuleRegistrationTests"
```

Expected: FAIL because image service types do not exist.

- [ ] **Step 3: Create service interface**

Create `apps/api/Modules/Catalog/Services/IAdminCatalogImageService.cs`:

```csharp
using LineCom.Api.Modules.Catalog.DTOs;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Services;

public interface IAdminCatalogImageService
{
    Task<AdminProductImagesResponse> GetProductImagesAsync(
        HttpContext httpContext,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<AdminProductImagesResponse> UploadProductImagesAsync(
        HttpContext httpContext,
        Guid productId,
        IReadOnlyList<IFormFile> files,
        CancellationToken cancellationToken = default);

    Task<AdminProductImageDto> UpdateProductImageAsync(
        HttpContext httpContext,
        Guid productId,
        Guid imageId,
        UpdateAdminProductImageCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminProductImagesResponse> UpdateProductImageOrderAsync(
        HttpContext httpContext,
        Guid productId,
        UpdateAdminProductImageOrderCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminProductImageDto> SetMainProductImageAsync(
        HttpContext httpContext,
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default);

    Task DeleteProductImageAsync(
        HttpContext httpContext,
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Implement service**

Create `AdminCatalogImageService.cs` with these exact rules:

- require staff before any repository or file action;
- `GET` checks product existence and returns active images;
- upload rejects empty file list with `admin_catalog.invalid_request`;
- upload gets product name before writing files;
- upload passes `storageDirectory = $"products/admin/{productId:N}"`;
- upload uses generated `Guid.NewGuid()` per file id;
- upload maps `InvalidLocalStoredFileException` to `invalid_image_type` or `image_too_large` by exception message;
- if repository insert fails after disk write, call `DeletePhysicalFileIfExistsAsync` for all written drafts, then rethrow mapped API error;
- image metadata update requires non-blank `Alt`, normalizes optional `Title`;
- order update rejects null/empty `ImageIds`;
- missing image maps to `admin_catalog.image_not_found`.

Core upload method:

```csharp
public async Task<AdminProductImagesResponse> UploadProductImagesAsync(
    HttpContext httpContext,
    Guid productId,
    IReadOnlyList<IFormFile> files,
    CancellationToken cancellationToken = default)
{
    var user = await _staffGuard.RequireStaffAsync(httpContext, cancellationToken);
    if (files.Count == 0)
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
            drafts.Add(await _fileWriter.SaveAsync(
                file,
                Guid.NewGuid(),
                "product_image",
                $"products/admin/{productId:N}",
                user.Id,
                cancellationToken));
        }

        var records = await _repository.AddProductImagesAsync(
            productId,
            drafts,
            productName,
            cancellationToken);

        return new AdminProductImagesResponse(records.Select(ToDto).ToArray());
    }
    catch (InvalidLocalStoredFileException exception)
    {
        await DeleteDraftsAsync(drafts, CancellationToken.None);
        throw MapUploadError(exception);
    }
    catch
    {
        await DeleteDraftsAsync(drafts, CancellationToken.None);
        throw;
    }
}
```

- [ ] **Step 5: Register service**

Modify `CatalogServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<IAdminCatalogImageService, AdminCatalogImageService>();
```

Add matching scoped registration test to `CatalogModuleRegistrationTests.cs`.

- [ ] **Step 6: Run tests**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalogImageServiceTests|CatalogModuleRegistrationTests"
```

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add apps/api/Modules/Catalog/Services/IAdminCatalogImageService.cs apps/api/Modules/Catalog/Services/AdminCatalogImageService.cs apps/api/Modules/Catalog/Services/AdminCatalogErrors.cs apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogImageServiceTests.cs tests/LineCom.Api.Tests/Modules/Catalog/CatalogModuleRegistrationTests.cs
git commit -m "feat: add admin product image service"
```

### Task 4: Product Image Controller

**Files:**
- Create: `apps/api/Modules/Catalog/Controllers/AdminCatalogProductImagesController.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductImagesEndpointTests.cs`

- [ ] **Step 1: Write failing endpoint tests**

Create endpoint tests with the same auth setup style as `AdminCatalogProductsEndpointTests.cs`:

```csharp
[Fact]
public async Task GetProductImages_WithoutAuth_ReturnsUnauthorizedError()
{
    await using var factory = CreateFactory(new ReturningAdminCatalogImageService(), "seller");
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    using var response = await client.GetAsync($"/api/admin/catalog/products/{ProductId}/images");

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}

[Fact]
public async Task UploadProductImages_WithCsrfToken_ReturnsImages()
{
    var imageService = new ReturningAdminCatalogImageService();
    await using var factory = CreateFactory(imageService, "seller");
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    var csrfToken = await LoginAsync(client);
    using var form = new MultipartFormDataContent();
    form.Add(new ByteArrayContent("image"u8.ToArray())
    {
        Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg") }
    }, "files", "cable.jpg");
    using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/catalog/products/{ProductId}/images")
    {
        Content = form
    };
    request.Headers.Add("X-CSRF-Token", csrfToken);

    using var response = await client.SendAsync(request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Single(imageService.LastUploadedFiles);
}

[Theory]
[InlineData("POST", "/api/admin/catalog/products/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/images")]
[InlineData("PUT", "/api/admin/catalog/products/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/images/order")]
[InlineData("PUT", "/api/admin/catalog/products/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/images/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")]
[InlineData("PUT", "/api/admin/catalog/products/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/images/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb/main")]
[InlineData("DELETE", "/api/admin/catalog/products/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/images/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")]
public async Task Mutations_WithoutCsrfToken_ReturnForbiddenError(string method, string path)
{
    await using var factory = CreateFactory(new ReturningAdminCatalogImageService(), "seller");
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    await LoginAsync(client);

    using var response = await client.SendAsync(CreateRequest(method, path));

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

Also cover:

- customer receives 403 on `GET`;
- `PUT /order` passes `UpdateAdminProductImageOrderCommand`;
- `PUT /{imageId}` passes `UpdateAdminProductImageCommand`;
- `PUT /{imageId}/main` calls service;
- `DELETE` calls service and returns 204.

- [ ] **Step 2: Run tests to verify failure**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter AdminCatalogProductImagesEndpointTests
```

Expected: FAIL because controller does not exist.

- [ ] **Step 3: Add controller**

Create `apps/api/Modules/Catalog/Controllers/AdminCatalogProductImagesController.cs`:

```csharp
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineCom.Api.Modules.Catalog.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/catalog/products/{productId:guid}/images")]
public sealed class AdminCatalogProductImagesController : ControllerBase
{
    private readonly IAdminCatalogImageService _imageService;

    public AdminCatalogProductImagesController(IAdminCatalogImageService imageService)
    {
        _imageService = imageService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminProductImagesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductImagesResponse>> GetProductImages(
        Guid productId,
        CancellationToken cancellationToken)
    {
        return Ok(await _imageService.GetProductImagesAsync(HttpContext, productId, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpPost]
    [ProducesResponseType(typeof(AdminProductImagesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductImagesResponse>> UploadProductImages(
        Guid productId,
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken)
    {
        return Ok(await _imageService.UploadProductImagesAsync(HttpContext, productId, files, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpPut("order")]
    [ProducesResponseType(typeof(AdminProductImagesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductImagesResponse>> UpdateProductImageOrder(
        Guid productId,
        UpdateAdminProductImageOrderCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _imageService.UpdateProductImageOrderAsync(HttpContext, productId, command, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpPut("{imageId:guid}")]
    [ProducesResponseType(typeof(AdminProductImageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductImageDto>> UpdateProductImage(
        Guid productId,
        Guid imageId,
        UpdateAdminProductImageCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _imageService.UpdateProductImageAsync(HttpContext, productId, imageId, command, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpPut("{imageId:guid}/main")]
    [ProducesResponseType(typeof(AdminProductImageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductImageDto>> SetMainProductImage(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        return Ok(await _imageService.SetMainProductImageAsync(HttpContext, productId, imageId, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpDelete("{imageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProductImage(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        await _imageService.DeleteProductImageAsync(HttpContext, productId, imageId, cancellationToken);
        return NoContent();
    }
}
```

- [ ] **Step 4: Run endpoint tests**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter AdminCatalogProductImagesEndpointTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add apps/api/Modules/Catalog/Controllers/AdminCatalogProductImagesController.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductImagesEndpointTests.cs
git commit -m "feat: expose admin product image endpoints"
```

### Task 5: Brand Logo Upload, Replace, Delete

**Files:**
- Modify: `apps/api/Modules/Catalog/Repositories/IAdminCatalogBrandRepository.cs`
- Modify: `apps/api/Modules/Catalog/Repositories/AdminCatalogBrandSql.cs`
- Modify: `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogBrandRepository.cs`
- Modify: `apps/api/Modules/Catalog/Services/IAdminCatalogBrandService.cs`
- Modify: `apps/api/Modules/Catalog/Services/AdminCatalogBrandService.cs`
- Modify: `apps/api/Modules/Catalog/Controllers/AdminCatalogBrandsController.cs`
- Modify: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogBrandSqlTests.cs`
- Modify: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogBrandServiceTests.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogBrandLogoEndpointTests.cs`

- [ ] **Step 1: Write failing brand logo SQL tests**

Add to `AdminCatalogBrandSqlTests.cs`:

```csharp
[Fact]
public void BrandLogoSql_RegistersBrandLogoFileAndMarksPreviousLogoDeletedWhenUnreferenced()
{
    Assert.Contains("INSERT INTO stored_files", AdminCatalogBrandSql.InsertStoredFile);
    Assert.Contains("'active'", AdminCatalogBrandSql.InsertStoredFile);
    Assert.Contains("UPDATE brands", AdminCatalogBrandSql.UpdateBrandLogo);
    Assert.Contains("logo_file_id = @LogoFileId", AdminCatalogBrandSql.UpdateBrandLogo);
    Assert.Contains("UPDATE stored_files", AdminCatalogBrandSql.MarkBrandLogoDeletedIfUnreferenced);
    Assert.Contains("NOT EXISTS", AdminCatalogBrandSql.MarkBrandLogoDeletedIfUnreferenced);
}

[Fact]
public void DeleteBrandLogo_ClearsLogoFileId()
{
    Assert.Contains("SET logo_file_id = NULL", AdminCatalogBrandSql.ClearBrandLogo);
    Assert.Contains("WHERE id = @BrandId", AdminCatalogBrandSql.ClearBrandLogo);
}
```

- [ ] **Step 2: Write failing brand logo service tests**

Add tests to `AdminCatalogBrandServiceTests.cs`:

```csharp
[Fact]
public async Task UploadLogoAsync_StoresBrandLogoUnderBrandDirectory()
{
    var repository = new CapturingAdminCatalogBrandRepository();
    var writer = new CapturingLocalStoredFileWriter();
    var service = CreateService("seller", repository, writer);

    await service.UploadLogoAsync(
        new DefaultHttpContext(),
        BrandId,
        FormFile("logo.png", "image/png"),
        CancellationToken.None);

    Assert.Equal("brands/admin/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", writer.LastStorageDirectory);
    Assert.Equal("brand_logo", writer.LastPurpose);
    Assert.NotNull(repository.LastLogoDraft);
}

[Fact]
public async Task DeleteLogoAsync_MissingBrand_ThrowsBrandNotFound()
{
    var repository = new CapturingAdminCatalogBrandRepository { Detail = null };
    var service = CreateService("admin", repository, new CapturingLocalStoredFileWriter());

    var exception = await Assert.ThrowsAsync<ApiException>(() =>
        service.DeleteLogoAsync(new DefaultHttpContext(), BrandId, CancellationToken.None));

    Assert.Equal("admin_catalog.brand_not_found", exception.Code);
}
```

Extend the existing test fake instead of creating another service style.

- [ ] **Step 3: Extend repository contract**

Add to `IAdminCatalogBrandRepository.cs`:

```csharp
using LineCom.Api.Infrastructure.Storage;

public sealed record AdminBrandLogoRecord(
    Guid StoredFileId,
    string Url,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Checksum);

Task<AdminBrandLogoRecord?> UpdateBrandLogoAsync(
    Guid brandId,
    LocalStoredFileDraft file,
    CancellationToken cancellationToken = default);

Task<bool> DeleteBrandLogoAsync(
    Guid brandId,
    CancellationToken cancellationToken = default);
```

- [ ] **Step 4: Add brand SQL**

Add to `AdminCatalogBrandSql.cs`:

```csharp
public const string InsertStoredFile = """
    INSERT INTO stored_files (
        id,
        storage_key,
        original_file_name,
        content_type,
        size_bytes,
        checksum,
        purpose,
        status,
        created_by_user_id
    )
    VALUES (
        @Id,
        @StorageKey,
        @OriginalFileName,
        @ContentType,
        @SizeBytes,
        @Checksum,
        @Purpose,
        'active',
        @CreatedByUserId
    );
    """;

public const string GetBrandLogoFileId = """
    SELECT logo_file_id
    FROM brands
    WHERE id = @BrandId
    FOR UPDATE;
    """;

public const string UpdateBrandLogo = """
    UPDATE brands
    SET logo_file_id = @LogoFileId
    WHERE id = @BrandId
    RETURNING logo_file_id;
    """;

public const string ClearBrandLogo = """
    UPDATE brands
    SET logo_file_id = NULL
    WHERE id = @BrandId
    RETURNING @PreviousLogoFileId;
    """;

public const string MarkBrandLogoDeletedIfUnreferenced = """
    UPDATE stored_files stored_file
    SET status = 'deleted'
    WHERE stored_file.id = @StoredFileId
        AND stored_file.purpose = 'brand_logo'
        AND NOT EXISTS (
            SELECT 1
            FROM brands brand
            WHERE brand.logo_file_id = stored_file.id
        );
    """;

public const string GetBrandLogo = """
    SELECT
        stored_file.id AS "StoredFileId",
        '/' || stored_file.storage_key AS "Url",
        stored_file.original_file_name AS "OriginalFileName",
        stored_file.content_type AS "ContentType",
        stored_file.size_bytes AS "SizeBytes",
        stored_file.checksum AS "Checksum"
    FROM brands brand
    INNER JOIN stored_files stored_file ON stored_file.id = brand.logo_file_id
        AND stored_file.status = 'active'
        AND stored_file.purpose = 'brand_logo'
    WHERE brand.id = @BrandId;
    """;
```

- [ ] **Step 5: Implement brand repository methods**

`UpdateBrandLogoAsync` transaction order:

1. `GetBrandLogoFileId` with `FOR UPDATE`.
2. Return `null` if brand is missing.
3. Insert new `stored_files` row.
4. Update `brands.logo_file_id`.
5. Mark previous logo as `deleted` only if it is no longer referenced by any brand.
6. Commit.
7. Return `GetBrandLogo`.

`DeleteBrandLogoAsync` transaction order:

1. `GetBrandLogoFileId` with `FOR UPDATE`.
2. Return `false` if brand is missing.
3. If previous logo is null, commit and return `true`.
4. Set `logo_file_id = NULL`.
5. Mark previous logo as `deleted` if unreferenced.
6. Commit and return `true`.

- [ ] **Step 6: Extend brand service interface and implementation**

Add to `IAdminCatalogBrandService.cs`:

```csharp
Task<AdminBrandLogoDto> UploadLogoAsync(
    HttpContext httpContext,
    Guid brandId,
    IFormFile file,
    CancellationToken cancellationToken = default);

Task DeleteLogoAsync(
    HttpContext httpContext,
    Guid brandId,
    CancellationToken cancellationToken = default);
```

Implement in `AdminCatalogBrandService`:

- require staff;
- check brand exists before writing a file;
- write with `purpose = "brand_logo"` and `storageDirectory = $"brands/admin/{brandId:N}"`;
- map invalid type/size to `AdminCatalogErrors.InvalidImageType()` or `ImageTooLarge()`;
- on repository failure after disk write, delete physical file and rethrow mapped error;
- return `AdminBrandLogoDto`;
- `DeleteLogoAsync` maps missing brand to `BrandNotFound()`.

- [ ] **Step 7: Add brand controller endpoints**

Modify `AdminCatalogBrandsController.cs`:

```csharp
[RequireCsrfToken]
[HttpPut("{id:guid}/logo")]
[ProducesResponseType(typeof(AdminBrandLogoDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<AdminBrandLogoDto>> UploadLogo(
    Guid id,
    [FromForm] IFormFile file,
    CancellationToken cancellationToken)
{
    return Ok(await _brandService.UploadLogoAsync(HttpContext, id, file, cancellationToken));
}

[RequireCsrfToken]
[HttpDelete("{id:guid}/logo")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> DeleteLogo(Guid id, CancellationToken cancellationToken)
{
    await _brandService.DeleteLogoAsync(HttpContext, id, cancellationToken);
    return NoContent();
}
```

- [ ] **Step 8: Add brand logo endpoint tests**

Create `AdminCatalogBrandLogoEndpointTests.cs` covering:

- unauthenticated `PUT /logo` returns 401;
- customer `PUT /logo` returns 403;
- seller `PUT /logo` with CSRF and multipart file returns `AdminBrandLogoDto`;
- seller `DELETE /logo` with CSRF returns 204;
- mutations without CSRF return 403.

- [ ] **Step 9: Run brand logo tests**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalogBrandSqlTests|AdminCatalogBrandServiceTests|AdminCatalogBrandLogoEndpointTests"
```

Expected: PASS.

- [ ] **Step 10: Commit**

```powershell
git add apps/api/Modules/Catalog/Repositories/IAdminCatalogBrandRepository.cs apps/api/Modules/Catalog/Repositories/AdminCatalogBrandSql.cs apps/api/Modules/Catalog/Repositories/DapperAdminCatalogBrandRepository.cs apps/api/Modules/Catalog/Services/IAdminCatalogBrandService.cs apps/api/Modules/Catalog/Services/AdminCatalogBrandService.cs apps/api/Modules/Catalog/Controllers/AdminCatalogBrandsController.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogBrandSqlTests.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogBrandServiceTests.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogBrandLogoEndpointTests.cs
git commit -m "feat: add admin brand logo endpoints"
```

### Task 6: PostgreSQL Behavior Tests For Image Safety

**Files:**
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogImagesDatabaseBehaviorTests.cs`

- [ ] **Step 1: Write opt-in PostgreSQL tests**

Use `[Collection(PostgresMigrationCollection.Name)]` and the existing early-return pattern:

```csharp
if (!_fixture.IsConfigured)
{
    return;
}
```

Create `AdminCatalogImagesDatabaseBehaviorTests.cs` with tests:

```csharp
[Fact]
public async Task ProductImages_AllowExactlyOneMainImagePerProduct()
{
    if (!_fixture.IsConfigured)
    {
        return;
    }

    await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
    var ids = await SeedProductWithTwoStoredProductImagesAsync(connection);

    await connection.ExecuteAsync(
        "INSERT INTO product_images (product_id, stored_file_id, alt, is_main) VALUES (@ProductId, @FileId, 'first', TRUE);",
        new { ids.ProductId, FileId = ids.FirstFileId });

    var exception = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
        "INSERT INTO product_images (product_id, stored_file_id, alt, is_main) VALUES (@ProductId, @FileId, 'second', TRUE);",
        new { ids.ProductId, FileId = ids.SecondFileId }));

    Assert.Equal(PostgresErrorCodes.UniqueViolation, exception.SqlState);
}

[Fact]
public async Task ProductImages_RejectBrandLogoStoredFile()
{
    if (!_fixture.IsConfigured)
    {
        return;
    }

    await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
    var ids = await SeedProductWithStoredFileAsync(connection, "brand_logo");

    var exception = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
        "INSERT INTO product_images (product_id, stored_file_id, alt, is_main) VALUES (@ProductId, @FileId, 'bad', TRUE);",
        new { ids.ProductId, ids.FileId }));

    Assert.Equal(PostgresErrorCodes.RaiseException, exception.SqlState);
}

[Fact]
public async Task BrandLogo_RejectsProductImageStoredFile()
{
    if (!_fixture.IsConfigured)
    {
        return;
    }

    await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
    var ids = await SeedBrandWithStoredFileAsync(connection, "product_image");

    var exception = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
        "UPDATE brands SET logo_file_id = @FileId WHERE id = @BrandId;",
        new { ids.BrandId, ids.FileId }));

    Assert.Equal(PostgresErrorCodes.RaiseException, exception.SqlState);
}
```

Add helper seed methods in the same test file. Use unique slugs based on `Guid.NewGuid().ToString("N")` to avoid cross-test collisions.

- [ ] **Step 2: Run without PostgreSQL**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter AdminCatalogImagesDatabaseBehaviorTests
```

Expected without `LINECOM_TEST_CONNECTION_STRING`: PASS by early return.

- [ ] **Step 3: Run with PostgreSQL when available**

```powershell
$env:LINECOM_TEST_CONNECTION_STRING="Host=localhost;Port=5432;Database=linecom_test;Username=postgres;Password=postgres"
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter AdminCatalogImagesDatabaseBehaviorTests
```

Expected with configured test database: PASS.

- [ ] **Step 4: Commit**

```powershell
git add tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogImagesDatabaseBehaviorTests.cs
git commit -m "test: cover admin catalog image database safety"
```

### Task 7: Full Images Verification

**Files:**
- Verify all files from Tasks 1-6.
- Modify docs only if verification proves a documented behavior mismatch.

- [ ] **Step 1: Run focused test suite**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "LocalStoredFileWriterTests|AdminCatalogImageSqlTests|AdminCatalogImageServiceTests|AdminCatalogProductImagesEndpointTests|AdminCatalogBrandSqlTests|AdminCatalogBrandServiceTests|AdminCatalogBrandLogoEndpointTests|AdminCatalogImagesDatabaseBehaviorTests|CatalogModuleRegistrationTests"
```

Expected: PASS. PostgreSQL behavior tests pass by early return when `LINECOM_TEST_CONNECTION_STRING` is not configured.

- [ ] **Step 2: Run broader admin catalog suite**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalog"
```

Expected: PASS.

- [ ] **Step 3: Run full backend test suite**

```powershell
dotnet test .\LineCom.sln
```

Expected: PASS. NU1900 warnings from unavailable NuGet vulnerability feed are acceptable only if there are 0 test failures.

- [ ] **Step 4: Run build**

```powershell
dotnet build .\LineCom.sln
```

Expected: PASS with 0 errors. NU1900 warnings are acceptable only if the vulnerability feed is unavailable.

- [ ] **Step 5: Inspect diff and technical debt**

```powershell
git diff --check
git status --short
rg -n "TODO|TBD|temporary|hack|EF|EntityFramework|DbContext" apps tests docs vault
```

Expected:

- `git diff --check` reports no whitespace errors;
- only intended files are modified;
- `admin-catalog-homepage-slice.png` remains untracked and is not staged;
- no Entity Framework usage is introduced;
- no intentional temporary decisions are left in code or docs.

- [ ] **Step 6: Manual storage sanity check**

Run the API locally only if needed for manual multipart verification. Use a test storage root and a test PostgreSQL database, then upload a JPEG through `PUT /api/admin/catalog/brands/{id}/logo` and `POST /api/admin/catalog/products/{id}/images`.

Expected:

- DB `stored_files.storage_key` begins with `storage/brands/admin/` or `storage/products/admin/`;
- physical files exist under `{Storage:RootPath}/brands/admin/...` and `{Storage:RootPath}/products/admin/...`;
- public URLs returned by API begin with `/storage/`;
- deleting product image or brand logo sets `stored_files.status = 'deleted'` and does not physically remove the file.

- [ ] **Step 7: Commit docs only if changed**

If documentation changed because verification revealed a mismatch:

```powershell
git add docs/superpowers/specs/2026-05-11-admin-catalog-homepage-design.md vault/Человекочитаемое
git commit -m "docs: update admin catalog image notes"
```

If no docs changed, do not create an empty commit.

## Handoff Notes

After this plan is complete, the backend image API slice is ready for a separate frontend UI plan. The next UI plan should build product image tab controls and brand logo controls against these endpoints, without changing homepage mutation endpoints.
