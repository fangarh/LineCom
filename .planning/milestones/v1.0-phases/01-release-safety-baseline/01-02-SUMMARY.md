---
phase: 01-release-safety-baseline
plan: "02"
subsystem: production-config
tags: [nextjs, aspnetcore, production-guard, seo, storage-config]
requires:
  - phase: 01-release-safety-baseline
    provides: 01-01 Program.cs middleware baseline
provides:
  - Production-only public site origin validation for frontend SEO helpers
  - API production guard for database connection and absolute storage root configuration
  - Environment template documentation for the public site origin
affects: [release-safety-baseline, seo, production-readiness, storage-config]
tech-stack:
  added: []
  patterns:
    - Production-only fail-fast configuration helpers
    - Focused guard unit tests for production vs development behavior
key-files:
  created:
    - apps/api/Infrastructure/Hosting/ProductionConfigurationGuard.cs
    - tests/LineCom.Api.Tests/Infrastructure/Hosting/ProductionConfigurationGuardTests.cs
  modified:
    - apps/front/src/lib/seo/site.ts
    - apps/front/src/lib/seo/site.test.ts
    - apps/front/.env.example
    - apps/api/Program.cs
key-decisions:
  - "Kept `LINECOM_API_ORIGIN ?? \"http://127.0.0.1:8080\"` unchanged per D-09."
  - "Limited API production guard to connection string presence and absolute `Storage:RootPath`; `/storage` access policy remains Phase 2 scope."
patterns-established:
  - "Production config guards should fail only in production and preserve local/test fallbacks."
requirements-completed: [PROD-01, PROD-03]
duration: 4 min
completed: 2026-05-14
---

# Phase 01 Plan 02: Production Origin And Environment Guardrails Summary

**Production startup/build guardrails reject unsafe public origins, blank database config, and local storage-root fallback**

## Performance

- **Duration:** 4 min
- **Started:** 2026-05-14T15:57:45Z
- **Completed:** 2026-05-14T16:01:30Z
- **Tasks:** 3
- **Files modified:** 6

## Accomplishments

- Added production-only validation for `LINECOM_PUBLIC_SITE_ORIGIN` that rejects missing, invalid, localhost, `127.0.0.1`, and `[::1]` values.
- Preserved development/test fallback to `http://127.0.0.1:3000`.
- Added `LINECOM_PUBLIC_SITE_ORIGIN=https://line-com.ru` to `apps/front/.env.example` while keeping the existing `LINECOM_API_ORIGIN` example.
- Added API production configuration guard for `ConnectionStrings:Default` and absolute `Storage:RootPath`, invoked before `AddDatabase`.
- Added focused xUnit/Vitest coverage for frontend and backend guard behavior.

## Task Commits

1. **Task 1: Make public site origin fail fast only in production** - `5756bd4` (fix)
2. **Task 2: Document frontend production origin in env example** - `5756bd4` (fix)
3. **Task 3: Add API production config guard for storage and database defaults** - `5756bd4` (fix)

**Plan metadata:** included in `docs(01-02): complete production guardrails plan`

## Files Created/Modified

- `apps/front/src/lib/seo/site.ts` - Production-safe public origin validation used by metadata, robots, and sitemap helpers.
- `apps/front/src/lib/seo/site.test.ts` - Regression tests for development fallback, production rejection, and allowed preview origins.
- `apps/front/.env.example` - Documents required public site origin while retaining API origin fallback example.
- `apps/api/Program.cs` - Invokes production configuration guard before database registration.
- `apps/api/Infrastructure/Hosting/ProductionConfigurationGuard.cs` - Production-only database/storage root guard.
- `tests/LineCom.Api.Tests/Infrastructure/Hosting/ProductionConfigurationGuardTests.cs` - Backend guard coverage for blank/relative/valid values.

## Decisions Made

- Kept `LINECOM_API_ORIGIN` fallback unchanged and did not edit `apps/front/next.config.ts`.
- Did not change `UseLocalStorageStaticFiles` or any `/storage` public/private serving policy; Phase 2 owns storage access boundaries.

## Deviations from Plan

None - plan executed exactly as written.

**Total deviations:** 0 auto-fixed.
**Impact on plan:** Production guardrails were added without broadening storage policy or frontend API origin scope.

## Issues Encountered

- `dotnet test` emitted existing NuGet vulnerability metadata warnings because `https://api.nuget.org/v3/index.json` was unavailable, but the required backend guard tests passed.

## Verification

- `npm --prefix apps/front test -- src/lib/seo/site.test.ts`: passed, 10/10 tests.
- `dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "FullyQualifiedName~ProductionConfigurationGuard"`: passed, 5/5 tests.
- `npm --prefix apps/front run build` with missing `LINECOM_PUBLIC_SITE_ORIGIN`: failed with the expected `LINECOM_PUBLIC_SITE_ORIGIN must be an absolute non-localhost URL in production, e.g. https://line-com.ru` message.
- `npm --prefix apps/front run build` with `LINECOM_PUBLIC_SITE_ORIGIN=https://line-com.ru`: passed.
- Inspection confirmed `apps/front/next.config.ts` still contains `LINECOM_API_ORIGIN ?? "http://127.0.0.1:8080"`.

## User Setup Required

None - no external service configuration required beyond the documented environment variable.

## Next Phase Readiness

All Phase 1 plans now have summaries. Phase 1 is ready for GSD phase verification against SEC-01, SEC-02, PROD-01, PROD-03, API-01, and API-02.

---
*Phase: 01-release-safety-baseline*
*Completed: 2026-05-14*
