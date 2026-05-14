# Phase 3: Import Storage Consistency - Context

**Gathered:** 2026-05-14
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 3 delivers recoverable and testable catalog import DB/file consistency for the existing WinForms/catalog-import core workflow. The phase must make import image writes follow a documented staging, promotion and cleanup flow, prevent failed apply paths from leaving unmanaged public files without database rows, and define reset physical-file behavior for import-managed images.

This phase is limited to `STOR-04`. It does not add web-based import/export, does not change the target storage approach, does not switch to S3/MinIO, does not extend public catalog functionality, does not expand Phase 2 diagnostics endpoints, and does not cover Phase 6 backup/restore policy.

</domain>

<decisions>
## Implementation Decisions

### Apply staging and promotion
- **D-01:** Import image writes must use staging before database mutation and promotion after a successful database commit.
- **D-02:** Final `storage_key` values remain the public catalog-import keys, for example under `storage/products/catalog-import/...`; only the physical write timing changes.
- **D-03:** Staging lives inside `Storage:RootPath` under a private per-run prefix, for example `.staging/catalog-import/{runId}`. This prefix must not be served publicly by static file middleware.
- **D-04:** Each import run gets its own staging directory and manifest. The manifest must map source asset information to staging paths and final storage keys so cleanup, retry and tests can be scoped by `runId`.
- **D-05:** Promotion happens only after DB commit. Promotion should retry local filesystem operations before failing.
- **D-06:** If promotion partially fails after DB commit, apply must return or log an explicit incomplete-promotion report listing the affected relative storage/staging keys. Do not attempt a second compensating database transaction to roll back already committed DB rows.

### Apply failure cleanup
- **D-07:** Failed apply cleanup must be scoped to the current `runId`/manifest only.
- **D-08:** On failure before completion, cleanup must remove the current-run staging directory and any current-run files that were already promoted.
- **D-09:** Old staging directories/manifests from previous interrupted or crashed runs are detected and reported only. Phase 3 must not add automatic retention cleanup for old runs.
- **D-10:** Cleanup should retry local filesystem deletes before reporting failure.
- **D-11:** Cleanup failure reports must be explicit and use relative staging/storage keys only. Do not expose absolute server filesystem paths.
- **D-12:** Phase 3 does not modify the Phase 2 `/api/admin/storage/diagnostics` endpoint. Staging and manifest naming should remain compatible with future diagnostics or maintenance work.

### Reset file behavior
- **D-13:** Reset must delete physical import-managed files after the reset database transaction commits successfully.
- **D-14:** Reset physical deletion is strictly scoped to files selected before reset from `stored_files` rows with `purpose = 'product_image'` and `storage_key LIKE 'storage/products/catalog-import/%'`.
- **D-15:** Post-reset physical delete should retry local filesystem operations. If some files cannot be deleted, the result/report must include partial cleanup failures; the database reset must not be rolled back via a second transaction.
- **D-16:** Reset deletes only DB-selected import-managed files. Other files found under `storage/products/catalog-import/` are not deleted automatically and must be reported as untracked leftovers.
- **D-17:** Keep DB reset impact and filesystem cleanup outcome separate. `CatalogImportResetImpact` remains about database counts; add a separate reset storage cleanup result model with counts and bounded failed/untracked relative paths.

### Storage key conflicts
- **D-18:** Existing fail-fast behavior on `storage_key` metadata mismatch remains correct. Do not generate alternate keys, overwrite files, or mutate existing rows to hide the conflict.
- **D-19:** Add a preflight DB conflict check before staging copy for clearer errors, while keeping the existing transaction-time insert/select guard to handle races.
- **D-20:** Conflict reports should include `storageKey`, source asset key or original file name, and expected/existing checksum, size and content type. Do not include absolute filesystem paths.
- **D-21:** If a metadata mismatch causes apply failure, normal current-run cleanup still applies.

### the agent's Discretion
- Exact `runId` format and manifest filename/JSON shape, provided it is deterministic enough for tests and recovery.
- Exact retry count/backoff for promotion and cleanup, provided tests can verify retry/failure behavior without slow sleeps.
- Exact result class names and report formatting, provided DB impact, promotion failures, cleanup failures and untracked leftovers are machine-testable.
- Exact helper/service decomposition inside `apps/catalog-import.core`, provided import logic stays outside the backend API.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project and phase scope
- `.planning/PROJECT.md` - project constraints, Local FileStorage target policy, and active release-stabilization scope.
- `.planning/REQUIREMENTS.md` - Phase 3 requirement `STOR-04`.
- `.planning/ROADMAP.md` - Phase 3 goal, success criteria, and planned split `03-01`/`03-02`.
- `.planning/STATE.md` - current workflow position and dirty-worktree constraints.

### Prior phase decisions
- `.planning/phases/02-storage-access-and-diagnostics/02-CONTEXT.md` - locked storage boundary decisions, read-only diagnostics scope, and deferred import staging/cleanup.
- `.planning/phases/02-storage-access-and-diagnostics/02-VERIFICATION.md` - confirms Phase 2 public storage boundary and diagnostics are complete.

### Codebase map
- `.planning/codebase/ARCHITECTURE.md` - importer architecture, Local FileStorage policy, Dapper patterns and storage diagnostics context.
- `.planning/codebase/CONCERNS.md` - import DB/disk atomicity, reset fragility and remaining cleanup risks.
- `.planning/codebase/TESTING.md` - catalog import test organization, SQL contract tests and database behavior test patterns.

### Import implementation
- `apps/catalog-import.core/Database/CatalogImportDatabase.cs` - current apply/reset flow, storage key generation, file copy behavior, DB transaction and reset SQL.
- `apps/catalog-import.winforms/MainForm.cs` - existing UI entry point and apply/reset options surface.
- `tests/LineCom.Api.Tests/CatalogImport/CatalogImportDatabaseSqlTests.cs` - current SQL/storage behavior assertions for importer.
- `tests/LineCom.Api.Tests/CatalogImport/CatalogImportPlannerTests.cs` - existing planner fixture patterns.
- `tests/LineCom.Api.Tests/CatalogImport/ProductImageManifestReaderTests.cs` - image manifest test patterns.

### Storage and diagnostics implementation
- `apps/api/Infrastructure/Storage/LocalStoredFileWriter.cs` - safe local storage key/path resolution and best-effort delete behavior used by API uploads.
- `apps/api/Infrastructure/Hosting/LocalStoragePathPolicy.cs` - Phase 2 public storage path policy that must keep staging private.
- `apps/api/Infrastructure/Hosting/LocalStorageStaticFilesExtensions.cs` - public static serving integration point.
- `apps/api/Modules/Catalog/Services/StorageDiagnosticsService.cs` - Phase 2 read-only DB/disk drift comparison model.
- `apps/api/Modules/Catalog/Repositories/StorageDiagnosticsSql.cs` - stored file diagnostics SQL.
- `tests/LineCom.Api.Tests/Modules/Catalog/StorageDiagnosticsServiceTests.cs` - bounded diagnostics and relative path testing patterns.
- `tests/LineCom.Api.Tests/Infrastructure/Hosting/LocalStorageStaticFilesTests.cs` - public/private storage serving boundary tests.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `CatalogImportDatabaseStorage.FormatProductImageStorageKey` already creates deterministic final keys under `storage/products/catalog-import/`.
- `CatalogImportDatabase.ApplyAsync` is the current transaction boundary where staging, preflight conflict checks, DB mutation, promotion and cleanup need to be coordinated.
- `CatalogImportDatabaseSql.CountResetImpact` and `ResetCatalog` already identify import-managed `stored_files` rows by purpose and `storage_key` prefix.
- `LocalStoredFileWriter` demonstrates safe root-contained path resolution and delete semantics that the importer should mirror instead of using ad hoc path handling.
- Phase 2 diagnostics provide a read-only model for missing files and untracked files, useful as a reference for report naming and relative-path-only output.

### Established Patterns
- Import logic belongs in `apps/catalog-import.core`; the backend API must not absorb 1C import planning/apply behavior.
- Database access uses explicit Dapper/Npgsql SQL; SQL constants are tested directly.
- Importer reports are generated as JSON/Markdown by catalog-import core and surfaced by the WinForms runner.
- Tests live under `tests/LineCom.Api.Tests/CatalogImport` for importer logic and can assert SQL/source ordering when live DB coverage is not required.
- Destructive behavior must be tightly scoped and test-covered; no broad cleanup of unrelated storage prefixes.

### Integration Points
- `CatalogImportApplyOptions` likely needs to carry storage root/run behavior without expanding into web import/export scope.
- `CatalogImportApplyResult` likely needs promotion and reset cleanup outcome fields in addition to existing category/product/image counts.
- `ResetCatalogAsync` needs to capture DB-selected import-managed storage keys before executing reset SQL.
- `CopyPreparedImagesToStorage` should be replaced or narrowed into staging write plus post-commit promotion.
- WinForms apply/report output should surface incomplete promotion, cleanup failures, old staging leftovers and reset untracked leftovers.

</code_context>

<specifics>
## Specific Ideas

- Prefer same-volume promotion by keeping staging under `Storage:RootPath`, but under a private prefix that Phase 2 public static serving does not expose.
- Treat partial promotion after DB commit as a recoverable operator-visible condition: retry first, then report exactly what remains incomplete.
- Keep all reports free of absolute filesystem paths; use relative staging paths, final storage keys, source asset keys and metadata.
- Old staging leftovers are signal, not automatic deletion target, in Phase 3.
- Reset physical cleanup is allowed because reset is already an explicitly guarded destructive operation, but deletion must be limited to DB-selected import-managed files.

</specifics>

<deferred>
## Deferred Ideas

- Automatic retention cleanup of old staging runs or old deleted/orphaned stored files.
- Extending `/api/admin/storage/diagnostics` to include import staging leftovers.
- Web-based import/export UI, import jobs, mapping persistence and row-level error review.
- Local FileStorage backup/restore documentation and release verification, which belong to Phase 6.

</deferred>

---

*Phase: 03-Import Storage Consistency*
*Context gathered: 2026-05-14*
