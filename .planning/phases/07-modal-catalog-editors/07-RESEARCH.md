# Phase 7: Modal Catalog Editors - Research

**Researched:** 2026-05-15
**Status:** Complete

## Scope

Research focused on the existing admin catalog React components, modal accessibility behavior, stale-request safety and the current Vitest/Testing Library patterns needed to plan Phase 7.

## Sources

### Project Sources
- `.planning/phases/07-modal-catalog-editors/07-CONTEXT.md`
- `.planning/ROADMAP.md`
- `.planning/REQUIREMENTS.md`
- `.planning/codebase/CONVENTIONS.md`
- `.planning/codebase/STRUCTURE.md`
- `.planning/codebase/TESTING.md`
- `docs/superpowers/specs/2026-05-15-admin-catalog-modal-editing-design.md`
- `apps/front/src/components/admin/catalog/admin-product-manager.tsx`
- `apps/front/src/components/admin/catalog/admin-product-editor.tsx`
- `apps/front/src/components/admin/catalog/admin-product-list-panel.tsx`
- `apps/front/src/components/admin/catalog/admin-category-manager.tsx`
- `apps/front/src/components/admin/catalog/admin-category-form.tsx`
- `apps/front/src/components/admin/catalog/admin-category-list-panel.tsx`
- `apps/front/src/components/admin/catalog/admin-category-parent-picker.tsx`
- `apps/front/src/styles/admin-catalog.css`
- `apps/front/src/components/admin/catalog/admin-product-manager.test.tsx`
- `apps/front/src/components/admin/catalog/admin-category-manager.test.tsx`

### Context7 Documentation
- `/reactjs/react.dev` - React `useEffect` guidance for synchronizing imperative dialog/external behavior and cleanup.
- `/testing-library/testing-library-docs` - Testing Library guidance for modal tests, user-event interactions, role/name queries and async assertions.

## Findings

### Modal Shell

- React docs emphasize using Effects only to synchronize with external systems and always returning cleanup for imperative subscriptions or DOM APIs. For Phase 7 this means Escape-key listeners, focus return and any body-scroll/focus behavior should live in one modal shell and clean up on close/unmount.
- A custom `role="dialog"` shell is a good fit for this project because the approved context already locks `role="dialog"`, `aria-modal="true"` and associated title behavior. A native `<dialog>` is possible, but it would introduce browser-specific imperative `showModal()` handling that the current code does not use.
- Backdrop and Escape behavior should be centralized in `AdminCatalogModal` so product/category modals do not duplicate close rules.
- Close requests should flow through a manager-owned callback because managers know whether a mutation is running and whether the current editor state is dirty.

### State Ownership and Dirty Guards

- Existing managers own loading, mutation state, stale request sequence refs, latest list params and selected entity state. This should remain true.
- The modal components should be presentational wrappers around `AdminProductEditor` and `AdminCategoryForm` plus category position controls.
- Dirty checks should compare current form state against a manager-owned baseline snapshot:
  - Product: reset the baseline after detail load, create start and successful save/create.
  - Category: reset the baseline after detail load, create start and successful save/create/move/sort; include form state, `moveParentId` and `newSortOrder`.
- Delete success should close the modal, clear selected entity/form state and refresh the list. Save/create success should keep the modal open and update the dirty baseline to the saved state.

### Product Editor Migration

- `AdminProductEditor` is already a focused presentational editor with product tabs, status/alert messages and action buttons. It should be wrapped, not rewritten.
- `AdminProductManager` already has stale guards via `detailRequestSeqRef`, `editorSessionRef` and selected-product refs. Opening/closing a modal should increment/cancel the relevant session/request refs so stale detail responses cannot hydrate a closed or newer modal.
- Product list context is already stored in manager filters/pagination. Removing the side editor should not move this state.

### Category Editor Migration

- `AdminCategoryForm` owns main category fields, while `AdminCategoryManager` currently renders move/sort controls beside it in the same editor section. Phase 7 should move those controls into the category modal as a compact `Position` section.
- `AdminCategoryParentPicker` and `getBlockedParentIds` already implement parent/self/descendant safety. The plan should preserve this path rather than add new category selection logic.
- Category manager currently lacks the stronger mutation staleness guard pattern used by product manager. The category modal migration should add session/request guards sufficient for closed/newer modal sessions before expanding tests.

### Testing Strategy

- Testing Library docs reinforce querying by role/name and interacting through `userEvent`. Modal tests should assert behavior through `role="dialog"`, accessible titles, buttons, Escape keyboard actions and alerts/success messages rather than CSS implementation details.
- Use `findBy*`/`waitFor` where list/detail/mutation calls are async.
- Product and category manager tests already mock API clients and use user-level flows. Extend those files instead of creating a new E2E framework.
- Focused verification should include:
  - product row and `New product` open `dialog`;
  - category tree item and `New category` open `dialog`;
  - `save/create` keep the dialog open with success;
  - `delete` closes dialog and refreshes list;
  - Escape/backdrop close only when allowed;
  - unsaved changes trigger a close confirmation;
  - stale detail responses do not open/hydrate closed or newer modal sessions;
  - list filters/pagination survive modal open/close.

## Planning Recommendations

1. Split Phase 7 into the roadmap's two plans:
   - `07-01`: shared modal shell plus product editor migration.
   - `07-02`: category editor migration plus list-width/responsive cleanup.
2. Keep `AdminCatalogModal` small, reusable and dependency-free.
3. Avoid editing `apps/front/src/styles/responsive.css` unless implementation proves it is necessary; it is currently user-owned dirty baseline. Prefer adding modal and manager-width CSS to `admin-catalog.css`.
4. Include targeted tests in each plan and a final frontend test run for the touched admin catalog suites.
5. Do not include Phase 8 quick category reassignment in Phase 7 plans.

## Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| Modal close discards edits silently | high | Add manager-owned dirty baseline and confirmation tests. |
| Stale detail response hydrates a closed modal | high | Increment request/session refs on close and create-start; test deferred response ordering. |
| Product/category managers grow into mixed modal/rendering files | medium | Add small modal wrapper components and keep managers responsible for state only. |
| Responsive cleanup touches unrelated dirty `responsive.css` baseline | medium | Prefer `admin-catalog.css`; if `responsive.css` must be edited, inspect targeted diff first and stage only intentional deltas. |
| Tests assert implementation details instead of user behavior | medium | Use Testing Library role/name queries and `userEvent` flows. |

## Out of Scope

- Quick product category reassignment and attribute-impact warning behavior; Phase 8 owns these.
- Bulk category changes.
- Generated OpenAPI/contracts.
- Broad admin redesign outside product/category editor layout.
- Public catalog, SEO landing pages, product comparison and web import/export.

## Research Complete

Phase 7 is ready for plan creation.
