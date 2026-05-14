# Testing Patterns

**Analysis Date:** 2026-05-14

## Test Framework

**Runner:**
- Backend: xUnit 2.5.3 with Microsoft.NET.Test.Sdk 17.8.0 on `net8.0`; config/dependencies in `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj`.
- Frontend: Vitest 4.1.5 with jsdom, globals enabled, and Testing Library setup; config in `apps/front/vitest.config.ts`.
- Python tool tests: standard `unittest` for helper scripts; example in `tests/tools/test_download_tktdf_product_images.py`.

**Assertion Library:**
- Backend: xUnit `Assert.*`, including `Assert.ThrowsAsync`, `Assert.Contains`, `Assert.DoesNotContain`, `Assert.Single`: examples in `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductServiceTests.cs`.
- Frontend: Vitest `expect` plus `@testing-library/jest-dom/vitest` setup: `apps/front/src/test/setup.ts`, `apps/front/src/components/request/request-draft-provider.test.tsx`.
- Python: `unittest.TestCase` assertions with `unittest.mock.patch`: `tests/tools/test_download_tktdf_product_images.py`.

**Run Commands:**
```bash
dotnet test LineCom.sln                         # Run backend/.NET tests
dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "FullyQualifiedName~StorageDiagnostics|FullyQualifiedName~LocalStorageStaticFiles"  # Phase 2 storage boundary/diagnostics
npm --prefix apps/front test                    # Run frontend Vitest suite once
npm --prefix apps/front run test:watch          # Run frontend Vitest in watch mode
npm --prefix apps/front run lint                # Run frontend ESLint checks
npm --prefix apps/front run build               # Build Next app and type-check production path
python -m unittest tests.tools.test_download_tktdf_product_images  # Run Python tool test module
```

## Test File Organization

**Location:**
- Backend tests live in a separate test project under `tests/LineCom.Api.Tests`, mirroring API/infrastructure/import domains: `tests/LineCom.Api.Tests/Modules/Catalog`, `tests/LineCom.Api.Tests/Modules/Requests`, `tests/LineCom.Api.Tests/Infrastructure/Database`, `tests/LineCom.Api.Tests/CatalogImport`.
- Frontend tests are co-located under `apps/front/src` beside components, pages, and helpers: `apps/front/src/components/admin/catalog/admin-product-manager.test.tsx`, `apps/front/src/lib/seo/sitemap.test.ts`, `apps/front/src/app/request/page.test.tsx`.
- Python tool tests live under `tests/tools`: `tests/tools/test_download_tktdf_product_images.py`.

**Naming:**
- Backend: `*Tests.cs`, with class names matching the subject plus `Tests`: `AdminCatalogProductServiceTests`, `AdminCatalogProductsEndpointTests`, `CatalogFoundationMigrationTests`.
- Frontend: `*.test.ts` for pure helpers/API clients and `*.test.tsx` for components/pages.
- SQL contract tests use explicit names for the SQL artifact under test: `AdminCatalogProductSqlTests.cs`, `RequestNumberSqlTests.cs`, `CatalogFoundationMigrationTests.cs`.

**Structure:**
```text
tests/LineCom.Api.Tests/
  Modules/<Domain>/*Tests.cs
  Infrastructure/<Area>/*Tests.cs
  CatalogImport/*Tests.cs
  Support/LineComWebApplicationFactory.cs

apps/front/src/
  components/**/<component>.test.tsx
  lib/**/*.test.ts
  app/**/*.test.ts(x)

tests/tools/
  test_*.py
```

## Test Structure

**Suite Organization:**
```csharp
public sealed class AdminCatalogProductServiceTests
{
    [Fact]
    public async Task CreateProductAsync_NormalizesTextBeforeRepositoryCall()
    {
        var repository = new CapturingAdminCatalogProductRepository();
        var service = CreateService("admin", repository);

        await service.CreateProductAsync(new DefaultHttpContext(), ValidCommand(), CancellationToken.None);

        Assert.NotNull(repository.LastUpsert);
        Assert.Equal("Cable", repository.LastUpsert.Name);
    }
}
```

```typescript
describe("admin product manager helpers", () => {
  it("builds product list params from pagination and filters", () => {
    expect(buildProductListParams(input)).toEqual(expected);
  });
});
```

**Patterns:**
- Backend test method names follow `MethodOrFeature_Condition_ExpectedResult`: `CreateProductAsync_RejectsBlankRequiredFields`, `GetNextSequence_UsesParameterizedYearAndAtomicUpsert`, `Products_PublishStatusRejectsArchived`.
- Use `[Theory]` and `[InlineData]` for validation matrices and status/code cases: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductServiceTests.cs`, `tests/LineCom.Api.Tests/Infrastructure/Database/CatalogFoundationMigrationTests.cs`.
- Keep test data builders private and local to the test file when only one suite uses them: `ValidCommand`, `ProductListRecord`, `ProductDetailRecord` in `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductServiceTests.cs`.
- Frontend component tests use Testing Library queries by role/label/text and user-level interactions through `userEvent.setup()`: `apps/front/src/components/request/request-draft-view.test.tsx`, `apps/front/src/components/layout/site-header.test.tsx`.
- Async frontend assertions use `waitFor` after render, user actions, or mocked request resolution: `apps/front/src/components/request/request-draft-provider.test.tsx`, `apps/front/src/app/request/page.test.tsx`.

## Mocking

**Framework:** Backend uses hand-written fakes/capturing test doubles; frontend uses Vitest mocks (`vi.mock`, `vi.fn`, `vi.stubGlobal`, `vi.hoisted`); Python uses `unittest.mock.patch`.

**Patterns:**
```typescript
const adminCatalogApiMock = vi.hoisted(() => ({
  getAdminProducts: vi.fn(),
  getAdminProduct: vi.fn(),
}));

vi.mock("@/lib/api/admin-catalog", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/admin-catalog")>();
  return { ...actual, getAdminProducts: adminCatalogApiMock.getAdminProducts };
});
```

```csharp
private sealed class CapturingAdminCatalogProductRepository : IAdminCatalogProductRepository
{
    public AdminProductUpsert? LastUpsert { get; private set; }
    public Task<AdminProductDetailRecord> CreateProductAsync(AdminProductUpsert command, CancellationToken cancellationToken = default)
    {
        LastUpsert = command;
        return Task.FromResult(ProductDetailRecord());
    }
}
```

**What to Mock:**
- Mock API clients and browser globals in frontend UI tests: `apps/front/src/components/admin/catalog/admin-product-manager.test.tsx`, `apps/front/src/lib/api/admin-catalog.test.ts`.
- Replace backend services/repositories with test doubles in endpoint tests using `ConfigureTestServices` and `RemoveAll<T>`: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductsEndpointTests.cs`.
- Mock external HTTP/image fetches in Python tooling tests: `tests/tools/test_download_tktdf_product_images.py`.
- Use capturing repositories for service-level backend tests to assert normalized commands and call ordering: `tests/LineCom.Api.Tests/Modules/Requests/CustomerRequestServiceTests.cs`.

**What NOT to Mock:**
- Do not mock SQL strings when testing SQL contracts; assert the exact SQL constants or migration files: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductSqlTests.cs`, `tests/LineCom.Api.Tests/Infrastructure/Database/CatalogFoundationMigrationTests.cs`.
- Do not mock PostgreSQL behavior when validating constraints/triggers/repository database behavior; use `PostgresMigrationFixture` with `LINECOM_TEST_CONNECTION_STRING`: `tests/LineCom.Api.Tests/Infrastructure/Database/PostgresMigrationFixture.cs`, `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogCrudDatabaseBehaviorTests.cs`.
- Do not mock user interactions in React component tests when Testing Library can drive them through `userEvent`: `apps/front/src/components/admin/catalog/admin-category-manager.test.tsx`.

## Fixtures and Factories

**Test Data:**
```csharp
private static UpsertAdminProductCommand ValidCommand(
    string? name = "Cable",
    string? slug = "cable",
    string? saleUnit = "coil",
    string? unitQuantity = "305 m")
{
    return new UpsertAdminProductCommand(...);
}
```

```typescript
const activeProduct: AdminProductListItem = {
  id: "product-active",
  name: "Кабель ВВГнг 3x2.5",
  slug: "kabel-vvgng-3x25",
  publishStatus: "draft",
  isActive: true,
  readiness: { canPublish: false, issues: [] },
};
```

**Location:**
- Backend fixtures are usually private static methods or nested classes inside each test file: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductServiceTests.cs`.
- Shared endpoint host setup is in `tests/LineCom.Api.Tests/Support/LineComWebApplicationFactory.cs`.
- PostgreSQL migration fixture and collection definition are in `tests/LineCom.Api.Tests/Infrastructure/Database/PostgresMigrationFixture.cs`.
- Frontend fixtures are usually top-level constants inside each test file, especially for larger admin component suites: `apps/front/src/components/admin/catalog/admin-product-manager.test.tsx`.

## Coverage

**Requirements:** No enforced coverage threshold detected. `coverlet.collector` is referenced in `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj`, but no coverage command or threshold config was found.

**View Coverage:**
```bash
dotnet test LineCom.sln --collect:"XPlat Code Coverage"  # Available through coverlet.collector
npm --prefix apps/front test -- --coverage               # Possible Vitest mode; no coverage config detected
```

## Test Types

**Unit Tests:**
- Backend service tests validate normalization, authorization guard behavior, domain error mapping, duplicate handling, readiness checks, and repository calls with in-memory test doubles: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductServiceTests.cs`, `tests/LineCom.Api.Tests/Modules/Requests/CustomerRequestServiceTests.cs`.
- Backend pure logic/import tests validate slug generation, import planning, 1C export parsing, and report writing: `tests/LineCom.Api.Tests/CatalogImport/SlugGeneratorTests.cs`, `tests/LineCom.Api.Tests/CatalogImport/CatalogImportPlannerTests.cs`.
- Frontend pure helper tests cover route/query building, reducers, SEO metadata, filtering, sitemap generation, and homepage product selection: `apps/front/src/lib/request-draft/reducer.test.ts`, `apps/front/src/lib/seo/metadata.test.ts`, `apps/front/src/lib/catalog/filtering.test.ts`, `apps/front/src/lib/homepage/featured-products.test.ts`.

**Integration Tests:**
- ASP.NET endpoint tests use `WebApplicationFactory<Program>`, real middleware/routing/auth plumbing, and replaced domain services: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductsEndpointTests.cs`, `tests/LineCom.Api.Tests/Modules/Auth/AuthLoginEndpointTests.cs`.
- Local storage static serving tests verify current public image prefixes and denied internal prefixes through the test host: `tests/LineCom.Api.Tests/Infrastructure/Hosting/LocalStorageStaticFilesTests.cs`.
- Admin storage diagnostics endpoint tests verify authentication/staff access and read-only response behavior with replaced diagnostics services: `tests/LineCom.Api.Tests/Modules/Catalog/StorageDiagnosticsEndpointTests.cs`.
- Database migration behavior tests run against PostgreSQL only when `LINECOM_TEST_CONNECTION_STRING` is configured; otherwise they return early: `tests/LineCom.Api.Tests/Infrastructure/Database/AdminCatalogFoundationDatabaseBehaviorTests.cs`.
- Repository/query database tests share the `PostgresMigration` collection and use Dapper/Npgsql against the migrated schema: `tests/LineCom.Api.Tests/Modules/Catalog/DapperPublicProductQueryDatabaseTests.cs`, `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductAttributeRepositoryDatabaseTests.cs`.

**E2E Tests:**
- Browser E2E framework is not detected in committed project config. No Playwright config is present under the main repo scope.

## Common Patterns

**Async Testing:**
```csharp
var exception = await Assert.ThrowsAsync<ApiException>(() =>
    service.UpdateProductAsync(new DefaultHttpContext(), ProductId, ValidCommand(), CancellationToken.None));

Assert.Equal("admin_catalog.product_not_ready", exception.Code);
```

```typescript
const user = userEvent.setup();
render(<AdminCategoryManager csrfToken="csrf-token" />);
await user.click(screen.getByRole("button", { name: /save/i }));
await waitFor(() => expect(adminCatalogApiMock.getAdminCategories).toHaveBeenCalled());
```

**Error Testing:**
```csharp
var exception = await Assert.ThrowsAsync<PostgresException>(() =>
    connection.ExecuteAsync("INSERT ...", parameters));

Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
```

```typescript
await expect(uploadAdminBrandLogo("brand-id", logo, "csrf-token")).rejects.toThrow(
  "Внутренняя ошибка сервера.",
);
```

**SQL and Migration Testing:**
- Assert critical SQL text for filters, joins, locks, transactions, and constraints: `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductSqlTests.cs`, `tests/LineCom.Api.Tests/Modules/Requests/RequestNumberSqlTests.cs`.
- Assert storage diagnostics SQL remains read-only and reports expected row categories: `tests/LineCom.Api.Tests/Modules/Catalog/StorageDiagnosticsSqlTests.cs`.
- Assert migration files contain expected tables, constraints, indexes, triggers, and forbidden schema choices such as public price columns or JSONB product model usage: `tests/LineCom.Api.Tests/Infrastructure/Database/CatalogFoundationMigrationTests.cs`.
- Use live PostgreSQL tests for behavior that string assertions cannot prove: trigger violations, check constraints, repository mappings, and migrated seed defaults in `tests/LineCom.Api.Tests/Infrastructure/Database/AdminCatalogFoundationDatabaseBehaviorTests.cs`.

**Frontend API Client Testing:**
- Stub `fetch` with `vi.stubGlobal("fetch", fetchMock)` and assert URL, method, `credentials: "include"`, cache mode, JSON/FormData body, and CSRF headers: `apps/front/src/lib/api/admin-catalog.test.ts`, `apps/front/src/lib/api/admin-requests.test.ts`.
- For multipart requests, assert `Content-Type` is not manually set so the browser can provide the boundary: `apps/front/src/lib/api/admin-catalog.test.ts`.

---

*Testing analysis: 2026-05-14*
