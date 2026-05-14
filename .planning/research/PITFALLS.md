# Research: Pitfalls

**Date:** 2026-05-14

## Pitfall 1: Public Storage Leakage

**Risk:** Serving the full storage root can expose future import/export/temp artifacts.

**Warning signs:**
- New file purposes are added to `stored_files` without access policy.
- Files are written under the same root and become reachable through `/storage`.

**Prevention:**
- Serve only public image prefixes or move non-public access behind authorized controllers.
- Add tests for public/private storage paths.

## Pitfall 2: Database/File Drift

**Risk:** Catalog import or image upload can leave DB rows without files, or files without DB rows.

**Warning signs:**
- File copy happens before DB commit without staging or cleanup.
- Reset operations remove DB rows but not physical files.

**Prevention:**
- Add storage integrity scan and retention-based cleanup.
- Use staging/promotion patterns for import files.

## Pitfall 3: SEO Origin Misconfiguration

**Risk:** Production metadata, sitemap or robots can publish localhost URLs.

**Warning signs:**
- Missing `LINECOM_PUBLIC_SITE_ORIGIN` does not fail production checks.
- Tests only cover helper output, not production config assumptions.

**Prevention:**
- Add startup/build verification for production origin.
- Add route-level tests for canonical, sitemap and robots behavior.

## Pitfall 4: Auth Abuse Without Throttling

**Risk:** Login/register endpoints allow brute-force or signup abuse up to password verification/account creation.

**Warning signs:**
- No ASP.NET Core rate limiting policies on auth endpoints.
- No 429 tests for repeated attempts.

**Prevention:**
- Add built-in ASP.NET Core rate limiting policies and tests.
- Keep password hashing strong; do not weaken hashing to improve throughput.

## Pitfall 5: Admin UI Becomes Harder To Change

**Risk:** Large stateful admin containers become the place where every new behavior is added.

**Warning signs:**
- Components exceed the project decomposition threshold and mix async loading, mutation guards, mapping and rendering.

**Prevention:**
- Split containers before feature expansion.
- Move pure mapping/payload logic to helper modules with unit tests.

## Pitfall 6: Contract Drift

**Risk:** Handwritten frontend API clients drift from backend DTOs.

**Warning signs:**
- Backend DTO fields change without frontend tests failing.
- Swagger exists only in dev runtime and is not used as a contract artifact.

**Prevention:**
- Add contract tests or generated/validated OpenAPI artifact for critical DTO surfaces.
