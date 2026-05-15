# Phase 5: Admin Maintainability And Contracts - Context

**Gathered:** 2026-05-15
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 5 reduces fragility in the existing admin catalog/homepage code and adds lightweight contract-drift checks for critical admin API surfaces.

This phase is limited to release-stabilization maintainability work around admin catalog/homepage areas already touched in the current dirty workspace, plus directly related helper extraction, focused tests and critical frontend/backend shape checks. It does not add new admin product capabilities, SEO landing pages, product comparison, web import/export, generated OpenAPI infrastructure, broad production-readiness docs, or a full rewrite of all admin managers.

</domain>

<decisions>
## Implementation Decisions

### Decomposition scope
- **D-01:** Phase 5 decomposition scope is restricted to current dirty admin areas and directly related files. The planner should not make all large admin containers mandatory targets.
- **D-02:** The likely target areas are the currently changed product/category/homepage files: `admin-product-*`, `admin-category-*`, `admin-homepage-*`, associated backend catalog/homepage repositories/SQL, and matching tests.
- **D-03:** `admin-attribute-manager.tsx` and `admin-brand-manager.tsx` are not mandatory Phase 5 targets unless research proves they are required for a selected helper extraction or contract check.

### Dirty-worktree handling
- **D-04:** Existing dirty changes are user-owned baseline. Do not revert them and do not assume they belong to the executor.
- **D-05:** Plans must require executors to inspect relevant `git diff` before editing dirty files and to stage/commit only the Phase 5 changes they intentionally make.
- **D-06:** If a pre-existing dirty change is needed as a dependency for a Phase 5 task, the plan must name it as an assumption/dependency instead of silently absorbing unrelated work.

### Contract drift gate
- **D-07:** Phase 5 uses lightweight critical shape tests, not a generated OpenAPI contract framework.
- **D-08:** Contract checks should focus on admin catalog/homepage surfaces where handwritten frontend API types mirror backend DTOs and endpoints.
- **D-09:** Sufficient checks include focused backend endpoint/serialization assertions and frontend API-client tests that fail on missing required fields, wrong endpoint paths, CSRF/credentials regressions, or critical enum/status shape drift.
- **D-10:** Generated OpenAPI artifacts, broad frontend/backend contract infrastructure and a full DTO-generation workflow are deferred outside Phase 5.

### Helper extraction priority
- **D-11:** Helper extraction priority should be derived from the actual dirty diff, with mandatory focus on pure helpers and focused unit tests.
- **D-12:** Candidate helper areas include payload builders and DTO/form mapping, category tree/reorder/parent-picker logic, homepage target resolution, and form-field normalization.
- **D-13:** The planner should prefer narrow, behavior-preserving decomposition before adding tests or contract checks to a large mixed orchestration/rendering file.

### the agent's Discretion
- Exact file split boundaries and helper names, provided they stay close to existing naming conventions and keep stateful containers focused on loading, mutations, async guards and data flow.
- Exact contract-test mechanism, provided it remains lightweight, automated and tied to critical admin catalog/homepage DTO/endpoint behavior.
- Exact wave split, provided dirty-worktree handling is explicit in every plan that touches dirty files.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project and phase scope
- `.planning/PROJECT.md` — project constraints, dirty-worktree rule, frontend decomposition rule, Dapper/DbUp constraints and source-of-truth rule.
- `.planning/REQUIREMENTS.md` — Phase 5 requirements `MAIN-01`, `MAIN-02`, `MAIN-03`.
- `.planning/ROADMAP.md` — Phase 5 goal, success criteria and planned split.
- `.planning/STATE.md` — current workflow state and Phase 5 position.

### Prior phase context
- `.planning/phases/04-public-seo-geo-reliability/04-VERIFICATION.md` — confirms Phase 4 complete and that broad contract drift checks were deferred to Phase 5.
- `.planning/phases/04-public-seo-geo-reliability/04-CONTEXT.md` — prior decision `D-16`/`D-17`/`D-18` around focused regression surfaces and deferred broad contract framework.
- `.planning/phases/01-release-safety-baseline/01-03-SUMMARY.md` — frontend API error normalization and existing API client test patterns.

### Codebase map
- `.planning/codebase/ARCHITECTURE.md` — admin catalog mutation flow, frontend API layer and backend module boundaries.
- `.planning/codebase/CONCERNS.md` — large admin container fragility and handwritten frontend API contract drift concerns.
- `.planning/codebase/TESTING.md` — Vitest/xUnit patterns, frontend API client tests and backend endpoint test patterns.
- `.planning/codebase/CONVENTIONS.md` — frontend helper/component split conventions and backend Dapper/repository conventions.
- `.planning/codebase/STRUCTURE.md` — admin component, API client, backend DTO/controller and test file locations.

### Source-of-truth docs
- `vault/Человекочитаемое/Сквозные требования.md` — no intentional technical debt and maintainability expectations.
- `vault/Человекочитаемое/Admin Homepage Management API.md` — admin homepage contract if plans touch homepage DTO/API shape.
- `vault/Человекочитаемое/Public Catalog API.md` — public catalog contract only if admin changes intersect public catalog assumptions.
- `vault/Человекочитаемое/Архитектура backend и БД.md` — backend/data-access constraints for Dapper, DTOs and API boundaries.

### Likely implementation surfaces
- `apps/front/src/lib/api/admin-catalog.ts` — handwritten frontend admin catalog API contract.
- `apps/front/src/lib/api/admin-catalog.test.ts` — existing frontend admin catalog API-client test pattern.
- `apps/front/src/lib/api/admin-homepage.ts` — handwritten frontend admin homepage API contract.
- `apps/front/src/lib/api/admin-homepage.test.ts` — existing frontend admin homepage API-client test pattern.
- `apps/front/src/components/admin/catalog/admin-product-manager.tsx` — large product admin orchestration container.
- `apps/front/src/components/admin/catalog/admin-product-manager-helpers.ts` — existing product helper extraction target.
- `apps/front/src/components/admin/catalog/admin-category-parent-picker.tsx` — currently dirty category parent-picker UI/helper-adjacent surface.
- `apps/front/src/components/admin/catalog/admin-category-tree-helpers.ts` — currently dirty pure category helper surface.
- `apps/front/src/components/admin/catalog/admin-product-main-fields.tsx` — currently dirty product form-field surface.
- `apps/front/src/components/admin/homepage/admin-homepage-manager.tsx` — homepage admin orchestration container.
- `apps/front/src/components/admin/homepage/admin-homepage-target-search.tsx` — currently dirty homepage target-search surface.
- `apps/api/Modules/Catalog/DTOs/AdminCatalogProductDtos.cs` — backend admin product DTO shape.
- `apps/api/Modules/Catalog/DTOs/AdminHomepageDtos.cs` — backend admin homepage DTO shape.
- `apps/api/Modules/Catalog/Controllers/AdminCatalogProductsController.cs` — backend product admin endpoint shape.
- `apps/api/Modules/Catalog/Controllers/AdminHomepageController.cs` — backend homepage admin endpoint shape.
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalogProductsEndpointTests.cs` — backend admin product endpoint test pattern.
- `tests/LineCom.Api.Tests/Modules/Catalog/AdminHomepageEndpointTests.cs` — backend admin homepage endpoint test pattern.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `apps/front/src/components/admin/catalog/admin-product-manager-helpers.ts` already hosts product list/query mapping helpers with tests.
- `apps/front/src/components/admin/catalog/admin-category-manager-helpers.ts` and `admin-category-tree-helpers.ts` provide category helper patterns and are natural locations for pure tree/parent-picker behavior.
- `apps/front/src/lib/api/admin-catalog.test.ts` and `admin-homepage.test.ts` already stub `fetch` and assert URL, method, credentials, CSRF and payload behavior.
- Backend endpoint tests under `tests/LineCom.Api.Tests/Modules/Catalog` use `WebApplicationFactory` and replaced services/repositories to assert serialized response behavior.

### Established Patterns
- Frontend admin pages should keep endpoint paths and DTO shapes in `apps/front/src/lib/api/*`, not inside components.
- Large frontend containers should be split when they mix orchestration, data mapping and UI rendering; stateful containers should keep loading/mutations/data flow while helpers/panels carry pure logic/rendering.
- Pure mapping, payload building, normalization, reorder and merge logic should live in helper modules with focused unit tests.
- Backend data access remains explicit Dapper/Npgsql SQL; Phase 5 must not introduce Entity Framework or schema changes unless a contract test proves a narrowly required migration.

### Integration Points
- Product admin: `admin-product-manager.tsx`, `admin-product-main-fields.tsx`, `admin-product-manager-helpers.ts`, `apps/front/src/lib/api/admin-catalog.ts`, admin product DTO/controller/endpoint tests.
- Category admin: `admin-category-parent-picker.tsx`, `admin-category-tree-helpers.ts`, category manager/helper tests, admin category API client and endpoint tests where relevant.
- Homepage admin: `admin-homepage-manager.tsx`, `admin-homepage-section-editor.tsx`, `admin-homepage-target-search.tsx`, `apps/front/src/lib/api/admin-homepage.ts`, admin homepage DTO/controller/endpoint tests.
- Current dirty diff includes product SQL/repository tests, homepage repository SQL/tests, category parent picker/tree helper changes, product main fields changes and homepage target-search tests; plans must account for this before editing.

</code_context>

<specifics>
## Specific Ideas

- Treat current dirty admin/backend/test changes as a baseline to inspect, not as executor-owned work.
- Start planning from the diff: identify which dirty areas already imply helper extraction or contract coverage, then define narrow execution plans around those seams.
- Prefer a small number of high-signal contract checks over a broad contract framework.
- Keep Phase 5 focused on release maintainability; broad admin UI redesign, new product features and generated contract infrastructure are out of scope.

</specifics>

<deferred>
## Deferred Ideas

- Generated OpenAPI or DTO generation pipeline for frontend/backend contracts.
- Full refactor of all large admin managers, including attribute and brand managers, unless directly required by dirty areas.
- New admin catalog/homepage capabilities or UI redesign.
- SEO landing pages, product comparison and web import/export workflows.

</deferred>

---

*Phase: 05-Admin Maintainability And Contracts*
*Context gathered: 2026-05-15*
