# Phase 3: Import Storage Consistency - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md - this log preserves the alternatives considered.

**Date:** 2026-05-14
**Phase:** 03-import-storage-consistency
**Areas discussed:** Apply staging/promotion, Apply failure cleanup, Reset file behavior, Storage key conflicts

---

## Apply Staging/Promotion

| Option | Description | Selected |
|--------|-------------|----------|
| Staging then promote after commit | Copy images to private staging, commit DB rows with final keys, then promote files to public storage. | yes |
| Current public copy plus compensation | Keep current flow close to public copy before commit, but track and delete current-run files on failure. | |
| Agent decides | Let the agent choose based on risk and testability criteria. | |

**User's choice:** Staging then promote after commit.
**Notes:** Final public catalog-import storage keys remain stable. Physical files should not become publicly visible before successful DB commit.

| Option | Description | Selected |
|--------|-------------|----------|
| Fail with incomplete promotion report | If promotion fails after DB commit, report missing promoted files and leave staging for operator action. | |
| Best-effort rollback DB rows | Try to compensate committed DB rows in a second transaction. | |
| Retry promotion before failing | Retry local promotion, then report incomplete promotion without rolling back committed DB rows. | yes |

**User's choice:** Retry promotion before failing.
**Notes:** Do not attempt a second compensating DB transaction after successful commit.

| Option | Description | Selected |
|--------|-------------|----------|
| Per-run staging directory with manifest | Isolate each import run with a manifest mapping source, staging and final keys. | yes |
| Single shared staging directory | Use one staging prefix for all runs. | |
| No persistent manifest | Keep file tracking only in memory. | |

**User's choice:** Per-run staging directory with manifest.
**Notes:** Manifest should support retry, cleanup and tests scoped by run.

| Option | Description | Selected |
|--------|-------------|----------|
| Inside storage root under private prefix | Keep staging under `Storage:RootPath`, for same-volume promotion, but outside public static prefixes. | yes |
| Outside storage root | Use a separate configured temporary path. | |
| Agent decides | Capture only constraints and let planning choose location. | |

**User's choice:** Inside storage root under private prefix.
**Notes:** The plan must verify the private staging prefix is not served publicly.

---

## Apply Failure Cleanup

| Option | Description | Selected |
|--------|-------------|----------|
| Delete current-run staging only | Delete only staging for the current run. | |
| Delete staging plus any promoted current-run files | Cleanup staging and any promoted files tracked in the current manifest. | yes |
| Leave staging for manual diagnosis | Do not delete automatically. | |

**User's choice:** Delete staging plus any promoted current-run files.
**Notes:** Cleanup must be scoped by manifest/runId and must not touch previous runs.

| Option | Description | Selected |
|--------|-------------|----------|
| Report only in Phase 3 | Detect/report old staging leftovers, but do not delete them automatically. | yes |
| Best-effort cleanup by age | Delete old staging runs older than a retention threshold. | |
| Cleanup only with explicit flag | Add an explicit option to remove old staging leftovers. | |

**User's choice:** Report only in Phase 3.
**Notes:** Retention/destructive cleanup for old runs remains future maintenance work.

| Option | Description | Selected |
|--------|-------------|----------|
| Fail loudly with cleanup errors | Report cleanup failures explicitly. | |
| Log and continue silently | Do not block result/report on cleanup failure. | |
| Retry cleanup then fail loudly | Retry local deletes, then report unresolved cleanup failures. | yes |

**User's choice:** Retry cleanup then fail loudly.
**Notes:** Reports must avoid absolute paths and use relative staging/storage keys.

| Option | Description | Selected |
|--------|-------------|----------|
| No API changes, import report only | Keep Phase 2 diagnostics endpoint unchanged and only report in importer output. | |
| Extend diagnostics | Add import staging leftovers to `/api/admin/storage/diagnostics`. | |
| Document future integration only | Keep endpoint unchanged but make naming compatible with future diagnostics/maintenance. | yes |

**User's choice:** Document future integration only.
**Notes:** Phase 3 must not expand Phase 2 diagnostics API surface.

---

## Reset File Behavior

| Option | Description | Selected |
|--------|-------------|----------|
| Delete import-managed physical files after DB reset | After successful DB commit, delete files selected from import-managed `stored_files` rows. | yes |
| Document retention only | Leave files on disk and document expected retention. | |
| Move reset files to quarantine | Move files into private quarantine instead of deleting. | |

**User's choice:** Delete import-managed physical files after successful DB reset.
**Notes:** Deletion must be scoped to `purpose = 'product_image'` and `storage_key LIKE 'storage/products/catalog-import/%'`.

| Option | Description | Selected |
|--------|-------------|----------|
| Return cleanup report with failures | Report failed deletes without marking the DB reset failed. | |
| Throw failure after DB reset | Throw even though DB reset already committed. | |
| Retry deletes then return partial cleanup report | Retry physical deletes and return partial cleanup failures if needed. | yes |

**User's choice:** Retry deletes then return partial cleanup report.
**Notes:** Do not roll back DB reset through a second transaction.

| Option | Description | Selected |
|--------|-------------|----------|
| Only DB-selected files | Delete only files selected from rows affected by reset. | |
| All files under catalog-import prefix | Delete every file under the catalog-import public prefix. | |
| DB-selected files plus report untracked leftovers | Delete DB-selected files and report other prefix files without deleting them. | yes |

**User's choice:** DB-selected files plus report untracked leftovers.
**Notes:** Destructive action remains scoped; leftover drift is visible.

| Option | Description | Selected |
|--------|-------------|----------|
| Extend `CatalogImportApplyResult.ResetImpact` | Mix DB counts and filesystem cleanup counts into one model. | |
| Separate reset cleanup report model | Keep DB impact separate from filesystem cleanup outcome. | yes |
| Report file only | Avoid result model changes and put cleanup outcome only in generated report. | |

**User's choice:** Separate reset cleanup report model.
**Notes:** DB reset and filesystem cleanup are separate resources and should be represented separately.

---

## Storage Key Conflicts

| Option | Description | Selected |
|--------|-------------|----------|
| Keep fail-fast on metadata mismatch | Stop import when existing `storage_key` metadata differs. | yes |
| Generate alternate storage key | Add a suffix and store a second file/row. | |
| Overwrite never, but mark row orphaned/deleted | Mutate lifecycle status and write a new row. | |

**User's choice:** Keep fail-fast on metadata mismatch.
**Notes:** Phase 3 makes this failure recoverable and cleaned, not silently hidden.

| Option | Description | Selected |
|--------|-------------|----------|
| Preflight DB conflict check before staging copy | Detect mismatches before copying files. | |
| Keep check during DB transaction | Rely on current insert/select guard. | |
| Both for clearer errors and safety | Add preflight and keep transaction-time guard for races. | yes |

**User's choice:** Both for clearer errors and safety.
**Notes:** Preflight improves UX/reporting; transaction guard remains necessary.

| Option | Description | Selected |
|--------|-------------|----------|
| Storage key plus metadata fields only | Report expected/existing metadata for the key. | |
| Storage key only | Minimal report, operator investigates manually. | |
| Storage key plus source asset info plus metadata | Include asset/original file plus expected/existing metadata. | yes |

**User's choice:** Storage key plus source asset info plus metadata.
**Notes:** Reports must avoid absolute filesystem paths.

---

## the agent's Discretion

- Exact manifest schema and naming.
- Exact retry counts/backoff.
- Exact result class names and report formatting.
- Exact helper decomposition inside `apps/catalog-import.core`.

## Deferred Ideas

- Automatic retention cleanup for old staging runs.
- Extending Phase 2 diagnostics endpoint to include staging leftovers.
- Web-based import/export.
- Local FileStorage backup/restore documentation.
