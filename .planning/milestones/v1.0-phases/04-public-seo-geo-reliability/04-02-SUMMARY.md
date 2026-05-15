---
phase: 04-public-seo-geo-reliability
plan: "02"
subsystem: seo-sitemap
tags: [seo, sitemap, nextjs, vitest, public-catalog]

requires:
  - phase: 01-release-safety-baseline
    provides: production-safe public site origin for absolute sitemap URLs
provides:
  - Bounded single `/sitemap.xml` product enumeration
  - Sitemap regression tests for API page limit and product URL truncation
  - Source-of-truth documentation for Phase 4 sitemap release limits
affects: [public-seo-geo-reliability, crawler-surface, frontend-seo]

tech-stack:
  added: []
  patterns:
    - visible route-local release limits for sitemap product enumeration
    - valid truncated sitemap behavior when catalog size exceeds Phase 4 bounds

key-files:
  created: []
  modified:
    - apps/front/src/app/sitemap.ts
    - apps/front/src/app/sitemap.test.ts
    - vault/Человекочитаемое/SEO GEO Public Catalog.md

key-decisions:
  - "Phase 4 keeps one `/sitemap.xml` and bounds product loading to 10 API pages and 500 product URLs."
  - "Segmented sitemap generation via Next.js `generateSitemaps` remains deferred until the catalog exceeds the Phase 4 release limit."

patterns-established:
  - "Sitemap route tests prove both API call bounds and output URL truncation."

requirements-completed: [SEO-02, SEO-03]

duration: 16min
completed: 2026-05-14
---

# Phase 04 Plan 02: Sitemap Scaling Strategy Summary

**Single public sitemap generation now has explicit page and product URL release limits with truncation tests and source-of-truth documentation.**

## Performance

- **Duration:** 16 min
- **Started:** 2026-05-14T19:18:00Z
- **Completed:** 2026-05-14T19:34:00Z
- **Tasks:** 4
- **Files modified:** 3

## Accomplishments

- Added `SITEMAP_MAX_PRODUCT_PAGES = 10` and `SITEMAP_MAX_PRODUCT_URLS = 500` next to the sitemap route.
- Updated `loadSitemapProducts` so product API enumeration stops at the page limit or URL limit.
- Preserved partial fallback behavior through the existing `Promise.allSettled` route structure.
- Added route-level tests proving max page count, max product URL truncation and existing fallback/static behavior.
- Documented the bounded single-sitemap release limit and deferred segmented sitemap path in `vault/Человекочитаемое/SEO GEO Public Catalog.md`.

## Task Commits

1. **Tasks 1-4: sitemap limits, truncation tests and documentation** - `551e2f8` (`feat(04-02)`)

**Plan metadata:** this summary commit.

## Files Created/Modified

- `apps/front/src/app/sitemap.ts` - added release limit constants and bounded product collection.
- `apps/front/src/app/sitemap.test.ts` - added route-level page limit and product URL truncation tests.
- `vault/Человекочитаемое/SEO GEO Public Catalog.md` - documented Phase 4 single sitemap limit and future `generateSitemaps` path.

## Decisions Made

- Kept limits as deterministic route-local constants rather than adding environment/config surface.
- Kept `robots.ts` unchanged because Phase 4 still exposes a single bounded `/sitemap.xml`.

## Deviations from Plan

None - plan executed exactly as written.

**Total deviations:** 0 auto-fixed.
**Impact on plan:** No scope changes.

## Issues Encountered

None.

## Verification

- `npm.cmd --prefix apps/front test -- src/app/sitemap.test.ts src/lib/seo/sitemap.test.ts`: passed, 2 files, 8 tests.
- `rg -n "SITEMAP_MAX_PRODUCT_PAGES|SITEMAP_MAX_PRODUCT_URLS|generateSitemaps|release limit|sitemap.xml" apps/front/src/app/sitemap.ts "vault/Человекочитаемое/SEO GEO Public Catalog.md" apps/front/src/app/robots.ts`: confirmed limits and docs; `robots.ts` still references one `absoluteSiteUrl("/sitemap.xml")`.
- Source inspection confirmed `loadSitemapProducts` applies both page and URL bounds and keeps static/category fallback behavior through `Promise.allSettled`.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Ready for Plan 04-03. Sitemap generation is bounded and documented; the remaining work is the focused SEO/GEO regression gate and frontend production build evidence.

---
*Phase: 04-public-seo-geo-reliability*
*Completed: 2026-05-14*
