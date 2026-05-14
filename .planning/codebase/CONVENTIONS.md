# Coding Conventions

**Analysis Date:** 2026-05-14

## Naming Patterns

**Files:**
- C# files use PascalCase matching the primary type: `apps/api/Modules/Catalog/Services/AdminCatalogProductService.cs`, `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogProductRepository.cs`, `apps/api/Modules/Catalog/DTOs/AdminCatalogProductDtos.cs`.
- Backend module folders group by feature and role: `apps/api/Modules/Catalog/Controllers`, `apps/api/Modules/Catalog/Services`, `apps/api/Modules/Catalog/Repositories`, `apps/api/Modules/Catalog/Queries`, `apps/api/Modules/Catalog/DTOs`.
- SQL migrations use ordered numeric prefixes and descriptive snake_case names: `apps/dbmigrator/Migrations/001_extensions.sql`, `apps/dbmigrator/Migrations/007_admin_catalog_foundation.sql`.
- Frontend files use kebab-case by component/domain: `apps/front/src/components/admin/catalog/admin-product-manager.tsx`, `apps/front/src/components/admin/catalog/admin-product-manager-helpers.ts`, `apps/front/src/lib/request-draft/reducer.ts`.
- Frontend tests are co-located with implementation using `.test.ts` or `.test.tsx`: `apps/front/src/components/request/add-to-request-button.test.tsx`, `apps/front/src/lib/seo/metadata.test.ts`.

**Functions:**
- C# async methods end with `Async` and accept `CancellationToken cancellationToken = default` where they call I/O: `GetProductsAsync` in `apps/api/Modules/Catalog/Services/AdminCatalogProductService.cs`, `OpenConnectionAsync` usage in `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogProductRepository.cs`.
- C# service helpers use imperative private names for validation and mapping: `ThrowIfDuplicateHardIdentityAsync`, `ThrowIfPublishingNotReadyAsync`, `ToUpsert`, `ToAttributeValueUpserts` in `apps/api/Modules/Catalog/Services/AdminCatalogProductService.cs`.
- TypeScript helpers use camelCase verbs and are exported when shared by tests/components: `buildProductListParams`, `productPageMetaFromResponse`, `loadCatalogOptionPages` in `apps/front/src/components/admin/catalog/admin-product-manager-helpers.ts`.
- React event handlers and loaders use verb names scoped to the UI state they mutate: `loadProductsForParams`, `changeSearch`, `changeProductPageSize`, `refreshProductList` in `apps/front/src/components/admin/catalog/admin-product-manager.tsx`.

**Variables:**
- C# private fields use `_camelCase`: `_productService` in `apps/api/Modules/Catalog/Controllers/AdminCatalogProductsController.cs`, `_connectionFactory` in `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogProductRepository.cs`.
- C# constants use PascalCase for class-level constants: `ProductInUseMessage` in `apps/api/Modules/Catalog/Services/AdminCatalogProductService.cs`.
- TypeScript constants use camelCase unless they represent cross-file constants: `allCatalogOptionsPageSize`, `defaultProductPageSize` in `apps/front/src/components/admin/catalog/admin-product-manager.tsx`; `SITEMAP_PRODUCT_PAGE_SIZE` in `apps/front/src/app/sitemap.ts`.
- React state follows `[value, setValue]` naming: `products/setProducts`, `selectedProduct/setSelectedProduct`, `isMutating/setIsMutating` in `apps/front/src/components/admin/catalog/admin-product-manager.tsx`.

**Types:**
- Backend interfaces use `I` prefix and live next to implementations: `IAdminCatalogProductService` with `AdminCatalogProductService` in `apps/api/Modules/Catalog/Services`, `IAdminCatalogProductRepository` with `DapperAdminCatalogProductRepository` in `apps/api/Modules/Catalog/Repositories`.
- Backend DTOs and commands are `public sealed record` types with PascalCase properties: `AdminProductListQuery`, `AdminProductDetailDto`, `UpsertAdminProductCommand` in `apps/api/Modules/Catalog/DTOs/AdminCatalogProductDtos.cs`.
- Backend repository read/write models use suffixes like `Record`, `Query`, `Upsert`, `Response`: examples in `apps/api/Modules/Catalog/Repositories/AdminHomepageRecords.cs` and `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogProductRepository.cs`.
- Frontend API contract types mirror backend JSON shape with camelCase properties: `AdminProductListResponse`, `AdminProductDetail`, `UpsertAdminCategoryCommand` in `apps/front/src/lib/api/admin-catalog.ts`.

## Code Style

**Formatting:**
- No root `.editorconfig`, Prettier config, or C# formatter config was detected.
- C# uses file-scoped namespaces, nullable reference types, implicit usings, four-space indentation, braces on new lines, and `sealed` for concrete classes/records: `apps/api/LineCom.Api.csproj`, `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj`, `apps/api/Modules/Catalog/Controllers/AdminCatalogProductsController.cs`.
- TypeScript uses strict mode, semicolons, double quotes, two-space indentation, and trailing commas in multiline literals: `apps/front/tsconfig.json`, `apps/front/src/components/admin/catalog/admin-product-manager-helpers.ts`.
- JSX keeps large UI flows in presentational sibling components when possible: `apps/front/src/components/admin/catalog/admin-product-manager.tsx` orchestrates loading/mutations and delegates UI to `AdminProductEditor` and `AdminProductListPanel`.

**Linting:**
- Frontend linting uses ESLint 9 with `eslint-config-next/core-web-vitals` and `eslint-config-next/typescript`: `apps/front/eslint.config.mjs`.
- Lint command is `npm --prefix apps/front run lint`, backed by `"lint": "eslint"` in `apps/front/package.json`.
- Backend lint/analyzer configuration is not detected beyond SDK defaults in `global.json` and `.csproj` nullable/implicit-usings settings.

## Import Organization

**Order:**
1. C# `using` directives start with external packages and project namespaces, then framework namespaces; examples: `apps/api/Modules/Catalog/Controllers/AdminCatalogProductsController.cs`, `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductsEndpointTests.cs`.
2. TypeScript imports group framework/runtime imports first, project alias imports next, and relative component/helper imports last: `apps/front/src/components/admin/catalog/admin-product-manager.tsx`.
3. Type-only imports are explicit with `type`: `apps/front/src/components/admin/catalog/admin-product-manager.tsx`, `apps/front/src/app/catalog/[categorySlug]/page.tsx`.

**Path Aliases:**
- Frontend uses `@/*` mapped to `apps/front/src/*`: `apps/front/tsconfig.json` and `apps/front/vitest.config.ts`.
- Backend uses project namespaces rooted at `LineCom.Api`, `LineCom.CatalogImport.Core`, and `LineCom.DbMigrator.Core`: `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs`, `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj`.

## Error Handling

**Patterns:**
- Backend public API errors use `ApiException` with stable machine-readable codes, human messages, and HTTP status codes: `apps/api/Shared/Errors/ApiException.cs`, `apps/api/Modules/Catalog/Services/AdminCatalogErrors.cs`.
- `ApiExceptionMiddleware` writes `{ code, message }` JSON for known API errors and logs only unhandled exceptions as server errors: `apps/api/Shared/Errors/ApiExceptionMiddleware.cs`.
- Controllers stay thin and let services throw domain/API exceptions: `apps/api/Modules/Catalog/Controllers/AdminCatalogProductsController.cs`.
- Services normalize inputs and map repository/domain failures to public API errors: `AdminCatalogProductService.CreateProductAsync` and `UpdateProductAsync` in `apps/api/Modules/Catalog/Services/AdminCatalogProductService.cs`.
- Dapper repositories catch `PostgresException` by SQL state/constraint and rethrow domain-specific exceptions: `apps/api/Modules/Catalog/Repositories/AdminProductPostgresExceptionMapper.cs`, `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogProductRepository.cs`.
- Frontend API clients throw `ApiClientError` for backend `{ code, message }` payloads and normalize unknown failures to `internal_error`: `apps/front/src/lib/api/http.ts`, `apps/front/src/lib/api/errors.ts`.
- App Router public pages catch backend failures and return controlled noindex/unavailable states; 404 API errors call `notFound()`: `apps/front/src/app/catalog/[categorySlug]/page.tsx`, `apps/front/src/app/products/[slug]/page.tsx`.

## Logging

**Framework:** ASP.NET Core logging and console output.

**Patterns:**
- API logging is centralized for unhandled exceptions in `apps/api/Shared/Errors/ApiExceptionMiddleware.cs`.
- Development logging policy can replace providers with console/debug logging: `apps/api/Infrastructure/Hosting/DevelopmentLoggingPolicy.cs`, wired in `apps/api/Program.cs`.
- DbUp migrator writes migration output and failures to console: `apps/dbmigrator/Program.cs`.
- Frontend code does not use an app-level logging wrapper; UI code surfaces errors as state/alerts via `normalizeApiError`, for example `apps/front/src/components/admin/catalog/admin-product-manager.tsx`.

## Comments

**When to Comment:**
- Comments are sparse and used primarily to explain configuration overrides, not routine code behavior: `apps/front/eslint.config.mjs`.
- SQL strings and migrations rely on explicit names and constraints rather than prose comments: `apps/api/Modules/Catalog/Repositories/AdminCatalogProductSql.cs`, `apps/dbmigrator/Migrations/002_catalog_foundation.sql`.
- Add comments only for non-obvious infrastructure or cross-cutting decisions; prefer expressive type names, SQL constraint names, and focused helper functions.

**JSDoc/TSDoc:**
- Not detected as a regular convention in `apps/front/src` or `apps/api`.

## Function Design

**Size:** Keep public service/controller methods focused on one operation. If a frontend implementation approaches 300-400 lines or mixes orchestration, mapping, and rendering, split helpers and panels before adding behavior. This pattern is visible in `apps/front/src/components/admin/catalog/admin-product-manager.tsx`, `apps/front/src/components/admin/catalog/admin-product-manager-helpers.ts`, `apps/front/src/components/admin/catalog/admin-product-editor.tsx`, and `apps/front/src/components/admin/catalog/admin-product-list-panel.tsx`.

**Parameters:** Pass request DTOs/command records into backend services and controllers; pass `HttpContext` only where auth/session context is required: `apps/api/Modules/Catalog/Services/IAdminCatalogProductService.cs`, `apps/api/Modules/Requests/Services/ICustomerRequestService.cs`.

**Return Values:** Backend services return DTO records or task results, not raw database rows: `apps/api/Modules/Catalog/Services/AdminCatalogProductService.cs`. Repositories return module-specific records and nullable results for missing rows: `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogProductRepository.cs`.

## Module Design

**Exports:** Backend module registration is centralized in `*ServiceCollectionExtensions.cs`; use this for new service/repository/query registrations: `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs`, `apps/api/Modules/Requests/RequestServiceCollectionExtensions.cs`, `apps/api/Modules/Auth/AuthServiceCollectionExtensions.cs`.

**Barrel Files:** Not used. Frontend imports concrete modules directly through `@/lib/...` or relative component paths: `apps/front/src/components/admin/catalog/admin-product-manager.tsx`.

## Data Access and Migrations

**Dapper/Npgsql:**
- Use `IDbConnectionFactory` and `NpgsqlDataSource`; do not instantiate ad hoc connection strings in repositories: `apps/api/Infrastructure/Database/DatabaseServiceCollectionExtensions.cs`, `apps/api/Infrastructure/Database/NpgsqlConnectionFactory.cs`.
- Use explicit SQL constants in repository/query SQL classes, not SQL embedded in controllers: `apps/api/Modules/Catalog/Repositories/AdminCatalogProductSql.cs`, `apps/api/Modules/Requests/Repositories/RequestNumberSql.cs`.
- Use `CommandDefinition` with `cancellationToken` for Dapper calls: `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogProductRepository.cs`.
- Use parameterized queries with anonymous parameter objects; do not concatenate request values into SQL: `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogProductRepository.cs`.
- Put transactional boundaries in application/repository operations that mutate multiple related records, with explicit rollback on known and unknown exceptions: `UpdateProductAsync` and `UpdateProductAttributesAsync` in `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogProductRepository.cs`.

**SQL migrations:**
- Add migrations under `apps/dbmigrator/Migrations` and ensure they are embedded by `apps/dbmigrator/LineCom.DbMigrator.csproj`.
- Keep migration discovery compatible with `MigrationScripts.IsMigrationScript` in `apps/dbmigrator.core/MigrationScripts.cs`.
- DbUp journaling uses `public.schema_versions`: `apps/dbmigrator/Program.cs`, `tests/LineCom.Api.Tests/Infrastructure/Database/PostgresMigrationFixture.cs`.
- Product/catalog integrity belongs in database constraints/triggers when feasible, with tests around migration text and live PostgreSQL behavior: `tests/LineCom.Api.Tests/Infrastructure/Database/CatalogFoundationMigrationTests.cs`, `tests/LineCom.Api.Tests/Infrastructure/Database/AdminCatalogFoundationDatabaseBehaviorTests.cs`.

## Frontend and SEO/GEO

**Next/React:**
- Use App Router server pages for public catalog/product routes and metadata: `apps/front/src/app/catalog/[categorySlug]/page.tsx`, `apps/front/src/app/products/[slug]/page.tsx`.
- Use `"use client"` only on interactive components/providers: `apps/front/src/components/admin/catalog/admin-product-manager.tsx`, `apps/front/src/components/request/request-draft-provider.tsx`.
- Centralize API fetch behavior in `apps/front/src/lib/api/http.ts` and endpoint wrappers in `apps/front/src/lib/api/*.ts`.
- Keep route construction in `apps/front/src/lib/routes.ts`; use `encodeURIComponent` for dynamic segments.
- For catalog, landing, routing, metadata, sitemap, or public content changes, preserve SEO helpers and tests in `apps/front/src/lib/seo`, `apps/front/src/app/sitemap.ts`, and `apps/front/src/app/robots.ts`.

---

*Convention analysis: 2026-05-14*
