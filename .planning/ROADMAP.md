# Roadmap: LineCom

## Milestones

- [x] **v1.0 Release Stabilization** - Phases 1-6 shipped on 2026-05-15. Archives: `.planning/milestones/v1.0-ROADMAP.md`, `.planning/milestones/v1.0-REQUIREMENTS.md`, `.planning/milestones/v1.0-MILESTONE-AUDIT.md`, `.planning/milestones/v1.0-phases/`.
- [ ] **v1.1 Admin Catalog UX** - Phases 7-8 in planning. Goal: modal catalog editing and safe quick product category reassignment.

## Completed Milestone Summary

<details>
<summary>v1.0 Release Stabilization - shipped 2026-05-15</summary>

Release stabilization hardened the existing LineCom codebase before product expansion:

- Phase 1: Release Safety Baseline - 3/3 plans complete.
- Phase 2: Storage Access And Diagnostics - 3/3 plans complete.
- Phase 3: Import Storage Consistency - 2/2 plans complete.
- Phase 4: Public SEO/GEO Reliability - 3/3 plans complete.
- Phase 5: Admin Maintainability And Contracts - 3/3 plans complete.
- Phase 6: Production Readiness Gate - 2/2 plans complete.

Final gate evidence is archived in `.planning/milestones/v1.0-phases/06-production-readiness-gate/06-VERIFICATION.md`.

</details>

## Active Milestone: v1.1 Admin Catalog UX

### Phase 7: Modal Catalog Editors

**Goal**: Product and category editing no longer consumes a permanent side block; existing editor behavior is preserved inside accessible modal dialogs.
**Mode:** mvp
**Depends on**: v1.0 release stabilization
**Requirements**: AUX-01, AUX-02, AUX-03, VER-01
**Success Criteria** (what must be TRUE):
  1. Product row selection and `New product` open the product editor in a modal with existing tabs, save and delete behavior preserved.
  2. Category selection and `New category` open the category editor in a modal with existing save, delete, move and sort behavior preserved.
  3. Product and category lists keep full working width without the always-visible side editor.
  4. Closing modals preserves list filters, pagination and stale-request safety.
  5. Focused frontend tests cover modal open/close, create/update/delete flows and stale detail responses.
**Plans**: 3 plans

Plans:
- [x] 07-01: Shared catalog modal shell and product editor modal migration.
- [x] 07-02: Category editor modal migration and responsive list layout cleanup.
- [ ] 07-03: Category modal sectioning for UAT gap closure.

### Phase 8: Quick Product Category Change

**Goal**: Admin can safely change a single product category from the product list without opening the full product editor.
**Mode:** mvp
**Depends on**: Phase 7
**Requirements**: CATUX-01, CATUX-02, CATUX-03, VER-02
**Success Criteria** (what must be TRUE):
  1. Each product row exposes a focused category change action separate from the full product editor.
  2. Quick category change opens a small modal with current product/category context and the existing category tree picker behavior.
  3. Parent categories are rejected and only valid leaf categories can be saved.
  4. The update preserves existing product fields and changes only `categoryId`.
  5. Warning behavior is visible before save when category-specific attribute values may be cleared or invalidated.
  6. Tests and manual QA cover list refresh, warning behavior, desktop layout and narrow viewport behavior.
**Plans**: 2 plans

Plans:
- [ ] 08-01: Product row quick category action and reassignment modal.
- [ ] 08-02: Category-change safety tests, warning behavior and viewport QA.

## Deferred Product Scope

These items remain outside v1.1 and should be reconsidered during a later milestone:

- Product comparison by normalized attributes.
- SEO/GEO landing pages.
- Web-based import/export workflow.
- Local FileStorage retention/cleanup automation.
- Bulk category changes for multiple selected products.

## Progress

| Milestone | Phases | Plans | Status | Shipped |
|-----------|--------|-------|--------|---------|
| v1.0 Release Stabilization | 1-6 | 16/16 | Shipped | 2026-05-15 |
| v1.1 Admin Catalog UX | 7-8 | 2/5 | In progress | - |
