# Admin Product Editor Decomposition Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Decompose the admin product editor without changing behavior before Task 8 verification.

**Architecture:** Keep `AdminProductManager` as the orchestration boundary for list/detail loading, mutations, duplicate checks, tab state, and stale async guards. Move presentational editor/list panels and pure mapping helpers into focused sibling files under `apps/front/src/components/admin/catalog`.

**Tech Stack:** Next.js client components, React 19, TypeScript, Vitest, Testing Library, existing admin catalog API client and global CSS.

---

### Task 1: Extract Pure Helpers

**Files:**
- Create: `apps/front/src/components/admin/catalog/admin-product-editor-helpers.ts`
- Create: `apps/front/src/components/admin/catalog/admin-product-editor-helpers.test.ts`
- Create: `apps/front/src/components/admin/catalog/admin-product-image-helpers.ts`
- Create: `apps/front/src/components/admin/catalog/admin-product-image-helpers.test.ts`
- Modify: `apps/front/src/components/admin/catalog/admin-product-manager.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-product-images-panel.tsx`

- [ ] Add helper tests for product form mapping, update payload building, attribute merge/payload building, and image reorder/forms mapping.
- [ ] Run helper tests and verify RED because helper modules do not exist yet.
- [ ] Move the existing pure logic into helper files without changing output shape.
- [ ] Update product manager and image panel imports.
- [ ] Run helper tests and focused product/image tests.

### Task 2: Extract Product Field And Panel Components

**Files:**
- Create: `apps/front/src/components/admin/catalog/admin-product-main-fields.tsx`
- Create: `apps/front/src/components/admin/catalog/admin-product-seo-fields.tsx`
- Create: `apps/front/src/components/admin/catalog/admin-product-publication-fields.tsx`
- Create: `apps/front/src/components/admin/catalog/admin-product-attributes-panel.tsx`
- Create: `apps/front/src/components/admin/catalog/admin-product-duplicate-panel.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-product-manager.tsx`

- [ ] Move field components with their current labels, control types, and class names.
- [ ] Move `AdminProductAttributesPanel` while preserving its stale async guards and save behavior.
- [ ] Move duplicate candidates markup into `AdminProductDuplicatePanel`.
- [ ] Run `npm.cmd test -- admin-product-manager.test.tsx admin-product-images-panel.test.tsx`.

### Task 3: Extract Product List And Editor Shell

**Files:**
- Create: `apps/front/src/components/admin/catalog/admin-product-list-panel.tsx`
- Create: `apps/front/src/components/admin/catalog/admin-product-editor.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-product-manager.tsx`

- [ ] Move list filters, rows, and create button into `AdminProductListPanel`.
- [ ] Move editor shell, tablist, tab panels, and action buttons into `AdminProductEditor`.
- [ ] Keep all async functions and state ownership in `AdminProductManager`.
- [ ] Run focused tests and TypeScript.

### Task 4: Final Verification

**Files:**
- Verify all changed frontend files.

- [ ] Run required focused tests.
- [ ] Run TypeScript `--noEmit`.
- [ ] Run scoped ESLint for changed TS/TSX files.
- [ ] Run whitespace checks and marker scan on changed implementation files.
- [ ] Confirm `admin-catalog-homepage-slice.png` remains untracked and unstaged.
- [ ] Run spec review and quality review against the approved scope.
- [ ] Commit with `refactor: decompose admin product editor` and push.
