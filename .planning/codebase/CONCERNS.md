# Codebase Concerns

**Analysis Date:** 2026-05-14

## Tech Debt

**Local FileStorage смонтирован как публичный static root:**
- Issue: `UseLocalStorageStaticFiles` открывает весь настроенный `Storage:RootPath` через `/storage`, хотя релизная модель `stored_files.purpose` уже допускает не только публичные изображения, но и `import_source`, `export_result`, `temp`.
- Files: `apps/api/Infrastructure/Hosting/LocalStorageStaticFilesExtensions.cs`, `apps/dbmigrator/Migrations/002_catalog_foundation.sql`, `vault/Человекочитаемое/Архитектура backend и БД.md`
- Impact: будущие файлы импорта, экспорта и временные артефакты могут стать публично доступными, если окажутся в том же storage root; политика доступа сейчас задается путем, а не назначением файла и статусом.
- Fix approach: отдавать статикой только публичные префиксы изображений (`products/`, `brands/`) или перевести чтение storage в controller, который проверяет `stored_files.purpose`, `status` и права доступа.

**Жизненный цикл StoredFile описан в БД, но нет cleanup/diagnostic контура:**
- Issue: `stored_files.status` поддерживает `active`, `deleted`, `orphaned`, а документы требуют диагностику расхождения БД и диска; реализация только помечает часть файлов как deleted и выполняет best-effort физическую очистку при ошибках загрузки.
- Files: `apps/dbmigrator/Migrations/002_catalog_foundation.sql`, `apps/api/Modules/Catalog/Repositories/AdminCatalogImageSql.cs`, `apps/api/Modules/Catalog/Repositories/AdminCatalogBrandSql.cs`, `apps/api/Infrastructure/Storage/LocalStoredFileWriter.cs`, `vault/Человекочитаемое/Архитектура backend и БД.md`
- Impact: удаленные и orphaned-файлы могут накапливаться на диске; активная запись в БД без файла на диске не выявляется приложением.
- Fix approach: добавить service/command обслуживания storage, который строит отчет по missing files, untracked files, stale `deleted`/`orphaned` rows и выполняет физическое удаление только после retention-срока.

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

**Frontend HTTP wrapper выбрасывает raw JSON parse errors на non-JSON responses:**
- Symptoms: если backend/proxy возвращает HTML, plain text или битый JSON при non-204 status, `JSON.parse(text)` падает до нормализации ответа в `ApiClientError`.
- Files: `apps/front/src/lib/api/http.ts`
- Trigger: любой `/api/...` или multipart request, получивший non-JSON error content от nginx, Next rewrite, ASP.NET startup failure или upstream 502/504 page.
- Workaround: caller-level `normalizeApiError` обрабатывает `ApiClientError`, но не все raw `SyntaxError` с ожидаемым backend-сообщением.

**Production SEO origin молча падает на localhost при отсутствующем env:**
- Symptoms: canonical URLs, `metadataBase`, robots host и sitemap URLs используют `http://127.0.0.1:3000`, если `LINECOM_PUBLIC_SITE_ORIGIN` отсутствует или невалиден.
- Files: `apps/front/src/lib/seo/site.ts`, `apps/front/src/app/robots.ts`, `apps/front/src/app/sitemap.ts`, `vault/Человекочитаемое/SEO GEO Public Catalog.md`
- Trigger: frontend service в production запущен без `LINECOM_PUBLIC_SITE_ORIGIN=https://line-com.ru`.
- Workaround: production docs требуют `/etc/linecom/front.env`; startup/build checks должны падать в production, если public origin не задан.

## Security Considerations

**Auth endpoints не имеют rate limiting или lockout:**
- Risk: `/api/auth/login` и `/api/auth/register` публичные и используют PBKDF2, но ASP.NET rate limiter, account lockout, captcha или failed-attempt tracking не зарегистрированы.
- Files: `apps/api/Modules/Auth/Controllers/AuthController.cs`, `apps/api/Modules/Auth/Services/Pbkdf2PasswordHasher.cs`, `apps/api/Program.cs`
- Current mitigation: пароли хэшируются через PBKDF2-SHA256 с 210,000 iterations; auth cookies HttpOnly и Secure в production.
- Recommendations: добавить IP/account based throttling для login/register и тесты на 429 behavior без ослабления password hashing.

**Public static storage может обойти authorization для будущих non-image files:**
- Risk: import source files, export results, temp files или internal reports под `Storage:RootPath` станут доступными через `/storage`.
- Files: `apps/api/Infrastructure/Hosting/LocalStorageStaticFilesExtensions.cs`, `apps/dbmigrator/Migrations/002_catalog_foundation.sql`
- Current mitigation: текущие image writers ограничивают admin uploads JPEG/PNG/WebP, а catalog import пишет PNG product images.
- Recommendations: ограничить static serving публичными image directories и требовать controller authorization для non-public file purposes до реализации import/export uploads.

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

**Frontend API transport error handling:**
- What's not tested: non-JSON response bodies, empty non-204 error responses и malformed JSON в `apiJson`/`apiForm`.
- Files: `apps/front/src/lib/api/http.ts`, `apps/front/src/lib/api/errors.test.ts`
- Risk: infrastructure failures показываются как raw parse errors вместо стандартной API error model.
- Priority: Medium

**Storage serving policy и cleanup jobs:**
- What's not tested: public/private storage access boundaries, stale `deleted`/`orphaned` cleanup, DB row без disk file и disk file без DB row.
- Files: `apps/api/Infrastructure/Hosting/LocalStorageStaticFilesExtensions.cs`, `apps/api/Infrastructure/Storage/LocalStoredFileWriter.cs`, `apps/dbmigrator/Migrations/002_catalog_foundation.sql`
- Risk: private artifacts могут быть раскрыты позже, а storage drift останется невидимым.
- Priority: High

**Rate limiting для auth endpoints:**
- What's not tested: repeated login/register attempts и throttled responses.
- Files: `apps/api/Modules/Auth/Controllers/AuthController.cs`, `tests/LineCom.Api.Tests/Modules/Auth`
- Risk: brute-force и signup abuse доходят до password verification и account creation без application-level throttling.
- Priority: High

**Catalog import DB/disk atomicity:**
- What's not tested: DB failure after image copy, reset cleanup of physical files и recovery from storage metadata conflicts.
- Files: `apps/catalog-import.core/Database/CatalogImportDatabase.cs`, `tests/LineCom.Api.Tests/CatalogImport`
- Risk: imports могут оставлять persistent storage drift, который позже проявится в public catalog как missing или stale images.
- Priority: High

---

*Concerns audit: 2026-05-14*
