# QA DB + Playwright Happy Path Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the current browser-QA blocker by applying the release database migrations to the configured QA database and rerunning the full catalog-to-request happy path with Playwright.

**Architecture:** Keep database schema changes inside the existing DbUp migrator and SQL migrations. Use the repository-local secret files only as runtime inputs, without printing connection strings or passwords. Verify through backend API behavior and the existing Next.js frontend proxy before updating iteration notes.

**Tech Stack:** ASP.NET Core API, DbUp, PostgreSQL, Npgsql, Next.js App Router, Playwright MCP, Vitest, xUnit.

---

## Source Context

- Main iteration note: `vault/Человекочитаемое/Frontend Auth Request Flow iterations.md`
- Frontend plan: `docs/superpowers/plans/2026-05-07-frontend-auth-request-flow.md`
- Migrator entry point: `apps/dbmigrator/Program.cs`
- Migrations:
  - `apps/dbmigrator/Migrations/001_extensions.sql`
  - `apps/dbmigrator/Migrations/002_catalog_foundation.sql`
  - `apps/dbmigrator/Migrations/003_auth_users_organizations.sql`
  - `apps/dbmigrator/Migrations/004_requests.sql`
- Backend local config: `apps/api/appsettings.Local.json`
- Frontend local config: `apps/front/.env.local`

## Task 1: Preflight Secret And Migration State

**Files:**
- Read only: `apps/api/appsettings.Local.json`
- Read only: `apps/front/.env.local`
- Read only: `apps/dbmigrator/Program.cs`

- [x] **Step 1: Confirm local connection secrets exist without printing values**

Run:

```powershell
$apiLocal = Get-Content apps/api/appsettings.Local.json -Raw | ConvertFrom-Json
$frontEnv = Get-Content apps/front/.env.local
[pscustomobject]@{
  ApiHasDefault = -not [string]::IsNullOrWhiteSpace($apiLocal.ConnectionStrings.Default)
  ApiHasPassword = $apiLocal.ConnectionStrings.Default -match 'Password='
  FrontHasMigratorConnection = [bool]($frontEnv | Where-Object { $_ -match '^LINECOM_CONNECTION_STRING=' })
  FrontHasApiOrigin = [bool]($frontEnv | Where-Object { $_ -match '^LINECOM_API_ORIGIN=' })
}
```

Expected: all four fields are `True`.

- [x] **Step 2: Review migrator behavior**

Confirm `apps/dbmigrator/Program.cs` reads `LINECOM_CONNECTION_STRING` and uses `schema_versions` as DbUp journal.

Expected: no schema edits are needed before trying the migrator.

## Task 2: Apply Release Migrations

**Files:**
- Read only: `apps/front/.env.local`
- Execute: `apps/dbmigrator/LineCom.DbMigrator.csproj`

- [x] **Step 1: Run DbUp migrator using the local connection string**

Run from repository root without echoing the secret:

```powershell
$line = Get-Content apps/front/.env.local | Where-Object { $_ -match '^LINECOM_CONNECTION_STRING=' } | Select-Object -First 1
$env:LINECOM_CONNECTION_STRING = ($line -split '=', 2)[1]
dotnet run --project apps/dbmigrator/LineCom.DbMigrator.csproj
```

Expected: either `Database migrations applied successfully.` or a DbUp report that no scripts need to run.

Progress note 2026-05-07: `.env.local` contains a `LINECOM_CONNECTION_STRING` entry, but its raw env-line value was
not directly parseable by Npgsql. Running DbUp with `ConnectionStrings:Default` from `apps/api/appsettings.Local.json`
connected successfully after network escalation and applied:

- `LineCom.DbMigrator.Migrations.003_auth_users_organizations.sql`;
- `LineCom.DbMigrator.Migrations.004_requests.sql`.

- [x] **Step 2: If migration fails because existing catalog tables are not journaled**

Stop and report the exact failure category. Do not manually insert rows into `schema_versions` or drop/recreate tables without explicit user approval.

Progress note 2026-05-07: this branch was not needed. DbUp journal existed and only pending migrations 003 and 004 ran.

## Task 3: API Smoke Verification

**Files:**
- Read only: `apps/front/.env.local`
- Execute: `apps/api/LineCom.Api.csproj`

- [x] **Step 1: Start backend on `http://127.0.0.1:8080`**

Run with the local connection string loaded from `apps/front/.env.local`.

Expected: backend listens on `http://127.0.0.1:8080` and logs no startup exception.

Progress note 2026-05-07: backend started on `http://127.0.0.1:8080`. ASP.NET DataProtection logged DPAPI warnings
for old keys, but the application continued listening.

- [x] **Step 2: Confirm public catalog endpoint responds**

Open `http://127.0.0.1:8080/api/public/catalog/categories`.

Expected: HTTP 200 with category JSON.

Progress note 2026-05-07: `GET /api/public/catalog/categories` returned HTTP 200 and included `vitaya-para`.

- [x] **Step 3: Confirm auth registration no longer fails on missing `users` table**

Use the frontend Playwright flow in Task 4 for the definitive check. If an API response still returns `500 internal_error`, inspect backend logs and report the database exception category.

Progress note 2026-05-07: Playwright registration succeeded after migrations. The next blockers were backend mapping bugs,
not missing tables:

- Dapper constructor materialization failed because Npgsql reads `timestamptz` as `DateTime`, while request row records used `DateTimeOffset`.
- `Location` header failed for Cyrillic request numbers until `response.Number` was URL-encoded in `Created(...)`.

## Task 4: Playwright Happy Path

**Files:**
- Execute: `apps/front`

- [x] **Step 1: Start frontend dev server**

Run:

```powershell
npm.cmd run dev
```

Expected: Next.js serves the app on an available localhost port.

Progress note 2026-05-07: an old frontend was found on `3004`, but its API proxy returned 404 for the QA product. A fresh
Next.js dev server was started on `http://127.0.0.1:3010` with `LINECOM_API_ORIGIN=http://127.0.0.1:8080`.

- [x] **Step 2: Run Playwright browser flow**

With Playwright:

1. Open `/`.
2. Navigate to `/catalog`.
3. Open category `/catalog/vitaya-para`.
4. Open product `/products/u-utp-cat-5e`.
5. Add the product to the request draft.
6. Open `/request`.
7. Submit unauthenticated and confirm redirect to `/auth/login?returnTo=%2Frequest`.
8. Open registration, create a unique QA user, and return to `/request`.
9. Submit the request.
10. Confirm redirect to `/account/requests/<number>`.
11. Open `/account/requests` and confirm the created request appears.

Expected:

- no blank pages;
- no horizontal overflow at desktop or mobile width;
- visible copy uses request language, not order/payment language;
- created request number appears in detail and list views.

Progress note 2026-05-07: Playwright happy path passed on `http://127.0.0.1:3010`.

- QA user: `qa-1778178414535@example.com`.
- Created request: `ЗК26-0002`.
- Visited routes: `/`, `/catalog`, `/catalog/vitaya-para`, `/products/u-utp-cat-5e`, `/request`,
  `/auth/login?returnTo=%2Frequest`, `/auth/register?returnTo=%2Frequest`, `/account/requests/%D0%97%D0%9A26-0002`,
  `/account/requests`.
- Mobile checks at `390px` covered `/catalog`, `/catalog/vitaya-para`, `/products/u-utp-cat-5e`, `/request`,
  `/account/requests`, `/account/requests/%D0%97%D0%9A26-0002`.
- Desktop and mobile scroll width matched viewport width; no horizontal overflow was detected.
- Browser console had no errors. Two `GET /api/auth/me net::ERR_ABORTED` entries were observed during navigation after
  route transitions; they did not block the flow.

## Task 5: Regression Checks And Documentation

**Files:**
- Modify: `vault/Человекочитаемое/Frontend Auth Request Flow iterations.md`
- Modify: `docs/superpowers/plans/2026-05-07-qa-db-playwright-happy-path.md`

- [x] **Step 1: Run code checks**

Run:

```powershell
dotnet build LineCom.sln -m:1
dotnet test LineCom.sln -m:1
npm.cmd run lint
npm.cmd test
npm.cmd run build
```

Expected: all commands pass. Known acceptable warning: NuGet vulnerability feed `NU1900` if the feed is unavailable.

Progress note 2026-05-07:

- `dotnet build LineCom.sln -m:1` passed with `NU1900` warnings only.
- `dotnet test LineCom.sln -m:1` passed: 289 tests.
- `npm.cmd run lint` passed.
- `npm.cmd test` passed: 17 files, 42 tests.
- `npm.cmd run build` passed.

- [x] **Step 2: Search for forbidden commerce language and debt markers**

Run:

```powershell
rg -n "Купить|Оформить заказ|оплат|цена \d|TODO|TBD|FIXME|заглуш|костыл" apps/front docs/superpowers/plans docs/superpowers/specs vault/Человекочитаемое
```

Expected: no forbidden commerce language in `apps/front`; documentation matches are only rule text or historical blocker notes.

Progress note 2026-05-07: no forbidden commerce language was found in `apps/front`. Matches were documentation rule text,
historical blocker notes, one planned search command in this plan, and one accidental `package-lock.json` integrity substring.

- [x] **Step 3: Update iteration notes**

Record:

- whether migrations were applied;
- exact Playwright route list;
- created request number if the flow reaches success;
- any remaining blocker with concrete backend/database error category.

## Self-Review

Spec coverage:

- The plan targets the documented current continuation point: applying migrations or connecting a migrated database, then repeating the full happy path.
- It does not add new product scope, prices, payments, or anonymous request submission.
- It keeps secrets out of console output and documentation.
- It stops before destructive or manual database repair actions.

No placeholders are intentionally left in implementation steps.
