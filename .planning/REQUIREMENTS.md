# Requirements: Admin Catalog UX

**Defined:** 2026-05-15
**Milestone:** v1.1 Admin Catalog UX
**Core Goal:** Existing admin catalog product/category editing becomes easier to use by replacing always-visible side editors with modal editing and adding safe single-product quick category reassignment.

## v1.1 Requirements

### Modal Editing

- [ ] **AUX-01**: Admin can open the product editor in a modal from an existing product row and from `New product`, with existing product editor tabs and save/delete behavior preserved.
- [ ] **AUX-02**: Admin can open the category editor in a modal from an existing category selection and from `New category`, with existing category save/delete/move/sort behavior preserved.
- [ ] **AUX-03**: Product and category lists remain usable at full working width after side editors are removed, with filters, pagination and current list context preserved when modals open or close.

### Quick Product Category Change

- [ ] **CATUX-01**: Admin can change a single product category from the product list without opening the full product editor.
- [ ] **CATUX-02**: Quick category change only allows valid leaf categories and preserves all other product fields in the update command.
- [ ] **CATUX-03**: Quick category change shows an explicit warning before save when changing category can clear or invalidate existing category-specific product attribute values.

### Verification

- [ ] **VER-01**: Frontend tests cover modal open/close, create/update/delete flows, stale detail response handling and list context preservation for product and category editors.
- [ ] **VER-02**: Frontend tests and manual QA cover quick category change, warning behavior, list refresh, desktop layout and narrow viewport behavior.

## Future Requirements

- **BULK-01**: Admin can select multiple products and change their category in one confirmed operation.
- **BULK-02**: Bulk product category changes show aggregate warnings for attribute value impact before applying changes.

## Out of Scope

| Feature | Reason |
|---------|--------|
| Bulk category change | Deferred until single-product quick category change is validated in use. |
| Broad admin redesign | v1.1 targets catalog product/category editing only. |
| Generated OpenAPI or contract framework | v1.1 changes existing frontend UX and does not require new contract infrastructure. |
| New catalog capabilities | v1.1 does not add new product/category business capabilities beyond editing surface and quick category reassignment. |
| Product comparison, SEO landing pages, web import/export | Deferred product scope remains outside this UX milestone. |

## Traceability

Roadmap mapping is created after requirements approval.

| Requirement | Phase | Status |
|-------------|-------|--------|
| AUX-01 | TBD | Pending |
| AUX-02 | TBD | Pending |
| AUX-03 | TBD | Pending |
| CATUX-01 | TBD | Pending |
| CATUX-02 | TBD | Pending |
| CATUX-03 | TBD | Pending |
| VER-01 | TBD | Pending |
| VER-02 | TBD | Pending |

