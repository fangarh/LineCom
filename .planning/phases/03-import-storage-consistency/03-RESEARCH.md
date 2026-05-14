# Phase 3: Import Storage Consistency - Research

**Researched:** 2026-05-14
**Status:** Complete

## RESEARCH COMPLETE

## Scope

Phase 3 covers `STOR-04`: catalog import file writes must use a documented staging/commit/cleanup model or equivalent compensation path. The work is limited to the existing `apps/catalog-import.core` and `apps/catalog-import.winforms` import workflow.

Out of scope:
- Web import/export jobs or admin web UI.
- Local FileStorage replacement with S3/MinIO.
- Phase 2 diagnostics endpoint changes.
- General retention cleanup and Phase 6 backup/restore documentation.

## Current Flow

`CatalogImportDatabase.ApplyAsync` currently:
1. Opens a PostgreSQL connection and transaction.
2. Optionally executes reset SQL inside the transaction.
3. Copies prepared images directly into public storage via `CopyPreparedImagesToStorage`.
4. Inserts/upserts categories, products, attributes, `stored_files`, and `product_images`.
5. Commits the transaction.

This means filesystem writes happen before DB commit and before `stored_files` rows are guaranteed to exist. If later DB work fails, public files under `storage/products/catalog-import/` can remain without rows.

`ResetCatalog` currently deletes import-managed DB rows using:

```sql
WHERE purpose = 'product_image'
  AND storage_key LIKE 'storage/products/catalog-import/%'
```

but it does not remove matching physical files.

## Implementation Findings

### Staging and promotion

Recommended shape:
- Create a per-run context with `runId`, private staging root, final public key list, and manifest items.
- Stage files under a private prefix inside `Storage:RootPath`, for example `.staging/catalog-import/{runId}`.
- Keep final `storage_key` values as `storage/products/catalog-import/...`.
- Write manifest data before/while staging so cleanup is scoped to current-run files.
- After successful DB commit, promote staged files into final public locations.
- Promotion should be idempotent where possible: if final file already exists with expected metadata, treat it as already promoted; if metadata differs, report conflict.

Important design constraint: promotion after commit cannot be fully atomic with DB state. The correct recovery model is retry, then explicit incomplete-promotion report. A second compensating DB transaction is more dangerous because it can partially remove committed data and race with operator inspection.

### Failure cleanup

The cleanup surface must be scoped by `runId`/manifest:
- Delete current-run staging files and directories on failed apply.
- Delete any current-run promoted files if failure happens after promotion starts.
- Do not delete old staging runs automatically.
- Report old staging leftovers as operator-visible drift.
- Report cleanup failures with relative staging paths/storage keys only.

The importer should avoid broad directory deletion based only on prefix strings. Use normalized path resolution that mirrors `LocalStoredFileWriter` root containment checks.

### Reset cleanup

Recommended shape:
1. Before reset SQL deletes rows, select affected import-managed storage keys.
2. Execute reset SQL in the DB transaction.
3. Commit the DB transaction.
4. Delete only the preselected physical files.
5. Report failed deletes after retry.
6. Scan/report untracked leftovers under `storage/products/catalog-import/` without deleting them.

This keeps destructive physical deletion tightly tied to rows that reset actually owned.

### Storage key conflicts

Current fail-fast semantics are correct:
- `InsertStoredFile` uses `ON CONFLICT (storage_key) DO NOTHING`.
- `SelectStoredFileByStorageKeyAndMetadata` accepts only exact metadata matches.
- A metadata mismatch raises an exception.

Plan should add preflight conflict detection before staging, while preserving the transaction-time guard for races. Conflict reports should include source asset information plus expected/existing checksum, size and content type.

### Reporting and WinForms

`CatalogImportReportWriter` currently writes plan-level dry-run/apply reports, not apply result details. Phase 3 should extend report context/result models so WinForms can surface:
- run id
- promotion failures
- cleanup failures
- old staging leftovers
- reset storage cleanup result
- reset untracked leftovers

Markdown table escaping already exists and should be reused for new report sections.

## Validation Architecture

Test strategy:
- Source/SQL contract tests for ordering:
  - staging happens before DB mutation;
  - direct public copy before DB insert is removed;
  - promotion happens after commit;
  - reset selects storage keys before delete SQL and deletes physical files after commit.
- Pure filesystem tests for staging/promotion helpers:
  - private staging path stays under storage root;
  - final key resolves under `products/catalog-import`;
  - current-run cleanup deletes staged/promoted files only;
  - old staging leftovers are reported, not deleted.
- Result/report tests:
  - no absolute server paths in conflict/promotion/cleanup reports;
  - reset DB impact and filesystem cleanup result are separate;
  - reports include partial cleanup failures and untracked leftovers.
- Existing focused command:
  - `dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "FullyQualifiedName~CatalogImport"`

No frontend or browser tests are needed for Phase 3.

## Planning Recommendation

Use two plans:
1. `03-01`: implement import staging, post-commit promotion, scoped cleanup, reset cleanup and report/result models in import core/WinForms.
2. `03-02`: add/adjust catalog import tests that prove DB/filesystem ordering, conflict handling, reset cleanup, report safety and no broad destructive cleanup.

Because the work touches destructive reset behavior and filesystem paths, every implementation task should include path traversal/root containment checks and relative-path-only reports.
