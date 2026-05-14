# Phase 4: Public SEO/GEO Reliability - Research

**Researched:** 2026-05-14
**Status:** Complete

## RESEARCH COMPLETE

## Scope

Phase 4 covers `SEO-01`, `SEO-02`, and `SEO-03`: public catalog, product, robots and sitemap routes must generate production-safe canonical URLs from validated site origin, sitemap generation must be bounded for catalog growth, and SEO/GEO behavior must have regression tests.

In scope:
- Existing public routes: `/catalog`, `/catalog/{categorySlug}`, `/products/{slug}`.
- Root metadata base and SEO helpers under `apps/front/src/lib/seo`.
- `apps/front/src/app/robots.ts` and `apps/front/src/app/sitemap.ts`.
- Public catalog API SEO fields: `seo.title`, `seo.description`, `seo.canonicalPath`.
- Focused frontend route/helper tests and narrowly targeted backend public catalog tests where canonical fields originate.
- Source-of-truth doc updates required to document bounded sitemap behavior and resolve public product canonical path expectations.

Out of scope:
- SEO/GEO landing pages.
- Product comparison.
- Broad frontend/backend contract drift framework.
- Admin maintainability work.
- Production deployment and final release audit.
- Segmented sitemap implementation in Phase 4.

## Current Implementation

Frontend:
- `apps/front/src/lib/seo/site.ts` validates `LINECOM_PUBLIC_SITE_ORIGIN` in production and exposes `metadataBase`/absolute URL helpers.
- `apps/front/src/lib/seo/metadata.ts` sets relative canonical paths through `alternates.canonical`; root `metadataBase` resolves them to absolute URLs in rendered metadata.
- `apps/front/src/app/catalog/[categorySlug]/page.tsx` and `apps/front/src/app/products/[slug]/page.tsx` use API-backed `seo.canonicalPath` in `generateMetadata`.
- `apps/front/src/app/robots.ts` returns `MetadataRoute.Robots` with `allow: "/"`, disallow for internal routes, `sitemap: absoluteSiteUrl("/sitemap.xml")`, and `host: getPublicSiteOrigin()`.
- `apps/front/src/app/sitemap.ts` loads category tree plus all product pages by walking `firstPage.totalPages` with `pageSize = 60`; this is currently linear and unbounded by an explicit release limit.

Backend/API:
- `PublicSeoDto` exposes `title`, `description`, and `canonicalPath`.
- Category canonical path is built as `/catalog/{slug}` in `PublicCategoryDetailBuilder`.
- Product canonical path is currently built as `/catalog/products/{slug}` in `PublicProductDetailResponseBuilder`.
- Existing backend tests currently assert `/catalog/products/{slug}` for product detail canonical path.

Docs:
- `vault/Человекочитаемое/SEO GEO Public Catalog.md` defines indexable product pages as `/products/{slug}`.
- `apps/front/src/lib/routes.ts`, sitemap tests, and frontend route files also use `/products/{slug}`.
- `vault/Человекочитаемое/Public Catalog API.md` still documents product `canonicalPath` as `/catalog/products/{slug}`, matching current backend tests but conflicting with the public route and SEO/GEO route contract.

## Next.js Documentation Findings

Official Next.js v16.2.2 documentation confirms:
- `robots.ts` can dynamically generate `robots.txt` by exporting a function returning `MetadataRoute.Robots`; `sitemap` may be a string or array and `host` is supported.
- `sitemap.ts` exports a function returning `MetadataRoute.Sitemap`.
- Special metadata route handlers such as sitemap files are cached by default unless request-time APIs or dynamic config make them dynamic.
- `metadataBase` lets relative metadata URLs, including `alternates.canonical`, resolve to fully qualified URLs.
- `generateSitemaps` is the supported approach for splitting large sitemap datasets; in Next.js 16 the sitemap `id` parameter is asynchronous and must be awaited.

Planning implication: Phase 4's bounded single sitemap is a valid stabilization step. It should explicitly document that `generateSitemaps` is deferred as the growth path when the single-sitemap release limit is no longer enough.

## Implementation Findings

### Canonical metadata

Representative frontend route metadata tests should target `generateMetadata` directly:
- Mock `@/lib/api/catalog`.
- Call `generateMetadata({ params: Promise.resolve({ categorySlug }) })` or product equivalent.
- Assert `alternates.canonical`, `title`, `description`, and indexable robots.

However, route-level frontend tests alone will not catch the current backend/API product canonical mismatch if the mocks provide expected values. Phase 4 should also update backend public product canonical tests and implementation so public API canonical path aligns with actual route `/products/{slug}`.

Recommended canonical work:
- Add/adjust backend tests in:
  - `tests/LineCom.Api.Tests/Modules/Catalog/PublicProductDetailResponseBuilderTests.cs`
  - `tests/LineCom.Api.Tests/Modules/Catalog/PublicProductsEndpointTests.cs`
  - `tests/LineCom.Api.Tests/Modules/Catalog/DapperPublicProductQueryDatabaseTests.cs` where the live PostgreSQL fixture is available.
- Change `PublicProductDetailResponseBuilder` canonical path to `/products/{slug}`.
- Update `vault/Человекочитаемое/Public Catalog API.md` product detail example/rules to `/products/{slug}` so source-of-truth docs stop conflicting.

### Sitemap bounded behavior

Current `loadSitemapProducts` trusts `firstPage.totalPages`. A malformed or very large total can force many API calls per sitemap generation.

Recommended Phase 4 behavior:
- Keep one `/sitemap.xml`.
- Add named constants near `apps/front/src/app/sitemap.ts`, for example:
  - `SITEMAP_PRODUCT_PAGE_SIZE = 60`
  - `SITEMAP_MAX_PRODUCT_PAGES`
  - `SITEMAP_MAX_PRODUCT_URLS`
- Load at most `SITEMAP_MAX_PRODUCT_PAGES` pages.
- Stop adding product entries once `SITEMAP_MAX_PRODUCT_URLS` is reached, even if fetched pages contain more.
- Keep partial fallback behavior: if product loading fails, sitemap still returns static/category entries where available.
- Add tests proving:
  - `getProducts` is not called beyond max page count.
  - product URLs are truncated to max product URL count.
  - sitemap still includes static entries and valid URLs.
- Document the release limit in `vault/Человекочитаемое/SEO GEO Public Catalog.md`.

### Robots

Current `robots.ts` already matches the selected strategy:
- single absolute `/sitemap.xml`;
- normalized `host`;
- internal route disallow list.

Plan should be tests-first:
- Strengthen/keep `apps/front/src/app/robots.test.ts`.
- Change production code only if tests show a mismatch.
- Do not pre-add segmented sitemap URLs.

### Regression gate

The durable gate should be tests plus planning/docs visibility:
- focused Vitest tests for `site`, `metadata`, `sitemap`, `robots`, category route metadata, product route metadata;
- backend public product canonical tests where the API owns the canonical field;
- frontend build to exercise Next metadata/build-time origin behavior.

Do not introduce a broad contract drift framework in Phase 4; that belongs to Phase 5.

## Validation Architecture

Recommended focused frontend commands:
- `npm.cmd --prefix apps/front test -- seo metadata sitemap robots`
- `npm.cmd --prefix apps/front test -- src/app/catalog/[categorySlug]/page.test.tsx src/app/products/[slug]/page.test.tsx src/app/sitemap.test.ts src/app/robots.test.ts src/lib/seo/metadata.test.ts src/lib/seo/site.test.ts src/lib/seo/sitemap.test.ts`
- `npm.cmd --prefix apps/front run build`

Recommended backend focused commands:
- `dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "FullyQualifiedName~PublicProductDetailResponseBuilderTests|FullyQualifiedName~PublicProductsEndpointTests|FullyQualifiedName~DapperPublicProductQueryDatabaseTests"`

Notes:
- Dapper public product database tests may skip live PostgreSQL assertions if `LINECOM_TEST_CONNECTION_STRING` is not configured; endpoint/builder tests should still cover canonical path behavior without live DB.
- Frontend build may be affected by old unrelated dirty changes in public/admin files. Execution should record unrelated failures separately and avoid reverting user changes.

## Planning Recommendation

Use three plans matching ROADMAP:

1. `04-01`: Canonical metadata and route verification.
   - Fix product canonical path mismatch in backend/API docs/tests.
   - Add route-level metadata tests for category and product happy paths.
   - Keep API failure/noindex fallback unchanged unless tests expose a regression.

2. `04-02`: Sitemap scaling strategy.
   - Add bounded single-sitemap release limits.
   - Add truncation/page-limit tests.
   - Document bounded behavior and deferred segmented sitemap path.

3. `04-03`: Robots/sitemap/metadata regression tests and verification gate.
   - Strengthen robots assertions.
   - Run focused SEO/GEO frontend tests and build.
   - Capture Phase 4 verification evidence and ensure SEO/GEO-sensitive surface remains visible in planning/docs.

Because Phase 4 is frontend-heavy but one canonical field originates in backend public catalog builders, keep Plan 04-01 as a small cross-layer plan and keep Plans 04-02/04-03 frontend/documentation focused.
