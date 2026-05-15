---
phase: 06-production-readiness-gate
plan: "01"
status: passed
created: 2026-05-15
requirements:
  - SEC-03
  - PROD-02
  - STOR-05
---

# Phase 6 Plan 01: Dependency Audit Evidence

## Scope

This audit covers the Phase 6 release-blocking dependency gate for the frontend npm dependency graph and NuGet solution dependency graph. Findings with `critical` or `high` severity block the release unless fixed or explicitly waived.

## Dirty Worktree Boundary

Known unrelated user-owned files were present before this plan and were not staged for Phase 6 audit work:

- `apps/front/src/app/about/page.tsx`
- `apps/front/src/app/delivery/page.tsx`
- `apps/front/src/app/page.tsx`
- `apps/front/src/styles/public.css`
- `apps/front/src/styles/responsive.css`
- `apps/front/src/lib/homepage/curated-product-resolver.ts`
- `apps/front/src/lib/homepage/curated-product-resolver.test.ts`
- `errors/`

No local credential or secret files were read or copied into this audit.

## npm Audit

Command:

```powershell
npm.cmd --prefix apps/front audit --json
```

Initial sandbox result:

- Exit status: `1`
- Result: registry advisory request failed against `https://registry.npmjs.org/-/npm/v1/security/advisories/bulk`
- Handling: retried with approved network access as required by Phase 6 decision D-03.

Network retry result before fix:

- Exit status: `1`
- Total vulnerable package records: `1`
- Critical: `0`
- High: `1`
- Moderate: `0`
- Low: `0`
- Affected direct package: `next`
- Resolved version before fix: `16.2.4`
- Available bounded fix: `next@16.2.6`

High advisories reported through the single `next` vulnerability record included:

- `GHSA-8h8q-6873-q5fj` - Denial of Service with Server Components, range `>=16.0.0 <16.2.5`
- `GHSA-26hh-7cqf-hhc6` - App Router segment-prefetch Middleware/Proxy bypass follow-up, range `>=16.0.0 <16.2.6`
- `GHSA-mg66-mrh9-m8jx` - Denial of Service via connection exhaustion in Cache Components, range `>=16.0.0 <16.2.5`
- `GHSA-c4j6-fc7j-m34r` - SSRF in WebSocket upgrades, range `>=16.0.0 <16.2.5`
- `GHSA-492v-c6pp-mqqv` - Middleware/Proxy bypass through dynamic route parameter injection, range `>=16.0.0 <16.2.5`
- `GHSA-267c-6grr-h53f` - App Router segment-prefetch Middleware/Proxy bypass, range `>=16.0.0 <16.2.5`
- `GHSA-36qx-fr4f-26g5` - Pages Router i18n Middleware/Proxy bypass, range `>=16.0.0 <16.2.5`

Fix applied:

```powershell
npm.cmd --prefix apps/front install next@16.2.6 --save-exact
```

Files changed:

- `apps/front/package.json`
- `apps/front/package-lock.json`

Verification after fix:

```powershell
npm.cmd --prefix apps/front audit --json
```

Result after fix:

- Exit status: `0`
- Critical: `0`
- High: `0`
- Moderate: `0`
- Low: `0`
- Total: `0`

## NuGet Vulnerability Audit

Restore command:

```powershell
dotnet restore LineCom.sln
```

Initial restore result:

- Exit status: `0`
- Result: restore completed, but some projects emitted `NU1900` because package vulnerability data could not be loaded from `https://api.nuget.org/v3/index.json`.
- Handling: retried with approved network access. The restore command still emitted intermittent `NU1900` warnings for some projects, so the dedicated vulnerable package audit command below was treated as the authoritative advisory evidence.

Audit command:

```powershell
dotnet list LineCom.sln package --vulnerable --include-transitive
```

Result before fix:

- Exit status: `0`
- Source used: `https://api.nuget.org/v3/index.json`
- `LineCom.Api`: no vulnerable packages
- `LineCom.DbMigrator`: no vulnerable packages
- `LineCom.DbMigrator.Core`: no vulnerable packages
- `LineCom.CatalogImport.Core`: no vulnerable packages
- `LineCom.CatalogImport.WinForms`: no vulnerable packages
- `LineCom.Api.Tests`: high transitive findings:
  - `System.Net.Http 4.3.0`, high, `GHSA-7jgj-8wvc-jh57`
  - `System.Text.RegularExpressions 4.3.0`, high, `GHSA-cmhx-cq75-c4mj`

Fix applied:

```powershell
dotnet add tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj package System.Net.Http --version 4.3.4
dotnet add tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj package System.Text.RegularExpressions --version 4.3.1
```

Files changed:

- `tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj`

The vulnerable packages came from the legacy test dependency graph through `NETStandard.Library/1.6.1`. The fix is intentionally limited to the test project and pins patched direct versions so the vulnerable transitive `4.3.0` packages are no longer selected.

Verification after fix:

```powershell
dotnet list LineCom.sln package --vulnerable --include-transitive
```

Result after fix:

- Exit status: `0`
- `LineCom.Api`: no vulnerable packages
- `LineCom.Api.Tests`: no vulnerable packages
- `LineCom.DbMigrator`: no vulnerable packages
- `LineCom.DbMigrator.Core`: no vulnerable packages
- `LineCom.CatalogImport.Core`: no vulnerable packages
- `LineCom.CatalogImport.WinForms`: no vulnerable packages

Optional current .NET CLI syntax check:

```powershell
dotnet package list LineCom.sln --include-transitive --vulnerable --format json
```

Result:

- Exit status: `1`
- Installed SDK does not support `dotnet package list`; it only exposes `dotnet package search`.
- Compatibility fallback used: `dotnet list LineCom.sln package --vulnerable --include-transitive`.

## Waivers

None. All critical/high findings found by the npm and NuGet audit commands were fixed with bounded dependency changes.

## Post-Fix Verification

Commands:

```powershell
npm.cmd --prefix apps/front test
dotnet test LineCom.sln
$env:LINECOM_PUBLIC_SITE_ORIGIN='https://line-com.ru'; npm.cmd --prefix apps/front run build
```

Results:

- Frontend tests: passed, `68` files, `294` tests.
- Backend tests: passed, `770` tests.
- Frontend production build: passed on Next.js `16.2.6`.
- `dotnet test` still emitted intermittent `NU1900` advisory-fetch warnings during restore for some projects; the dedicated post-fix `dotnet list LineCom.sln package --vulnerable --include-transitive` audit completed with NuGet sources and reported no vulnerable packages.

## Follow-Ups

- Keep using `dotnet list LineCom.sln package --vulnerable --include-transitive` until the installed SDK supports the newer `dotnet package list` command.
- Treat future `NU1900` restore warnings as advisory fetch failures that require retry or explicit blocked evidence.
