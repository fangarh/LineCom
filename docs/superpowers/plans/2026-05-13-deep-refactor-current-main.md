# Deep Refactor Current Main Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stabilize the current `main`, then refactor LineCom frontend/backend hotspots without changing product behavior.

**Architecture:** Work in isolated branches from current `main`; keep behavior-preserving decomposition separate from feature transfer. Treat `vault/Человекочитаемое` as the source of truth and transfer `feature/account-request-quick-view` changes only by selective patches.

**Tech Stack:** Next.js App Router 16, React 19, TypeScript, Vitest, ASP.NET Core 8, Dapper, Npgsql, DbUp, PostgreSQL.

---

### Task 1: Baseline Stabilization

**Files:**
- Modify: `apps/front/src/components/admin/homepage/admin-homepage-target-search.tsx`
- Test: `apps/front/src/components/admin/homepage/admin-homepage-manager.test.tsx`

- [ ] Reproduce current quality baseline: `npm.cmd test`, `npm.cmd run lint`, `npm.cmd run build`, `dotnet test LineCom.sln`.
- [ ] Fix the existing `react-hooks/set-state-in-effect` lint error in `admin-homepage-target-search.tsx` without changing user-facing behavior.
- [ ] Run focused homepage tests and full frontend lint.
- [ ] Re-run full frontend tests/build and backend tests.

### Task 2: CSS Decomposition

**Files:**
- Modify: `apps/front/src/app/globals.css`
- Create: `apps/front/src/styles/layout.css`
- Create: `apps/front/src/styles/public.css`
- Create: `apps/front/src/styles/account.css`
- Create: `apps/front/src/styles/admin-requests.css`
- Create: `apps/front/src/styles/admin-catalog.css`
- Create: `apps/front/src/styles/admin-homepage.css`
- Modify: `apps/front/src/app/layout.tsx`

- [ ] Move only related CSS blocks into domain stylesheets; keep selectors and declarations behavior-preserving.
- [ ] Keep `globals.css` for tokens, reset, shared typography, buttons, forms, status pills, and global media rules.
- [ ] Import global styles from App Router layout-compatible imports.
- [ ] Run `npm.cmd run lint`, `npm.cmd test`, and `npm.cmd run build`.

### Task 3: Auth/Header Quick-View Transfer

**Files:**
- Modify: `apps/api/Modules/Auth/Controllers/AuthController.cs`
- Modify: `tests/LineCom.Api.Tests/Modules/Auth/AuthLoginEndpointTests.cs`
- Modify: `apps/front/src/lib/api/auth.ts`
- Modify: `apps/front/src/components/auth/auth-provider.tsx`
- Modify: `apps/front/src/components/auth/auth-provider.test.tsx`
- Modify: `apps/front/src/components/layout/site-header.tsx`
- Modify: `apps/front/src/components/layout/site-header.test.tsx`

- [ ] Add CSRF-protected logout endpoint and backend tests.
- [ ] Add frontend logout API and tests.
- [ ] Integrate logout/user display into the current `AuthProvider` model, preserving `initialSession`, `status`, and stale restore guards.
- [ ] Keep the current role-aware admin dropdown and draft badge behavior in `SiteHeader`.
- [ ] Run focused auth/header tests, frontend quality commands, and backend tests.

### Task 4: Request Quick Preview Transfer

**Files:**
- Create: `apps/front/src/components/account/request-preview-drawer.tsx`
- Create: `apps/front/src/components/admin/admin-request-preview-drawer.tsx`
- Modify: `apps/front/src/app/account/requests/requests-page-client.tsx`
- Modify: `apps/front/src/app/account/requests/requests-page-client.test.tsx`
- Modify: `apps/front/src/components/account/request-list.tsx`
- Modify: `apps/front/src/components/account/request-list.test.tsx`
- Modify: `apps/front/src/app/admin/requests/requests-page-client.tsx`
- Modify: `apps/front/src/app/admin/requests/requests-page-client.test.tsx`
- Modify: `apps/front/src/components/admin/admin-request-list.tsx`
- Modify: `apps/front/src/components/admin/admin-request-list.test.tsx`
- Modify: relevant extracted CSS from Task 2

- [ ] Add customer and admin preview drawers using current API types.
- [ ] Add preview open/close and stale response guards to list pages.
- [ ] Preserve existing detail-page routes as the full workflow.
- [ ] Run focused request tests and full frontend checks.

### Task 5: Admin Catalog Frontend Decomposition

**Files:**
- Modify: `apps/front/src/components/admin/catalog/admin-attribute-manager.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-brand-manager.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-category-manager.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-product-manager.tsx`
- Modify: `apps/front/src/lib/api/admin-catalog.ts`
- Create focused sibling components and helpers under `apps/front/src/components/admin/catalog/`
- Create focused API files under `apps/front/src/lib/api/admin-catalog/`

- [ ] Split stateful containers from presentational panels/forms/tables.
- [ ] Move pure mapping, payload building, reorder, merge, and normalization logic into helper modules with tests.
- [ ] Keep public imports stable until all consumers are migrated.
- [ ] Run focused catalog tests after each manager split.

### Task 6: Backend Catalog/Account Decomposition

**Files:**
- Modify: `apps/api/Modules/Catalog/Services/AdminCatalogProductService.cs`
- Modify: `apps/api/Modules/Catalog/Repositories/AdminCatalogProductSql.cs`
- Modify: `apps/api/Modules/Catalog/Repositories/DapperAdminCatalogProductRepository.cs`
- Modify related Catalog service/repository tests
- Review Account module files after logout/password integration

- [ ] Split large Catalog service logic only where responsibilities are already clear.
- [ ] Keep Dapper, Npgsql, DbUp, and SQL migration approach unchanged.
- [ ] Preserve Local FileStorage behavior.
- [ ] Run backend focused tests and full `dotnet test LineCom.sln`.

### Task 7: Final Verification

- [ ] Confirm `feature/account-request-quick-view` was not merged directly.
- [ ] Confirm `stash@{0}` was not applied or dropped.
- [ ] Run `npm.cmd test`, `npm.cmd run lint`, `npm.cmd run build`, and `dotnet test LineCom.sln`.
- [ ] Check for temporary markers, unfinished comments, security gaps, and migration risks.
- [ ] Summarize changed files, verification evidence, and remaining decisions.
