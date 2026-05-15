# Milestones

## v1.0 Release Stabilization (Shipped: 2026-05-15)

**Phases completed:** 6 phases, 16 plans, 55 tasks

**Audit:** `.planning/milestones/v1.0-MILESTONE-AUDIT.md` found no blocking product, integration, requirement, security, migration, storage or production-readiness gaps. Accepted process debt: explicit Nyquist `*-VALIDATION.md` artifacts exist only for Phase 3; phase verification and final release gate evidence cover all v1 requirements.

**Key accomplishments:**

- Login/register throttling with controlled `auth.rate_limited` responses and production cookie regression coverage
- Production startup/build guardrails reject unsafe public origins, blank database config, and local storage-root fallback
- Shared frontend API parsing turns malformed upstream responses into stable `transport.invalid_response` client errors
- Local FileStorage static serving is restricted to current public product and brand image prefixes without changing public URL shape.
- Read-only staff/admin storage diagnostics report compares `stored_files` metadata with Local FileStorage disk state.
- XUnit coverage proves public storage boundaries, read-only diagnostics classification, bounded details, and staff/admin endpoint access.
- Catalog import images now stage privately, promote after DB commit, and report scoped cleanup/reset outcomes.
- Catalog import storage consistency is covered by source-order, filesystem lifecycle, and report safety tests.
- Product canonical metadata now resolves to `/products/{slug}` from the API and is protected by backend and route-level frontend tests.
- Single public sitemap generation now has explicit page and product URL release limits with truncation tests and source-of-truth documentation.
- SEO/GEO regression evidence now covers helpers, robots, bounded sitemap, representative route metadata and a production frontend build with a safe origin.
- Verified reusable category picker and homepage target-search decomposition over the current dirty admin baseline
- Dependency security gate with bounded npm/NuGet fixes plus production backup/restore runbook for PostgreSQL and Local FileStorage
- Final release gate with complete v1 traceability, clean audits, passing tests/build and milestone closure artifact

---
