---
phase: 04-public-seo-geo-reliability
plan: "03"
subsystem: seo-regression-gate
tags: [seo, robots, sitemap, metadata, nextjs, vitest, production-build]

requires:
  - phase: 04-public-seo-geo-reliability
    provides: 04-01 canonical route/API coverage and 04-02 bounded sitemap behavior
provides:
  - Focused SEO/GEO regression gate evidence
  - Strengthened robots route assertion for normalized host and absolute sitemap URL
  - Frontend production build evidence with a non-localhost public origin
affects: [public-seo-geo-reliability, release-verification, frontend-seo]

tech-stack:
  added: []
  patterns:
    - focused Vitest command for SEO helpers, robots, sitemap and route metadata
    - production build verification with explicit non-localhost `LINECOM_PUBLIC_SITE_ORIGIN`

key-files:
  created: []
  modified:
    - apps/front/src/app/robots.test.ts

key-decisions:
  - "Robots remains a single-sitemap route; no segmented sitemap URLs are advertised before segmented sitemap files exist."
  - "The Phase 4 regression gate is focused tests plus build evidence, not a broad frontend/backend contract framework."

patterns-established:
  - "Future SEO/GEO-sensitive changes should update focused tests around public routes, metadata helpers, robots, sitemap, and public API SEO fields."

requirements-completed: [SEO-01, SEO-02, SEO-03]

duration: 18min
completed: 2026-05-14
---

# Phase 04 Plan 03: Robots/Sitemap/Metadata Regression Tests Summary

**SEO/GEO regression evidence now covers helpers, robots, bounded sitemap, representative route metadata and a production frontend build with a safe origin.**

## Performance

- **Duration:** 18 min
- **Started:** 2026-05-14T19:34:00Z
- **Completed:** 2026-05-14T19:52:00Z
- **Tasks:** 4
- **Files modified:** 1

## Accomplishments

- Strengthened `robots.test.ts` so the origin input includes path/query/fragment and the expected host/sitemap prove normalization.
- Ran the focused SEO/GEO Vitest surface for SEO helpers, metadata helpers, sitemap helper/route, robots route, and route-level category/product metadata tests.
- Ran frontend production build with `LINECOM_PUBLIC_SITE_ORIGIN=https://linecom.example.ru`; Next.js built `robots.txt` and `sitemap.xml` successfully.
- Recorded the SEO/GEO-sensitive surface for future route/API maintainers.

## Task Commits

1. **Task 1: strengthen robots route assertions** - `c7a2d28` (`test(04-03)`)

**Plan metadata:** this summary commit.

## Files Created/Modified

- `apps/front/src/app/robots.test.ts` - verifies normalized host and a single absolute `/sitemap.xml` URL from a non-localhost public origin containing path/query/fragment.

## Decisions Made

- Kept `robots.ts` unchanged because the existing implementation already satisfied D-12 through D-15.
- Used explicit route test paths for bracketed App Router folders after the pattern command, matching the plan's Windows guidance.

## SEO/GEO-Sensitive Surface

Future changes must update the relevant focused tests when they touch:

- public routes: `/catalog`, `/catalog/{categorySlug}`, `/products/{slug}`;
- route metadata and root `metadataBase`;
- `apps/front/src/app/robots.ts`;
- `apps/front/src/app/sitemap.ts`;
- `apps/front/src/lib/seo/*`;
- public catalog API SEO fields: `seo.canonicalPath`, `seo.title`, `seo.description`.

Broad frontend/backend contract drift checks remain deferred to Phase 5. Segmented sitemap implementation remains deferred until catalog growth exceeds the Phase 4 single-sitemap release limit.

## Deviations from Plan

None - plan executed exactly as written.

**Total deviations:** 0 auto-fixed.
**Impact on plan:** No scope changes.

## Issues Encountered

None.

## Verification

- `npm.cmd --prefix apps/front test -- seo metadata sitemap robots`: passed, 5 files, 22 tests.
- `npm.cmd --prefix apps/front test -- "src/app/catalog/[categorySlug]/page.test.tsx" "src/app/products/[slug]/page.test.tsx"`: passed, 2 files, 2 tests.
- `$env:LINECOM_PUBLIC_SITE_ORIGIN='https://linecom.example.ru'; npm.cmd --prefix apps/front run build`: passed. Next.js 16.2.4 compiled successfully and generated `robots.txt` and `sitemap.xml` metadata routes.
- `rg -n "sitemap.xml|generateSitemaps|admin|account|auth" apps/front/src/app/robots.ts apps/front/src/app/robots.test.ts`: confirmed one `/sitemap.xml` reference and required internal disallow entries.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Phase 4 implementation plans are complete. The phase is ready for GSD phase verification and roadmap/state completion updates.

---
*Phase: 04-public-seo-geo-reliability*
*Completed: 2026-05-14*
