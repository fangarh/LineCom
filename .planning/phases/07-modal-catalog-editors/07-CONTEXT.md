# Phase 7: Modal Catalog Editors - Context

**Gathered:** 2026-05-15
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 7 moves the existing admin product and category editors out of permanent side blocks and into accessible modal dialogs. It preserves the current product/category editing behavior, stale-request guards, CSRF-backed mutations, list filters, pagination and selection context while giving the product and category lists full working width.

This phase covers AUX-01, AUX-02, AUX-03 and VER-01 only. Quick product category reassignment belongs to Phase 8.

</domain>

<decisions>
## Implementation Decisions

### Save, Create and Delete Behavior
- **D-01:** Product and category `save/create` actions keep the modal open after a successful mutation and show the existing success message inside the modal.
- **D-02:** Product and category `delete` actions close the modal after a successful mutation, refresh the relevant list and return the admin to the list context.
- **D-03:** List filters, pagination and latest-list refresh behavior must remain tied to the manager state, not to the modal component.

### Modal Closing Rules
- **D-04:** Product and category modals must provide explicit close controls and support Escape and backdrop close when no mutation is running.
- **D-05:** Close actions must be blocked while a mutation is running.
- **D-06:** If the editor has unsaved changes, closing through `X`, Escape or backdrop must ask for confirmation before discarding the current form state.

### Category Position Controls
- **D-07:** Category `save/delete/move/sort` behavior stays in one category editor modal.
- **D-08:** Move and sort controls should be presented as a compact `Position` section below the main category form, not as a side block or separate always-visible panel.
- **D-09:** Existing parent-picker rules must remain intact, including blocking self/descendant parent choices.

### Modal Size and Layout
- **D-10:** Use one shared `AdminCatalogModal` shell for catalog admin dialogs.
- **D-11:** On desktop, use a wide responsive dialog around `min(1120px, calc(100vw - 48px))` with height capped around `calc(100vh - 48px)`.
- **D-12:** The modal should have a stable header/footer and a scrollable body so long product tabs and category forms do not push controls off-screen.
- **D-13:** On narrow viewports, the modal should behave close to fullscreen with 12-16px viewport padding.

### the agent's Discretion
- Preserve existing component naming, state ownership and test style. Managers own API orchestration, modal session state, stale guards and list refreshes; modal components receive state and callbacks.
- Implementation details such as exact CSS class names and minor spacing can follow the current `admin-catalog.css` conventions as long as the locked behavior above is preserved.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Milestone and Scope
- `.planning/ROADMAP.md` - Phase 7 goal, success criteria and plan split.
- `.planning/REQUIREMENTS.md` - AUX-01, AUX-02, AUX-03 and VER-01 requirements.
- `.planning/PROJECT.md` - v1.1 scope, constraints and dirty-worktree guardrails.
- `docs/superpowers/specs/2026-05-15-admin-catalog-modal-editing-design.md` - Approved design direction for modal catalog editing and quick category change.

### Codebase Maps
- `.planning/codebase/CONVENTIONS.md` - Frontend naming, decomposition and test conventions.
- `.planning/codebase/STRUCTURE.md` - Admin frontend file locations.
- `.planning/codebase/TESTING.md` - Vitest and Testing Library patterns for admin component tests.

### Existing Admin Catalog Code
- `apps/front/src/components/admin/catalog/admin-product-manager.tsx` - Product manager state, stale guards, product list loading and mutations.
- `apps/front/src/components/admin/catalog/admin-product-editor.tsx` - Existing product editor tabs and form actions to wrap in a modal.
- `apps/front/src/components/admin/catalog/admin-product-list-panel.tsx` - Product list/table surface and row selection entry point.
- `apps/front/src/components/admin/catalog/admin-category-manager.tsx` - Category manager state, parent options, move/sort/delete mutations and stale guards.
- `apps/front/src/components/admin/catalog/admin-category-form.tsx` - Existing category form to wrap in a modal.
- `apps/front/src/components/admin/catalog/admin-category-list-panel.tsx` - Category list/tree entry point.
- `apps/front/src/components/admin/catalog/admin-category-parent-picker.tsx` - Reusable tree picker for parent selection.
- `apps/front/src/components/admin/catalog/admin-category-tree.tsx` - Category tree selection surface.
- `apps/front/src/styles/admin-catalog.css` - Existing admin catalog layout and component styles.
- `apps/front/src/components/admin/catalog/admin-product-manager.test.tsx` - Existing product manager regression style and fixtures.
- `apps/front/src/components/admin/catalog/admin-category-manager.test.tsx` - Existing category manager regression style and fixtures.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AdminProductEditor`: already contains product tabs, status/alert messages, duplicate checks, image and attribute panels; wrap it rather than rewrite it.
- `AdminCategoryForm`: already owns the category form fields; keep it as the main content of the category modal.
- `AdminCategoryParentPicker` / `AdminCategoryTreePicker`: reusable picker for category move/parent selection and later Phase 8 category reassignment.
- `AdminProductListPanel` and `AdminCategoryListPanel`: list surfaces should expand to full width once side editors are removed.

### Established Patterns
- Managers use request sequence refs to ignore stale list/detail responses. Modal session changes must preserve or extend these guards.
- Managers currently own mutation state, alert/status messages, selected entity state and latest-list refresh params. Modal components should stay presentational.
- Frontend tests are Testing Library/Vitest tests colocated with components and drive behavior through user interactions and accessible roles/labels.
- Existing CSS uses `admin-catalog-*`, `admin-product-manager__*` and `admin-category-manager__*` classes in `apps/front/src/styles/admin-catalog.css`.

### Integration Points
- Product modal opens from product row selection and `New product`.
- Category modal opens from category tree selection and `New category`.
- Product delete, category delete, category move and category sort must refresh list state using the existing manager refresh paths.
- Modal accessibility should be testable through `role="dialog"`, associated title, Escape/backdrop close behavior and focus return.

</code_context>

<specifics>
## Specific Ideas

- Use one shared catalog modal shell rather than embedding modal markup directly in both managers.
- Product editor should continue to use the existing tabs: main, attributes, images, SEO and publication.
- Category move/sort should be labelled as a position-oriented section inside the modal.
- The current dirty worktree outside planning is user-owned baseline and must not be reverted or included in planning/product commits.

</specifics>

<deferred>
## Deferred Ideas

- Quick single-product category reassignment is Phase 8, not Phase 7.
- Bulk product category changes remain deferred future scope.
- Broad admin redesign, generated OpenAPI/contracts, product comparison, SEO landing pages and web import/export remain out of scope for v1.1 Phase 7.

</deferred>

---

*Phase: 7-Modal Catalog Editors*
*Context gathered: 2026-05-15*
