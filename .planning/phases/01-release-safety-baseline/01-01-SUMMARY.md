---
phase: 01-release-safety-baseline
plan: "01"
subsystem: auth
tags: [aspnetcore, rate-limiting, cookie-auth, xunit]
requires: []
provides:
  - Auth-only fixed-window throttling for login and registration
  - Controlled 429 JSON response for repeated credential attempts
  - Production cookie option regression coverage
affects: [release-safety-baseline, security, auth]
tech-stack:
  added: []
  patterns:
    - ASP.NET Core endpoint-specific rate limiting policy
    - WebApplicationFactory endpoint throttling regression tests
key-files:
  created:
    - apps/api/Modules/Auth/AuthRateLimiting.cs
  modified:
    - apps/api/Program.cs
    - apps/api/Modules/Auth/Controllers/AuthController.cs
    - tests/LineCom.Api.Tests/Modules/Auth/AuthLoginEndpointTests.cs
    - tests/LineCom.Api.Tests/Modules/Auth/AuthRegisterEndpointTests.cs
    - tests/LineCom.Api.Tests/Modules/Auth/AuthModuleRegistrationTests.cs
key-decisions:
  - "Followed Phase 1 decisions D-01 through D-05: throttle only POST login/register by IP plus endpoint path."
  - "Kept production cookie behavior at HttpOnly, SameSite=Lax, SecurePolicy=Always and preserved non-production SameAsRequest."
patterns-established:
  - "Auth endpoint throttling lives in LineCom.Api.Modules.Auth.AuthRateLimiting and is applied only through endpoint metadata."
requirements-completed: [SEC-01, SEC-02]
duration: 3 min
completed: 2026-05-14
---

# Phase 01 Plan 01: Auth Rate Limiting And Cookie Production Verification Summary

**Login/register throttling with controlled `auth.rate_limited` responses and production cookie regression coverage**

## Performance

- **Duration:** 3 min
- **Started:** 2026-05-14T15:50:45Z
- **Completed:** 2026-05-14T15:53:16Z
- **Tasks:** 3
- **Files modified:** 6

## Accomplishments

- Added an ASP.NET Core fixed-window auth rate limiter with `PermitLimit = 5`, `Window = TimeSpan.FromMinutes(1)`, and `QueueLimit = 0`.
- Applied `EnableRateLimiting(AuthRateLimiting.PolicyName)` only to `POST /api/auth/register` and `POST /api/auth/login`.
- Added endpoint tests proving the sixth repeated login/register request returns 429 with JSON code `auth.rate_limited`.
- Added cookie option tests proving production uses HttpOnly, SameSite=Lax, and SecurePolicy=Always while non-production keeps SameAsRequest.

## Task Commits

1. **Task 1: Add auth-only fixed-window rate limiting policy** - `a9d0bb2` (feat)
2. **Task 2: Apply throttling only to login and register** - `a9d0bb2` (feat)
3. **Task 3: Add endpoint and cookie production regression tests** - `a9d0bb2` (feat)

**Plan metadata:** included in `docs(01-01): complete auth rate limiting plan`

## Files Created/Modified

- `apps/api/Modules/Auth/AuthRateLimiting.cs` - Auth-only fixed-window limiter policy and 429 JSON rejection body.
- `apps/api/Program.cs` - Registers auth rate limiting and places `UseRateLimiter()` before authentication.
- `apps/api/Modules/Auth/Controllers/AuthController.cs` - Applies endpoint-specific throttling to register and login only.
- `tests/LineCom.Api.Tests/Modules/Auth/AuthLoginEndpointTests.cs` - Covers repeated login throttling.
- `tests/LineCom.Api.Tests/Modules/Auth/AuthRegisterEndpointTests.cs` - Covers repeated registration throttling.
- `tests/LineCom.Api.Tests/Modules/Auth/AuthModuleRegistrationTests.cs` - Covers production and non-production cookie options.

## Decisions Made

None - followed plan as specified.

## Deviations from Plan

None - plan executed exactly as written.

**Total deviations:** 0 auto-fixed.
**Impact on plan:** No scope expansion; no database, storage, request-submit, logout, or `/api/auth/me` throttling changes were introduced.

## Issues Encountered

- `dotnet test` emitted existing NuGet vulnerability metadata warnings because `https://api.nuget.org/v3/index.json` was unavailable, but the required test runs completed successfully.

## Verification

- `dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "FullyQualifiedName~Auth"`: passed, 71/71 tests.
- `dotnet test LineCom.sln --no-restore --filter "FullyQualifiedName~Auth"`: passed, 71/71 tests.
- Manual inspection: `EnableRateLimiting` appears only on `Register` and `Login`; `/api/auth/me`, `/api/auth/logout`, and request-submit endpoints were not changed.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Ready for Wave 1 plan `01-03` and Wave 2 plan `01-02`. `01-02` can build on the updated `Program.cs` without revisiting auth throttling scope.

---
*Phase: 01-release-safety-baseline*
*Completed: 2026-05-14*
