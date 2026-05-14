<!-- refreshed: 2026-05-14 -->
# Architecture

**Analysis Date:** 2026-05-14

## System Overview

```text
┌─────────────────────────────────────────────────────────────┐
│                     Next.js App Router                      │
├────────────────────┬────────────────────┬───────────────────┤
│ Public SEO pages   │ Account/Admin UI   │ Client providers  │
│ `apps/front/src/app` │ `apps/front/src/components` │ `apps/front/src/components/auth` │
└──────────┬─────────┴──────────┬─────────┴─────────┬─────────┘
           │                    │                   │
           ▼                    ▼                   ▼
┌─────────────────────────────────────────────────────────────┐
│                  Frontend API client layer                  │
│         `apps/front/src/lib/api`                            │
└──────────────────────────────┬──────────────────────────────┘
                               │ HTTP `/api/*`, `/storage/*`
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                ASP.NET Core modular monolith                │
│ `apps/api/Program.cs`, `apps/api/Modules`, `apps/api/Infrastructure` │
├──────────────┬──────────────┬──────────────┬────────────────┤
│ Auth         │ Account      │ Catalog      │ Requests       │
│ `apps/api/Modules/Auth` │ `apps/api/Modules/Account` │ `apps/api/Modules/Catalog` │ `apps/api/Modules/Requests` │
└──────┬───────┴──────┬───────┴──────┬───────┴───────┬────────┘
       │              │              │               │
       ▼              ▼              ▼               ▼
┌─────────────────────────────────────────────────────────────┐
│ Dapper repositories / query services over Npgsql            │
│ `apps/api/Modules/*/Repositories`, `apps/api/Modules/Catalog/Queries` │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│ PostgreSQL schema + local file storage                      │
│ `apps/dbmigrator/Migrations`, `apps/api/storage`, `/storage` │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ Offline/admin data tooling                                  │
│ DbUp migrator `apps/dbmigrator` + WinForms importer         │
│ `apps/catalog-import.winforms`, `apps/catalog-import.core`  │
└─────────────────────────────────────────────────────────────┘
```

## Component Responsibilities

| Component | Responsibility | File |
|-----------|----------------|------|
| API composition root | Builds middleware pipeline, registers infrastructure and modules, maps controllers, serves local storage | `apps/api/Program.cs` |
| Database infrastructure | Creates singleton `NpgsqlDataSource`, scoped `IDbConnectionFactory`, and local storage writer options | `apps/api/Infrastructure/Database/DatabaseServiceCollectionExtensions.cs` |
| Auth module | Cookie authentication, registration/login/current session/logout, CSRF token claim and password hashing | `apps/api/Modules/Auth` |
| Account module | Customer profile, organization card, password changes for authenticated users | `apps/api/Modules/Account` |
| Catalog module | Public catalog queries, admin category/brand/attribute/product/image CRUD, homepage sections | `apps/api/Modules/Catalog` |
| Requests module | Customer request creation/history and admin request processing | `apps/api/Modules/Requests` |
| Shared errors | Converts `ApiException` and unhandled exceptions into JSON API errors | `apps/api/Shared/Errors/ApiExceptionMiddleware.cs` |
| Db migrator | Applies embedded SQL migrations through DbUp and journals to `public.schema_versions` | `apps/dbmigrator/Program.cs` |
| Migrator core | Shared migration configuration and script name filter used by migrator/tests | `apps/dbmigrator.core` |
| Catalog import core | Reads 1C JSON, image manifests, builds import plan, writes reports, applies catalog/image upserts | `apps/catalog-import.core` |
| Catalog import UI | WinForms shell for dry-run, report writing, guarded apply/reset into PostgreSQL | `apps/catalog-import.winforms/MainForm.cs` |
| Frontend shell | Root layout, global providers, SEO metadata base, CSS bundle imports | `apps/front/src/app/layout.tsx` |
| Frontend API client | Typed wrappers over backend endpoints, cookie credentials, CSRF headers, server-side origin resolution | `apps/front/src/lib/api` |
| Frontend SEO | Metadata, canonical, robots and sitemap helpers for public pages | `apps/front/src/lib/seo`, `apps/front/src/app/robots.ts`, `apps/front/src/app/sitemap.ts` |
| Test boundary | xUnit tests for backend/importer/migrator and Vitest tests for frontend components/helpers | `tests/LineCom.Api.Tests`, `apps/front/src/**/*.test.*` |

## Pattern Overview

**Overall:** Модульный монолит + отдельный frontend + отдельные operational tools.

**Key Characteristics:**
- Backend держится в одном ASP.NET Core Web API приложении `apps/api` и делится на предметные модули в `apps/api/Modules`.
- Доступ к PostgreSQL выполняется явным SQL через Dapper/Npgsql; Entity Framework и EF migrations не используются.
- Frontend использует Next.js App Router: публичные SEO-страницы получают данные из backend API на сервере, личный кабинет и админка работают как client components.
- Миграции выполняются отдельным DbUp runner `apps/dbmigrator`, не из бизнес-логики API.
- Импорт каталога из 1C отделен от API: reusable logic в `apps/catalog-import.core`, ручной UI в `apps/catalog-import.winforms`.

## Layers

**Product Source of Truth:**
- Purpose: Фиксирует продуктовые, архитектурные, data model и cross-cutting требования.
- Location: `vault/Человекочитаемое`
- Contains: `Архитектура backend и БД.md`, `Структура данных релиза.md`, `Продуктовая модель.md`, `Сквозные требования.md`, API contracts.
- Depends on: Not applicable.
- Used by: Любые планы, изменения API/БД/frontend public routing и импортные контуры.

**Frontend Routing and Rendering:**
- Purpose: Маршруты, metadata, page-level data loading, публичные/закрытые экраны.
- Location: `apps/front/src/app`
- Contains: public pages `page.tsx`, `catalog/[categorySlug]/page.tsx`, `products/[slug]/page.tsx`; account/admin pages; `robots.ts`; `sitemap.ts`.
- Depends on: `apps/front/src/lib/api`, `apps/front/src/lib/seo`, `apps/front/src/components`.
- Used by: Browser clients and Next.js runtime.

**Frontend Components and State:**
- Purpose: UI panels/forms/lists and client-side state.
- Location: `apps/front/src/components`, `apps/front/src/lib/request-draft`
- Contains: auth provider, request draft provider/reducer/storage, catalog/admin/account/request components.
- Depends on: typed API clients in `apps/front/src/lib/api`, helpers in `apps/front/src/lib`.
- Used by: Next.js pages in `apps/front/src/app`.

**Frontend API Client Layer:**
- Purpose: Typed fetch wrappers and DTO contracts matching backend routes.
- Location: `apps/front/src/lib/api`
- Contains: `http.ts`, `catalog.ts`, `auth.ts`, `requests.ts`, `admin-catalog.ts`, `admin-homepage.ts`, `admin-requests.ts`, `account.ts`.
- Depends on: browser/server `fetch`, `LINECOM_API_ORIGIN` for server-side backend origin.
- Used by: Server pages, client components, providers.

**API Controllers:**
- Purpose: HTTP route boundary; bind DTO/query/body/form data and delegate to services/query services.
- Location: `apps/api/Modules/*/Controllers`
- Contains: `[Route]`, `[Authorize]`, `[Http*]`, `[RequireCsrfToken]` controllers.
- Depends on: module services or query interfaces.
- Used by: ASP.NET Core `app.MapControllers()` in `apps/api/Program.cs`.

**API Application Services:**
- Purpose: Authorization checks, input normalization, business validation, DTO mapping, transactional intent.
- Location: `apps/api/Modules/*/Services`
- Contains: `CustomerRequestService`, `AdminRequestService`, `AdminCatalogProductService`, auth/account services.
- Depends on: repositories/query services, reference data, current user service.
- Used by: Controllers.

**API Data Access:**
- Purpose: Explicit SQL execution, read models, mutations, transaction blocks.
- Location: `apps/api/Modules/*/Repositories`, `apps/api/Modules/Catalog/Queries`
- Contains: `Dapper*Repository`, `Dapper*Query`, `*Sql.cs` constants/builders.
- Depends on: `IDbConnectionFactory`, Dapper, Npgsql.
- Used by: Services and controllers for public query endpoints.

**Infrastructure:**
- Purpose: Cross-module technical services.
- Location: `apps/api/Infrastructure`
- Contains: database connection factory, hosting/reverse proxy/HTTPS/logging policies, local storage writer/static file serving.
- Depends on: ASP.NET Core hosting, Npgsql, filesystem.
- Used by: `apps/api/Program.cs` and module services.

**Database Migration Layer:**
- Purpose: PostgreSQL schema, constraints, triggers, indexes, migration execution.
- Location: `apps/dbmigrator`, `apps/dbmigrator.core`
- Contains: embedded SQL in `apps/dbmigrator/Migrations/*.sql`, DbUp runner, migration script filter/configuration.
- Depends on: DbUp PostgreSQL, PostgreSQL connection string.
- Used by: Deployment/setup and database tests.

**Catalog Import Layer:**
- Purpose: Offline/admin catalog ingestion from 1C export and reviewed image manifests.
- Location: `apps/catalog-import.core`, `apps/catalog-import.winforms`
- Contains: source readers, image manifest reader, planner, database applier, report writer, WinForms runner.
- Depends on: JSON files in `Assets`, product-image files, Npgsql/Dapper, PostgreSQL schema.
- Used by: Manual catalog import workflow and importer tests.

**Test Layer:**
- Purpose: Regression coverage for API endpoints/services/SQL/migrations/importer/frontend helpers/components.
- Location: `tests/LineCom.Api.Tests`, `apps/front/src/**/*.test.*`
- Contains: xUnit + `WebApplicationFactory`, Postgres migration fixture, database behavior tests, Vitest/jsdom tests.
- Depends on: app projects and frontend package scripts.
- Used by: Verification and future phase gates.

## Data Flow

### Public Catalog Page Path

1. Next.js route loads public page data and metadata in `apps/front/src/app/catalog/[categorySlug]/page.tsx` using `getCategory`, `getCategoryTree`, `getCategoryFilters`, and `getProducts`.
2. API client `apps/front/src/lib/api/catalog.ts` calls `/api/public/catalog/categories/*`, `/api/public/catalog/filters`, and `/api/public/catalog/products` with `next: { revalidate: 60 }`.
3. `apps/front/src/lib/api/http.ts` resolves server-side `/api/*` calls to `LINECOM_API_ORIGIN` or `http://127.0.0.1:8080`; browser-side calls stay relative and use Next rewrites.
4. ASP.NET controllers `apps/api/Modules/Catalog/Controllers/PublicCategoriesController.cs` and `apps/api/Modules/Catalog/Controllers/PublicProductsController.cs` delegate to `IPublicCategoryQuery` / `IPublicProductQuery`.
5. Dapper query services in `apps/api/Modules/Catalog/Queries` execute explicit SQL and response builders map rows into public DTOs.
6. Next.js renders SEO-indexable HTML with canonical metadata from `apps/front/src/lib/seo/metadata.ts`.

### Product Detail Path

1. `apps/front/src/app/products/[slug]/page.tsx` calls `getProduct(slug)` for metadata and body.
2. `apps/front/src/lib/api/catalog.ts` requests `/api/public/catalog/products/{slug}`.
3. `apps/api/Modules/Catalog/Controllers/PublicProductsController.cs` calls `IPublicProductQuery.GetProductDetailAsync`.
4. `apps/api/Modules/Catalog/Queries/DapperPublicProductQuery.cs` executes `PublicProductSql.GetProductDetail` via `QueryMultipleAsync`.
5. `PublicProductDetailResponseBuilder` builds product, images, attributes, breadcrumbs and SEO DTOs.

### Authenticated Admin Catalog Mutation

1. Client page `apps/front/src/app/admin/catalog/catalog-page-client.tsx` restores session through `getMe`, checks role `seller` or `admin`, and passes CSRF token into admin components.
2. Admin API client `apps/front/src/lib/api/admin-catalog.ts` sends JSON/form requests with `X-CSRF-Token`.
3. Backend admin controllers in `apps/api/Modules/Catalog/Controllers` require `[Authorize]`; mutation actions also use `[RequireCsrfToken]`.
4. Services such as `apps/api/Modules/Catalog/Services/AdminCatalogProductService.cs` call `IAdminCatalogStaffGuard`, normalize input, check duplicates/readiness, and delegate to repositories.
5. Dapper repositories in `apps/api/Modules/Catalog/Repositories` execute parameterized SQL and transaction blocks for multi-table mutations.

### Customer Request Creation

1. Frontend request draft is stored client-side by `apps/front/src/components/request/request-draft-provider.tsx` and `apps/front/src/lib/request-draft`.
2. Request submit uses `apps/front/src/lib/api/requests.ts` with cookie credentials and CSRF token.
3. `apps/api/Modules/Requests/Controllers/CustomerRequestsController.cs` requires `[Authorize]` and `[RequireCsrfToken]` for `POST`.
4. `apps/api/Modules/Requests/Services/CustomerRequestService.cs` validates source/items/current session and maps repository output.
5. `apps/api/Modules/Requests/Repositories/DapperCustomerRequestRepository.cs` opens a connection, begins a transaction, generates `ЗКYY-0001`, snapshots organization/product fields, inserts request/items/history, then commits.

### Database Migration Flow

1. `apps/dbmigrator/Program.cs` reads connection string from CLI/environment using `apps/dbmigrator.core/MigrationConfiguration.cs`.
2. DbUp loads embedded scripts selected by `apps/dbmigrator.core/MigrationScripts.cs`.
3. SQL files under `apps/dbmigrator/Migrations` create extensions, tables, constraints, indexes, triggers and schema version journal.
4. Tests reuse the same migrator project references from `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj`.

### Catalog Import Flow

1. WinForms UI `apps/catalog-import.winforms/MainForm.cs` collects source JSON, optional image manifest, report path, connection string and storage root.
2. Dry-run reads 1C JSON through `apps/catalog-import.core/Source/OneCExportReader.cs`, image candidates through `apps/catalog-import.core/Images/ProductImageManifestReader.cs`, and builds a plan in `apps/catalog-import.core/Planning/CatalogImportPlanner.cs`.
3. Report writing uses `apps/catalog-import.core/Reporting/CatalogImportReportWriter.cs`.
4. Apply uses `apps/catalog-import.core/Database/CatalogImportDatabase.cs` to upsert categories/products/attributes/images in one PostgreSQL transaction; reset is guarded by explicit dev/QA confirmation.

**State Management:**
- Server state lives in PostgreSQL and local file storage; schema and integrity are controlled by `apps/dbmigrator/Migrations`.
- API request state is scoped per HTTP request through ASP.NET Core DI; `NpgsqlDataSource` is singleton and connections are opened via scoped factory.
- Auth state uses HttpOnly cookie `linecom_auth` and CSRF token claim surfaced through `GET /api/auth/me`.
- Frontend session state lives in `AuthProvider`; request draft/cart-like state lives in `RequestDraftProvider` and localStorage helpers.
- Public page SEO data is fetched server-side and may use Next.js revalidation; admin/account API calls use `cache: "no-store"`.

## Key Abstractions

**Module Registration Extension:**
- Purpose: Keep dependency registration per module.
- Examples: `apps/api/Modules/Auth/AuthServiceCollectionExtensions.cs`, `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs`, `apps/api/Modules/Requests/RequestServiceCollectionExtensions.cs`.
- Pattern: `AddXModule(IServiceCollection)` registers interfaces to concrete services/repositories.

**Connection Factory:**
- Purpose: Single sanctioned way to open PostgreSQL connections from API data access code.
- Examples: `apps/api/Infrastructure/Database/IDbConnectionFactory.cs`, `apps/api/Infrastructure/Database/NpgsqlConnectionFactory.cs`.
- Pattern: Repositories/query services request `IDbConnectionFactory`, call `OpenConnectionAsync`, and use Dapper commands.

**Dapper Repository / Query Service:**
- Purpose: Separate command-style repositories from read-heavy catalog query services.
- Examples: `apps/api/Modules/Requests/Repositories/DapperCustomerRequestRepository.cs`, `apps/api/Modules/Catalog/Queries/DapperPublicProductQuery.cs`.
- Pattern: SQL in `*Sql.cs` or builder files, parameters passed through Dapper `CommandDefinition`, response rows mapped explicitly.

**Reference Data Services:**
- Purpose: Stable code/label mapping for statuses, sale units and availability.
- Examples: `apps/api/Modules/Requests/Services/RequestReferenceData.cs`, `apps/api/Modules/Catalog/Services/PublicCatalogReferenceData.cs`.
- Pattern: Singleton services validate known codes and return DTO labels.

**API Error Contract:**
- Purpose: Uniform JSON error responses.
- Examples: `apps/api/Shared/Errors/ApiException.cs`, `apps/api/Shared/Errors/ApiErrorResponse.cs`, `apps/api/Shared/Errors/ApiExceptionMiddleware.cs`.
- Pattern: Services throw `ApiException`; middleware returns `{ code, message }`.

**CSRF Guard Attribute:**
- Purpose: Protect unsafe authenticated cookie-auth methods.
- Examples: `apps/api/Modules/Auth/Services/RequireCsrfTokenAttribute.cs`.
- Pattern: Mutation controller actions use `[RequireCsrfToken]`; frontend sends `X-CSRF-Token`.

**Typed Frontend API Modules:**
- Purpose: Keep endpoint paths, DTO shapes and fetch options out of UI components.
- Examples: `apps/front/src/lib/api/catalog.ts`, `apps/front/src/lib/api/admin-catalog.ts`, `apps/front/src/lib/api/http.ts`.
- Pattern: `apiJson<T>`/`apiForm<T>` wrappers with typed functions per API area.

**SEO Helpers:**
- Purpose: Centralize canonical/index/noindex metadata and sitemap construction.
- Examples: `apps/front/src/lib/seo/metadata.ts`, `apps/front/src/lib/seo/sitemap.ts`, `apps/front/src/lib/seo/site.ts`.
- Pattern: Public pages call `indexablePageMetadata`; account/admin/auth pages call `noindexPageMetadata`.

**Import Plan:**
- Purpose: Make catalog import previewable before database writes.
- Examples: `apps/catalog-import.core/Planning/CatalogImportModels.cs`, `apps/catalog-import.core/Planning/CatalogImportPlanner.cs`.
- Pattern: Source reader + image manifest reader -> deterministic `CatalogImportPlan` -> report/apply.

## Entry Points

**Backend API:**
- Location: `apps/api/Program.cs`
- Triggers: `dotnet run --project apps/api/LineCom.Api.csproj`, IIS/Kestrel hosting, test host.
- Responsibilities: Add local config, logging policy, controllers, Swagger, forwarded headers, database, auth/account/catalog/request modules, middleware and static storage.

**Frontend App:**
- Location: `apps/front/src/app/layout.tsx`, `apps/front/src/app/page.tsx`
- Triggers: `npm run dev`, `npm run build`, `npm run start` from `apps/front`.
- Responsibilities: Root HTML/lang/theme/providers/site shell and public homepage rendering.

**Next.js Rewrites:**
- Location: `apps/front/next.config.ts`
- Triggers: Browser requests to `/api/:path*` and `/storage/:path*`.
- Responsibilities: Proxy frontend-relative backend and local storage paths to `LINECOM_API_ORIGIN`.

**Db Migrator:**
- Location: `apps/dbmigrator/Program.cs`
- Triggers: `dotnet run --project apps/dbmigrator/LineCom.DbMigrator.csproj` or deployment command.
- Responsibilities: Apply embedded SQL migrations and write DbUp journal.

**Catalog Importer UI:**
- Location: `apps/catalog-import.winforms/Program.cs`
- Triggers: WinForms executable.
- Responsibilities: Launch `MainForm` for dry-run/report/apply catalog import.

**Backend Tests:**
- Location: `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj`
- Triggers: `dotnet test LineCom.sln` or project-level test command.
- Responsibilities: xUnit coverage for API endpoints/services/SQL/migrations/importer.

**Frontend Tests:**
- Location: `apps/front/vitest.config.ts`
- Triggers: `npm test` from `apps/front`.
- Responsibilities: Vitest/jsdom tests under `apps/front/src/**/*.{test,spec}.{ts,tsx}`.

## Architectural Constraints

- **Threading:** ASP.NET Core uses async request handling; WinForms importer uses UI event handlers with `Task.Run` for dry-run planning and async apply. Do not block request threads or WinForms UI thread with synchronous database/network work.
- **Global state:** Keep mutable process state out of API modules. Existing process-level state is limited to DI singletons/reference data (`NpgsqlDataSource`, `IPublicCatalogReferenceData`, `IRequestReferenceData`, `IPasswordHasher`), static SQL/constants, and frontend React contexts.
- **Circular imports:** No known circular project references. `LineCom.sln` references API, tests, migrator, migrator core, import core and WinForms importer; `tests/LineCom.Api.Tests` references app projects. Keep app projects independent of tests.
- **Data access:** Use Dapper/Npgsql with explicit parameterized SQL. Do not introduce Entity Framework, EF migrations, or generic repositories.
- **Migrations:** Add schema changes as ordered SQL scripts in `apps/dbmigrator/Migrations`; do not mutate schema implicitly in API startup.
- **Storage:** Local FileStorage is the target approach. API serves storage from `Storage:RootPath` or `apps/api/storage` through `/storage`; files must be coordinated with DB records.
- **SEO/GEO:** Public catalog, product, landing, metadata, sitemap, robots and URL changes must preserve SEO/GEO requirements from `vault/Человекочитаемое/Сквозные требования.md`.
- **Auth:** Cookie auth uses HttpOnly cookies. Unsafe authenticated methods require CSRF header and backend attribute coverage.
- **Product model:** Product attributes stay normalized in relational tables; do not store core product attributes or SEO filter conditions in JSONB.
- **Dirty worktree:** Existing product-code modifications are present outside `.planning/codebase`; do not overwrite or revert them while updating codebase maps.

## Anti-Patterns

### SQL in Controllers

**What happens:** Putting SQL or transaction blocks directly in `apps/api/Modules/*/Controllers`.
**Why it's wrong:** Controllers in this codebase are HTTP boundaries; business validation belongs in services and SQL belongs in repositories/query services.
**Do this instead:** Add methods under `apps/api/Modules/<Module>/Services`, `apps/api/Modules/<Module>/Repositories`, or `apps/api/Modules/Catalog/Queries` and register them in the module `*ServiceCollectionExtensions.cs`.

### JSONB for Core Catalog Data

**What happens:** Storing product attributes, public filters, or SEO landing-page conditions as opaque JSON.
**Why it's wrong:** The source-of-truth docs require relational integrity, filtering, comparison and SEO observability for catalog attributes.
**Do this instead:** Extend normalized tables through SQL migrations in `apps/dbmigrator/Migrations` and query them from Dapper repositories/query services.

### Frontend Fetches Embedded in Large UI Panels

**What happens:** Calling raw `fetch` and duplicating endpoint paths inside components under `apps/front/src/components`.
**Why it's wrong:** Existing frontend keeps API DTO/path/fetch behavior in `apps/front/src/lib/api` and UI components focused on rendering/state.
**Do this instead:** Add typed functions to `apps/front/src/lib/api/<area>.ts`; client components should consume those functions and pass CSRF tokens where needed.

### Import Logic Inside Backend API

**What happens:** Mixing one-off 1C import planning/apply code into `apps/api`.
**Why it's wrong:** The importer is a separate operational tool with dry-run/report/apply boundaries and guarded reset behavior.
**Do this instead:** Keep source readers, planning, database apply and reports in `apps/catalog-import.core`; use `apps/catalog-import.winforms` or a future runner as the UI/entry point.

## Error Handling

**Strategy:** Domain/application errors become `ApiException`; middleware serializes stable API error codes/messages. Unexpected exceptions are logged and returned as generic `internal_error`.

**Patterns:**
- Throw module-specific errors from services, for example `AuthErrors`, `RequestErrors`, `AdminCatalogErrors`.
- Let `apps/api/Shared/Errors/ApiExceptionMiddleware.cs` write the response; controllers should not duplicate error serialization.
- Frontend catches and normalizes backend errors through `apps/front/src/lib/api/errors.ts`.
- Importer surfaces errors through WinForms message boxes/log and report output; `CatalogImportReportWriter` creates JSON/Markdown reports.

## Cross-Cutting Concerns

**Logging:** API uses standard ASP.NET Core logging; development console/debug providers are controlled by `apps/api/Infrastructure/Hosting/DevelopmentLoggingPolicy.cs`. Middleware logs unhandled API exceptions.

**Validation:** DTO binding happens in controllers; business validation is in services; database integrity is enforced by SQL constraints/triggers in `apps/dbmigrator/Migrations`. Import source validation is in readers/planners under `apps/catalog-import.core`.

**Authentication:** Backend uses ASP.NET Core cookie auth in `apps/api/Modules/Auth/AuthServiceCollectionExtensions.cs`; frontend session state is in `apps/front/src/components/auth/auth-provider.tsx`. Public catalog endpoints are anonymous; account/admin endpoints require authentication; admin services enforce staff roles.

**Authorization:** Admin catalog/request access is controller-level `[Authorize]` plus service-level staff role checks such as `apps/api/Modules/Catalog/Services/AdminCatalogStaffGuard.cs`. Frontend admin pages also gate UI based on `session.user.role`, but backend remains authoritative.

**CSRF:** Mutation endpoints use `[RequireCsrfToken]`; frontend sends `X-CSRF-Token` via `apiJson`/`apiForm`.

**SEO/GEO:** Public routes use `indexablePageMetadata`, server-side data loading, sitemap and robots helpers. Admin/account/auth pages use noindex metadata and robots disallow internal paths.

**Files:** API local storage is configured through `Storage:RootPath` and served at `/storage`; stored file records are modeled in migrations and manipulated by API/importer storage code.

---

*Architecture analysis: 2026-05-14*
