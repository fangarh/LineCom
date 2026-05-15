---
phase: 04-public-seo-geo-reliability
plan: "01"
subsystem: seo-api-testing
tags: [seo, canonical, nextjs, aspnetcore, public-catalog, vitest, xunit]

requires:
  - phase: 01-release-safety-baseline
    provides: production-safe public origin validation for frontend SEO metadata
provides:
  - Product detail API canonical path aligned with `/products/{slug}`
  - Route-level category and product metadata tests for API-backed canonical fields
  - Public Catalog API documentation aligned with the public product route
affects: [public-seo-geo-reliability, public-catalog-api, frontend-route-metadata]

tech-stack:
  added: []
  patterns:
    - colocated App Router metadata tests that mock public catalog API clients
    - backend public DTO canonical assertions in builder, endpoint and Dapper query tests

key-files:
  created:
    - apps/front/src/app/catalog/[categorySlug]/page.test.tsx
    - apps/front/src/app/products/[slug]/page.test.tsx
  modified:
    - apps/api/Modules/Catalog/Queries/PublicProductDetailResponseBuilder.cs
    - tests/LineCom.Api.Tests/Modules/Catalog/PublicProductDetailResponseBuilderTests.cs
    - tests/LineCom.Api.Tests/Modules/Catalog/PublicProductsEndpointTests.cs
    - tests/LineCom.Api.Tests/Modules/Catalog/DapperPublicProductQueryDatabaseTests.cs
    - vault/Человекочитаемое/Public Catalog API.md

key-decisions:
  - "Product canonical path is `/products/{slug}` while the API endpoint remains `/api/public/catalog/products/{slug}`."
  - "Route metadata tests assert that category/product pages consume API `seo.canonicalPath` instead of rebuilding canonicals in the frontend."

patterns-established:
  - "Public entity metadata route tests call `generateMetadata` directly with resolved App Router params."

requirements-completed: [SEO-01, SEO-03]

duration: 28min
completed: 2026-05-14
---

# Phase 04 Plan 01: Canonical Metadata And Route Verification Summary

**Product canonical metadata now resolves to `/products/{slug}` from the API and is protected by backend and route-level frontend tests.**

## Performance

- **Duration:** 28 min
- **Started:** 2026-05-14T18:50:13Z
- **Completed:** 2026-05-14T19:18:00Z
- **Tasks:** 4
- **Files modified:** 7

## Accomplishments

- Corrected `PublicProductDetailResponseBuilder` so `seo.canonicalPath` matches the real public product route.
- Updated builder, endpoint and Dapper public product tests to fail on the obsolete `/catalog/products/{slug}` canonical.
- Added route-level metadata tests for `/catalog/{categorySlug}` and `/products/{slug}` that verify title, description, canonical and indexable robots metadata from public API SEO fields.
- Updated `vault/Человекочитаемое/Public Catalog API.md` so source-of-truth docs no longer document the obsolete product canonical path.

## Task Commits

1. **Tasks 1-4: canonical API fix, backend tests, route metadata tests and docs** - `8d0f825` (`fix(04-01)`)

**Plan metadata:** this summary commit.

## Files Created/Modified

- `apps/api/Modules/Catalog/Queries/PublicProductDetailResponseBuilder.cs` - product SEO canonical path changed to `/products/{slug}`.
- `tests/LineCom.Api.Tests/Modules/Catalog/PublicProductDetailResponseBuilderTests.cs` - builder canonical assertion updated.
- `tests/LineCom.Api.Tests/Modules/Catalog/PublicProductsEndpointTests.cs` - serialized product detail canonical assertion updated.
- `tests/LineCom.Api.Tests/Modules/Catalog/DapperPublicProductQueryDatabaseTests.cs` - live-query canonical expectation updated.
- `apps/front/src/app/catalog/[categorySlug]/page.test.tsx` - category `generateMetadata` route test added.
- `apps/front/src/app/products/[slug]/page.test.tsx` - product `generateMetadata` route test added.
- `vault/Человекочитаемое/Public Catalog API.md` - product canonical example and SEO/GEO rule updated.

## Decisions Made

- Kept public API endpoint routes unchanged; only public site canonical metadata changed.
- Preserved existing noindex fallback behavior because the plan required no fallback changes unless tests exposed a regression.

## Deviations from Plan

None - plan executed exactly as written.

**Total deviations:** 0 auto-fixed.
**Impact on plan:** No scope changes.

## Issues Encountered

- Backend test command passed, but restore/build emitted `NU1900` vulnerability-data warnings because `https://api.nuget.org/v3/index.json` was unavailable in the environment. This did not block the focused test run.

## Verification

- `dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "FullyQualifiedName~PublicProductDetailResponseBuilderTests|FullyQualifiedName~PublicProductsEndpointTests|FullyQualifiedName~DapperPublicProductQueryDatabaseTests"`: passed, 19 tests, 0 failed, 0 skipped; `NU1900` warnings recorded above.
- `npm.cmd --prefix apps/front test -- "src/app/catalog/[categorySlug]/page.test.tsx" "src/app/products/[slug]/page.test.tsx"`: passed, 2 files, 2 tests.
- `rg -n "/catalog/products/" apps/api/Modules/Catalog tests/LineCom.Api.Tests/Modules/Catalog "vault/Человекочитаемое/Public Catalog API.md"`: no obsolete product canonical assertions/examples remain; remaining matches are API/admin endpoint paths.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Ready for Plan 04-02. The public API product canonical mismatch found during research is resolved and route-level metadata coverage exists for representative category/product pages.

---
*Phase: 04-public-seo-geo-reliability*
*Completed: 2026-05-14*
