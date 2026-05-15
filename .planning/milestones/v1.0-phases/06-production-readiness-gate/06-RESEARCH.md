# Phase 6 Research: Production Readiness Gate

**Date:** 2026-05-15
**Status:** Ready for planning

## Scope

Phase 6 is a release-readiness gate, not a feature phase. It covers:

- `SEC-03`: dependency vulnerability audit for npm and NuGet packages.
- `PROD-02`: deployment documentation for API, frontend, DbUp migrator, PostgreSQL and Local FileStorage.
- `STOR-05`: backup/restore expectations for coordinated PostgreSQL plus Local FileStorage recovery.
- `VER-01`: requirement traceability and verification evidence.
- `VER-02`: final GSD verification for technical debt, security gaps, migration risks and maintainability risks.

## Current Project Evidence

- `vault/Человекочитаемое/Production deployment line-com.ru.md` already documents the production host, domain/DNS, nginx/TLS, systemd services, release directories, API/frontend config paths and Local FileStorage root.
- `.planning/codebase/INTEGRATIONS.md` confirms production hosting is a single Ubuntu server with nginx reverse proxy, ASP.NET Core Runtime 8, Node.js, PostgreSQL 16, systemd and Local FileStorage at `/var/lib/linecom/storage`.
- `.planning/codebase/CONCERNS.md` identifies dependency audit as not fully checked because NuGet advisory data was unavailable during prior test restore, and identifies Local FileStorage backup/restore posture as the remaining release hardening item.
- Phase 1-5 verification files provide the prior evidence that Phase 6 should aggregate rather than rediscover.

## Dependency Audit Findings

Context7 npm CLI docs:

- `npm audit` submits the dependency tree to the registry for vulnerability scanning.
- `npm audit --json` emits a detailed machine-readable report.
- `npm audit --audit-level=high` changes the exit threshold while still reporting all vulnerabilities.
- `npm audit` exits non-zero by default when vulnerabilities are found.

Recommended npm command for this repo:

```powershell
npm.cmd --prefix apps/front audit --json
```

If execution needs severity-specific gate behavior:

```powershell
npm.cmd --prefix apps/front audit --audit-level=high --json
```

Context7 .NET CLI docs:

- Current docs describe `dotnet package list --include-transitive --vulnerable --format json` for transitive vulnerability reporting.
- The docs note the noun-first `dotnet package list` command is a current syntax; on earlier SDKs, restore/build assets may need to exist first.

Recommended .NET commands for this repo:

```powershell
dotnet restore LineCom.sln
dotnet list LineCom.sln package --vulnerable --include-transitive
```

If the installed SDK supports the current noun-first command and JSON output:

```powershell
dotnet package list LineCom.sln --include-transitive --vulnerable --format json
```

Plan execution should treat network/advisory registry failures as retriable with network access. If the registry remains unavailable, record blocked evidence and a follow-up instead of silently passing.

## Production Runbook Requirements

The runbook should update `vault/Человекочитаемое/Production deployment line-com.ru.md` rather than duplicating the operational truth in `.planning`.

Required additions:

- Release artifact build/publish checklist for API, frontend and DbUp migrator.
- Environment/config checklist for `/etc/linecom/api.env`, `/etc/linecom/front.env`, PostgreSQL connection, public origins and storage root.
- DbUp migration execution and rollback/stop conditions.
- Coordinated backup point procedure:
  - PostgreSQL dump;
  - Local FileStorage archive from `/var/lib/linecom/storage`;
  - metadata tying both artifacts to the same release/backup point.
- Dry-run restore procedure to a separate host/database/storage path.
- Post-restore smoke checks for API health, frontend, `/api/public/...`, `/storage/products/...`, robots and sitemap.

## Final Verification Requirements

Required gate:

- Full backend test run.
- Frontend test run.
- Frontend production build with production-safe public origin.
- Phase-specific commands from prior verification files where they are materially different from full runs.
- `gsd-sdk.cmd query verify.schema-drift 06`.
- Requirement traceability table from all v1 requirements to phase verification evidence.
- Explicit dirty-worktree note for unrelated user-owned files.

Potential commands:

```powershell
dotnet test LineCom.sln
npm.cmd --prefix apps/front test
$env:LINECOM_PUBLIC_SITE_ORIGIN='https://line-com.ru'; npm.cmd --prefix apps/front run build
gsd-sdk.cmd query verify.schema-drift 06
```

## Planning Implications

- Split Phase 6 into the roadmap's two plans:
  - `06-01`: dependency audit plus production deployment/storage documentation.
  - `06-02`: final verification, traceability and milestone closure.
- `06-02` should depend on `06-01` because final closure needs the audit/runbook results.
- Do not stage unrelated dirty public page/style, public homepage resolver or `errors/` files.
- If dependency fixes are needed, keep them narrowly scoped and commit them separately from planning/docs artifacts during execution.
