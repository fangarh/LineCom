# Codebase Concerns

**Analysis Date:** 2026-05-14

## Tech Debt

**Resolved in Phase 2 - Local FileStorage no longer exposes the whole root anonymously:**
- Status: fixed by `.planning/phases/02-storage-access-and-diagnostics/02-01-SUMMARY.md`.
- Files: `apps/api/Infrastructure/Hosting/LocalStorageStaticFilesExtensions.cs`, `apps/api/Infrastructure/Hosting/LocalStoragePathPolicy.cs`, `tests/LineCom.Api.Tests/Infrastructure/Hosting/LocalStorageStaticFilesTests.cs`.
- Current behavior: anonymous static serving is limited to `/storage/products` and `/storage/brands`; internal prefixes such as `import_source`, `export_result` and `temp` are not mapped as public static file roots.
- Remaining work: future non-public file purposes still need explicit controller authorization or a separate access policy before they are exposed through web workflows.

**Storage lifecycle has read-only diagnostics, but no cleanup command yet:**
- Status: diagnostics added by `.planning/phases/02-storage-access-and-diagnostics/02-02-SUMMARY.md`; cleanup remains out of Phase 2 scope.
- Files: `apps/api/Modules/Catalog/Controllers/AdminStorageDiagnosticsController.cs`, `apps/api/Modules/Catalog/Services/StorageDiagnosticsService.cs`, `apps/api/Modules/Catalog/Repositories/StorageDiagnosticsSql.cs`, `tests/LineCom.Api.Tests/Modules/Catalog/StorageDiagnostics*Tests.cs`.
- Current behavior: staff-only diagnostics report missing active files, untracked disk files, stale deleted rows and orphaned rows without mutating disk or database state and without exposing absolute paths.
- Remaining work: retention-based physical cleanup and import staging/promotion consistency are still future phases.

**Импорт каталога может рассинхронизировать БД и диск:**
- Issue: `CatalogImportDatabase.ApplyAsync` открывает DB transaction, затем копирует подготовленные изображения в local storage до вставки DB rows и до commit. `ResetCatalog` удаляет строки каталога и catalog-import `stored_files`, но не удаляет физические файлы.
- Files: `apps/catalog-import.core/Database/CatalogImportDatabase.cs`, `apps/catalog-import.winforms/MainForm.cs`, `vault/Человекочитаемое/Архитектура backend и БД.md`
- Impact: упавший импорт может оставить файлы без DB rows; reset import может удалить DB records, оставив физические файлы в storage.
- Fix approach: писать изображения импорта в staging directory, после commit продвигать их в публичный storage с compensating cleanup; reset cleanup включить в общий storage maintenance command.

**Крупные stateful admin UI containers превышают проектный порог декомпозиции:**
- Issue: несколько frontend-файлов одновременно держат загрузку, mutation guards, data mapping и состояние rendering/forms, хотя проектное правило требует проверять декомпозицию на 300-400 строках.
- Files: `apps/front/src/components/admin/catalog/admin-attribute-manager.tsx`, `apps/front/src/components/admin/catalog/admin-brand-manager.tsx`, `apps/front/src/components/admin/catalog/admin-product-manager.tsx`, `apps/front/src/components/admin/catalog/admin-category-manager.tsx`
- Impact: добавлять поведение в admin catalog screens хрупко: async guards, selected entity state, form state и panel rendering связаны в одном компоненте.
- Fix approach: разделять stateful containers на focused hooks/controllers и presentational panels; mapping и payload builders держать в helper modules с unit tests.

**Frontend API contracts вручную дублируют backend DTO:**
- Issue: `apps/front/src/lib/api/admin-catalog.ts` содержит крупное handwritten-зеркало backend DTO/endpoints, тогда как backend contracts живут в C# DTO/controllers, а Swagger включен только в development runtime.
- Files: `apps/front/src/lib/api/admin-catalog.ts`, `apps/api/Modules/Catalog/DTOs`, `apps/api/Modules/Catalog/Controllers`, `apps/api/Program.cs`
- Impact: при изменениях catalog admin легко получить drift между backend и frontend, особенно в product, image, attribute и homepage workflows.
- Fix approach: сохранять OpenAPI artifact или добавить contract tests, которые сверяют критичные DTO fields и endpoint shapes между backend и frontend API clients.

## Known Bugs

**Resolved in Phase 1 - frontend API transport invalid responses normalize to controlled errors:**
- Status: fixed by `.planning/phases/01-release-safety-baseline/01-03-SUMMARY.md`.
- Files: `apps/front/src/lib/api/http.ts`, `apps/front/src/lib/api/errors.ts`, `apps/front/src/lib/api/http.test.ts`.
- Current behavior: non-JSON, empty and malformed non-204 responses throw `ApiClientError` with `transport.invalid_response`; diagnostics are retained without exposing raw upstream body or parser messages to users.

**Resolved in Phase 1 - production SEO origin no longer silently falls back to localhost:**
- Status: fixed by `.planning/phases/01-release-safety-baseline/01-02-SUMMARY.md`.
- Files: `apps/front/src/lib/seo/site.ts`, `apps/front/src/lib/seo/site.test.ts`, `apps/front/.env.example`.
- Current behavior: production build/startup paths reject missing, invalid and localhost public site origins with a clear `LINECOM_PUBLIC_SITE_ORIGIN` error; development/test fallback remains available.

## Security Considerations

**Resolved in Phase 1 - auth login/register now have endpoint throttling:**
- Status: fixed by `.planning/phases/01-release-safety-baseline/01-01-SUMMARY.md`.
- Files: `apps/api/Modules/Auth/AuthRateLimiting.cs`, `apps/api/Modules/Auth/Controllers/AuthController.cs`, `tests/LineCom.Api.Tests/Modules/Auth`.
- Current mitigation: `/api/auth/login` and `/api/auth/register` use ASP.NET Core endpoint-specific fixed-window rate limiting keyed by remote IP plus endpoint path, with tested 429 `auth.rate_limited` behavior. Account lockout/captcha remain outside the Phase 1 scope.

**Resolved in Phase 2 - public static storage boundary is directory-limited:**
- Status: fixed by `.planning/phases/02-storage-access-and-diagnostics/02-01-SUMMARY.md`.
- Files: `apps/api/Infrastructure/Hosting/LocalStorageStaticFilesExtensions.cs`, `apps/api/Infrastructure/Hosting/LocalStoragePathPolicy.cs`, `tests/LineCom.Api.Tests/Infrastructure/Hosting/LocalStorageStaticFilesTests.cs`.
- Current mitigation: only `/storage/products` and `/storage/brands` are served as anonymous static files, preserving current catalog image URLs while excluding future non-image purposes from static mapping.
- Remaining risk: future import/export/temp downloads must use explicit authorization before any new public path is introduced.

## Performance Bottlenecks

**Sitemap generation последовательно загружает все product pages на каждый request:**
- Problem: `sitemap()` загружает первую страницу товаров, затем идет по `totalPages` с `pageSize` 60.
- Files: `apps/front/src/app/sitemap.ts`, `apps/front/src/lib/api/catalog.ts`, `vault/Человекочитаемое/SEO GEO Public Catalog.md`
- Cause: sitemap строится из public API pagination во время route execution без segmented sitemap files или route-level caching.
- Improvement path: кешировать/revalidate sitemap data, генерировать segmented sitemaps или добавить backend sitemap feed, оптимизированный для crawlers.

**Admin/search queries используют широкие `ILIKE '%term%'` и OFFSET pagination:**
- Problem: catalog и request admin search ищут substring по нескольким text columns и используют OFFSET pagination.
- Files: `apps/api/Modules/Catalog/Repositories/AdminCatalogProductSql.cs`, `apps/api/Modules/Catalog/Repositories/AdminCatalogBrandSql.cs`, `apps/api/Modules/Catalog/Repositories/AdminCatalogCategorySql.cs`, `apps/api/Modules/Requests/Repositories/AdminRequestSql.cs`
- Cause: только часть searchable surface имеет trigram indexes; request contact/organization search и joined category/brand search не имеют matching expression indexes.
- Improvement path: добавить targeted trigram/expression indexes или перевести high-volume admin search на PostgreSQL full-text/trigram queries с keyset pagination там, где это полезно.

## Fragile Areas

**Catalog import reset destructive и зависит от UI flags:**
- Files: `apps/catalog-import.core/Database/CatalogImportDatabase.cs`, `apps/catalog-import.winforms/MainForm.cs`
- Why fragile: reset удаляет categories, products, attributes, product images и catalog-import stored file rows; защита проверяет только request item references и explicit `AllowResetInCurrentEnvironment`.
- Safe modification: держать reset выключенным по умолчанию, показывать target database в подтверждении и добавить integration test на reset refusal при product references и homepage references.
- Test coverage: SQL и planner tests есть, но storage-side cleanup и production-safety behavior требуют отдельного покрытия.

**Storage и DB writes не являются одной atomic resource:**
- Files: `apps/api/Modules/Catalog/Services/AdminCatalogImageService.cs`, `apps/api/Modules/Catalog/Services/AdminCatalogBrandService.cs`, `apps/api/Infrastructure/Storage/LocalStoredFileWriter.cs`, `apps/catalog-import.core/Database/CatalogImportDatabase.cs`
- Why fragile: product image/brand logo services компенсируют failed DB writes удалением физических файлов, а catalog import копирует файлы до DB mutation и имеет другой cleanup behavior.
- Safe modification: централизовать file-write transaction patterns и новые file features вести через общий staging/commit/cleanup model.
- Test coverage: admin image/brand cleanup покрыт; catalog import file promotion и orphan cleanup покрыты слабее.

**SEO/GEO зависит от configuration и API availability сразу в нескольких слоях:**
- Files: `apps/front/src/lib/seo/site.ts`, `apps/front/src/app/sitemap.ts`, `apps/front/src/app/catalog/[categorySlug]/page.tsx`, `apps/front/src/app/products/[slug]/page.tsx`, `vault/Человекочитаемое/Сквозные требования.md`
- Why fragile: metadata, canonical paths, sitemap и noindex fallback распределены между frontend helpers, route files и public catalog API fields.
- Safe modification: при изменениях catalog routes, slugs, metadata или public API canonical fields обновлять route tests и sitemap tests вместе.
- Test coverage: SEO helper и route-level tests есть; production SEO с real API data зафиксирован как manual follow-up в `vault/Человекочитаемое/SEO GEO Public Catalog handoff.md`.

## Scaling Limits

**Local FileStorage ограничен одним сервером:**
- Current capacity: ограничен `/var/lib/linecom/storage` на production host из `vault/Человекочитаемое/Production deployment line-com.ru.md`.
- Limit: multi-server deployment, blue/green releases и disaster recovery требуют явных backup/sync процедур; target architecture намеренно не использует S3/MinIO.
- Scaling path: оставить local storage как целевой подход, но добавить backup, restore, integrity scan и deploy-time storage path checks.

**Sitemap product enumeration растет линейно с числом товаров:**
- Current capacity: product sitemap page size ограничен 60 через public catalog API.
- Limit: каждые дополнительные 60 опубликованных товаров добавляют еще один API call во время sitemap generation.
- Scaling path: ввести segmented sitemap generation или precomputed sitemap snapshots до существенного роста каталога.

## Dependencies at Risk

**Not detected:**
- Risk: при статическом картировании не найден dependency с подтвержденной repository-local vulnerability.
- Impact: `dotnet test --no-restore` показал NuGet vulnerability-data warnings из-за недоступного `https://api.nuget.org/v3/index.json`, поэтому online vulnerability audit в этом mapping не выполнялся.
- Migration plan: в network-enabled verification step запустить `dotnet list package --vulnerable`, `npm audit` или эквивалентный dependency audit.

## Missing Critical Features

**SEO landing pages описаны в модели, но не реализованы backend modules:**
- Problem: source-of-truth documents задают normalized landing page entities и admin/public API expectations, но migrations для `landing_pages` и реализация `Modules/Seo` отсутствуют.
- Files: `vault/Человекочитаемое/Структура данных релиза.md`, `vault/Человекочитаемое/Архитектура backend и БД.md`, `apps/dbmigrator/Migrations`, `apps/api/Modules`
- Blocks: filter-based SEO/GEO landing pages и admin workflow для них.

**Product comparison есть в product model, но не реализован:**
- Problem: comparison описан как user-facing catalog capability, а текущий frontend/backend покрывает catalog browsing, request draft, account, admin catalog и homepage management.
- Files: `vault/Человекочитаемое/Продуктовая модель.md`, `vault/Человекочитаемое/Структура данных релиза.md`, `apps/front/src/app`, `apps/api/Modules/Catalog`
- Blocks: compare workflow и compare-oriented structured attribute UX.

**Excel import/export release contour не завершен в web app:**
- Problem: source-of-truth описывает `ImportJob`, mappings, row errors и export files, но текущая реализация - WinForms/catalog-import core tool плюс SQL, а не admin web API/UI import-export module.
- Files: `vault/Человекочитаемое/Структура данных релиза.md`, `vault/Человекочитаемое/Архитектура backend и БД.md`, `apps/catalog-import.core`, `apps/catalog-import.winforms`, `apps/api/Modules`
- Blocks: browser-based admin import/export workflow, mapping persistence, import row error review и controlled file lifecycle для import/export artifacts.

## Test Coverage Gaps

**Resolved in Phase 1 - frontend API transport error handling:**
- Status: covered by `.planning/phases/01-release-safety-baseline/01-03-SUMMARY.md`.
- Files: `apps/front/src/lib/api/http.ts`, `apps/front/src/lib/api/errors.test.ts`, `apps/front/src/lib/api/http.test.ts`, `apps/front/src/lib/api/admin-catalog.test.ts`.
- Current coverage: non-JSON response bodies, empty non-204 responses, malformed JSON, valid backend API errors, 204 responses and multipart invalid responses.

**Storage cleanup jobs and import DB/disk consistency:**
- What's not tested: retention-based cleanup execution, import staging/promotion, reset cleanup of physical files and recovery from storage metadata conflicts.
- Files: `apps/catalog-import.core/Database/CatalogImportDatabase.cs`, `apps/api/Infrastructure/Storage/LocalStoredFileWriter.cs`, `apps/dbmigrator/Migrations/002_catalog_foundation.sql`
- Risk: imports can still leave persistent storage drift even though Phase 2 now detects drift through read-only diagnostics.
- Priority: High

**Resolved in Phase 2 - storage access boundary and diagnostics coverage:**
- Status: covered by `.planning/phases/02-storage-access-and-diagnostics/02-03-SUMMARY.md`.
- Files: `tests/LineCom.Api.Tests/Infrastructure/Hosting/LocalStorageStaticFilesTests.cs`, `tests/LineCom.Api.Tests/Modules/Catalog/StorageDiagnosticsServiceTests.cs`, `tests/LineCom.Api.Tests/Modules/Catalog/StorageDiagnosticsEndpointTests.cs`, `tests/LineCom.Api.Tests/Modules/Catalog/StorageDiagnosticsSqlTests.cs`.
- Current coverage: public image prefixes are served, internal prefixes are not served, diagnostics are staff-only/read-only, bounded, relative-path-only, and SQL has no mutation commands.

**Resolved in Phase 1 - rate limiting для auth endpoints:**
- Status: covered by `.planning/phases/01-release-safety-baseline/01-01-SUMMARY.md`.
- Files: `apps/api/Modules/Auth/AuthRateLimiting.cs`, `apps/api/Modules/Auth/Controllers/AuthController.cs`, `tests/LineCom.Api.Tests/Modules/Auth/AuthLoginEndpointTests.cs`, `tests/LineCom.Api.Tests/Modules/Auth/AuthRegisterEndpointTests.cs`.
- Current coverage: repeated login/register attempts from the same client reach tested 429 behavior with JSON code `auth.rate_limited`.

**Catalog import DB/disk atomicity:**
- What's not tested: DB failure after image copy, reset cleanup of physical files и recovery from storage metadata conflicts.
- Files: `apps/catalog-import.core/Database/CatalogImportDatabase.cs`, `tests/LineCom.Api.Tests/CatalogImport`
- Risk: imports могут оставлять persistent storage drift, который позже проявится в public catalog как missing или stale images.
- Priority: High

---

*Concerns audit: 2026-05-14*
