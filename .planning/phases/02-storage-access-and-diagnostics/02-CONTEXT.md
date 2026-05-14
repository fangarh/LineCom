# Phase 2: Storage Access And Diagnostics - Context

**Gathered:** 2026-05-14
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 2 delivers Local FileStorage access boundaries and read-only diagnostics only. It must prevent anonymous `/storage` access to non-public file purposes, keep existing public catalog image URLs working, and expose an operator-facing diagnostic report for DB/disk drift. This phase does not change the target storage approach, does not implement import staging/promotion/cleanup from Phase 3, and does not add destructive cleanup or retention workflows.

</domain>

<decisions>
## Implementation Decisions

### Public storage access boundary
- **D-01:** Use a hybrid access model: public catalog images remain available through a restricted static-file path, while future private-purpose files must use a separate authorized path/controller if they need download support.
- **D-02:** In Phase 2, anonymous static serving is limited to the current public image directories: `storage/products/...` and `storage/brands/...`.
- **D-03:** Preserve existing public image URLs. Do not mass-change `stored_files.storage_key`, frontend URLs, or public catalog DTOs.
- **D-04:** Prove the boundary with test-host/static-file integration tests: allowed public image directories return 200 and private-like directories such as import/export/temp return 404.

### Storage diagnostics report
- **D-05:** Expose diagnostics as a backend admin endpoint, for example under `/api/admin/storage/diagnostics`, protected for staff/admin access. A frontend UI is not required for Phase 2 unless planning finds a minimal existing admin surface fit.
- **D-06:** The report shape should include a summary/counts section plus bounded detail lists for `missingFiles`, `untrackedFiles`, `staleDeletedRows`, and `orphanedRows`.
- **D-07:** Diagnostics compare database rows in `stored_files` against physical files under `Storage:RootPath`.
- **D-08:** Do not expand Phase 2 diagnostics to product/brand reference consistency beyond existing database constraints and tests.
- **D-09:** Counts must represent the full scan result, while detail lists are bounded, e.g. `maxItems=100` per category with `truncated=true` when more rows/files exist.

### Read-only diagnostics and data exposure
- **D-10:** Phase 2 diagnostics are read-only. Do not delete files, update `stored_files`, mark rows, or add cleanup endpoints in this phase.
- **D-11:** `staleDeletedRows` means `stored_files.status = 'deleted'` and the physical file still exists.
- **D-12:** `orphanedRows` means `stored_files.status = 'orphaned'` regardless of physical file presence; include `fileExists` in item details.
- **D-13:** API responses should expose `storageKey` or relative paths only, not absolute server filesystem paths.

### the agent's Discretion
- Exact class/service/controller names for the diagnostics implementation.
- Exact authorization attribute or staff-role guard reuse, as long as endpoint access is restricted to admin/staff and tests cover anonymous rejection.
- Exact `maxItems` default and whether a bounded query parameter is accepted, as long as the report is bounded and deterministic.
- Whether the static allowlist is implemented as multiple `UseStaticFiles` registrations, a small path policy helper, or another ASP.NET Core-supported mechanism, as long as current public URLs remain compatible.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project and phase scope
- `.planning/PROJECT.md` - project context, constraints, Local FileStorage target policy, and validated Phase 1 baseline.
- `.planning/REQUIREMENTS.md` - Phase 2 requirements `STOR-01`, `STOR-02`, `STOR-03`.
- `.planning/ROADMAP.md` - Phase 2 goal, success criteria, and planned plan split.
- `.planning/phases/01-release-safety-baseline/01-VERIFICATION.md` - confirms Phase 1 is complete before Phase 2 work.

### Codebase map
- `.planning/codebase/ARCHITECTURE.md` - API infrastructure, storage, Dapper, and test boundary.
- `.planning/codebase/CONCERNS.md` - Local FileStorage public static root and DB/disk lifecycle risks.
- `.planning/codebase/TESTING.md` - xUnit/WebApplicationFactory and database test patterns.

### Storage and database implementation
- `apps/api/Program.cs` - middleware pipeline and `UseLocalStorageStaticFiles` integration point.
- `apps/api/Infrastructure/Hosting/LocalStorageStaticFilesExtensions.cs` - current broad `/storage` static-file serving.
- `apps/api/Infrastructure/Storage/LocalStoredFileWriter.cs` - storage key format, physical path resolution, file write/delete behavior.
- `apps/api/Infrastructure/Storage/LocalStoredFileDraft.cs` - stored file draft shape used by repositories.
- `apps/api/Infrastructure/Database/DatabaseServiceCollectionExtensions.cs` - storage options and writer registration.
- `apps/dbmigrator/Migrations/002_catalog_foundation.sql` - `stored_files` purpose/status constraints and product/brand image relations.

### Catalog storage references
- `apps/api/Modules/Catalog/Repositories/AdminCatalogImageSql.cs` - product image stored-file SQL and deleted-row marking.
- `apps/api/Modules/Catalog/Repositories/AdminCatalogBrandSql.cs` - brand logo stored-file SQL and deleted-row marking.
- `apps/api/Modules/Catalog/Queries/PublicProductSql.cs` - public catalog image references.
- `tests/LineCom.Api.Tests/Infrastructure/Storage/LocalStoredFileWriterTests.cs` - existing storage writer tests.
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogImageSqlTests.cs` - existing product image stored-file SQL tests.
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogBrandSqlTests.cs` - existing brand logo stored-file SQL tests.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `LocalStoredFileWriter` already centralizes storage key validation and physical path resolution rules for writes/deletes.
- `stored_files` already stores `storage_key`, `purpose`, `status`, checksum, size, and creator metadata needed for diagnostics.
- Existing catalog image/brand SQL already filters public image usage by `status = 'active'` and `purpose IN ('product_image', 'brand_logo')`.
- `LineComWebApplicationFactory` and endpoint tests already support middleware/routing integration tests.

### Established Patterns
- Backend cross-cutting hosting behavior lives under `apps/api/Infrastructure/Hosting` and is wired in `Program.cs`.
- Backend modules keep HTTP controllers thin and place SQL in repository/query classes; new diagnostics should avoid SQL inside controllers.
- Admin/staff access should rely on backend authorization as the authority, not frontend-only hiding.
- Tests should be focused and mirror existing xUnit naming/fixture conventions.

### Integration Points
- Public serving policy connects through `apps/api/Infrastructure/Hosting/LocalStorageStaticFilesExtensions.cs` and `app.UseLocalStorageStaticFiles(builder.Configuration)`.
- Diagnostics likely needs `IDbConnectionFactory` plus `Storage:RootPath`/host environment access.
- Diagnostic endpoint should live in a backend admin area/module or infrastructure-facing admin controller and return the stable JSON API model used by other endpoints.
- Tests should cover static-file behavior via test host and diagnostic report behavior through service/controller tests.

</code_context>

<specifics>
## Specific Ideas

- Keep public image URLs stable; the release should not require URL migration for product images or brand logos.
- Treat Phase 2 as protective/read-only hardening. Cleanup/retention/destructive operations should be deferred and planned explicitly later.
- Prefer bounded diagnostics responses: full counts for operator visibility, limited item details to prevent huge responses.
- Do not expose absolute filesystem paths in API responses; use `storageKey` or relative path values.

</specifics>

<deferred>
## Deferred Ideas

- Destructive cleanup endpoint or maintenance command for `deleted`/`orphaned` rows and physical files.
- Import image staging/promotion/cleanup behavior, which belongs to Phase 3.
- Product/brand reference consistency diagnostics beyond existing FK and purpose constraints.
- Frontend admin UI for storage diagnostics, unless a later plan explicitly scopes a minimal view.

</deferred>

---

*Phase: 2-Storage Access And Diagnostics*
*Context gathered: 2026-05-14*
