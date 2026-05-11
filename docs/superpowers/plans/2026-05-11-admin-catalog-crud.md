# Admin Catalog CRUD Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the backend admin CRUD/API foundation for catalog categories, brands, category attributes/options, products, product attributes, and duplicate-candidate access.

**Architecture:** This plan adds authenticated staff-only ASP.NET Core controllers under `/api/admin/catalog`, thin services for authorization/input normalization/business rules, and Dapper repositories with SQL kept out of controllers. PostgreSQL remains the source of data integrity; DbUp migrations are not expected in this plan unless a verification task proves a schema mismatch. Image upload endpoints, homepage mutation endpoints, import/export, UI, and Local FileStorage image handling stay in later plans.

**Tech Stack:** ASP.NET Core Web API, cookie auth + CSRF for mutations, PostgreSQL, Npgsql, Dapper, xUnit.

---

## Scope

This plan implements backend CRUD/API contracts only:

- categories: list/tree/detail/create/update/delete/move/sort;
- brands: list/detail/create/update/delete;
- category attributes and `select` options: list/create/update/delete/inherit-from-parent;
- products: list/detail/create/update/delete, publication readiness, product attribute values;
- duplicate-candidates endpoint that wraps the foundation query from `admin-catalog-foundation`.

Out of scope:

- product image upload and ordering endpoints;
- brand logo upload endpoint;
- homepage section mutation endpoints;
- frontend admin UI;
- Excel import/export;
- audit log;
- LLM duplicate checking;
- pricing, stock accounting, online payment.

## Source Of Truth And Existing Patterns

Read before implementing:

- `docs/superpowers/specs/2026-05-11-admin-catalog-homepage-design.md`
- `docs/superpowers/plans/2026-05-11-admin-catalog-foundation.md`
- `vault/Человекочитаемое/Архитектура backend и БД.md`
- `vault/Человекочитаемое/Сквозные требования.md`
- `apps/api/Modules/Requests/Controllers/AdminRequestsController.cs`
- `apps/api/Modules/Requests/Services/AdminRequestService.cs`
- `apps/api/Modules/Requests/Repositories/DapperAdminRequestRepository.cs`
- `tests/LineCom.Api.Tests/Modules/Requests/AdminRequestsEndpointTests.cs`
- `tests/LineCom.Api.Tests/Modules/Requests/AdminRequestServiceTests.cs`

Follow these patterns:

- controllers use `[Authorize]`, `[ApiController]`, typed DTOs, and no SQL;
- mutation endpoints use `[RequireCsrfToken]`;
- staff access means current user role is `seller` or `admin`;
- services call `IAuthCurrentUserService.GetCurrentSessionAsync` and throw `AuthErrors.Forbidden()` for non-staff;
- repository implementations use `IDbConnectionFactory`, Dapper `CommandDefinition`, and explicit transactions for multi-step writes;
- errors use `ApiException` through module-specific error helpers.

## File Structure

Create:

- `apps/api/Modules/Catalog/DTOs/AdminCatalogCategoryDtos.cs`
- `apps/api/Modules/Catalog/DTOs/AdminCatalogBrandDtos.cs`
- `apps/api/Modules/Catalog/DTOs/AdminCatalogAttributeDtos.cs`
- `apps/api/Modules/Catalog/DTOs/AdminCatalogProductDtos.cs`
- `apps/api/Modules/Catalog/Services/AdminCatalogErrors.cs`
- `apps/api/Modules/Catalog/Services/AdminCatalogInput.cs`
- `apps/api/Modules/Catalog/Services/AdminCatalogStaffGuard.cs`
- `apps/api/Modules/Catalog/Services/IAdminCatalogCategoryService.cs`
- `apps/api/Modules/Catalog/Services/AdminCatalogCategoryService.cs`
- `apps/api/Modules/Catalog/Services/IAdminCatalogBrandService.cs`
- `apps/api/Modules/Catalog/Services/AdminCatalogBrandService.cs`
- `apps/api/Modules/Catalog/Services/IAdminCatalogAttributeService.cs`
- `apps/api/Modules/Catalog/Services/AdminCatalogAttributeService.cs`
- `apps/api/Modules/Catalog/Services/IAdminCatalogProductService.cs`
- `apps/api/Modules/Catalog/Services/AdminCatalogProductService.cs`
- `apps/api/Modules/Catalog/Repositories/IAdminCatalogCategoryRepository.cs`
- `apps/api/Modules/Catalog/Repositories/AdminCatalogCategorySql.cs`
- `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogCategoryRepository.cs`
- `apps/api/Modules/Catalog/Repositories/IAdminCatalogBrandRepository.cs`
- `apps/api/Modules/Catalog/Repositories/AdminCatalogBrandSql.cs`
- `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogBrandRepository.cs`
- `apps/api/Modules/Catalog/Repositories/IAdminCatalogAttributeRepository.cs`
- `apps/api/Modules/Catalog/Repositories/AdminCatalogAttributeSql.cs`
- `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogAttributeRepository.cs`
- `apps/api/Modules/Catalog/Repositories/IAdminCatalogProductRepository.cs`
- `apps/api/Modules/Catalog/Repositories/AdminCatalogProductSql.cs`
- `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogProductRepository.cs`
- `apps/api/Modules/Catalog/Controllers/AdminCatalogCategoriesController.cs`
- `apps/api/Modules/Catalog/Controllers/AdminCatalogBrandsController.cs`
- `apps/api/Modules/Catalog/Controllers/AdminCatalogAttributesController.cs`
- `apps/api/Modules/Catalog/Controllers/AdminCatalogProductsController.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogCategoryServiceTests.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogBrandServiceTests.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogAttributeServiceTests.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductServiceTests.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogCategoriesEndpointTests.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogBrandsEndpointTests.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogAttributesEndpointTests.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductsEndpointTests.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogCategorySqlTests.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogBrandSqlTests.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogAttributeSqlTests.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductSqlTests.cs`

Modify:

- `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs`
- `tests/LineCom.Api.Tests/Modules/Catalog/CatalogModuleRegistrationTests.cs`

Do not modify:

- frontend files;
- file upload infrastructure;
- `apps/dbmigrator/Migrations` unless a test proves a missing schema requirement;
- `admin-catalog-homepage-slice.png`.

## Shared Contracts

Use these shared constants and status values across tasks:

```csharp
internal static class AdminCatalogRoles
{
    public const string Seller = "seller";
    public const string Admin = "admin";
}
```

Use these publication readiness issue codes:

```csharp
public static class AdminCatalogReadinessIssueCodes
{
    public const string ProductInactive = "product_inactive";
    public const string ProductDraft = "product_draft";
    public const string MissingName = "missing_name";
    public const string MissingSlug = "missing_slug";
    public const string MissingCategory = "missing_category";
    public const string InactiveCategory = "inactive_category";
    public const string MissingSaleUnit = "missing_sale_unit";
    public const string MissingUnitQuantity = "missing_unit_quantity";
    public const string MissingRequiredAttribute = "missing_required_attribute";
    public const string InvalidAttributeValue = "invalid_attribute_value";
}
```

Use exact endpoint paths:

```text
GET    /api/admin/catalog/categories
POST   /api/admin/catalog/categories
GET    /api/admin/catalog/categories/{id}
PUT    /api/admin/catalog/categories/{id}
DELETE /api/admin/catalog/categories/{id}
PUT    /api/admin/catalog/categories/{id}/move
PUT    /api/admin/catalog/categories/{id}/sort

GET    /api/admin/catalog/brands
POST   /api/admin/catalog/brands
GET    /api/admin/catalog/brands/{id}
PUT    /api/admin/catalog/brands/{id}
DELETE /api/admin/catalog/brands/{id}

GET    /api/admin/catalog/categories/{categoryId}/attributes
POST   /api/admin/catalog/categories/{categoryId}/attributes
PUT    /api/admin/catalog/categories/{categoryId}/attributes/{attributeId}
DELETE /api/admin/catalog/categories/{categoryId}/attributes/{attributeId}
POST   /api/admin/catalog/categories/{categoryId}/attributes/inherit-from-parent
POST   /api/admin/catalog/categories/{categoryId}/attributes/{attributeId}/options
PUT    /api/admin/catalog/categories/{categoryId}/attributes/{attributeId}/options/{optionId}
DELETE /api/admin/catalog/categories/{categoryId}/attributes/{attributeId}/options/{optionId}

GET    /api/admin/catalog/products
POST   /api/admin/catalog/products
GET    /api/admin/catalog/products/{id}
PUT    /api/admin/catalog/products/{id}
DELETE /api/admin/catalog/products/{id}
PUT    /api/admin/catalog/products/{id}/attributes
GET    /api/admin/catalog/products/duplicate-candidates
```

---

### Task 1: Admin Catalog Shared Services And Registration

**Files:**
- Create: `apps/api/Modules/Catalog/Services/AdminCatalogErrors.cs`
- Create: `apps/api/Modules/Catalog/Services/AdminCatalogInput.cs`
- Create: `apps/api/Modules/Catalog/Services/AdminCatalogStaffGuard.cs`
- Modify: `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs`
- Modify: `tests/LineCom.Api.Tests/Modules/Catalog/CatalogModuleRegistrationTests.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogStaffGuardTests.cs`

- [ ] **Step 1: Write failing staff guard and registration tests**

Add `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogStaffGuardTests.cs`:

```csharp
using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.Services;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminCatalogStaffGuardTests
{
    [Theory]
    [InlineData("seller")]
    [InlineData("admin")]
    public async Task RequireStaffAsync_AllowsSellerAndAdmin(string role)
    {
        var guard = new AdminCatalogStaffGuard(new ReturningCurrentUserService(role));

        var user = await guard.RequireStaffAsync(new DefaultHttpContext(), CancellationToken.None);

        Assert.Equal(role, user.Role);
    }

    [Fact]
    public async Task RequireStaffAsync_RejectsCustomer()
    {
        var guard = new AdminCatalogStaffGuard(new ReturningCurrentUserService("customer"));

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            guard.RequireStaffAsync(new DefaultHttpContext(), CancellationToken.None));

        Assert.Equal("auth.forbidden", exception.Code);
    }

    private sealed class ReturningCurrentUserService : IAuthCurrentUserService
    {
        private readonly string _role;

        public ReturningCurrentUserService(string role)
        {
            _role = role;
        }

        public Task<AuthSessionDto> GetCurrentSessionAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuthSessionDto(
                new CurrentUserDto(
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    "Staff User",
                    "staff@example.com",
                    null,
                    _role),
                "csrf-token"));
        }
    }
}
```

Add this assertion to `CatalogModuleRegistrationTests.cs`:

```csharp
[Fact]
public void AddCatalogModule_RegistersAdminCatalogStaffGuardAsScoped()
{
    var services = new ServiceCollection();
    services.AddCatalogModule();

    var descriptor = Assert.Single(services, service => service.ServiceType == typeof(IAdminCatalogStaffGuard));

    Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    Assert.Equal(typeof(AdminCatalogStaffGuard), descriptor.ImplementationType);
}
```

- [ ] **Step 2: Run tests to verify failure**

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalogStaffGuardTests|CatalogModuleRegistrationTests"
```

Expected: FAIL because `IAdminCatalogStaffGuard` and `AdminCatalogStaffGuard` do not exist.

- [ ] **Step 3: Add shared services**

Create `apps/api/Modules/Catalog/Services/AdminCatalogErrors.cs`:

```csharp
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Services;

internal static class AdminCatalogErrors
{
    public static ApiException InvalidRequest()
    {
        return new ApiException(
            "admin_catalog.invalid_request",
            "Некорректный запрос каталога.",
            StatusCodes.Status400BadRequest);
    }

    public static ApiException CategoryNotFound()
    {
        return new ApiException("admin_catalog.category_not_found", "Категория не найдена.", StatusCodes.Status404NotFound);
    }

    public static ApiException BrandNotFound()
    {
        return new ApiException("admin_catalog.brand_not_found", "Бренд не найден.", StatusCodes.Status404NotFound);
    }

    public static ApiException ProductNotFound()
    {
        return new ApiException("admin_catalog.product_not_found", "Товар не найден.", StatusCodes.Status404NotFound);
    }

    public static ApiException SlugAlreadyExists()
    {
        return new ApiException("admin_catalog.slug_already_exists", "Slug уже используется.", StatusCodes.Status409Conflict);
    }

    public static ApiException SkuAlreadyExists()
    {
        return new ApiException("admin_catalog.sku_already_exists", "SKU уже используется.", StatusCodes.Status409Conflict);
    }

    public static ApiException ExternalIdAlreadyExists()
    {
        return new ApiException("admin_catalog.external_id_already_exists", "ExternalId уже используется.", StatusCodes.Status409Conflict);
    }

    public static ApiException EntityInUse(string message)
    {
        return new ApiException("admin_catalog.entity_in_use", message, StatusCodes.Status409Conflict);
    }
}
```

Create `apps/api/Modules/Catalog/Services/AdminCatalogInput.cs`:

```csharp
namespace LineCom.Api.Modules.Catalog.Services;

internal static class AdminCatalogInput
{
    public static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static string RequireText(string? value)
    {
        return NormalizeText(value) ?? throw AdminCatalogErrors.InvalidRequest();
    }

    public static int NormalizePage(int? value)
    {
        return value is null or < 1 ? 1 : value.Value;
    }

    public static int NormalizePageSize(int? value)
    {
        return value is null or < 1 ? 20 : Math.Min(value.Value, 60);
    }
}
```

Create `apps/api/Modules/Catalog/Services/AdminCatalogStaffGuard.cs`:

```csharp
using LineCom.Api.Modules.Auth.DTOs;
using LineCom.Api.Modules.Auth.Services;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Services;

public interface IAdminCatalogStaffGuard
{
    Task<CurrentUserDto> RequireStaffAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
}

public sealed class AdminCatalogStaffGuard : IAdminCatalogStaffGuard
{
    private readonly IAuthCurrentUserService _currentUserService;

    public AdminCatalogStaffGuard(IAuthCurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task<CurrentUserDto> RequireStaffAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var session = await _currentUserService.GetCurrentSessionAsync(httpContext, cancellationToken);
        if (session.User.Role is "seller" or "admin")
        {
            return session.User;
        }

        throw AuthErrors.Forbidden();
    }
}
```

Modify `CatalogServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<IAdminCatalogStaffGuard, AdminCatalogStaffGuard>();
```

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalogStaffGuardTests|CatalogModuleRegistrationTests"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add apps/api/Modules/Catalog/Services/AdminCatalogErrors.cs apps/api/Modules/Catalog/Services/AdminCatalogInput.cs apps/api/Modules/Catalog/Services/AdminCatalogStaffGuard.cs apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogStaffGuardTests.cs tests/LineCom.Api.Tests/Modules/Catalog/CatalogModuleRegistrationTests.cs
git commit -m "feat: add admin catalog shared services"
```

### Task 2: Category Admin CRUD

**Files:**
- Create: `apps/api/Modules/Catalog/DTOs/AdminCatalogCategoryDtos.cs`
- Create: `apps/api/Modules/Catalog/Repositories/IAdminCatalogCategoryRepository.cs`
- Create: `apps/api/Modules/Catalog/Repositories/AdminCatalogCategorySql.cs`
- Create: `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogCategoryRepository.cs`
- Create: `apps/api/Modules/Catalog/Services/IAdminCatalogCategoryService.cs`
- Create: `apps/api/Modules/Catalog/Services/AdminCatalogCategoryService.cs`
- Create: `apps/api/Modules/Catalog/Controllers/AdminCatalogCategoriesController.cs`
- Modify: `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogCategorySqlTests.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogCategoryServiceTests.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogCategoriesEndpointTests.cs`
- Modify: `tests/LineCom.Api.Tests/Modules/Catalog/CatalogModuleRegistrationTests.cs`

- [ ] **Step 1: Write failing category SQL and registration tests**

Create `AdminCatalogCategorySqlTests.cs`:

```csharp
using LineCom.Api.Modules.Catalog.Repositories;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminCatalogCategorySqlTests
{
    [Fact]
    public void ListCategories_SelectsAdminFieldsAndUsageCounts()
    {
        Assert.Contains("FROM categories category", AdminCatalogCategorySql.ListCategories);
        Assert.Contains("COUNT(product.id)::int AS \"ProductsCount\"", AdminCatalogCategorySql.ListCategories);
        Assert.Contains("COUNT(child.id)::int AS \"ChildrenCount\"", AdminCatalogCategorySql.ListCategories);
        Assert.Contains("ORDER BY category.parent_id NULLS FIRST, category.sort_order, category.name", AdminCatalogCategorySql.ListCategories);
    }

    [Fact]
    public void DeleteCategory_BlocksUsedCategories()
    {
        Assert.Contains("FROM products", AdminCatalogCategorySql.CountCategoryUsage);
        Assert.Contains("FROM categories child", AdminCatalogCategorySql.CountCategoryUsage);
        Assert.Contains("FROM homepage_section_items", AdminCatalogCategorySql.CountCategoryUsage);
    }
}
```

Add registration tests for `IAdminCatalogCategoryService` and `IAdminCatalogCategoryRepository`.

- [ ] **Step 2: Run failing tests**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalogCategorySqlTests|CatalogModuleRegistrationTests"
```

Expected: FAIL because category admin types do not exist.

- [ ] **Step 3: Add category DTOs and repository contracts**

Create `AdminCatalogCategoryDtos.cs`:

```csharp
namespace LineCom.Api.Modules.Catalog.DTOs;

public sealed record AdminCategoryListQuery(
    int? Page,
    int? PageSize,
    Guid? ParentId,
    string? Search,
    bool? IsActive);

public sealed record AdminCategoryListResponse(
    IReadOnlyList<AdminCategoryListItemDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record AdminCategoryListItemDto(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Slug,
    int SortOrder,
    bool IsActive,
    bool IsVisibleInMenu,
    int ProductsCount,
    int ChildrenCount);

public sealed record AdminCategoryDetailDto(
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

public sealed record UpsertAdminCategoryCommand(
    Guid? ParentId,
    string? Name,
    string? Slug,
    string? Description,
    string? SeoTitle,
    string? SeoDescription,
    string? H1,
    int? SortOrder,
    bool? IsActive,
    bool? IsVisibleInMenu);

public sealed record MoveAdminCategoryCommand(Guid? ParentId);

public sealed record SortAdminCategoryCommand(int SortOrder);
```

Create `IAdminCatalogCategoryRepository.cs` with records:

```csharp
namespace LineCom.Api.Modules.Catalog.Repositories;

public sealed record AdminCategoryReadListQuery(int Page, int PageSize, Guid? ParentId, string? Search, bool? IsActive);
public sealed record AdminCategoryListRecordResponse(IReadOnlyList<AdminCategoryRecord> Items, int TotalItems);
public sealed record AdminCategoryRecord(Guid Id, Guid? ParentId, string Name, string Slug, string? Description, string? SeoTitle, string? SeoDescription, string? H1, int SortOrder, bool IsActive, bool IsVisibleInMenu, int ProductsCount, int ChildrenCount);
public sealed record AdminCategoryUpsert(Guid? ParentId, string Name, string Slug, string? Description, string? SeoTitle, string? SeoDescription, string? H1, int SortOrder, bool IsActive, bool IsVisibleInMenu);

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
```

- [ ] **Step 4: Add category SQL and repository**

Create `AdminCatalogCategorySql.cs` with SQL constants for count/list/detail/insert/update/move/sort/usage/delete. Include:

```sql
WHERE (@ParentId IS NULL OR category.parent_id = @ParentId)
AND (@Search IS NULL OR category.name ILIKE '%' || @Search || '%' OR category.slug ILIKE '%' || @Search || '%')
AND (@IsActive IS NULL OR category.is_active = @IsActive)
```

Usage SQL must count children, products, and homepage items:

```sql
SELECT
    (
        SELECT COUNT(*)::int FROM categories child WHERE child.parent_id = @Id
    )
    + (
        SELECT COUNT(*)::int FROM products product WHERE product.primary_category_id = @Id
    )
    + (
        SELECT COUNT(*)::int FROM homepage_section_items item WHERE item.category_id = @Id
    );
```

Create `DapperAdminCatalogCategoryRepository.cs` with Dapper methods matching the interface and transactions for move/sort/delete when needed.

- [ ] **Step 5: Add category service tests and service**

Create `AdminCatalogCategoryServiceTests.cs` covering:

- seller/admin allowed, customer forbidden;
- trims required strings;
- blank `Name` or `Slug` throws `admin_catalog.invalid_request`;
- delete with usage count > 0 throws `admin_catalog.entity_in_use`;
- missing detail throws `admin_catalog.category_not_found`.

Create `IAdminCatalogCategoryService.cs` and `AdminCatalogCategoryService.cs` mapping records to DTOs, computing total pages, and using `IAdminCatalogStaffGuard`.

- [ ] **Step 6: Add category endpoints and endpoint tests**

Create `AdminCatalogCategoriesController.cs`:

```csharp
using LineCom.Api.Modules.Auth.Services;
using LineCom.Api.Modules.Catalog.DTOs;
using LineCom.Api.Modules.Catalog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LineCom.Api.Modules.Catalog.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/catalog/categories")]
public sealed class AdminCatalogCategoriesController : ControllerBase
{
    private readonly IAdminCatalogCategoryService _service;

    public AdminCatalogCategoriesController(IAdminCatalogCategoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<AdminCategoryListResponse>> GetCategories(
        [FromQuery] AdminCategoryListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.GetCategoriesAsync(HttpContext, query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminCategoryDetailDto>> GetCategory(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _service.GetCategoryAsync(HttpContext, id, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpPost]
    public async Task<ActionResult<AdminCategoryDetailDto>> CreateCategory(
        UpsertAdminCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var created = await _service.CreateCategoryAsync(HttpContext, command, cancellationToken);
        return CreatedAtAction(nameof(GetCategory), new { id = created.Id }, created);
    }

    [RequireCsrfToken]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminCategoryDetailDto>> UpdateCategory(
        Guid id,
        UpsertAdminCategoryCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.UpdateCategoryAsync(HttpContext, id, command, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteCategoryAsync(HttpContext, id, cancellationToken);
        return NoContent();
    }

    [RequireCsrfToken]
    [HttpPut("{id:guid}/move")]
    public async Task<ActionResult<AdminCategoryDetailDto>> MoveCategory(
        Guid id,
        MoveAdminCategoryCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.MoveCategoryAsync(HttpContext, id, command, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpPut("{id:guid}/sort")]
    public async Task<ActionResult<AdminCategoryDetailDto>> SortCategory(
        Guid id,
        SortAdminCategoryCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.SortCategoryAsync(HttpContext, id, command, cancellationToken));
    }
}
```

Create endpoint tests following `AdminRequestsEndpointTests`: unauthenticated returns 401, customer returns 403, seller can list/detail, mutations require CSRF.

- [ ] **Step 7: Register and run tests**

Register:

```csharp
services.AddScoped<IAdminCatalogCategoryRepository, DapperAdminCatalogCategoryRepository>();
services.AddScoped<IAdminCatalogCategoryService, AdminCatalogCategoryService>();
```

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalogCategorySqlTests|AdminCatalogCategoryServiceTests|AdminCatalogCategoriesEndpointTests|CatalogModuleRegistrationTests"
```

Expected: PASS.

- [ ] **Step 8: Commit**

```powershell
git add apps/api/Modules/Catalog/DTOs/AdminCatalogCategoryDtos.cs apps/api/Modules/Catalog/Repositories/IAdminCatalogCategoryRepository.cs apps/api/Modules/Catalog/Repositories/AdminCatalogCategorySql.cs apps/api/Modules/Catalog/Repositories/DapperAdminCatalogCategoryRepository.cs apps/api/Modules/Catalog/Services/IAdminCatalogCategoryService.cs apps/api/Modules/Catalog/Services/AdminCatalogCategoryService.cs apps/api/Modules/Catalog/Controllers/AdminCatalogCategoriesController.cs apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogCategorySqlTests.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogCategoryServiceTests.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogCategoriesEndpointTests.cs tests/LineCom.Api.Tests/Modules/Catalog/CatalogModuleRegistrationTests.cs
git commit -m "feat: add admin category crud api"
```

### Task 3: Brand Admin CRUD

**Files:**
- Create: `apps/api/Modules/Catalog/DTOs/AdminCatalogBrandDtos.cs`
- Create: `apps/api/Modules/Catalog/Repositories/IAdminCatalogBrandRepository.cs`
- Create: `apps/api/Modules/Catalog/Repositories/AdminCatalogBrandSql.cs`
- Create: `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogBrandRepository.cs`
- Create: `apps/api/Modules/Catalog/Services/IAdminCatalogBrandService.cs`
- Create: `apps/api/Modules/Catalog/Services/AdminCatalogBrandService.cs`
- Create: `apps/api/Modules/Catalog/Controllers/AdminCatalogBrandsController.cs`
- Modify: `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogBrandSqlTests.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogBrandServiceTests.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogBrandsEndpointTests.cs`
- Modify: `tests/LineCom.Api.Tests/Modules/Catalog/CatalogModuleRegistrationTests.cs`

- [ ] **Step 1: Write failing brand tests**

Create SQL tests asserting fields:

```csharp
Assert.Contains("FROM brands brand", AdminCatalogBrandSql.ListBrands);
Assert.Contains("COUNT(product.id)::int AS \"ProductsCount\"", AdminCatalogBrandSql.ListBrands);
Assert.Contains("brand.logo_file_id AS \"LogoFileId\"", AdminCatalogBrandSql.GetBrand);
Assert.Contains("DELETE FROM brands", AdminCatalogBrandSql.DeleteBrand);
```

Create service tests for staff authorization, blank name/slug invalid, missing brand not found, delete with products count > 0 throws `entity_in_use`, and quick-create command requires only name.

- [ ] **Step 2: Run failing tests**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalogBrandSqlTests|AdminCatalogBrandServiceTests|CatalogModuleRegistrationTests"
```

Expected: FAIL because brand admin types do not exist.

- [ ] **Step 3: Add brand DTOs and repository contracts**

Create DTOs:

```csharp
namespace LineCom.Api.Modules.Catalog.DTOs;

public sealed record AdminBrandListQuery(int? Page, int? PageSize, string? Search, bool? IsActive);
public sealed record AdminBrandListResponse(IReadOnlyList<AdminBrandListItemDto> Items, int Page, int PageSize, int TotalItems, int TotalPages);
public sealed record AdminBrandListItemDto(Guid Id, string Name, string Slug, bool IsActive, int ProductsCount);
public sealed record AdminBrandDetailDto(Guid Id, string Name, string Slug, string? Description, string? SeoTitle, string? SeoDescription, Guid? LogoFileId, bool IsActive, int ProductsCount);
public sealed record UpsertAdminBrandCommand(string? Name, string? Slug, string? Description, string? SeoTitle, string? SeoDescription, Guid? LogoFileId, bool? IsActive);
public sealed record QuickCreateAdminBrandCommand(string? Name);
```

Create repository records and interface for list/detail/create/update/delete/quick-create. Quick-create must generate slug in service by lowercasing ASCII-safe transliteration is not required; use a deterministic fallback slug `brand-{Guid.NewGuid():N}` only if no slug generator exists.

- [ ] **Step 4: Add SQL, repository, service, controller**

Controller path: `/api/admin/catalog/brands`.

Mutations:

- `POST` create full brand;
- `POST quick` is not in approved API, so do not create a separate endpoint; quick-create is service/repository support for future product editor, not exposed in this plan.

Delete policy:

- if products use brand, service throws `AdminCatalogErrors.EntityInUse("Бренд используется товарами.")`;
- otherwise physical delete is allowed.

- [ ] **Step 5: Register and run tests**

Register repository and service. Add registration tests.

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalogBrandSqlTests|AdminCatalogBrandServiceTests|AdminCatalogBrandsEndpointTests|CatalogModuleRegistrationTests"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add apps/api/Modules/Catalog/DTOs/AdminCatalogBrandDtos.cs apps/api/Modules/Catalog/Repositories/IAdminCatalogBrandRepository.cs apps/api/Modules/Catalog/Repositories/AdminCatalogBrandSql.cs apps/api/Modules/Catalog/Repositories/DapperAdminCatalogBrandRepository.cs apps/api/Modules/Catalog/Services/IAdminCatalogBrandService.cs apps/api/Modules/Catalog/Services/AdminCatalogBrandService.cs apps/api/Modules/Catalog/Controllers/AdminCatalogBrandsController.cs apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogBrandSqlTests.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogBrandServiceTests.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogBrandsEndpointTests.cs tests/LineCom.Api.Tests/Modules/Catalog/CatalogModuleRegistrationTests.cs
git commit -m "feat: add admin brand crud api"
```

### Task 4: Category Attributes And Options Admin CRUD

**Files:**
- Create: `apps/api/Modules/Catalog/DTOs/AdminCatalogAttributeDtos.cs`
- Create: `apps/api/Modules/Catalog/Repositories/IAdminCatalogAttributeRepository.cs`
- Create: `apps/api/Modules/Catalog/Repositories/AdminCatalogAttributeSql.cs`
- Create: `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogAttributeRepository.cs`
- Create: `apps/api/Modules/Catalog/Services/IAdminCatalogAttributeService.cs`
- Create: `apps/api/Modules/Catalog/Services/AdminCatalogAttributeService.cs`
- Create: `apps/api/Modules/Catalog/Controllers/AdminCatalogAttributesController.cs`
- Modify: `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogAttributeSqlTests.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogAttributeServiceTests.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogAttributesEndpointTests.cs`
- Modify: `tests/LineCom.Api.Tests/Modules/Catalog/CatalogModuleRegistrationTests.cs`

- [ ] **Step 1: Write failing tests**

Tests must cover:

- list loads category attributes and select options;
- allowed types are `text`, `number`, `select`, `boolean`;
- cannot change attribute type if values exist;
- cannot delete attribute if values exist;
- cannot delete option if product values use it;
- inherit-from-parent copies missing attributes and options and skips duplicates by `code`.

- [ ] **Step 2: Run failing tests**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalogAttributeSqlTests|AdminCatalogAttributeServiceTests|CatalogModuleRegistrationTests"
```

Expected: FAIL because attribute admin types do not exist.

- [ ] **Step 3: Add DTOs and repository contracts**

Create DTOs:

```csharp
namespace LineCom.Api.Modules.Catalog.DTOs;

public sealed record AdminCategoryAttributesResponse(IReadOnlyList<AdminCategoryAttributeDto> Items);
public sealed record AdminCategoryAttributeDto(Guid Id, Guid CategoryId, string Name, string Code, string Type, string? Unit, bool IsRequired, bool IsFilterable, bool IsComparable, bool IsVisibleInProduct, bool IsSeoImportant, bool IsUsedInGeneratedName, int SortOrder, bool IsActive, int ProductValuesCount, IReadOnlyList<AdminAttributeOptionDto> Options);
public sealed record AdminAttributeOptionDto(Guid Id, string Value, string Slug, string NormalizedValue, int SortOrder, bool IsActive, int ProductValuesCount);
public sealed record UpsertAdminCategoryAttributeCommand(string? Name, string? Code, string? Type, string? Unit, bool? IsRequired, bool? IsFilterable, bool? IsComparable, bool? IsVisibleInProduct, bool? IsSeoImportant, bool? IsUsedInGeneratedName, int? SortOrder, bool? IsActive);
public sealed record UpsertAdminAttributeOptionCommand(string? Value, string? Slug, string? NormalizedValue, int? SortOrder, bool? IsActive);
public sealed record InheritAdminCategoryAttributesResponse(int Added, int Skipped);
```

Create repository interface methods for attributes/options and inheritance transaction.

- [ ] **Step 4: Implement SQL and service**

SQL must use existing tables:

- `category_attributes`;
- `attribute_options`;
- `attribute_value_aliases`;
- `product_attribute_values`.

Service rules:

- blank name/code/type invalid;
- type must be one of `text`, `number`, `select`, `boolean`;
- changing type is allowed only when product values count is 0;
- physical delete is allowed only when product values count is 0;
- option delete is allowed only when no `product_attribute_values.attribute_option_id` references it;
- inherit-from-parent copies missing attributes by `code` and copies options for copied `select` attributes.

- [ ] **Step 5: Add controller and endpoint tests**

Controller base routes:

```csharp
[Route("api/admin/catalog/categories/{categoryId:guid}/attributes")]
```

Endpoint tests must cover:

- unauthenticated 401;
- customer 403;
- seller list OK;
- mutation without CSRF 403;
- mutation with CSRF calls service.

- [ ] **Step 6: Register and run tests**

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalogAttributeSqlTests|AdminCatalogAttributeServiceTests|AdminCatalogAttributesEndpointTests|CatalogModuleRegistrationTests"
```

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add apps/api/Modules/Catalog/DTOs/AdminCatalogAttributeDtos.cs apps/api/Modules/Catalog/Repositories/IAdminCatalogAttributeRepository.cs apps/api/Modules/Catalog/Repositories/AdminCatalogAttributeSql.cs apps/api/Modules/Catalog/Repositories/DapperAdminCatalogAttributeRepository.cs apps/api/Modules/Catalog/Services/IAdminCatalogAttributeService.cs apps/api/Modules/Catalog/Services/AdminCatalogAttributeService.cs apps/api/Modules/Catalog/Controllers/AdminCatalogAttributesController.cs apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogAttributeSqlTests.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogAttributeServiceTests.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogAttributesEndpointTests.cs tests/LineCom.Api.Tests/Modules/Catalog/CatalogModuleRegistrationTests.cs
git commit -m "feat: add admin category attribute crud api"
```

### Task 5: Product Admin List Detail And Basic CRUD

**Files:**
- Create: `apps/api/Modules/Catalog/DTOs/AdminCatalogProductDtos.cs`
- Create: `apps/api/Modules/Catalog/Repositories/IAdminCatalogProductRepository.cs`
- Create: `apps/api/Modules/Catalog/Repositories/AdminCatalogProductSql.cs`
- Create: `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogProductRepository.cs`
- Create: `apps/api/Modules/Catalog/Services/IAdminCatalogProductService.cs`
- Create: `apps/api/Modules/Catalog/Services/AdminCatalogProductService.cs`
- Create: `apps/api/Modules/Catalog/Controllers/AdminCatalogProductsController.cs`
- Modify: `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductSqlTests.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductServiceTests.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductsEndpointTests.cs`
- Modify: `tests/LineCom.Api.Tests/Modules/Catalog/CatalogModuleRegistrationTests.cs`

- [ ] **Step 1: Write failing product tests**

Cover:

- list filters by category, brand, active status, publish status, search;
- detail loads product, brand, category, attributes, images summary;
- create/update normalizes text and rejects blank required fields;
- duplicate hard identities map to conflict errors for slug/SKU/externalId;
- delete product with request items or homepage items throws `entity_in_use`;
- inactive product remains returned in admin list/detail.

- [ ] **Step 2: Run failing tests**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalogProductSqlTests|AdminCatalogProductServiceTests|CatalogModuleRegistrationTests"
```

Expected: FAIL because product admin types do not exist.

- [ ] **Step 3: Add product DTOs and repository contracts**

Create DTOs:

```csharp
namespace LineCom.Api.Modules.Catalog.DTOs;

public sealed record AdminProductListQuery(int? Page, int? PageSize, Guid? CategoryId, Guid? BrandId, bool? IsActive, string? PublishStatus, string? Search);
public sealed record AdminProductListResponse(IReadOnlyList<AdminProductListItemDto> Items, int Page, int PageSize, int TotalItems, int TotalPages);
public sealed record AdminProductListItemDto(Guid Id, string Name, string Slug, string? Sku, string? ExternalId, string CategoryName, string CategorySlug, string? BrandName, string PublishStatus, bool IsActive, string AvailabilityStatus, int SortOrder, AdminProductReadinessDto Readiness);
public sealed record AdminProductDetailDto(Guid Id, Guid CategoryId, string CategoryName, Guid? BrandId, string? BrandName, string Name, string Slug, string? Sku, string? ExternalId, string? Description, string? ShortDescription, string AvailabilityStatus, string SaleUnit, string UnitQuantity, string PublishStatus, bool IsActive, string? SeoTitle, string? SeoDescription, string? H1, int SortOrder, AdminProductReadinessDto Readiness, IReadOnlyList<AdminProductAttributeValueDto> Attributes);
public sealed record AdminProductReadinessDto(bool CanPublish, IReadOnlyList<AdminProductReadinessIssueDto> Issues);
public sealed record AdminProductReadinessIssueDto(string Code, string Message);
public sealed record AdminProductAttributeValueDto(Guid AttributeId, string Code, string Name, string Type, string? Unit, string? ValueText, decimal? ValueNumber, bool? ValueBoolean, Guid? AttributeOptionId, string? OptionValue);
public sealed record UpsertAdminProductCommand(Guid? CategoryId, Guid? BrandId, string? Name, string? Slug, string? Sku, string? ExternalId, string? Description, string? ShortDescription, string? AvailabilityStatus, string? SaleUnit, string? UnitQuantity, string? PublishStatus, bool? IsActive, string? SeoTitle, string? SeoDescription, string? H1, int? SortOrder);
```

Repository contracts must include list/detail/create/update/delete/count usage and duplicate hard-identity checks.

- [ ] **Step 4: Implement SQL and repository**

List SQL must include:

```sql
FROM products product
INNER JOIN categories category ON category.id = product.primary_category_id
LEFT JOIN brands brand ON brand.id = product.brand_id
WHERE (@CategoryId IS NULL OR product.primary_category_id = @CategoryId)
    AND (@BrandId IS NULL OR product.brand_id = @BrandId)
    AND (@IsActive IS NULL OR product.is_active = @IsActive)
    AND (@PublishStatus IS NULL OR product.publish_status = @PublishStatus)
```

Delete usage SQL must count:

```sql
SELECT
    (SELECT COUNT(*)::int FROM request_items item WHERE item.product_id = @Id)
    + (SELECT COUNT(*)::int FROM homepage_section_items item WHERE item.product_id = @Id);
```

- [ ] **Step 5: Implement readiness builder in service**

Service readiness rules:

- `isActive = false` adds `product_inactive`;
- `publishStatus != published` adds `product_draft`;
- blank name/slug/category/sale unit/unit quantity add missing issues;
- inactive category adds `inactive_category`;
- required category attributes without values add `missing_required_attribute`;
- invalid value type adds `invalid_attribute_value`.

Publishing with unresolved blocking issues throws `admin_catalog.product_not_ready`.

- [ ] **Step 6: Add controller and endpoint tests**

Controller route:

```csharp
[Route("api/admin/catalog/products")]
```

Endpoint tests must cover:

- unauthenticated 401;
- customer 403;
- seller list/detail OK;
- create/update/delete require CSRF;
- duplicate-candidates route is not implemented in this task.

- [ ] **Step 7: Register and run tests**

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalogProductSqlTests|AdminCatalogProductServiceTests|AdminCatalogProductsEndpointTests|CatalogModuleRegistrationTests"
```

Expected: PASS.

- [ ] **Step 8: Commit**

```powershell
git add apps/api/Modules/Catalog/DTOs/AdminCatalogProductDtos.cs apps/api/Modules/Catalog/Repositories/IAdminCatalogProductRepository.cs apps/api/Modules/Catalog/Repositories/AdminCatalogProductSql.cs apps/api/Modules/Catalog/Repositories/DapperAdminCatalogProductRepository.cs apps/api/Modules/Catalog/Services/IAdminCatalogProductService.cs apps/api/Modules/Catalog/Services/AdminCatalogProductService.cs apps/api/Modules/Catalog/Controllers/AdminCatalogProductsController.cs apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductSqlTests.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductServiceTests.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductsEndpointTests.cs tests/LineCom.Api.Tests/Modules/Catalog/CatalogModuleRegistrationTests.cs
git commit -m "feat: add admin product crud api"
```

### Task 6: Product Attribute Values

**Files:**
- Modify: `apps/api/Modules/Catalog/DTOs/AdminCatalogProductDtos.cs`
- Modify: `apps/api/Modules/Catalog/Repositories/IAdminCatalogProductRepository.cs`
- Modify: `apps/api/Modules/Catalog/Repositories/AdminCatalogProductSql.cs`
- Modify: `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogProductRepository.cs`
- Modify: `apps/api/Modules/Catalog/Services/IAdminCatalogProductService.cs`
- Modify: `apps/api/Modules/Catalog/Services/AdminCatalogProductService.cs`
- Modify: `apps/api/Modules/Catalog/Controllers/AdminCatalogProductsController.cs`
- Modify: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductSqlTests.cs`
- Modify: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductServiceTests.cs`
- Modify: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductsEndpointTests.cs`

- [ ] **Step 1: Write failing product attribute tests**

Add command DTO:

```csharp
public sealed record UpdateAdminProductAttributesCommand(
    IReadOnlyList<UpsertAdminProductAttributeValueCommand> Values);

public sealed record UpsertAdminProductAttributeValueCommand(
    Guid AttributeId,
    string? ValueText,
    decimal? ValueNumber,
    bool? ValueBoolean,
    Guid? AttributeOptionId);
```

Tests must cover:

- product uses only attributes from its primary category;
- `text` requires `ValueText`;
- `number` requires `ValueNumber`;
- `boolean` requires `ValueBoolean`;
- `select` requires active option belonging to same attribute;
- update replaces previous values transactionally;
- response detail shows updated attributes.

- [ ] **Step 2: Run failing tests**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalogProductServiceTests|AdminCatalogProductSqlTests"
```

Expected: FAIL because attribute update methods do not exist.

- [ ] **Step 3: Implement SQL/repository/service/controller**

Add endpoint:

```csharp
[RequireCsrfToken]
[HttpPut("{id:guid}/attributes")]
public async Task<ActionResult<AdminProductDetailDto>> UpdateAttributes(
    Guid id,
    UpdateAdminProductAttributesCommand command,
    CancellationToken cancellationToken)
{
    return Ok(await _service.UpdateAttributesAsync(HttpContext, id, command, cancellationToken));
}
```

Repository update must:

- open transaction;
- lock product row;
- validate attribute metadata;
- delete existing `product_attribute_values` for product;
- insert values using one storage column per row;
- commit;
- return detail.

- [ ] **Step 4: Run tests**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalogProductServiceTests|AdminCatalogProductSqlTests|AdminCatalogProductsEndpointTests"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add apps/api/Modules/Catalog/DTOs/AdminCatalogProductDtos.cs apps/api/Modules/Catalog/Repositories/IAdminCatalogProductRepository.cs apps/api/Modules/Catalog/Repositories/AdminCatalogProductSql.cs apps/api/Modules/Catalog/Repositories/DapperAdminCatalogProductRepository.cs apps/api/Modules/Catalog/Services/IAdminCatalogProductService.cs apps/api/Modules/Catalog/Services/AdminCatalogProductService.cs apps/api/Modules/Catalog/Controllers/AdminCatalogProductsController.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductSqlTests.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductServiceTests.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductsEndpointTests.cs
git commit -m "feat: add admin product attribute editing"
```

### Task 7: Product Duplicate Candidates Endpoint

**Files:**
- Modify: `apps/api/Modules/Catalog/DTOs/AdminCatalogProductDtos.cs`
- Modify: `apps/api/Modules/Catalog/Services/IAdminCatalogProductService.cs`
- Modify: `apps/api/Modules/Catalog/Services/AdminCatalogProductService.cs`
- Modify: `apps/api/Modules/Catalog/Controllers/AdminCatalogProductsController.cs`
- Modify: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductServiceTests.cs`
- Modify: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductsEndpointTests.cs`

- [ ] **Step 1: Write failing duplicate endpoint tests**

Tests:

- `GET /api/admin/catalog/products/duplicate-candidates?name=Cable&categoryId=...&slug=cable` as seller returns candidates;
- customer gets 403;
- service trims query values and clamps limit/threshold through `IAdminProductDuplicateQuery`;
- blank query with no name/sku/externalId/slug throws `admin_catalog.invalid_request`.

- [ ] **Step 2: Run failing tests**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalogProductServiceTests|AdminCatalogProductsEndpointTests"
```

Expected: FAIL because service/controller method does not exist.

- [ ] **Step 3: Implement service/controller wrapper**

Add DTO:

```csharp
public sealed record AdminProductDuplicateCandidatesQueryDto(
    string? Name,
    Guid? CategoryId,
    Guid? BrandId,
    string? Sku,
    string? ExternalId,
    string? Slug,
    Guid? ExcludeProductId,
    int? Limit,
    decimal? SimilarityThreshold);
```

Add controller action before `{id:guid}` route:

```csharp
[HttpGet("duplicate-candidates")]
public async Task<ActionResult<AdminProductDuplicateCandidatesResponse>> GetDuplicateCandidates(
    [FromQuery] AdminProductDuplicateCandidatesQueryDto query,
    CancellationToken cancellationToken)
{
    return Ok(await _service.FindDuplicateCandidatesAsync(HttpContext, query, cancellationToken));
}
```

Service maps DTO to `AdminProductDuplicateCandidateQuery`.

- [ ] **Step 4: Run tests**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminProductDuplicateSqlTests|AdminCatalogProductServiceTests|AdminCatalogProductsEndpointTests"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add apps/api/Modules/Catalog/DTOs/AdminCatalogProductDtos.cs apps/api/Modules/Catalog/Services/IAdminCatalogProductService.cs apps/api/Modules/Catalog/Services/AdminCatalogProductService.cs apps/api/Modules/Catalog/Controllers/AdminCatalogProductsController.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductServiceTests.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductsEndpointTests.cs
git commit -m "feat: expose admin product duplicate candidates"
```

### Task 8: PostgreSQL Behavior Tests For CRUD Safety

**Files:**
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogCrudDatabaseBehaviorTests.cs`

- [ ] **Step 1: Write opt-in PostgreSQL tests**

Use `[Collection(PostgresMigrationCollection.Name)]` and existing fallback pattern:

```csharp
if (!_fixture.IsConfigured)
{
    return;
}
```

Cover:

- deleting category with child fails through service/repository safety;
- deleting category with product fails;
- deleting category with homepage item fails;
- deleting product with request item fails;
- deleting product with homepage item fails;
- changing attribute type with existing value fails;
- deleting used attribute option fails.

- [ ] **Step 2: Run without PostgreSQL**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter AdminCatalogCrudDatabaseBehaviorTests
```

Expected without `LINECOM_TEST_CONNECTION_STRING`: PASS by early return.

- [ ] **Step 3: Run with PostgreSQL when available**

```powershell
$env:LINECOM_TEST_CONNECTION_STRING="Host=localhost;Port=5432;Database=linecom_test;Username=postgres;Password=postgres"
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter AdminCatalogCrudDatabaseBehaviorTests
```

Expected with configured test database: PASS.

- [ ] **Step 4: Commit**

```powershell
git add tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogCrudDatabaseBehaviorTests.cs
git commit -m "test: cover admin catalog crud database safety"
```

### Task 9: Full CRUD Verification

**Files:**
- Verify all files from Tasks 1-8.
- Modify docs only if verification proves a documented behavior mismatch.

- [ ] **Step 1: Run focused test suite**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminCatalogStaffGuardTests|AdminCatalogCategorySqlTests|AdminCatalogCategoryServiceTests|AdminCatalogCategoriesEndpointTests|AdminCatalogBrandSqlTests|AdminCatalogBrandServiceTests|AdminCatalogBrandsEndpointTests|AdminCatalogAttributeSqlTests|AdminCatalogAttributeServiceTests|AdminCatalogAttributesEndpointTests|AdminCatalogProductSqlTests|AdminCatalogProductServiceTests|AdminCatalogProductsEndpointTests|AdminCatalogCrudDatabaseBehaviorTests|AdminProductDuplicateSqlTests|CatalogModuleRegistrationTests"
```

Expected: PASS. PostgreSQL behavior tests pass by early return when `LINECOM_TEST_CONNECTION_STRING` is not configured.

- [ ] **Step 2: Run full backend test suite**

```powershell
dotnet test .\LineCom.sln
```

Expected: PASS.

- [ ] **Step 3: Run build**

```powershell
dotnet build .\LineCom.sln
```

Expected: PASS with 0 errors.

- [ ] **Step 4: Inspect git diff**

```powershell
git diff --check
git status --short
```

Expected:

- no whitespace errors;
- only intended files are modified;
- `admin-catalog-homepage-slice.png` may remain untracked and must not be staged or committed.

- [ ] **Step 5: Commit docs only if changed**

If documentation changed due verified mismatch:

```powershell
git add docs/superpowers/specs/2026-05-11-admin-catalog-homepage-design.md vault/Человекочитаемое
git commit -m "docs: update admin catalog crud notes"
```

If no docs changed, do not create an empty commit.

## Handoff Notes

After this plan is complete, write the next implementation plan for `admin-catalog-images`. That plan should cover product images and brand logos through Local FileStorage upload endpoints, leaving frontend UI to a separate plan.
