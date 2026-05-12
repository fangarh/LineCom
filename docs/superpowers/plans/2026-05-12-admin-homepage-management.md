# Admin Homepage Management Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the staff-facing admin workflow for managing fixed homepage sections and selected product/category cards, then make the public homepage prefer those curated sections.

**Architecture:** Keep homepage management in the existing Catalog module. Reuse the existing `homepage_sections` and `homepage_section_items` tables plus `DapperAdminHomepageQuery` for the admin read model, add a Dapper mutation repository and thin staff-guarded service, and expose endpoints under `/api/admin/homepage`. Frontend adds a focused `/admin/homepage` screen and API client; the public homepage keeps its current layout and uses curated sections when the public read endpoint returns usable data.

**Tech Stack:** ASP.NET Core Web API, Cookie Auth, CSRF guard, PostgreSQL, Npgsql, Dapper, DbUp-owned schema, Next.js 16 App Router, React 19, TypeScript, Vitest, Testing Library, xUnit.

---

## Current Baseline

- Admin catalog backend foundation already created `homepage_sections` and `homepage_section_items`.
- Existing query files:
  - `apps/api/Modules/Catalog/DTOs/AdminHomepageDtos.cs`
  - `apps/api/Modules/Catalog/Queries/IAdminHomepageQuery.cs`
  - `apps/api/Modules/Catalog/Queries/DapperAdminHomepageQuery.cs`
  - `apps/api/Modules/Catalog/Queries/AdminHomepageSql.cs`
- Existing admin catalog UI is complete under `/admin/catalog`.
- Expected untracked file remains `admin-catalog-homepage-slice.png`; do not stage, edit, delete, or commit it.

## Scope

In scope:

- Staff-only admin homepage read/update endpoints:
  - `GET /api/admin/homepage/sections`
  - `PUT /api/admin/homepage/sections/{id}`
  - `POST /api/admin/homepage/sections/{id}/items`
  - `PUT /api/admin/homepage/sections/{id}/items/order`
  - `PUT /api/admin/homepage/sections/{id}/items/{itemId}`
  - `DELETE /api/admin/homepage/sections/{id}/items/{itemId}`
- Public read endpoint for active homepage sections:
  - `GET /api/public/homepage/sections`
- Admin UI route `/admin/homepage`.
- Section enable/title/item limit editing.
- Product/category item add, active toggle, sort order/reorder, remove.
- Visibility status display for inactive/unpublished selections.
- Public homepage uses curated `hero_products`, `featured_products`, and `direction_categories` when available, with current automatic fallback preserved.

Out of scope:

- Arbitrary CMS blocks.
- Creating or deleting homepage sections.
- Changing section `code` or `type`.
- Image crop/resize/media library.
- Import/export.
- Audit log.
- LLM duplicate checking.
- Prices, stock, checkout, online payment.

## Task 1: Backend DTOs, SQL Contracts, And Repository Interface

**Files:**
- Modify: `apps/api/Modules/Catalog/DTOs/AdminHomepageDtos.cs`
- Create: `apps/api/Modules/Catalog/Repositories/IAdminHomepageRepository.cs`
- Create: `apps/api/Modules/Catalog/Repositories/AdminHomepageRepositorySql.cs`
- Create: `apps/api/Modules/Catalog/Repositories/AdminHomepageRecords.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminHomepageRepositorySqlTests.cs`

- [x] **Step 1: Add failing SQL contract tests**

Create `AdminHomepageRepositorySqlTests.cs` with these assertions:

```csharp
using LineCom.Api.Modules.Catalog.Repositories;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminHomepageRepositorySqlTests
{
    [Theory]
    [InlineData("UPDATE homepage_sections")]
    [InlineData("RETURNING id")]
    [InlineData("INSERT INTO homepage_section_items")]
    [InlineData("num_nonnulls(product_id, category_id)")]
    [InlineData("UPDATE homepage_section_items")]
    [InlineData("DELETE FROM homepage_section_items")]
    [InlineData("WHERE section_id = @SectionId")]
    public void Sql_ContainsHomepageMutationContracts(string expected)
    {
        var sql = string.Join(
            "\n",
            AdminHomepageRepositorySql.UpdateSection,
            AdminHomepageRepositorySql.InsertSectionItem,
            AdminHomepageRepositorySql.UpdateSectionItem,
            AdminHomepageRepositorySql.UpdateSectionItemOrder,
            AdminHomepageRepositorySql.DeleteSectionItem);

        Assert.Contains(expected, sql);
    }
}
```

- [x] **Step 2: Run SQL contract test and verify RED**

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter AdminHomepageRepositorySqlTests -m:1
```

Expected: compile failure because `AdminHomepageRepositorySql` does not exist.

- [x] **Step 3: Add DTO commands and repository contracts**

Extend `AdminHomepageDtos.cs`:

```csharp
public sealed record UpdateAdminHomepageSectionCommand(
    string? Title,
    int? ItemLimit,
    int? SortOrder,
    bool? IsActive);

public sealed record CreateAdminHomepageSectionItemCommand(
    Guid? ProductId,
    Guid? CategoryId,
    int? SortOrder,
    bool? IsActive);

public sealed record UpdateAdminHomepageSectionItemCommand(
    int? SortOrder,
    bool? IsActive);

public sealed record UpdateAdminHomepageSectionItemOrderCommand(
    IReadOnlyList<Guid> ItemIds);

public sealed record PublicHomepageSectionsResponse(
    IReadOnlyList<PublicHomepageSectionDto> Sections);

public sealed record PublicHomepageSectionDto(
    string Code,
    string Title,
    string Type,
    IReadOnlyList<PublicHomepageSectionItemDto> Items);

public sealed record PublicHomepageSectionItemDto(
    Guid Id,
    Guid? ProductId,
    Guid? CategoryId,
    string Name,
    string? Slug,
    string? SecondaryText);
```

Create `IAdminHomepageRepository.cs`:

```csharp
using LineCom.Api.Modules.Catalog.DTOs;

namespace LineCom.Api.Modules.Catalog.Repositories;

public interface IAdminHomepageRepository
{
    Task<bool> SectionExistsAsync(Guid sectionId, CancellationToken cancellationToken = default);
    Task<AdminHomepageSectionDto?> UpdateSectionAsync(Guid sectionId, UpdateAdminHomepageSectionCommand command, CancellationToken cancellationToken = default);
    Task<AdminHomepageSectionItemDto?> InsertItemAsync(Guid sectionId, CreateAdminHomepageSectionItemCommand command, CancellationToken cancellationToken = default);
    Task<AdminHomepageSectionItemDto?> UpdateItemAsync(Guid sectionId, Guid itemId, UpdateAdminHomepageSectionItemCommand command, CancellationToken cancellationToken = default);
    Task<bool> UpdateItemOrderAsync(Guid sectionId, IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken = default);
    Task<bool> DeleteItemAsync(Guid sectionId, Guid itemId, CancellationToken cancellationToken = default);
}
```

Create `AdminHomepageRecords.cs`:

```csharp
namespace LineCom.Api.Modules.Catalog.Repositories;

internal sealed record AdminHomepageItemTarget(Guid? ProductId, Guid? CategoryId);
```

Create `AdminHomepageRepositorySql.cs` with string constants for each mutation. Keep SQL inside repository files, not controllers or services.

- [x] **Step 4: Run SQL contract test and verify GREEN**

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter AdminHomepageRepositorySqlTests -m:1
```

Expected: test passes.

- [x] **Step 5: Commit backend contracts**

```powershell
git add apps/api/Modules/Catalog/DTOs/AdminHomepageDtos.cs apps/api/Modules/Catalog/Repositories/IAdminHomepageRepository.cs apps/api/Modules/Catalog/Repositories/AdminHomepageRepositorySql.cs apps/api/Modules/Catalog/Repositories/AdminHomepageRecords.cs tests/LineCom.Api.Tests/Modules/Catalog/AdminHomepageRepositorySqlTests.cs
git commit -m "test: cover admin homepage repository contracts"
```

## Task 2: Backend Repository, Service, And Admin Endpoints

**Files:**
- Create: `apps/api/Modules/Catalog/Repositories/DapperAdminHomepageRepository.cs`
- Create: `apps/api/Modules/Catalog/Services/IAdminHomepageService.cs`
- Create: `apps/api/Modules/Catalog/Services/AdminHomepageService.cs`
- Create: `apps/api/Modules/Catalog/Controllers/AdminHomepageController.cs`
- Modify: `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminHomepageServiceTests.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/AdminHomepageEndpointTests.cs`

- [x] **Step 1: Add failing service tests**

Create service tests covering:

```csharp
[Fact]
public async Task GetSectionsAsync_RequiresStaffRole()
{
    var service = new AdminHomepageService(new RejectingStaffGuard(), new StubQuery(), new StubRepository());

    await Assert.ThrowsAsync<ApiException>(() => service.GetSectionsAsync(new DefaultHttpContext()));
}

[Fact]
public async Task CreateItemAsync_RejectsMissingAndDoubleTargets()
{
    var service = new AdminHomepageService(new AllowingStaffGuard(), new StubQuery(), new StubRepository());

    await Assert.ThrowsAsync<ApiException>(() => service.CreateItemAsync(
        new DefaultHttpContext(),
        Guid.NewGuid(),
        new CreateAdminHomepageSectionItemCommand(null, null, 10, true)));

    await Assert.ThrowsAsync<ApiException>(() => service.CreateItemAsync(
        new DefaultHttpContext(),
        Guid.NewGuid(),
        new CreateAdminHomepageSectionItemCommand(Guid.NewGuid(), Guid.NewGuid(), 10, true)));
}

[Fact]
public async Task UpdateItemOrderAsync_RejectsEmptyOrder()
{
    var service = new AdminHomepageService(new AllowingStaffGuard(), new StubQuery(), new StubRepository());

    await Assert.ThrowsAsync<ApiException>(() => service.UpdateItemOrderAsync(
        new DefaultHttpContext(),
        Guid.NewGuid(),
        new UpdateAdminHomepageSectionItemOrderCommand([])));
}
```

- [x] **Step 2: Add failing endpoint tests**

Create endpoint tests using the existing `LineComWebApplicationFactory` pattern from admin catalog endpoint tests:

```csharp
[Theory]
[InlineData("customer", HttpStatusCode.Forbidden)]
[InlineData("seller", HttpStatusCode.OK)]
[InlineData("admin", HttpStatusCode.OK)]
public async Task GetSections_UsesStaffAuthorization(string role, HttpStatusCode expectedStatus)
{
    using var factory = new LineComWebApplicationFactory();
    var client = factory.CreateAuthenticatedClient(role);

    var response = await client.GetAsync("/api/admin/homepage/sections");

    Assert.Equal(expectedStatus, response.StatusCode);
}

[Fact]
public async Task UpdateSection_RequiresCsrf()
{
    using var factory = new LineComWebApplicationFactory();
    var client = factory.CreateAuthenticatedClient("seller");

    var response = await client.PutAsJsonAsync(
        $"/api/admin/homepage/sections/{Guid.NewGuid()}",
        new UpdateAdminHomepageSectionCommand("Главные товары", 6, 10, true));

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

- [x] **Step 3: Run backend tests and verify RED**

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminHomepageServiceTests|AdminHomepageEndpointTests" -m:1
```

Expected: compile failure because service/controller/repository are missing.

- [x] **Step 4: Implement repository**

`DapperAdminHomepageRepository` opens connections through `IDbConnectionFactory`, executes SQL constants, catches PostgreSQL check/foreign-key/unique violations, and returns the refreshed admin read model item/section shape. Use the existing DB triggers to enforce section type compatibility; do not duplicate those rules with stringly controller logic.

- [x] **Step 5: Implement service and controller**

Service behavior:

- call `IAdminCatalogStaffGuard.RequireStaffAsync` on every method;
- normalize section title through `AdminCatalogInput.NormalizeRequiredText`;
- clamp section `itemLimit` to `1..24`;
- require exactly one of `ProductId` or `CategoryId` when adding an item;
- reject empty reorder lists;
- return `not_found` when section or item does not exist;
- convert repository constraint exceptions to existing admin catalog error responses where possible.

Controller shape:

```csharp
[Authorize]
[ApiController]
[Route("api/admin/homepage/sections")]
public sealed class AdminHomepageController : ControllerBase
{
    private readonly IAdminHomepageService _service;

    public AdminHomepageController(IAdminHomepageService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<AdminHomepageSectionsResponse>> GetSections(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetSectionsAsync(HttpContext, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminHomepageSectionDto>> UpdateSection(Guid id, UpdateAdminHomepageSectionCommand command, CancellationToken cancellationToken)
    {
        return Ok(await _service.UpdateSectionAsync(HttpContext, id, command, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpPost("{id:guid}/items")]
    public async Task<ActionResult<AdminHomepageSectionItemDto>> CreateItem(Guid id, CreateAdminHomepageSectionItemCommand command, CancellationToken cancellationToken)
    {
        var created = await _service.CreateItemAsync(HttpContext, id, command, cancellationToken);
        return CreatedAtAction(nameof(GetSections), new { id }, created);
    }

    [RequireCsrfToken]
    [HttpPut("{id:guid}/items/order")]
    public async Task<ActionResult<AdminHomepageSectionsResponse>> UpdateItemOrder(Guid id, UpdateAdminHomepageSectionItemOrderCommand command, CancellationToken cancellationToken)
    {
        return Ok(await _service.UpdateItemOrderAsync(HttpContext, id, command, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpPut("{id:guid}/items/{itemId:guid}")]
    public async Task<ActionResult<AdminHomepageSectionItemDto>> UpdateItem(Guid id, Guid itemId, UpdateAdminHomepageSectionItemCommand command, CancellationToken cancellationToken)
    {
        return Ok(await _service.UpdateItemAsync(HttpContext, id, itemId, command, cancellationToken));
    }

    [RequireCsrfToken]
    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        await _service.DeleteItemAsync(HttpContext, id, itemId, cancellationToken);
        return NoContent();
    }
}
```

- [x] **Step 6: Register services**

Add scoped registrations in `CatalogServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<IAdminHomepageRepository, DapperAdminHomepageRepository>();
services.AddScoped<IAdminHomepageService, AdminHomepageService>();
```

- [x] **Step 7: Run backend tests and verify GREEN**

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminHomepageServiceTests|AdminHomepageEndpointTests|AdminHomepageRepositorySqlTests" -m:1
```

Expected: all focused backend tests pass.

- [x] **Step 8: Commit backend endpoints**

```powershell
git add apps/api/Modules/Catalog tests/LineCom.Api.Tests/Modules/Catalog
git commit -m "feat: add admin homepage endpoints"
```

## Task 3: Public Homepage Read Endpoint

**Files:**
- Create: `apps/api/Modules/Catalog/Queries/IPublicHomepageQuery.cs`
- Create: `apps/api/Modules/Catalog/Queries/DapperPublicHomepageQuery.cs`
- Create: `apps/api/Modules/Catalog/Queries/PublicHomepageSql.cs`
- Create: `apps/api/Modules/Catalog/Controllers/PublicHomepageController.cs`
- Modify: `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/PublicHomepageQueryTests.cs`
- Create: `tests/LineCom.Api.Tests/Modules/Catalog/PublicHomepageEndpointTests.cs`

- [x] **Step 1: Add failing public query tests**

Cover the public visibility rule:

```csharp
[Fact]
public void PublicHomepageSql_OnlyReturnsActiveVisibleItems()
{
    Assert.Contains("section.is_active = TRUE", PublicHomepageSql.GetSections);
    Assert.Contains("item.is_active = TRUE", PublicHomepageSql.GetSectionItems);
    Assert.Contains("product.publish_status = 'published'", PublicHomepageSql.GetSectionItems);
    Assert.Contains("product.is_active = TRUE", PublicHomepageSql.GetSectionItems);
    Assert.Contains("category.is_active = TRUE", PublicHomepageSql.GetSectionItems);
}
```

- [x] **Step 2: Run public query tests and verify RED**

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter PublicHomepage -m:1
```

Expected: compile failure because public homepage query/controller do not exist.

- [x] **Step 3: Implement public query and controller**

Public query returns only active sections and active items that are safe to show publicly. Product items require active product, published product, active product category, and a non-null slug. Category items require active category and a non-null slug.

Controller:

```csharp
[ApiController]
[Route("api/public/homepage")]
public sealed class PublicHomepageController : ControllerBase
{
    [HttpGet("sections")]
    public async Task<ActionResult<PublicHomepageSectionsResponse>> GetSections(CancellationToken cancellationToken)
    {
        return Ok(await _query.GetSectionsAsync(cancellationToken));
    }
}
```

- [x] **Step 4: Register public query**

Add:

```csharp
services.AddScoped<IPublicHomepageQuery, DapperPublicHomepageQuery>();
```

- [x] **Step 5: Run focused public homepage tests**

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter PublicHomepage -m:1
```

Expected: public homepage tests pass.

- [x] **Step 6: Commit public read endpoint**

```powershell
git add apps/api/Modules/Catalog tests/LineCom.Api.Tests/Modules/Catalog
git commit -m "feat: add public homepage sections api"
```

## Task 4: Frontend Admin Homepage API Client

**Files:**
- Create: `apps/front/src/lib/api/admin-homepage.ts`
- Create: `apps/front/src/lib/api/admin-homepage.test.ts`
- Modify: `apps/front/src/lib/routes.ts`

- [x] **Step 1: Add failing API client tests**

Create tests asserting paths, methods, CSRF headers, and payloads:

```ts
import {
  getAdminHomepageSections,
  updateAdminHomepageSection,
  addAdminHomepageSectionItem,
  updateAdminHomepageSectionItemOrder,
  updateAdminHomepageSectionItem,
  deleteAdminHomepageSectionItem,
} from "./admin-homepage";

it("updates homepage section with csrf", async () => {
  const fetchMock = vi.spyOn(global, "fetch").mockResolvedValue(jsonResponse({ id: "section-1" }));

  await updateAdminHomepageSection("section-1", { title: "Главные товары", itemLimit: 6, sortOrder: 10, isActive: true }, "csrf");

  expect(fetchMock).toHaveBeenCalledWith("/api/admin/homepage/sections/section-1", expect.objectContaining({
    method: "PUT",
    credentials: "include",
  }));
});
```

- [x] **Step 2: Run client tests and verify RED**

Run:

```powershell
npm.cmd test -- admin-homepage
```

Expected: compile failure because `admin-homepage.ts` does not exist.

- [x] **Step 3: Implement client and route helper**

`admin-homepage.ts` exports typed functions for all admin endpoints. `routes.ts` adds:

```ts
adminHomepage: () => "/admin/homepage",
```

- [x] **Step 4: Run client tests and verify GREEN**

Run:

```powershell
npm.cmd test -- admin-homepage
```

Expected: client tests pass.

- [x] **Step 5: Commit frontend API client**

```powershell
git add apps/front/src/lib/api/admin-homepage.ts apps/front/src/lib/api/admin-homepage.test.ts apps/front/src/lib/routes.ts
git commit -m "feat: add admin homepage frontend api client"
```

## Task 5: Frontend Admin Homepage UI

**Files:**
- Create: `apps/front/src/app/admin/homepage/page.tsx`
- Create: `apps/front/src/app/admin/homepage/homepage-page-client.tsx`
- Create: `apps/front/src/app/admin/homepage/homepage-page-client.test.tsx`
- Create: `apps/front/src/components/admin/homepage/admin-homepage-manager.tsx`
- Create: `apps/front/src/components/admin/homepage/admin-homepage-manager.test.tsx`
- Modify: `apps/front/src/components/layout/site-header.tsx`
- Modify: `apps/front/src/app/globals.css`

- [x] **Step 1: Add failing access tests**

Test `/admin/homepage` mirrors `/admin/catalog` access behavior:

```tsx
it("redirects unauthorized users to login with returnTo", async () => {
  getMeMock.mockRejectedValue(new ApiClientError(401, { code: "auth.unauthorized", message: "Требуется вход." }));

  render(<HomepagePageClient />);

  await waitFor(() => expect(routerPushMock).toHaveBeenCalledWith("/auth/login?returnTo=%2Fadmin%2Fhomepage"));
});

it("shows forbidden state for customer role", async () => {
  getMeMock.mockResolvedValue({ user: { id: "u1", name: "Customer", email: null, phone: null, role: "customer" }, csrfToken: "csrf" });

  render(<HomepagePageClient />);

  expect(await screen.findByText("У вас нет доступа к управлению главной страницей.")).toBeInTheDocument();
});
```

- [x] **Step 2: Add failing manager tests**

Cover the core UI:

```tsx
it("renders sections, item visibility statuses, and mutation controls", async () => {
  getAdminHomepageSectionsMock.mockResolvedValue(homepageSectionsResponse());

  render(<AdminHomepageManager csrfToken="csrf" />);

  expect(await screen.findByRole("heading", { name: "Главная страница" })).toBeInTheDocument();
  expect(screen.getByText("hero_products")).toBeInTheDocument();
  expect(screen.getByText("product_unpublished")).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Добавить товар" })).toBeEnabled();
  expect(screen.getByRole("button", { name: "Сохранить секцию" })).toBeEnabled();
});
```

- [x] **Step 3: Run UI tests and verify RED**

Run:

```powershell
npm.cmd test -- admin-homepage
```

Expected: tests fail because route and components are missing.

- [x] **Step 4: Implement page client**

Follow `apps/front/src/app/admin/catalog/catalog-page-client.tsx`:

- call `getMe`;
- redirect unauthorized users to `routes.login(routes.adminHomepage())`;
- show forbidden for non-`seller`/`admin`;
- pass `csrfToken` into `AdminHomepageManager`.

- [x] **Step 5: Implement manager**

Manager responsibilities:

- load sections with `getAdminHomepageSections`;
- edit section title, item limit, sort order, active flag;
- show items with target name, slug/secondary text, active flag, sort order, visibility status;
- add product/category by UUID into the selected section;
- save item order from numeric sort order fields;
- toggle item active;
- remove item;
- show server errors through `normalizeApiError`;
- keep state and mutation guards in the manager, not route components.

- [x] **Step 6: Add dense admin CSS**

Add classes under existing admin catalog CSS:

```css
.admin-homepage-manager { display: grid; gap: 1rem; }
.admin-homepage-section { display: grid; gap: 0.75rem; }
.admin-homepage-item { display: grid; gap: 0.5rem; }
.admin-homepage-item__meta { color: var(--muted-foreground); font-size: 0.9rem; }
@media (min-width: 960px) {
  .admin-homepage-manager { grid-template-columns: minmax(0, 1fr) minmax(360px, 0.7fr); }
}
```

- [x] **Step 7: Run UI tests and verify GREEN**

Run:

```powershell
npm.cmd test -- admin-homepage homepage-page-client
```

Expected: admin homepage UI tests pass.

- [x] **Step 8: Commit admin homepage UI**

```powershell
git add apps/front/src/app/admin/homepage apps/front/src/components/admin/homepage apps/front/src/components/layout/site-header.tsx apps/front/src/app/globals.css
git commit -m "feat: add admin homepage management UI"
```

## Task 6: Public Homepage Uses Curated Sections

**Files:**
- Create: `apps/front/src/lib/api/homepage.ts`
- Create: `apps/front/src/lib/api/homepage.test.ts`
- Create: `apps/front/src/lib/homepage/curated-homepage.ts`
- Create: `apps/front/src/lib/homepage/curated-homepage.test.ts`
- Modify: `apps/front/src/app/page.tsx`

- [x] **Step 1: Add failing helper tests**

Test that curated sections override automatic selection only when they contain usable items:

```ts
it("uses curated hero and featured products when public sections provide product ids", () => {
  const result = applyCuratedHomepageSections({
    products: productList(),
    categories: categoryTree(),
    sections: publicHomepageSectionsResponse(),
  });

  expect(result.heroProducts.map((product) => product.id)).toEqual(["product-1", "product-2", "product-3"]);
  expect(result.featuredProducts.map((product) => product.id)).toEqual(["product-1", "product-4"]);
  expect(result.highlights.map((category) => category.id)).toEqual(["category-1", "category-2"]);
});
```

- [x] **Step 2: Run helper tests and verify RED**

Run:

```powershell
npm.cmd test -- curated-homepage homepage
```

Expected: compile failure because helper/client files do not exist.

- [x] **Step 3: Implement public homepage client**

`apps/front/src/lib/api/homepage.ts` exports:

```ts
export function getHomepageSections() {
  return apiJson<PublicHomepageSectionsResponse>("/api/public/homepage/sections", {
    next: { revalidate: 60 },
  });
}
```

- [x] **Step 4: Implement curated mapping helper**

The helper maps section item IDs to already-loaded public products/categories. If the API is unavailable, empty, or references missing public items, keep the current automatic fallback from `selectFeaturedProducts` and `categoryHighlights`.

- [x] **Step 5: Update home page server component**

`apps/front/src/app/page.tsx` loads homepage sections with `Promise.allSettled`, then uses curated values when available:

```ts
const [categoryResult, productResult, homepageResult] = await Promise.allSettled([
  getCategoryTree(),
  getProducts({ pageSize: 60, sort: "category" }),
  getHomepageSections(),
]);
```

- [x] **Step 6: Run homepage tests and build**

Run:

```powershell
npm.cmd test -- curated-homepage homepage
npm.cmd run build
```

Expected: tests and build pass.

- [x] **Step 7: Commit public homepage integration**

```powershell
git add apps/front/src/lib/api/homepage.ts apps/front/src/lib/api/homepage.test.ts apps/front/src/lib/homepage/curated-homepage.ts apps/front/src/lib/homepage/curated-homepage.test.ts apps/front/src/app/page.tsx
git commit -m "feat: use curated homepage sections"
```

## Task 7: Documentation, Verification, And Browser QA

**Files:**
- Create: `vault/Человекочитаемое/Admin Homepage Management API.md`
- Modify: `vault/Человекочитаемое/README.md`
- Verify all changed backend and frontend files.

- [x] **Step 1: Document implemented contract**

Create `Admin Homepage Management API.md` with:

- endpoint list;
- role rules;
- CSRF requirements;
- public visibility rules;
- fixed section codes and types;
- out-of-scope items.

- [x] **Step 2: Run focused tests**

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "AdminHomepage|PublicHomepage" -m:1
npm.cmd test -- admin-homepage curated-homepage homepage
```

Expected: all focused tests pass.

- [x] **Step 3: Run full verification**

```powershell
dotnet test .\LineCom.sln -m:1
dotnet build .\LineCom.sln -m:1
npm.cmd test
npm.cmd run lint
npm.cmd run build
```

Expected: commands pass. NU1900 warnings are acceptable if commands exit successfully.

- [x] **Step 4: Browser QA**

Start the API and frontend if needed. Verify:

- `/admin/homepage` redirects unauthorized users to login;
- customer role sees forbidden state;
- seller/admin can see homepage sections;
- section form, item list, add controls, active toggle, and reorder controls fit at desktop width;
- same screen has no horizontal overflow at mobile width `390`;
- public `/` renders curated product/category sections when mocked API returns sections;
- public `/` falls back to existing automatic sections when mocked public homepage API returns an empty list;
- no console errors in staff/public happy paths.

- [x] **Step 5: Hygiene checks**

```powershell
git diff --check
git status --short --branch
rg -n "TODO|TBD|temporary|hack|EntityFramework|DbContext" apps/front apps/api tests docs vault
```

Expected:

- no whitespace errors;
- only intended files changed;
- `admin-catalog-homepage-slice.png` remains untracked and unstaged;
- no EF/DbContext usage introduced;
- no unresolved work markers in changed implementation files.

- [x] **Step 6: Commit docs and final fixes**

If documentation or verification fixes changed files:

```powershell
git add vault/Человекочитаемое/Admin\ Homepage\ Management\ API.md vault/Человекочитаемое/README.md apps tests
git commit -m "docs: document admin homepage management"
```

If no files changed after verification, do not create an empty commit.
