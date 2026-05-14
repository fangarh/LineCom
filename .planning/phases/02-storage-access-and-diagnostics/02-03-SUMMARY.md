---
phase: 02-storage-access-and-diagnostics
plan: "03"
subsystem: testing
tags: [xunit, aspnet-core, storage, diagnostics]
requires:
  - phase: 02-storage-access-and-diagnostics
    provides: storage serving policy and diagnostics endpoint from 02-01/02-02
provides:
  - Regression tests for public/private storage serving boundaries
  - Regression tests for diagnostics classification, bounds, authorization, and read-only behavior
affects: [storage, diagnostics, release-verification]
tech-stack:
  added: []
  patterns:
    - Test-host storage root override through in-memory configuration
    - Service-level diagnostics tests use fake repository data plus temp filesystem data
key-files:
  created:
    - tests/LineCom.Api.Tests/Infrastructure/Hosting/LocalStorageStaticFilesTests.cs
    - tests/LineCom.Api.Tests/Modules/Catalog/StorageDiagnosticsEndpointTests.cs
    - tests/LineCom.Api.Tests/Modules/Catalog/StorageDiagnosticsServiceTests.cs
    - tests/LineCom.Api.Tests/Modules/Catalog/StorageDiagnosticsSqlTests.cs
  modified: []
key-decisions:
  - "Storage boundary tests use HTTP test host rather than helper-only unit tests."
  - "Diagnostics endpoint tests use a fake service; classification is covered separately at service level."
patterns-established:
  - "Static storage behavior is covered through `LineComWebApplicationFactory` with temp storage roots."
requirements-completed: [STOR-01, STOR-02, STOR-03]
duration: 15min
completed: 2026-05-14
---

# Phase 02 Plan 03: Storage Boundary And Diagnostic Tests Summary

**XUnit coverage proves public storage boundaries, read-only diagnostics classification, bounded details, and staff/admin endpoint access.**

## Performance

- **Duration:** 15 min
- **Started:** 2026-05-14T16:43:00Z
- **Completed:** 2026-05-14T16:58:00Z
- **Tasks:** 4
- **Files modified:** 4

## Accomplishments

- Added test-host static-file tests proving `/storage/products/...` and `/storage/brands/...` are public while `/storage/import`, `/storage/export`, and `/storage/temp` return 404.
- Added diagnostics service tests for missing files, untracked files, stale deleted rows, orphaned rows, full counts, bounded details, truncation, and no absolute path exposure.
- Added endpoint tests for anonymous 401, customer 403, and seller/admin 200 behavior.
- Added read-only guard tests for diagnostics SQL and controller HTTP method exposure.

## Task Commits

1. **Task 1: Complete public/private static storage integration tests** - `b0ac6c7` (test)
2. **Task 2: Test diagnostics classification and bounded details** - `b0ac6c7` (test)
3. **Task 3: Test admin diagnostics endpoint authorization and contract** - `b0ac6c7` (test)
4. **Task 4: Add read-only diagnostics guard tests and final Phase 2 verification command** - `b0ac6c7` (test)

## Files Created/Modified

- `tests/LineCom.Api.Tests/Infrastructure/Hosting/LocalStorageStaticFilesTests.cs` - Public/private static storage integration tests.
- `tests/LineCom.Api.Tests/Modules/Catalog/StorageDiagnosticsServiceTests.cs` - Diagnostics classification and bounds tests.
- `tests/LineCom.Api.Tests/Modules/Catalog/StorageDiagnosticsEndpointTests.cs` - Authorization and contract endpoint tests.
- `tests/LineCom.Api.Tests/Modules/Catalog/StorageDiagnosticsSqlTests.cs` - Read-only SQL/controller guard tests.

## Decisions Made

- Service tests use fake repository data and temp files so diagnostics classification is deterministic and independent of PostgreSQL availability.
- Endpoint tests reuse existing auth fixture patterns and fake the diagnostics service to isolate authorization/contract behavior.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- Initial RED run failed at compile time because diagnostics types did not exist yet, as expected.
- After implementation, test helper paths initially used `System.IO.Path` inside `LineCom.Api.Tests.*` namespaces, which resolved as `LineCom.Api.Tests.System`. Fixed by using `global::System.IO.Path`.

## Verification

- `dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "FullyQualifiedName~StorageDiagnostics|FullyQualifiedName~LocalStorageStaticFiles"` passed: 16/16.
- `dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "FullyQualifiedName~LocalStoredFileWriter"` passed: 15/15.
- NuGet vulnerability-data warnings appeared because `https://api.nuget.org/v3/index.json` was unavailable; tests still restored from local cache and passed.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Phase 2 has automated evidence for storage access boundaries and diagnostics. Phase 3 can proceed to import storage consistency without adding cleanup behavior retroactively to Phase 2.

---
*Phase: 02-storage-access-and-diagnostics*
*Completed: 2026-05-14*
