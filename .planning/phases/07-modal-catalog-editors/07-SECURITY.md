---
phase: "07"
slug: modal-catalog-editors
status: verified
threats_open: 0
asvs_level: 1
created: 2026-05-15
updated: 2026-05-15T13:32:00+03:00
---

# Phase 07 - Security

Per-phase security contract: threat register, accepted risks, and audit trail for Phase 7 admin catalog modal UX.

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Admin browser to catalog API | Product/category create, update, delete, move and sort requests leave the browser and require the existing CSRF token. | Admin catalog mutations, category hierarchy changes, product publication data |
| Modal UI state to persisted admin data | Unsaved editor state is held client-side before an explicit save/move/sort/delete action. | Product/category form fields, parent category, sort order |
| Async API responses to current modal session | Product/category details and list refreshes can resolve after the user closes or switches editor sessions. | Product/category detail payloads and refreshed list rows |
| Phase scope boundary | Phase 7 is limited to modal editor UX and must not introduce Phase 8 quick category reassignment or unrelated public-site changes. | Admin UI behavior and planning scope |

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-07-01 | Data integrity | Product modal close | Mitigate | Manager-owned product form baseline, close confirmation and tests for dirty close. Evidence: `admin-product-manager.tsx`, `admin-product-manager.test.tsx`. | closed |
| T-07-02 | Data integrity | Product detail loading | Mitigate | Product detail/session refs are incremented on select/create/close; stale detail tests cover closed/newer sessions. Evidence: `detailRequestSeqRef`, `editorSessionRef`, stale detail tests. | closed |
| T-07-03 | Tampering regression | Product save/create/delete | Mitigate | Existing mutation functions are preserved, CSRF token is still passed, and modal-specific save/delete tests assert outcomes. | closed |
| T-07-04 | Maintainability | Shared modal shell | Mitigate | `AdminCatalogModal` centralizes dialog/backdrop/Escape/close behavior for product and category editors. | closed |
| T-07-05 | Process integrity | Responsive styling scope | Mitigate | Modal/list styling stayed in `admin-catalog.css`; user-owned dirty public styles and `responsive.css` were not staged. | closed |
| T-07-06 | Tampering regression | Category move/sort | Mitigate | Move/sort controls remain in `AdminCategoryEditorModal`; tests assert `moveAdminCategory` and `sortAdminCategory` calls with CSRF token. | closed |
| T-07-07 | Data integrity | Category detail loading | Mitigate | Category detail/session refs prevent stale detail hydration after close/newer sessions; deferred response tests cover this. | closed |
| T-07-08 | Data integrity | Category dirty close | Mitigate | Dirty baseline includes form, `moveParentId` and `newSortOrder`; tests cover close confirmation after position changes. | closed |
| T-07-09 | Scope integrity | Product/category list layout | Mitigate | CSS changes are scoped to product/category manager full-width layouts; brand/attribute managers keep their side-editor layouts. | closed |
| T-07-10 | Process integrity | Dirty responsive baseline | Mitigate | `responsive.css` was avoided during Phase 7 implementation and remained unstaged. | closed |
| T-07-11 | Tampering regression | Category save/delete/move/sort payloads | Mitigate | API orchestration remains in `AdminCategoryManager`; tests cover create/update/delete/move/sort payloads with CSRF. | closed |
| T-07-12 | Data integrity | Split/tabbed category controls | Mitigate | Form/move/sort state stays manager-owned while tab panels only render existing controls; dirty close includes every state source. | closed |
| T-07-13 | Accessibility | Category modal tabs | Mitigate | Category editor uses `tablist`, `tab`, `tabpanel`, `aria-selected`, `aria-controls`, keyboard arrow navigation and regression coverage. | closed |
| T-07-14 | Availability/usability | Modal viewport behavior | Mitigate | Scoped modal/tab CSS, hidden-panel fix and browser QA on desktop plus 390px viewport verified no overlapping panels. | closed |
| T-07-15 | Scope integrity | Phase 8 drift | Mitigate | No quick product category reassignment, bulk category changes or new admin capabilities were added in Phase 7. | closed |

## Accepted Risks Log

No accepted risks.

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-05-15 | 15 | 15 | 0 | Codex inline `$gsd-secure-phase 7` |

## Verification Evidence

- `npm.cmd test -- src/components/admin/catalog/admin-category-manager.test.tsx src/components/admin/catalog/admin-product-manager.test.tsx` - 48 tests passed.
- `npm.cmd run lint` - 0 errors, 1 unrelated pre-existing warning in `admin-homepage-manager.test.tsx`.
- `$env:LINECOM_PUBLIC_SITE_ORIGIN='https://line-com.ru'; npm.cmd run build` - passed.
- Browser QA at `http://127.0.0.1:3010/admin/catalog` verified category tabs on desktop and 390px viewport.
- `gsd-sdk.cmd query audit-open --json` - no open planning/UAT/verification items for the phase.

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-05-15
