---
phase: 02-storage-access-and-diagnostics
plan: "02"
subsystem: api
tags: [aspnet-core, dapper, local-filestorage, diagnostics]
requires:
  - phase: 02-storage-access-and-diagnostics
    provides: public storage root policy from 02-01
provides:
  - Read-only admin storage diagnostics endpoint
  - DB/disk drift classification for stored_files and physical files
affects: [storage, catalog-admin, production-readiness]
tech-stack:
  added: []
  patterns:
    - Diagnostics controller delegates to service and Dapper repository
    - Diagnostics API exposes storage keys, not absolute filesystem paths
key-files:
  created:
    - apps/api/Modules/Catalog/Controllers/AdminStorageDiagnosticsController.cs
    - apps/api/Modules/Catalog/DTOs/AdminStorageDiagnosticsDtos.cs
    - apps/api/Modules/Catalog/Repositories/DapperStorageDiagnosticsRepository.cs
    - apps/api/Modules/Catalog/Repositories/IStorageDiagnosticsRepository.cs
    - apps/api/Modules/Catalog/Repositories/StorageDiagnosticsSql.cs
    - apps/api/Modules/Catalog/Services/IStorageDiagnosticsService.cs
    - apps/api/Modules/Catalog/Services/StorageDiagnosticsService.cs
  modified:
    - apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs
key-decisions:
  - "Diagnostics are read-only and expose only relative storage keys."
  - "Diagnostics compare `stored_files` against disk files without checking product/brand reference consistency."
patterns-established:
  - "Storage diagnostics use thin controller -> service -> read-only Dapper repository."
requirements-completed: [STOR-03]
duration: 18min
completed: 2026-05-14
---

# Phase 02 Plan 02: Storage Diagnostics Endpoint Summary

**Read-only staff/admin storage diagnostics report compares `stored_files` metadata with Local FileStorage disk state.**

## Performance

- **Duration:** 18 min
- **Started:** 2026-05-14T16:43:00Z
- **Completed:** 2026-05-14T17:01:00Z
- **Tasks:** 4
- **Files modified:** 8

## Accomplishments

- Added `GET /api/admin/storage/diagnostics` behind existing authenticated staff/admin guard flow.
- Added summary counts plus bounded detail sections for `missingFiles`, `untrackedFiles`, `staleDeletedRows`, and `orphanedRows`.
- Implemented deterministic DB/disk comparison using storage keys only, with no cleanup or mutation path.
- Registered diagnostics service and repository in the catalog module.

## Task Commits

1. **Task 1: Define diagnostics DTO contract with relative paths only** - `75ab14f` (feat)
2. **Task 2: Add read-only stored_files diagnostics repository** - `75ab14f` (feat)
3. **Task 3: Implement DB/disk comparison service with bounded details** - `75ab14f` (feat)
4. **Task 4: Expose protected GET admin diagnostics endpoint and register services** - `75ab14f` (feat)

## Files Created/Modified

- `apps/api/Modules/Catalog/DTOs/AdminStorageDiagnosticsDtos.cs` - Diagnostics response contract.
- `apps/api/Modules/Catalog/Repositories/StorageDiagnosticsSql.cs` - Read-only `stored_files` query.
- `apps/api/Modules/Catalog/Repositories/DapperStorageDiagnosticsRepository.cs` - Dapper read model.
- `apps/api/Modules/Catalog/Services/StorageDiagnosticsService.cs` - Classification and bounded details.
- `apps/api/Modules/Catalog/Controllers/AdminStorageDiagnosticsController.cs` - Staff/admin GET endpoint.
- `apps/api/Modules/Catalog/CatalogServiceCollectionExtensions.cs` - DI registration.

## Decisions Made

- `missingFiles` is limited to active rows with missing physical files so orphaned rows remain a distinct diagnostic category.
- `orphanedRows` always reports rows with `status = 'orphaned'` and includes `fileExists`.
- `maxItems` is clamped to a deterministic bounded range and defaults to 100.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## Verification

- `dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "FullyQualifiedName~StorageDiagnostics|FullyQualifiedName~LocalStorageStaticFiles"` passed: 16/16.
- `Select-String` mutation scan found no diagnostics write SQL or file deletion calls; matches were only category names such as `staleDeletedRows`/`IsDeleted`.
- DTO inspection confirms no absolute path fields are exposed.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Phase 3 can use the diagnostics report as a read-only visibility layer while planning import staging/promotion/cleanup separately.

---
*Phase: 02-storage-access-and-diagnostics*
*Completed: 2026-05-14*
