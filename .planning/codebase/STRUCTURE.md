# Codebase Structure

**Analysis Date:** 2026-05-14

## Directory Layout

```text
LineCom/
├── AGENTS.md                         # Project rules for language, GSD, source of truth, stack constraints
├── LineCom.sln                       # .NET solution for API, tests, migrator, importer projects
├── global.json                       # .NET SDK pin
├── apps/                             # Applications and service/tool projects
│   ├── api/                          # ASP.NET Core Web API modular monolith
│   │   ├── Program.cs                # API composition root and middleware pipeline
│   │   ├── Infrastructure/           # Database, hosting, local storage infrastructure
│   │   ├── Modules/                  # Account/Auth/Catalog/Requests/System modules
│   │   ├── Shared/Errors/            # Shared API exception contract/middleware
│   │   └── storage/                  # Local development storage root for served files
│   ├── dbmigrator/                   # DbUp executable and SQL migrations
│   │   └── Migrations/               # Ordered PostgreSQL SQL migration scripts
│   ├── dbmigrator.core/              # Shared migrator configuration/script filtering
│   ├── catalog-import.core/          # Reusable catalog import source/planning/database/report logic
│   ├── catalog-import.winforms/      # Windows Forms operator UI for importer
│   └── front/                        # Next.js App Router frontend
│       ├── src/app/                  # Routes, layouts, metadata, sitemap, robots
│       ├── src/components/           # UI components by domain
│       ├── src/lib/                  # API clients, SEO, routing, helpers, state reducers
│       ├── src/styles/               # Global CSS split by surface
│       └── public/                   # Frontend static assets
├── tests/
│   ├── LineCom.Api.Tests/            # xUnit tests for backend, migrations, importer core
│   └── tools/                        # Python tests for asset/download helper scripts
├── tools/                            # Standalone Python asset/catalog helper scripts
├── Assets/                           # Source materials, 1C exports, image manifests, product images
├── images/                           # Design/logo/image working assets
├── vault/Человекочитаемое/           # Product/architecture/data/cross-cutting source of truth
├── docs/superpowers/                 # Historical specs, plans, prompts
├── errors/                           # Current screenshot/error artifacts in working tree
└── .planning/codebase/               # Generated codebase maps for GSD planning
```

## Directory Purposes

**`vault/Человекочитаемое`:**
- Purpose: Source of truth for product model, backend/DB architecture, release data model, API contracts and cross-cutting requirements.
- Contains: Markdown requirements/decisions such as `vault/Человекочитаемое/Архитектура backend и БД.md`, `vault/Человекочитаемое/Структура данных релиза.md`, `vault/Человекочитаемое/Продуктовая модель.md`, `vault/Человекочитаемое/Сквозные требования.md`.
- Key files: `vault/Человекочитаемое/Структура проекта.md`, `vault/Человекочитаемое/Технический стек.md`, `vault/Человекочитаемое/Public Catalog API.md`, `vault/Человекочитаемое/Auth Request Core API.md`, `vault/Человекочитаемое/Admin Homepage Management API.md`.

**`apps/api`:**
- Purpose: ASP.NET Core Web API modular monolith.
- Contains: Composition root, infrastructure, domain modules, shared error handling, local storage.
- Key files: `apps/api/Program.cs`, `apps/api/LineCom.Api.csproj`, `apps/api/appsettings.json`, `apps/api/LineCom.Api.http`.

**`apps/api/Modules`:**
- Purpose: Module-local controllers, DTOs, services, repositories, queries and DI registration.
- Contains: `Account`, `Auth`, `Catalog`, `Requests`, `System`.
- Key files: `apps/api/Modules/Auth/AuthServiceCollectionExtensions.cs`, `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs`, `apps/api/Modules/Requests/RequestServiceCollectionExtensions.cs`, `apps/api/Modules/Account/AccountServiceCollectionExtensions.cs`.

**`apps/api/Modules/Auth`:**
- Purpose: Registration, login, current session, logout, cookie auth, CSRF, password hashing.
- Contains: `Controllers`, `DTOs`, `Repositories`, `Services`.
- Key files: `apps/api/Modules/Auth/Controllers/AuthController.cs`, `apps/api/Modules/Auth/Services/CookieAuthSessionService.cs`, `apps/api/Modules/Auth/Services/RequireCsrfTokenAttribute.cs`, `apps/api/Modules/Auth/Repositories/DapperUserLoginRepository.cs`.

**`apps/api/Modules/Account`:**
- Purpose: Authenticated account profile, organization and password operations.
- Contains: `Controllers`, `DTOs`, `Repositories`, `Services`.
- Key files: `apps/api/Modules/Account/Controllers/AccountProfileController.cs`, `apps/api/Modules/Account/Services/AccountProfileService.cs`, `apps/api/Modules/Account/Repositories/DapperAccountProfileRepository.cs`.

**`apps/api/Modules/Catalog`:**
- Purpose: Public catalog reads and admin management for homepage, categories, brands, attributes, products and images.
- Contains: `Controllers`, `DTOs`, `Queries`, `Repositories`, `Services`.
- Key files: `apps/api/Modules/Catalog/Controllers/PublicProductsController.cs`, `apps/api/Modules/Catalog/Controllers/PublicCategoriesController.cs`, `apps/api/Modules/Catalog/Controllers/AdminCatalogProductsController.cs`, `apps/api/Modules/Catalog/Queries/DapperPublicProductQuery.cs`, `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogProductRepository.cs`, `apps/api/Modules/Catalog/Services/AdminCatalogProductService.cs`.

**`apps/api/Modules/Requests`:**
- Purpose: Customer request creation/history and admin request processing.
- Contains: `Controllers`, `DTOs`, `Repositories`, `Services`.
- Key files: `apps/api/Modules/Requests/Controllers/CustomerRequestsController.cs`, `apps/api/Modules/Requests/Controllers/AdminRequestsController.cs`, `apps/api/Modules/Requests/Services/CustomerRequestService.cs`, `apps/api/Modules/Requests/Repositories/DapperCustomerRequestRepository.cs`, `apps/api/Modules/Requests/Repositories/RequestNumberSql.cs`.

**`apps/api/Infrastructure`:**
- Purpose: Technical services shared by modules.
- Contains: `Database`, `Hosting`, `Storage`.
- Key files: `apps/api/Infrastructure/Database/DatabaseServiceCollectionExtensions.cs`, `apps/api/Infrastructure/Database/NpgsqlConnectionFactory.cs`, `apps/api/Infrastructure/Hosting/LocalStorageStaticFilesExtensions.cs`, `apps/api/Infrastructure/Storage/LocalStoredFileWriter.cs`.

**`apps/api/Shared`:**
- Purpose: Shared contracts not owned by one module.
- Contains: `Errors`.
- Key files: `apps/api/Shared/Errors/ApiException.cs`, `apps/api/Shared/Errors/ApiErrorResponse.cs`, `apps/api/Shared/Errors/ApiExceptionMiddleware.cs`.

**`apps/dbmigrator`:**
- Purpose: Executable DbUp migration runner and embedded migration scripts.
- Contains: `Program.cs`, `ProgramMarker.cs`, `Migrations/*.sql`.
- Key files: `apps/dbmigrator/Program.cs`, `apps/dbmigrator/LineCom.DbMigrator.csproj`, `apps/dbmigrator/Migrations/001_extensions.sql`, `apps/dbmigrator/Migrations/007_admin_catalog_foundation.sql`.

**`apps/dbmigrator.core`:**
- Purpose: Migrator helpers shared with tests.
- Contains: Migration configuration and script detection.
- Key files: `apps/dbmigrator.core/MigrationConfiguration.cs`, `apps/dbmigrator.core/MigrationScripts.cs`, `apps/dbmigrator.core/LineCom.DbMigrator.Core.csproj`.

**`apps/catalog-import.core`:**
- Purpose: Importer domain logic independent of WinForms.
- Contains: `Source`, `Images`, `Planning`, `Database`, `Reporting`.
- Key files: `apps/catalog-import.core/Source/OneCExportReader.cs`, `apps/catalog-import.core/Images/ProductImageManifestReader.cs`, `apps/catalog-import.core/Planning/CatalogImportPlanner.cs`, `apps/catalog-import.core/Database/CatalogImportDatabase.cs`, `apps/catalog-import.core/Reporting/CatalogImportReportWriter.cs`.

**`apps/catalog-import.winforms`:**
- Purpose: Operator UI for catalog import dry-run/report/apply.
- Contains: WinForms entry point and form.
- Key files: `apps/catalog-import.winforms/Program.cs`, `apps/catalog-import.winforms/MainForm.cs`, `apps/catalog-import.winforms/LineCom.CatalogImport.WinForms.csproj`.

**`apps/front`:**
- Purpose: Next.js App Router frontend.
- Contains: `src/app`, `src/components`, `src/lib`, `src/styles`, `public`, config/package files.
- Key files: `apps/front/package.json`, `apps/front/next.config.ts`, `apps/front/tsconfig.json`, `apps/front/vitest.config.ts`, `apps/front/eslint.config.mjs`.

**`apps/front/src/app`:**
- Purpose: Route tree, page-level data loading, metadata, sitemap, robots.
- Contains: public routes, account routes, auth routes, admin routes.
- Key files: `apps/front/src/app/layout.tsx`, `apps/front/src/app/page.tsx`, `apps/front/src/app/catalog/[categorySlug]/page.tsx`, `apps/front/src/app/products/[slug]/page.tsx`, `apps/front/src/app/sitemap.ts`, `apps/front/src/app/robots.ts`.

**`apps/front/src/components`:**
- Purpose: Reusable and domain-specific UI components.
- Contains: `account`, `admin`, `auth`, `catalog`, `home`, `layout`, `request`.
- Key files: `apps/front/src/components/auth/auth-provider.tsx`, `apps/front/src/components/request/request-draft-provider.tsx`, `apps/front/src/components/admin/catalog/admin-catalog-shell.tsx`, `apps/front/src/components/catalog/product-card.tsx`.

**`apps/front/src/lib`:**
- Purpose: Typed API clients, SEO helpers, route helpers, domain helpers and reducers.
- Contains: `api`, `catalog`, `homepage`, `request-draft`, `seo`, `format.ts`, `product-images.ts`, `routes.ts`.
- Key files: `apps/front/src/lib/api/http.ts`, `apps/front/src/lib/api/catalog.ts`, `apps/front/src/lib/api/admin-catalog.ts`, `apps/front/src/lib/seo/metadata.ts`, `apps/front/src/lib/request-draft/reducer.ts`.

**`apps/front/src/styles`:**
- Purpose: Global CSS split by UI surface.
- Contains: layout/public/account/admin/request/responsive styles.
- Key files: `apps/front/src/styles/layout.css`, `apps/front/src/styles/public.css`, `apps/front/src/styles/admin-catalog.css`, `apps/front/src/styles/admin-homepage.css`, `apps/front/src/styles/responsive.css`.

**`tests/LineCom.Api.Tests`:**
- Purpose: Backend, migrator and importer tests.
- Contains: `Infrastructure`, `Modules`, `CatalogImport`, `Shared`, `System`, `Support`.
- Key files: `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj`, `tests/LineCom.Api.Tests/Support/LineComWebApplicationFactory.cs`, `tests/LineCom.Api.Tests/Infrastructure/Database/PostgresMigrationFixture.cs`.

**`tests/tools`:**
- Purpose: Python-side tests for repository helper scripts.
- Contains: tool tests.
- Key files: `tests/tools/test_download_tktdf_product_images.py`.

**`tools`:**
- Purpose: Standalone Python scripts for source asset/product image extraction/downloading.
- Contains: scripts that operate on `Assets`.
- Key files: `tools/extract_1c_41_01.py`, `tools/download_tktdf_product_images.py`, `tools/download_product_image_candidates.py`, `tools/download_product_png_review_batch.py`.

**`Assets`:**
- Purpose: Source materials and generated data for catalog/import/image workflows.
- Contains: 1C exports, scraper output, image candidates/manifests, product images.
- Key files: `Assets/1c_export.xlsx`, `Assets/1c_export_41_01_nomenclature_by_category.json`, `Assets/product-images/tktdf_manifest.json`.

**`docs/superpowers`:**
- Purpose: Historical design specs/plans/prompts used by earlier workflows.
- Contains: `specs`, `plans`, `prompts`.
- Key files: `docs/superpowers/plans/2026-05-13-deep-refactor-current-main.md`, `docs/superpowers/specs/2026-05-14-admin-homepage-product-category-picker-design.md`.

## Key File Locations

**Entry Points:**
- `apps/api/Program.cs`: ASP.NET Core backend entry point.
- `apps/front/src/app/layout.tsx`: Next.js root layout and providers.
- `apps/front/src/app/page.tsx`: Public homepage.
- `apps/dbmigrator/Program.cs`: Database migration runner.
- `apps/catalog-import.winforms/Program.cs`: WinForms importer process entry.
- `apps/catalog-import.winforms/MainForm.cs`: Importer dry-run/report/apply workflow.

**Configuration:**
- `LineCom.sln`: .NET solution membership.
- `global.json`: .NET SDK version pin.
- `apps/api/appsettings.json`: Backend default configuration shape; connection string value is empty by default.
- `apps/api/appsettings.Development.json`: Backend development configuration shape.
- `apps/front/package.json`: Frontend scripts and dependencies.
- `apps/front/next.config.ts`: Standalone output and `/api`/`/storage` rewrites to backend origin.
- `apps/front/tsconfig.json`: TypeScript strict mode and `@/*` path alias.
- `apps/front/vitest.config.ts`: Frontend test runner config.
- `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj`: Backend/importer/migrator test dependencies and project references.

**Core Logic:**
- `apps/api/Modules/Auth`: Cookie auth, current user, CSRF, password hashing.
- `apps/api/Modules/Account`: Account and organization operations.
- `apps/api/Modules/Catalog`: Public/admin catalog, homepage, products, images.
- `apps/api/Modules/Requests`: Customer/admin request workflows.
- `apps/api/Infrastructure/Database`: PostgreSQL connection setup.
- `apps/api/Infrastructure/Storage`: Local file write behavior.
- `apps/dbmigrator/Migrations`: PostgreSQL DDL, constraints, triggers and indexes.
- `apps/catalog-import.core`: 1C/catalog import logic.
- `apps/front/src/lib/api`: Frontend API boundary.
- `apps/front/src/lib/seo`: SEO/GEO helpers.
- `apps/front/src/lib/request-draft`: Request draft state model, reducer and storage.

**Testing:**
- `tests/LineCom.Api.Tests/Modules`: Backend module endpoint/service/repository tests.
- `tests/LineCom.Api.Tests/Infrastructure/Database`: Migration and database behavior tests.
- `tests/LineCom.Api.Tests/CatalogImport`: Importer core tests.
- `tests/LineCom.Api.Tests/System`: Health endpoint tests.
- `apps/front/src/**/*.test.ts`: Frontend helper/reducer/API tests.
- `apps/front/src/**/*.test.tsx`: Frontend component/page tests.
- `tests/tools/test_download_tktdf_product_images.py`: Python tool tests.

## Naming Conventions

**Files:**
- Backend controllers: `*Controller.cs`, e.g. `apps/api/Modules/Catalog/Controllers/AdminCatalogProductsController.cs`.
- Backend services/interfaces: `*Service.cs` and `I*Service.cs`, e.g. `apps/api/Modules/Requests/Services/CustomerRequestService.cs`.
- Backend repositories/interfaces: `Dapper*Repository.cs` and `I*Repository.cs`, e.g. `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogProductRepository.cs`.
- Backend query services: `Dapper*Query.cs` and `I*Query.cs`, e.g. `apps/api/Modules/Catalog/Queries/DapperPublicProductQuery.cs`.
- Backend SQL holders/builders: `*Sql.cs` and `*SqlBuilder.cs`, e.g. `apps/api/Modules/Catalog/Queries/PublicProductListSqlBuilder.cs`.
- Backend DTO files: `*Dtos.cs`, e.g. `apps/api/Modules/Catalog/DTOs/PublicProductDtos.cs`.
- Migration scripts: zero-padded ordered SQL, e.g. `apps/dbmigrator/Migrations/004_requests.sql`.
- Frontend pages: App Router `page.tsx` under route folders, e.g. `apps/front/src/app/admin/catalog/page.tsx`.
- Frontend client page components: `*-page-client.tsx`, e.g. `apps/front/src/app/admin/catalog/catalog-page-client.tsx`.
- Frontend components/helpers: kebab-case, e.g. `apps/front/src/components/admin/catalog/admin-product-manager.tsx`.
- Frontend tests: colocated `*.test.ts` / `*.test.tsx`, e.g. `apps/front/src/lib/seo/metadata.test.ts`.

**Directories:**
- Backend modules use PascalCase: `apps/api/Modules/Catalog`.
- Backend module subfolders use PascalCase plural: `Controllers`, `DTOs`, `Services`, `Repositories`, `Queries`.
- Frontend route folders follow URL segments: `apps/front/src/app/catalog/[categorySlug]`, `apps/front/src/app/products/[slug]`.
- Frontend component/lib folders use lowercase/kebab-case: `apps/front/src/components/request`, `apps/front/src/lib/request-draft`.
- Test folders mirror backend boundaries: `tests/LineCom.Api.Tests/Modules/Catalog`, `tests/LineCom.Api.Tests/Infrastructure/Database`.

## Where to Add New Code

**New backend feature module:**
- Primary code: `apps/api/Modules/<ModuleName>`
- DI registration: `apps/api/Modules/<ModuleName>/<ModuleName>ServiceCollectionExtensions.cs`
- Composition root hook: `apps/api/Program.cs`
- Tests: `tests/LineCom.Api.Tests/Modules/<ModuleName>`

**New backend endpoint in existing module:**
- Controller: `apps/api/Modules/<Module>/Controllers`
- DTOs: `apps/api/Modules/<Module>/DTOs`
- Business logic: `apps/api/Modules/<Module>/Services`
- SQL/data access: `apps/api/Modules/<Module>/Repositories` or `apps/api/Modules/Catalog/Queries` for public/read-heavy catalog queries
- Registration: existing `apps/api/Modules/<Module>/*ServiceCollectionExtensions.cs`
- Tests: matching `tests/LineCom.Api.Tests/Modules/<Module>` folder.

**New database schema change:**
- Migration: next ordered script in `apps/dbmigrator/Migrations`
- Migrator helper changes: `apps/dbmigrator.core`
- Database behavior tests: `tests/LineCom.Api.Tests/Infrastructure/Database` or module-specific repository database tests.

**New frontend public page:**
- Route: `apps/front/src/app/<route>/page.tsx`
- Metadata: `apps/front/src/lib/seo/metadata.ts` helpers from page.
- URL helper: `apps/front/src/lib/routes.ts`
- Data client: `apps/front/src/lib/api/<area>.ts`
- UI components: `apps/front/src/components/<area>`
- Styles: existing surface file in `apps/front/src/styles` or a focused new CSS file imported from `apps/front/src/app/layout.tsx`.

**New frontend admin/account page:**
- Route: `apps/front/src/app/admin/<area>/page.tsx` or `apps/front/src/app/account/<area>/page.tsx`
- Client orchestration: `*-page-client.tsx` next to the route page.
- API client: `apps/front/src/lib/api/admin-*.ts` or `apps/front/src/lib/api/account.ts`.
- Components: `apps/front/src/components/admin/<area>` or `apps/front/src/components/account`.
- Metadata: use `noindexPageMetadata`.

**New frontend component:**
- Domain component: `apps/front/src/components/<domain>`
- Pure helper/reducer: `apps/front/src/lib/<domain>`
- Tests: colocated `*.test.tsx` for components and `*.test.ts` for pure helpers.

**New catalog import behavior:**
- Source parsing: `apps/catalog-import.core/Source`
- Image manifest behavior: `apps/catalog-import.core/Images`
- Planning/mapping: `apps/catalog-import.core/Planning`
- Database apply: `apps/catalog-import.core/Database`
- Reports: `apps/catalog-import.core/Reporting`
- Operator UI only: `apps/catalog-import.winforms/MainForm.cs`
- Tests: `tests/LineCom.Api.Tests/CatalogImport`.

**New local storage behavior:**
- API storage writer/options: `apps/api/Infrastructure/Storage`
- Static serving policy: `apps/api/Infrastructure/Hosting/LocalStorageStaticFilesExtensions.cs`
- Schema changes for stored files: `apps/dbmigrator/Migrations`
- Tests: `tests/LineCom.Api.Tests/Infrastructure/Storage`.

**Utilities:**
- Frontend shared helpers: `apps/front/src/lib`
- Backend cross-module helpers: `apps/api/Shared` only when not owned by a single module.
- Asset/download scripts: `tools`
- Product source/reference data: `Assets`

## Special Directories

**`apps/front/node_modules`:**
- Purpose: Frontend dependency install output.
- Generated: Yes.
- Committed: No.

**`apps/**/bin` and `apps/**/obj`:**
- Purpose: .NET build output and intermediate files.
- Generated: Yes.
- Committed: No.

**`tests/LineCom.Api.Tests/TestResults`:**
- Purpose: .NET test result output.
- Generated: Yes.
- Committed: No.

**`apps/api/storage`:**
- Purpose: Local development file storage root served by API when `Storage:RootPath` is absent.
- Generated: Partly; contains runtime/storage artifacts.
- Committed: Treat as runtime data unless a specific fixture is intentionally added.

**`Assets/product-images`:**
- Purpose: Product image source/review/import assets and manifests.
- Generated: Partly.
- Committed: Yes for curated source/import assets that are part of catalog work.

**`.planning/codebase`:**
- Purpose: Generated architecture/structure/quality/stack maps for GSD planning.
- Generated: Yes.
- Committed: Yes when orchestrator chooses to persist planning artifacts.

**`.codex-local`, `.codex-smoke`, `.codex-tmp`, `.playwright-mcp`:**
- Purpose: Local assistant/test/browser scratch state.
- Generated: Yes.
- Committed: No.

**`.worktrees`:**
- Purpose: Local git worktrees.
- Generated: Yes.
- Committed: No.

**`errors`:**
- Purpose: Current screenshot/error artifacts visible in working tree.
- Generated: Yes.
- Committed: Depends on workflow; do not modify for architecture mapping.

---

*Structure analysis: 2026-05-14*
