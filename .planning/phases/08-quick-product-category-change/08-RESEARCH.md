---
phase: 08-quick-product-category-change
status: complete
created: 2026-05-15
source:
  - .planning/ROADMAP.md
  - .planning/REQUIREMENTS.md
  - .planning/phases/07-modal-catalog-editors/07-VALIDATION.md
  - .planning/phases/07-modal-catalog-editors/07-SECURITY.md
  - apps/front/src/components/admin/catalog/admin-product-manager.tsx
  - apps/front/src/components/admin/catalog/admin-product-list-panel.tsx
  - apps/front/src/components/admin/catalog/admin-category-parent-picker.tsx
  - apps/front/src/components/admin/catalog/admin-product-editor-helpers.ts
  - apps/front/src/lib/api/admin-catalog.ts
  - apps/api/Modules/Catalog/Services/AdminCatalogProductService.cs
  - apps/api/Modules/Catalog/Repositories/AdminCatalogProductSql.cs
---

# Phase 08 Research - Quick Product Category Change

## Scope

Phase 8 adds a single-product quick category change from the admin product list. It must not reopen the full product editor, must not add bulk category change, and must not introduce generated OpenAPI or new backend infrastructure.

## Existing Patterns To Reuse

| Area | Current pattern | Phase 8 implication |
|------|-----------------|---------------------|
| Modal shell | `AdminCatalogModal` handles dialog semantics, Escape/backdrop close, focus return and disabled close. | Reuse it for a focused category-change modal instead of adding modal behavior again. |
| Category picker | `AdminCategoryTreePicker` supports tree rendering, disabled options and custom disabled reasons. | Reuse it and disable categories with children to enforce leaf-only quick reassignment. |
| Product update API | `updateAdminProduct(id, command, csrfToken)` already updates product category via `UpsertAdminProductCommand`. | No new backend endpoint is required if the frontend builds a full command from latest product detail and changes only `categoryId`. |
| Product command mapping | `formFromAdminProductDetail` + `buildAdminProductCommand` normalize full product payloads. | Extract/reuse a pure helper that builds a full update command from latest detail plus target category. |
| Product manager state | `AdminProductManager` owns list params, stale refs, mutation state and refresh behavior. | Quick reassignment state should stay in the manager; the modal is presentational. |
| Tests | `admin-product-manager.test.tsx` already mocks admin catalog APIs and verifies modal/list/stale behavior. | Add quick reassignment tests there, plus helper tests if a pure helper is extracted. |

## Backend Contract Findings

- `UpsertAdminProductCommand` requires a full product payload, including `categoryId`, name, slug, sale unit, quantity and publish state.
- `AdminCatalogProductService.UpdateProductAsync` reads the existing product, validates duplicate identity and publish readiness, then calls repository update.
- `DapperAdminCatalogProductRepository.UpdateProductAsync` runs `DeleteProductAttributesOnCategoryChange` before `UpdateProduct`.
- `DeleteProductAttributesOnCategoryChange` deletes `product_attribute_values` when `product.primary_category_id <> @CategoryId`.
- Existing database coverage includes `UpdateProductAsync_CategoryChangeClearsOldAttributeValues`.

Conclusion: quick category change can safely use the existing update endpoint, but the UI must warn that category-specific attributes can be cleared. It must build the update command from the latest detail so fields outside `categoryId` are preserved.

## Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Partial product update accidentally nulls unrelated fields. | Product data loss. | Use a helper that converts latest `AdminProductDetail` into a full `UpsertAdminProductCommand` and overrides only `categoryId`; test exact payload. |
| Parent category can be selected. | Invalid catalog assignment / backend rejection. | Disable categories with `hasChildren` or `childrenCount > 0` in the picker; block save unless selected category is a leaf. |
| Attribute values disappear without warning. | Admin surprise and data loss perception. | Show explicit warning when changing to a different category and the product has attribute values; keep warning visible before save. |
| Quick modal races with full editor selection or stale product detail. | Wrong product/category can be saved. | Add separate quick-change session refs and stale guards in `AdminProductManager`; tests should defer detail/update responses. |
| List refresh loses filters/pagination. | Admin loses context after save. | Reuse `latestListParamsRef` and `refreshProductList`. |
| Row action causes accidental full editor open. | UX conflict with Phase 7 row click behavior. | Add a distinct action button in a new action column/cell and stop event propagation where relevant. |
| Responsive table action becomes cramped. | Narrow viewport usability regression. | Add scoped table/action styles and browser QA for desktop plus narrow viewport. |

## Recommended Implementation Shape

1. Add `admin-product-category-change-helpers.ts` with pure helpers:
   - build full product update command from latest detail and target category.
   - determine whether a category is a leaf.
   - determine warning visibility from source category, target category and existing attributes.
2. Add `AdminProductCategoryChangeModal`:
   - Compose `AdminCatalogModal`.
   - Show product name, current category, selected category and warning.
   - Reuse `AdminCategoryTreePicker` with parent categories disabled.
   - Expose save/cancel callbacks; no API calls inside modal.
3. Update `AdminProductListPanel`:
   - Add a focused row action separate from the product-name edit button.
   - Keep existing row selection behavior unchanged.
4. Update `AdminProductManager`:
   - Add quick-change detail loading, modal state, session refs and mutation state.
   - On save, load/use latest product detail, build full command, call `updateAdminProduct`, close modal and refresh latest list.
5. Extend tests:
   - Quick action opens modal, not full editor.
   - Parent categories are disabled and cannot be saved.
   - Save changes only `categoryId` in the full command and preserves other fields.
   - Warning appears before save when attributes can be cleared.
   - List refresh uses latest filters after save.
   - Stale detail/update responses do not affect a newer or closed quick modal.

## Out Of Scope

- Bulk product category changes.
- New backend endpoint or generated API contract framework.
- Broad admin redesign.
- Product comparison, SEO landing pages, web import/export.
- Changing category attributes or product attribute values directly inside the quick-change modal.
