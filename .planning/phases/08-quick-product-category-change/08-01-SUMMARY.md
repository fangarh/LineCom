---
phase: 08-quick-product-category-change
plan: "01"
subsystem: ui
tags: [react, admin-catalog, modal, category-picker, vitest]

requires:
  - phase: 07-modal-catalog-editors
    provides: AdminCatalogModal and product/category modal interaction patterns
provides:
  - Row-level single-product quick category change action
  - Focused category-change modal using the existing category tree picker
  - Full product update payload helper that changes only categoryId
  - Leaf-only category selection and attribute-clearing warning surface
affects: [admin-catalog, product-manager, phase-08-hardening]

tech-stack:
  added: []
  patterns:
    - Manager-owned mutation flow with presentational modal
    - Pure product update payload helper covered by focused unit tests

key-files:
  created:
    - apps/front/src/components/admin/catalog/admin-product-category-change-helpers.ts
    - apps/front/src/components/admin/catalog/admin-product-category-change-helpers.test.ts
    - apps/front/src/components/admin/catalog/admin-product-category-change-modal.tsx
  modified:
    - apps/front/src/components/admin/catalog/admin-product-list-panel.tsx
    - apps/front/src/components/admin/catalog/admin-product-manager.tsx
    - apps/front/src/components/admin/catalog/admin-product-manager.test.tsx
    - apps/front/src/styles/admin-catalog.css

key-decisions:
  - "Quick category reassignment uses the existing updateAdminProduct endpoint with a full command built from latest AdminProductDetail."
  - "The row action has a short accessible name scoped by row context so existing product-name edit tests keep targeting the full editor button."
  - "Parent categories are disabled in the reused AdminCategoryTreePicker and save is blocked unless the target is a leaf."

patterns-established:
  - "Single-purpose admin modal components receive state/callbacks only; API ownership stays in the manager."
  - "Category-change helpers wrap existing product editor mapping instead of duplicating full payload normalization."

requirements-completed: [CATUX-01, CATUX-02, CATUX-03, VER-02]

duration: 15min
completed: 2026-05-15
---

# Phase 08 Plan 01 Summary

**Single-product quick category reassignment from the admin product list with leaf-only modal selection and full update payload preservation**

## Performance

- **Duration:** 15 min
- **Started:** 2026-05-15T13:44:00+03:00
- **Completed:** 2026-05-15T13:59:00+03:00
- **Tasks:** 5
- **Files modified:** 7

## Accomplishments

- Added a separate product-row "Сменить" action that opens a focused category-change modal without opening the full product editor.
- Added pure helpers and tests proving that the update command preserves existing product fields and changes only `categoryId`.
- Reused `AdminCategoryTreePicker` with parent categories disabled and save blocked for unchanged, missing or non-leaf targets.
- Added an explicit pre-save warning when category-specific product attributes may be cleared.

## Task Commits

1. **Tasks 2-4: Helper extraction, modal and manager wiring** - `8db9007` (feat)

## Verification

- `npm.cmd --prefix apps/front test -- src/components/admin/catalog/admin-product-category-change-helpers.test.ts` - pass, 3 tests.
- `npm.cmd --prefix apps/front test -- src/components/admin/catalog/admin-product-manager.test.tsx src/components/admin/catalog/admin-product-category-change-helpers.test.ts` - pass, 31 tests.
- `npm.cmd --prefix apps/front run lint` - pass with 1 pre-existing warning in `apps/front/src/components/admin/homepage/admin-homepage-manager.test.tsx`.
- Targeted diff reviewed; no backend endpoints, bulk category actions or unrelated public-site files were staged.

## Deviations from Plan

None - plan executed within the intended Phase 8 surface.

## Issues Encountered

- Initial quick-action accessible label included the product name and conflicted with existing tests that locate the product-name edit button. Fixed by keeping the row action name short and querying it through row context.

## User Setup Required

None.

## Next Phase Readiness

08-02 can build directly on this slice to harden stale detail/save behavior, mutation guards, warning precision, latest-filter refresh coverage and browser viewport QA.

---
*Phase: 08-quick-product-category-change*
*Completed: 2026-05-15*
