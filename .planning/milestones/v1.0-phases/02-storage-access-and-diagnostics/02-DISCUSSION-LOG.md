# Phase 2: Storage Access And Diagnostics - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md - this log preserves the alternatives considered.

**Date:** 2026-05-14
**Phase:** 2-storage-access-and-diagnostics
**Areas discussed:** Public storage access boundary, Storage diagnostics report, Read-only diagnostics and data exposure

---

## Public Storage Access Boundary

| Option | Description | Selected |
|--------|-------------|----------|
| Restrict static files to public prefixes | Keep current public image directories static; keep private purposes out of those directories. | |
| Replace `/storage` with DB-checking controller | Check `stored_files.purpose/status` on each request. | |
| Hybrid | Public images through static whitelist; private-purpose files only through a separate authorized path/controller. | yes |

**User's choice:** Hybrid.
**Notes:** The phase should close anonymous access to non-public storage while preserving current public catalog image behavior.

| Option | Description | Selected |
|--------|-------------|----------|
| Only current image directories | Public anonymous access only for `storage/products/...` and `storage/brands/...`. | yes |
| Purpose from DB | Publicness determined by `stored_files.purpose`. | |
| Config allowlist | Use configurable public prefixes. | |

**User's choice:** Only current image directories.
**Notes:** No DB lookup is required for public static path in this phase.

| Option | Description | Selected |
|--------|-------------|----------|
| Preserve current URLs | Avoid mass changes to storage keys, frontend URLs, or catalog DTOs. | yes |
| Introduce new public prefix | Add `/storage/public/...` and close old `/storage/...`. | |
| Support both temporarily | Add migration path with both old and new URLs. | |

**User's choice:** Preserve current URLs.
**Notes:** Phase 2 changes access boundaries, not public URL shape.

| Option | Description | Selected |
|--------|-------------|----------|
| Endpoint/static-file integration tests | Use test host with files in allowed and forbidden storage subdirectories. | yes |
| Unit tests only | Test only a path policy helper. | |
| Both levels | Helper unit tests plus endpoint integration tests. | |

**User's choice:** Endpoint/static-file integration tests.
**Notes:** Tests should prove 200 for public image dirs and 404 for private-like dirs.

---

## Storage Diagnostics Report

| Option | Description | Selected |
|--------|-------------|----------|
| Backend admin endpoint | Expose diagnostics under an admin API path and protect it for staff/admin. | yes |
| CLI/service only | No HTTP endpoint. | |
| Future import/export contour | Fold into later web import/export work. | |

**User's choice:** Backend admin endpoint.
**Notes:** Frontend UI is not required for Phase 2.

| Option | Description | Selected |
|--------|-------------|----------|
| Summary plus problem lists | Counts plus bounded lists for missing, untracked, stale deleted, and orphaned categories. | yes |
| Counts only | Less detail. | |
| Full dump | All rows/files. | |

**User's choice:** Summary plus bounded problem lists.
**Notes:** Operators need enough detail to investigate without huge responses.

| Option | Description | Selected |
|--------|-------------|----------|
| DB `stored_files` vs disk | Compare DB rows against physical files under `Storage:RootPath`. | yes |
| Public image files only | Narrower check. | |
| DB/disk plus product/brand references | Adds extra reference consistency checks. | |

**User's choice:** DB `stored_files` vs disk.
**Notes:** Product/brand reference consistency remains out of scope beyond existing constraints.

| Option | Description | Selected |
|--------|-------------|----------|
| Bounded details plus counts | Full counts, limited details such as `maxItems=100`, `truncated=true`. | yes |
| Configurable only | Limit only from config. | |
| No limit | Return all detail rows/files. | |

**User's choice:** Bounded details plus counts.
**Notes:** The planner may choose exact default/query mechanism.

---

## Read-Only Diagnostics And Data Exposure

| Option | Description | Selected |
|--------|-------------|----------|
| Read-only diagnostics only | No file deletes or DB mutations in Phase 2. | yes |
| Cleanup endpoint | Add destructive cleanup for deleted/orphaned. | |
| Dry-run cleanup plan | Show proposed actions without executing. | |

**User's choice:** Read-only diagnostics only.
**Notes:** Cleanup and retention should be planned later, not slipped into Phase 2.

| Option | Description | Selected |
|--------|-------------|----------|
| Status categories plus file existence | Deleted rows with existing files are stale; orphaned rows always reported with `fileExists`. | yes |
| Only rows with physical files | Hide orphaned rows without files. | |
| One non-active category | Simpler but less useful. | |

**User's choice:** Status categories plus file existence.
**Notes:** `staleDeletedRows` and `orphanedRows` have distinct meanings.

| Option | Description | Selected |
|--------|-------------|----------|
| Only storageKey/relative path | Avoid exposing absolute server paths. | yes |
| Absolute path only in development | More debug detail with conditional contract. | |
| Always absolute path | Simpler locally but leaks infrastructure detail. | |

**User's choice:** Only storageKey/relative path.
**Notes:** API responses should avoid absolute filesystem paths.

## the agent's Discretion

- Exact class/service/controller names.
- Exact authorization implementation, as long as access is restricted to admin/staff.
- Exact bounded detail default and whether query params are supported.
- Exact static whitelist implementation mechanism.

## Deferred Ideas

- Cleanup endpoints or maintenance commands.
- Import staging/promotion/cleanup.
- Product/brand reference consistency diagnostics beyond existing constraints.
- Frontend admin UI for diagnostics.
