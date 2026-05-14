# Phase 02 Research: Storage Access And Diagnostics

**Researched:** 2026-05-14
**Mode:** inline fallback because GSD SDK reports subagents as missing
**Scope:** Phase 2 only

## Summary

Phase 2 can be implemented inside the existing ASP.NET Core API without schema changes and without changing public catalog DTOs. The current storage keys already use `storage/products/...` and `storage/brands/...`, and public URLs are emitted as `"/" + stored_file.storage_key`, so preserving public catalog image URLs means keeping `/storage/products/...` and `/storage/brands/...` available.

The main risk is `apps/api/Infrastructure/Hosting/LocalStorageStaticFilesExtensions.cs`: it currently maps the whole configured storage root to `/storage`, which also exposes future `import_source`, `export_result`, and `temp` files if they are written under the same root. The narrowest Phase 2 fix is to replace the broad root mapping with two public static registrations, one for `products` and one for `brands`, while keeping the external request paths unchanged.

Diagnostics should be read-only and backend-only. The existing module pattern favors thin controllers, service/repository separation, Dapper SQL outside controllers, and staff authorization through `IAdminCatalogStaffGuard`. A storage diagnostics contour can follow the catalog admin module pattern without adding frontend UI.

## Existing Implementation Findings

### Public Storage Serving

- `UseLocalStorageStaticFiles` resolves `Storage:RootPath`, creates the root directory, and serves it through one `UseStaticFiles` registration at `/storage`.
- `LocalStoredFileWriter` stores keys with the `storage/` prefix and resolves physical paths under the configured root. Saved product/brand images are stored below directories passed by services, such as `products/...` and `brands/...`.
- Catalog SQL emits public image URLs as `"/" || stored_file.storage_key`, so current URLs depend on `/storage/products/...` and `/storage/brands/...`.

### Storage Metadata

- `stored_files` has `storage_key`, `purpose`, `status`, `size_bytes`, `checksum`, `created_by_user_id`, and `created_at`.
- Allowed purposes are `product_image`, `brand_logo`, `import_source`, `export_result`, and `temp`.
- Allowed statuses are `active`, `deleted`, and `orphaned`.
- Product/brand SQL already filters public usage to active product images and brand logos.

### Authorization and API Shape

- Admin controllers use `[Authorize]` and delegate role checks to services through `IAdminCatalogStaffGuard`.
- Existing endpoint tests use `LineComWebApplicationFactory`, fake auth login services, and JSON assertions for `401`/`403` behavior.
- Mutation endpoints use CSRF, but diagnostics are read-only and should be `GET` only.

## Recommended Plan Shape

1. Public/private static storage boundary
   - Add a small storage-root resolver/policy helper if useful.
   - Serve only `/storage/products` from `<root>/products` and `/storage/brands` from `<root>/brands`.
   - Do not change `stored_files.storage_key`, frontend paths, or catalog DTOs.

2. Read-only diagnostics service and admin endpoint
   - Add DTOs for summary counts plus bounded detail lists.
   - Add Dapper query/repository for `stored_files`.
   - Add a filesystem scanner under `Storage:RootPath`.
   - Compare DB rows with disk files using relative storage keys only.
   - Expose `GET /api/admin/storage/diagnostics`, protected by staff/admin access.

3. Tests
   - Static file integration tests: public product/brand file returns `200`, private-like `import`, `export`, and `temp` paths return `404`.
   - Diagnostic service/repository tests: missing files, untracked files, stale deleted rows, orphaned rows, bounded details/truncation, no absolute paths.
   - Endpoint authorization tests: anonymous `401`, customer `403`, seller/admin `200`.

## Constraints for Execution

- Do not add import staging, promotion, cleanup, or reset behavior; those belong to Phase 3.
- Do not add destructive cleanup endpoints, file deletion, DB updates, or row marking in Phase 2 diagnostics.
- Do not expose absolute filesystem paths in diagnostic API responses.
- Do not introduce Entity Framework or migrations unless execution discovers an unavoidable schema need; none is expected from research.
- Preserve unrelated dirty worktree changes in `apps/`, `tests/`, and `errors/`.

## Verification Focus

- `STOR-01`: anonymous static access is limited to current public image directories.
- `STOR-02`: non-public storage paths cannot be fetched anonymously through `/storage`.
- `STOR-03`: diagnostics report DB/disk drift categories with full counts, bounded details, and relative path data only.

## Research Complete

Proceed with three Phase 2 plans matching `ROADMAP.md`:

- `02-01`: Public/private storage serving policy.
- `02-02`: Storage integrity diagnostic model and report.
- `02-03`: Storage boundary and diagnostic tests.
