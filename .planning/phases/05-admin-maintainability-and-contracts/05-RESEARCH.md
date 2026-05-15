# Phase 5: Admin Maintainability And Contracts - Research

**Researched:** 2026-05-15
**Mode:** inline fallback because GSD subagents are not installed in this runtime
**Status:** Complete

## Research Inputs

- `.planning/phases/05-admin-maintainability-and-contracts/05-CONTEXT.md`
- `.planning/ROADMAP.md`
- `.planning/REQUIREMENTS.md`
- `.planning/codebase/ARCHITECTURE.md`
- `.planning/codebase/CONCERNS.md`
- `.planning/codebase/TESTING.md`
- `.planning/codebase/CONVENTIONS.md`
- `.planning/codebase/STRUCTURE.md`
- Current `git status --short` and targeted `git diff` for Phase 5 admin files

## Dirty Baseline Findings

The worktree already contains user-owned changes before Phase 5 execution. Plans must treat these as baseline until an executor intentionally changes them.

Relevant admin maintainability surfaces:

- `apps/front/src/components/admin/catalog/admin-category-parent-picker.tsx` already extracts a reusable `AdminCategoryTreePicker` from the parent picker.
- `apps/front/src/components/admin/catalog/admin-category-tree-helpers.ts` adds `hasChildren` to flattened category nodes.
- `apps/front/src/components/admin/catalog/admin-product-main-fields.tsx` switches product category selection from a flat `<select>` to the reusable tree picker and disables parent categories.
- `apps/front/src/components/admin/catalog/admin-product-main-fields.test.tsx` is untracked and covers product category picker behavior.
- `apps/front/src/components/admin/catalog/admin-product-manager.test.tsx` has pending changes for leaf-category selection.
- `apps/front/src/components/admin/homepage/admin-homepage-section-editor.tsx` passes added product/category IDs to target search.
- `apps/front/src/components/admin/homepage/admin-homepage-target-search.tsx` disables already-added target buttons and keeps long target names out of visible button text while preserving accessible labels.
- `apps/front/src/components/admin/homepage/admin-homepage-manager.test.tsx` has pending tests for duplicate target prevention and compact add button text.

Relevant contract/backend surfaces:

- `apps/api/Modules/Catalog/Repositories/AdminCatalogProductSql.cs` and `DapperAdminCatalogProductRepository.cs` have pending behavior to clear product attribute values when the product category changes.
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductAttributeRepositoryDatabaseTests.cs` has pending database coverage for the category-change attribute cleanup.
- `apps/api/Modules/Catalog/Repositories/AdminHomepageRepositorySql.cs` has pending SQL behavior returning an existing homepage section item when a duplicate product/category target is added.
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminHomepageRepositorySqlTests.cs` has pending SQL text coverage for duplicate target handling.

Unrelated dirty baseline that Phase 5 plans must not touch:

- Public pages and public styling: `apps/front/src/app/about/page.tsx`, `apps/front/src/app/delivery/page.tsx`, `apps/front/src/app/page.tsx`, `apps/front/src/styles/public.css`, `apps/front/src/styles/responsive.css`.
- Untracked public homepage helper work: `apps/front/src/lib/homepage/curated-product-resolver.ts` and its test.
- Screenshot artifacts under `errors/`.

## Planning Implications

1. Phase 5 should be planned as three safe steps matching ROADMAP:
   - `05-01`: finish narrow admin UI decomposition around the dirty category/product/homepage surfaces.
   - `05-02`: extract/lock pure helpers and focused frontend tests for mapping, selection and duplicate-target logic.
   - `05-03`: add lightweight frontend/backend API contract drift checks for admin catalog/homepage.
2. Execution should not make `admin-attribute-manager.tsx` or `admin-brand-manager.tsx` mandatory targets. They remain out of scope unless a selected helper/test directly requires them.
3. Contract drift should be enforced through existing xUnit and Vitest patterns:
   - Frontend API-client tests assert endpoint path, method, credentials, CSRF, request body and critical response fixture shape.
   - Backend endpoint/serialization tests assert required DTO fields on admin catalog/homepage responses.
   - SQL/repository tests may cover behavior behind those contracts, but Phase 5 must not introduce generated OpenAPI infrastructure.
4. Every execution plan touching dirty files must start with targeted `git diff -- <paths>` and must stage only executor-owned deltas.

## Recommended Verification Surface

- Frontend focused tests:
  - `npm.cmd --prefix apps/front test -- src/components/admin/catalog/admin-product-main-fields.test.tsx src/components/admin/catalog/admin-product-manager.test.tsx`
  - `npm.cmd --prefix apps/front test -- src/components/admin/homepage/admin-homepage-manager.test.tsx`
  - `npm.cmd --prefix apps/front test -- src/lib/api/admin-catalog.test.ts src/lib/api/admin-homepage.test.ts`
- Backend focused tests:
  - `dotnet test tests/LineCom.Api.Tests/LineCom.Api.Tests.csproj --filter "FullyQualifiedName~AdminCatalogProductsEndpointTests|FullyQualifiedName~AdminHomepageEndpointTests|FullyQualifiedName~AdminCatalogProductAttributeRepositoryDatabaseTests|FullyQualifiedName~AdminHomepageRepositorySqlTests"`
- Line-count/maintainability checks:
  - Inspect touched admin containers after decomposition and avoid adding behavior to already-large mixed orchestration/rendering files.

## Research Complete

Phase 5 can proceed with three executable plans. The plans should be narrow, dirty-diff aware, and test-focused.
