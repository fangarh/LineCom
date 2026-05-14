# Research Summary

**Date:** 2026-05-14

## Key Findings

**Stack:** Existing stack is coherent and should be preserved: ASP.NET Core/.NET 8, PostgreSQL, Npgsql/Dapper, DbUp SQL migrations, Next.js App Router, xUnit, Vitest and Local FileStorage.

**Table stakes:** release hardening should cover auth throttling, production configuration checks, storage access boundaries, DB/file lifecycle diagnostics, frontend API error normalization, SEO/GEO route verification and contract drift checks.

**Watch out for:** public storage leakage, database/file drift, localhost canonical/sitemap URLs in production, auth abuse without rate limiting, large admin UI containers and frontend/backend DTO drift.

## Roadmap Implications

- First phase should deliver auth/config/API-error safety because it is release-critical and comparatively contained.
- Storage should be its own phase because it crosses DB, filesystem, API static serving, import tooling and operations.
- SEO/GEO should be its own phase because it crosses Next.js metadata, sitemap, robots, public catalog data and production environment.
- Admin maintainability should precede more admin feature work.
- Deferred product expansions should remain visible but not become release-stabilization blockers.

## Sources Used

- `.planning/codebase/STACK.md`
- `.planning/codebase/ARCHITECTURE.md`
- `.planning/codebase/CONCERNS.md`
- `vault/Человекочитаемое`
- Context7 ASP.NET Core docs for rate limiting, cookies and middleware order
- Context7 Next.js App Router docs for metadata, robots and sitemap APIs
