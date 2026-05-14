---
phase: 02-storage-access-and-diagnostics
plan: "01"
subsystem: infra
tags: [aspnet-core, local-filestorage, static-files, storage-boundary]
requires:
  - phase: 01-release-safety-baseline
    provides: production storage configuration guardrails
provides:
  - Restricted anonymous static serving for Local FileStorage public image directories
  - Preserved `/storage/products/...` and `/storage/brands/...` URL compatibility
affects: [storage, catalog-images, phase-03-import-storage-consistency]
tech-stack:
  added: []
  patterns:
    - Public storage prefixes are centralized in `LocalStoragePathPolicy`
key-files:
  created:
    - apps/api/Infrastructure/Hosting/LocalStoragePathPolicy.cs
  modified:
    - apps/api/Infrastructure/Hosting/LocalStorageStaticFilesExtensions.cs
key-decisions:
  - "Static file serving now registers only public image subroots instead of the whole storage root."
  - "Existing public catalog image URL shape remains unchanged."
patterns-established:
  - "Storage root resolution is shared through a small hosting policy helper."
requirements-completed: [STOR-01, STOR-02]
duration: 12min
completed: 2026-05-14
---

# Phase 02 Plan 01: Public Storage Serving Policy Summary

**Local FileStorage static serving is restricted to current public product and brand image prefixes without changing public URL shape.**

## Performance

- **Duration:** 12 min
- **Started:** 2026-05-14T16:43:00Z
- **Completed:** 2026-05-14T16:55:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Replaced broad `/storage` root static serving with explicit `/storage/products` and `/storage/brands` registrations.
- Kept root path fallback semantics compatible with existing development/test behavior.
- Confirmed catalog SQL still emits `"/" || stored_file.storage_key`, preserving `/storage/products/...` and `/storage/brands/...`.

## Task Commits

1. **Task 1: Replace broad storage-root static serving with public-prefix serving** - `230e0ca` (feat)
2. **Task 2: Keep public catalog URL compatibility explicit** - `230e0ca` (feat)

## Files Created/Modified

- `apps/api/Infrastructure/Hosting/LocalStoragePathPolicy.cs` - Centralizes storage root resolution and public storage prefixes.
- `apps/api/Infrastructure/Hosting/LocalStorageStaticFilesExtensions.cs` - Registers static files only for public image subdirectories.

## Decisions Made

- Used multiple `UseStaticFiles` registrations with physical subroots to preserve existing URLs while closing anonymous access to non-public top-level directories.
- Left catalog SQL, DTOs, frontend URL construction, and `stored_files.storage_key` unchanged.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## Verification

- `dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "FullyQualifiedName~LocalStoredFileWriter"` passed: 15/15.
- `rg "'/' \|\| stored_file.storage_key|storage_key" apps/api/Modules/Catalog` confirmed public product image and brand logo URL construction remains storage-key based.
- Dependent Plan 02-03 added and passed static-file boundary tests.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Plan 02-02 diagnostics can rely on the same storage root resolution helper; Phase 3 import work must continue to avoid assuming all Local FileStorage paths are public.

---
*Phase: 02-storage-access-and-diagnostics*
*Completed: 2026-05-14*
