# Admin Catalog Frontend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the staff-facing Next.js admin catalog UI for managing categories, brands, category attributes/options, products, product attributes, product images, and brand logos using the backend API already pushed to `main`.

**Architecture:** This is a frontend-only slice in `apps/front`. It adds typed API clients, admin catalog routes under `/admin/catalog`, dense operational UI components, and tests around API contract usage, auth/role handling, form flows, and image/logo actions. Backend endpoints, migrations, Local FileStorage internals, homepage mutation endpoints, import/export, audit log, and LLM duplicate checking stay out of scope.

**Tech Stack:** Next.js 16 App Router, React 19 client components, TypeScript, Vitest, Testing Library, existing `apiJson`/cookie auth/CSRF patterns, existing CSS in `apps/front/src/app/globals.css`.

---

## Current Baseline

- Branch: `main`, synced with `origin/main`.
- Last pushed commit before this plan: `7338949 fix: harden admin catalog image safety`.
- Backend admin catalog API is ready and covered by tests:
  - categories CRUD/move/sort;
  - brands CRUD;
  - category attributes/options CRUD and inherit-from-parent;
  - products CRUD;
  - product attribute editing;
  - duplicate candidates endpoint;
  - product image upload/list/update/delete/order/main;
  - brand logo upload/update/delete.
- Expected untracked file: `admin-catalog-homepage-slice.png`. Do not stage, edit, delete, or commit it.

## Source Of Truth

Read before implementation:

- `AGENTS.md`;
- `docs/superpowers/specs/2026-05-11-admin-catalog-homepage-design.md`;
- `docs/superpowers/plans/2026-05-11-admin-catalog-foundation.md`;
- `docs/superpowers/plans/2026-05-11-admin-catalog-crud.md`;
- `docs/superpowers/plans/2026-05-11-admin-catalog-images.md`;
- `vault/Человекочитаемое`;
- `apps/front/src/app/admin/requests/requests-page-client.tsx`;
- `apps/front/src/components/admin/admin-request-list.tsx`;
- `apps/front/src/lib/api/admin-requests.ts`;
- `apps/front/src/lib/api/http.ts`;
- backend DTOs in `apps/api/Modules/Catalog/DTOs`.

Context7 note: current Next.js docs confirm App Router client navigation should use `next/navigation` hooks inside `"use client"` components, and client-side file upload can use `FormData` with `fetch`. Follow the existing app style rather than introducing Server Actions in this slice.

## Scope

In scope:

- typed admin catalog API client for JSON and multipart endpoints;
- `/admin/catalog` route with staff-only loading, redirect to login on unauthorized, forbidden state for `customer`;
- catalog dashboard/navigation for `products`, `categories`, `brands`, and `attributes`;
- category list/detail/create/update/delete/move/sort UI;
- brand list/detail/create/update/delete UI and brand logo upload/delete UI;
- category attribute/option editor and inherit-from-parent action;
- product list/detail/create/update/delete UI;
- product editor tabs: `Основное`, `Характеристики`, `Изображения`, `SEO`, `Публикация`;
- duplicate candidates panel in product editor;
- product image upload/list/alt-title edit/order/main/delete UI;
- responsive dense admin styling and browser QA.

Out of scope:

- new backend endpoints;
- homepage mutation endpoints and homepage UI;
- import/export;
- audit log;
- LLM duplicate checking;
- crop/resize/image editor;
- media library;
- prices, stock accounting, checkout, online payment;
- public catalog redesign.

## API Contracts

Use these existing backend endpoints:

```text
GET    /api/admin/catalog/categories
POST   /api/admin/catalog/categories
GET    /api/admin/catalog/categories/{id}
PUT    /api/admin/catalog/categories/{id}
DELETE /api/admin/catalog/categories/{id}
PUT    /api/admin/catalog/categories/{id}/move
PUT    /api/admin/catalog/categories/{id}/sort

GET    /api/admin/catalog/brands
POST   /api/admin/catalog/brands
GET    /api/admin/catalog/brands/{id}
PUT    /api/admin/catalog/brands/{id}
DELETE /api/admin/catalog/brands/{id}
PUT    /api/admin/catalog/brands/{id}/logo
DELETE /api/admin/catalog/brands/{id}/logo

GET    /api/admin/catalog/categories/{categoryId}/attributes
POST   /api/admin/catalog/categories/{categoryId}/attributes
PUT    /api/admin/catalog/categories/{categoryId}/attributes/{attributeId}
DELETE /api/admin/catalog/categories/{categoryId}/attributes/{attributeId}
POST   /api/admin/catalog/categories/{categoryId}/attributes/inherit-from-parent
POST   /api/admin/catalog/categories/{categoryId}/attributes/{attributeId}/options
PUT    /api/admin/catalog/categories/{categoryId}/attributes/{attributeId}/options/{optionId}
DELETE /api/admin/catalog/categories/{categoryId}/attributes/{attributeId}/options/{optionId}

GET    /api/admin/catalog/products
POST   /api/admin/catalog/products
GET    /api/admin/catalog/products/{id}
PUT    /api/admin/catalog/products/{id}
DELETE /api/admin/catalog/products/{id}
PUT    /api/admin/catalog/products/{id}/attributes
GET    /api/admin/catalog/products/duplicate-candidates

GET    /api/admin/catalog/products/{id}/images
POST   /api/admin/catalog/products/{id}/images
PUT    /api/admin/catalog/products/{id}/images/order
PUT    /api/admin/catalog/products/{id}/images/{imageId}
PUT    /api/admin/catalog/products/{id}/images/{imageId}/main
DELETE /api/admin/catalog/products/{id}/images/{imageId}
```

Multipart field names:

```text
POST /products/{id}/images: files
PUT  /brands/{id}/logo: file
```

All mutation calls must include the current session `csrfToken`.

## File Structure

Create:

- `apps/front/src/lib/api/admin-catalog.ts`
- `apps/front/src/lib/api/admin-catalog.test.ts`
- `apps/front/src/app/admin/catalog/page.tsx`
- `apps/front/src/app/admin/catalog/catalog-page-client.tsx`
- `apps/front/src/app/admin/catalog/catalog-page-client.test.tsx`
- `apps/front/src/components/admin/catalog/admin-catalog-shell.tsx`
- `apps/front/src/components/admin/catalog/admin-catalog-shell.test.tsx`
- `apps/front/src/components/admin/catalog/admin-category-manager.tsx`
- `apps/front/src/components/admin/catalog/admin-category-manager.test.tsx`
- `apps/front/src/components/admin/catalog/admin-brand-manager.tsx`
- `apps/front/src/components/admin/catalog/admin-brand-manager.test.tsx`
- `apps/front/src/components/admin/catalog/admin-attribute-manager.tsx`
- `apps/front/src/components/admin/catalog/admin-attribute-manager.test.tsx`
- `apps/front/src/components/admin/catalog/admin-product-manager.tsx`
- `apps/front/src/components/admin/catalog/admin-product-manager.test.tsx`
- `apps/front/src/components/admin/catalog/admin-product-images-panel.tsx`
- `apps/front/src/components/admin/catalog/admin-product-images-panel.test.tsx`

Modify:

- `apps/front/src/lib/api/http.ts`
- `apps/front/src/lib/routes.ts`
- `apps/front/src/app/globals.css`
- optionally `apps/front/src/components/layout/site-header.tsx` if admin catalog navigation should be discoverable from the existing header.

Do not modify:

- `apps/api`;
- `apps/dbmigrator`;
- public catalog pages unless a route import is required;
- `admin-catalog-homepage-slice.png`.

## Shared Frontend Rules

- UI language is Russian.
- Keep admin UI dense, work-focused, and scannable. Avoid marketing hero layout, decorative cards, oversized headings, and nested cards.
- Use ordinary form controls for data entry: inputs, selects, checkboxes, textareas, file inputs, and buttons.
- No visible instructional text explaining how the UI works; labels, validation messages, statuses, and action names are allowed.
- Keep SEO/GEO fields visible in category/product/brand editors: `slug`, `h1`, `seoTitle`, `seoDescription`.
- Never invent data not returned by backend. If a dropdown needs data, load it from existing list endpoints.
- Preserve existing auth behavior from `/admin/requests`: unauthenticated users go to login with `returnTo`, `customer` sees forbidden state, `seller` and `admin` can use the UI.
- Use `apiJson` for JSON. Add a small multipart helper for FormData because uploads must not set `Content-Type` manually.

---

### Task 1: Admin Catalog API Client

**Files:**
- Modify: `apps/front/src/lib/api/http.ts`
- Create: `apps/front/src/lib/api/admin-catalog.ts`
- Create: `apps/front/src/lib/api/admin-catalog.test.ts`
- Modify: `apps/front/src/lib/routes.ts`

- [ ] **Step 1: Add failing API client tests**

Create `admin-catalog.test.ts` with coverage for:

- `routes.adminCatalog()` returns `/admin/catalog`;
- product list builds `/api/admin/catalog/products?page=2&pageSize=20&search=Cable`;
- create product sends JSON with CSRF;
- upload product images sends `FormData` with field name `files` and CSRF, without `Content-Type`;
- upload brand logo sends `FormData` with field name `file` and CSRF;
- delete image/logo use correct routes and CSRF.

- [ ] **Step 2: Run tests to verify failure**

```powershell
npm.cmd test -- admin-catalog.test.ts
```

Expected: FAIL because the API module and routes do not exist.

- [ ] **Step 3: Add multipart helper**

Add to `http.ts`:

```ts
type FormRequestOptions = {
  method?: "POST" | "PUT" | "PATCH" | "DELETE";
  body: FormData;
  csrfToken?: string | null;
};

export async function apiForm<T>(path: string, options: FormRequestOptions): Promise<T> {
  const headers = new Headers();
  headers.set("Accept", "application/json");
  if (options.csrfToken) {
    headers.set("X-CSRF-Token", options.csrfToken);
  }

  const response = await fetch(resolveApiPath(path), {
    method: options.method ?? "POST",
    headers,
    body: options.body,
    credentials: "include",
  });

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  const payload = text ? (JSON.parse(text) as unknown) : null;
  if (!response.ok) {
    const apiError = isApiErrorResponse(payload)
      ? payload
      : { code: "internal_error", message: "Внутренняя ошибка сервера." };
    throw new ApiClientError(response.status, apiError);
  }

  return payload as T;
}
```

- [ ] **Step 4: Add routes and typed client**

Add to `routes.ts`:

```ts
adminCatalog: () => "/admin/catalog",
```

Create `admin-catalog.ts` with exported DTO types matching backend records and functions for every endpoint listed in the API Contracts section. Use `URLSearchParams` for list filters and `encodeURIComponent` for ids in URL path segments.

- [ ] **Step 5: Run client tests**

```powershell
npm.cmd test -- admin-catalog.test.ts
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add apps/front/src/lib/api/http.ts apps/front/src/lib/api/admin-catalog.ts apps/front/src/lib/api/admin-catalog.test.ts apps/front/src/lib/routes.ts
git commit -m "feat: add admin catalog frontend api client"
```

### Task 2: Admin Catalog Page Shell And Access Handling

**Files:**
- Create: `apps/front/src/app/admin/catalog/page.tsx`
- Create: `apps/front/src/app/admin/catalog/catalog-page-client.tsx`
- Create: `apps/front/src/app/admin/catalog/catalog-page-client.test.tsx`
- Create: `apps/front/src/components/admin/catalog/admin-catalog-shell.tsx`
- Create: `apps/front/src/components/admin/catalog/admin-catalog-shell.test.tsx`
- Modify: `apps/front/src/app/globals.css`

- [ ] **Step 1: Add failing shell tests**

Tests must verify:

- seller loads the page and sees tab buttons for `Товары`, `Категории`, `Бренды`, `Характеристики`;
- customer sees forbidden state and no API list calls;
- unauthorized API error redirects to `/auth/login?returnTo=%2Fadmin%2Fcatalog`;
- switching tabs keeps the shell mounted;
- shell has one `h1` and stable tab state.

- [ ] **Step 2: Run tests to verify failure**

```powershell
npm.cmd test -- catalog-page-client.test.tsx admin-catalog-shell.test.tsx
```

Expected: FAIL because files do not exist.

- [ ] **Step 3: Implement shell**

`page.tsx` returns `<CatalogPageClient />`.

`CatalogPageClient` follows the existing `/admin/requests` pattern:

- calls `getMe()`;
- updates `AuthProvider` session;
- redirects unauthenticated users through `routes.login(routes.adminCatalog())`;
- blocks non-staff;
- renders `AdminCatalogShell` after auth passes.

`AdminCatalogShell` owns active section state:

```ts
type AdminCatalogSection = "products" | "categories" | "brands" | "attributes";
```

It renders section containers for managers introduced in later tasks, with empty placeholders that contain no fake data.

- [ ] **Step 4: Add CSS**

Add compact admin catalog classes to `globals.css`:

- `.admin-catalog-page`;
- `.admin-catalog-shell`;
- `.admin-catalog-tabs`;
- `.admin-catalog-toolbar`;
- `.admin-catalog-grid`;
- `.admin-catalog-panel`;
- `.admin-catalog-table`;
- `.admin-catalog-form`;
- `.admin-catalog-status`.

Use responsive grids and keep text inside controls from overflowing.

- [ ] **Step 5: Run shell tests**

```powershell
npm.cmd test -- catalog-page-client.test.tsx admin-catalog-shell.test.tsx
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add apps/front/src/app/admin/catalog/page.tsx apps/front/src/app/admin/catalog/catalog-page-client.tsx apps/front/src/app/admin/catalog/catalog-page-client.test.tsx apps/front/src/components/admin/catalog/admin-catalog-shell.tsx apps/front/src/components/admin/catalog/admin-catalog-shell.test.tsx apps/front/src/app/globals.css
git commit -m "feat: add admin catalog frontend shell"
```

### Task 3: Categories Manager

**Files:**
- Create: `apps/front/src/components/admin/catalog/admin-category-manager.tsx`
- Create: `apps/front/src/components/admin/catalog/admin-category-manager.test.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-catalog-shell.tsx`
- Modify: `apps/front/src/app/globals.css`

- [ ] **Step 1: Add failing category tests**

Cover:

- initial load calls `getAdminCategories`;
- filters by search, parent, and active state;
- selecting a category loads detail into the form;
- create/update sends name, slug, parent, visibility, sort, description, H1, SEO fields with CSRF;
- delete uses CSRF and refreshes list;
- move/sort controls call dedicated endpoints;
- API errors are shown as alerts.

- [ ] **Step 2: Run category tests to verify failure**

```powershell
npm.cmd test -- admin-category-manager.test.tsx
```

Expected: FAIL because manager does not exist.

- [ ] **Step 3: Implement category manager**

Build a two-column admin layout:

- left: filter toolbar and category rows;
- right: editor form for selected category or new category.

Fields:

- `name`, `slug`, `parentId`, `description`, `h1`, `seoTitle`, `seoDescription`, `sortOrder`, `isActive`, `isVisibleInMenu`.

Actions:

- create;
- save;
- delete;
- move parent;
- update sort.

- [ ] **Step 4: Run category tests**

```powershell
npm.cmd test -- admin-category-manager.test.tsx
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add apps/front/src/components/admin/catalog/admin-category-manager.tsx apps/front/src/components/admin/catalog/admin-category-manager.test.tsx apps/front/src/components/admin/catalog/admin-catalog-shell.tsx apps/front/src/app/globals.css
git commit -m "feat: add admin category manager UI"
```

### Task 4: Brands Manager With Logo Controls

**Files:**
- Create: `apps/front/src/components/admin/catalog/admin-brand-manager.tsx`
- Create: `apps/front/src/components/admin/catalog/admin-brand-manager.test.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-catalog-shell.tsx`
- Modify: `apps/front/src/app/globals.css`

- [ ] **Step 1: Add failing brand tests**

Cover:

- list/search/active filters;
- select brand loads detail;
- create/update sends name, slug, description, SEO fields, `isActive`;
- delete handles entity-in-use error;
- logo file input calls `uploadAdminBrandLogo` with `file` FormData and CSRF;
- logo delete calls `deleteAdminBrandLogo`;
- logo preview uses returned `url` and preserves alt text based on brand name.

- [ ] **Step 2: Run brand tests to verify failure**

```powershell
npm.cmd test -- admin-brand-manager.test.tsx
```

Expected: FAIL because manager does not exist.

- [ ] **Step 3: Implement brand manager**

Build:

- list panel with search and active filter;
- editor fields: `name`, `slug`, `description`, `seoTitle`, `seoDescription`, `isActive`;
- logo panel with image preview, file input, replace button, delete button, status/error messages.

Do not store `logoFileId` manually from the form; logo upload/delete uses the dedicated endpoints.

- [ ] **Step 4: Run brand tests**

```powershell
npm.cmd test -- admin-brand-manager.test.tsx
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add apps/front/src/components/admin/catalog/admin-brand-manager.tsx apps/front/src/components/admin/catalog/admin-brand-manager.test.tsx apps/front/src/components/admin/catalog/admin-catalog-shell.tsx apps/front/src/app/globals.css
git commit -m "feat: add admin brand manager UI"
```

### Task 5: Category Attributes And Options Manager

**Files:**
- Create: `apps/front/src/components/admin/catalog/admin-attribute-manager.tsx`
- Create: `apps/front/src/components/admin/catalog/admin-attribute-manager.test.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-catalog-shell.tsx`
- Modify: `apps/front/src/app/globals.css`

- [ ] **Step 1: Add failing attribute tests**

Cover:

- category selector loads attributes for selected category;
- create/update attribute sends all boolean flags and sort order;
- `select` attribute shows option editor;
- option create/update/delete use correct nested routes;
- inherit-from-parent action displays added/skipped result;
- used attributes/options display product values count and deletion errors.

- [ ] **Step 2: Run attribute tests to verify failure**

```powershell
npm.cmd test -- admin-attribute-manager.test.tsx
```

Expected: FAIL because manager does not exist.

- [ ] **Step 3: Implement attribute manager**

Build:

- category picker sourced from categories list;
- attribute table;
- attribute editor with fields: `name`, `code`, `type`, `unit`, flags, `sortOrder`, `isActive`;
- option editor for `type === "select"` with `value`, `slug`, `normalizedValue`, `sortOrder`, `isActive`;
- inherit-from-parent button.

Use select controls for `type` and checkboxes for flags.

- [ ] **Step 4: Run attribute tests**

```powershell
npm.cmd test -- admin-attribute-manager.test.tsx
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add apps/front/src/components/admin/catalog/admin-attribute-manager.tsx apps/front/src/components/admin/catalog/admin-attribute-manager.test.tsx apps/front/src/components/admin/catalog/admin-catalog-shell.tsx apps/front/src/app/globals.css
git commit -m "feat: add admin category attribute manager UI"
```

### Task 6: Products Manager With Main Editor Tabs

**Files:**
- Create: `apps/front/src/components/admin/catalog/admin-product-manager.tsx`
- Create: `apps/front/src/components/admin/catalog/admin-product-manager.test.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-catalog-shell.tsx`
- Modify: `apps/front/src/app/globals.css`

- [ ] **Step 1: Add failing product tests**

Cover:

- list filters by search, category, brand, active state, publish status;
- create product with required fields;
- edit product fields in `Основное`, `SEO`, and `Публикация` tabs;
- readiness issues render from backend response;
- duplicate candidates query runs from product identity fields and displays candidate rows;
- delete product handles entity-in-use errors.

- [ ] **Step 2: Run product tests to verify failure**

```powershell
npm.cmd test -- admin-product-manager.test.tsx
```

Expected: FAIL because product manager does not exist.

- [ ] **Step 3: Implement product manager**

Build:

- product list and filters;
- editor tabs: `Основное`, `Характеристики`, `Изображения`, `SEO`, `Публикация`;
- base fields: category, brand, name, slug, sku, externalId, description, shortDescription, availabilityStatus, saleUnit, unitQuantity, sortOrder;
- SEO fields: h1, seoTitle, seoDescription;
- publication fields: publishStatus, isActive, readiness issues;
- duplicate candidates panel using existing endpoint.

Use category and brand list endpoints for dropdowns.

- [ ] **Step 4: Run product tests**

```powershell
npm.cmd test -- admin-product-manager.test.tsx
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add apps/front/src/components/admin/catalog/admin-product-manager.tsx apps/front/src/components/admin/catalog/admin-product-manager.test.tsx apps/front/src/components/admin/catalog/admin-catalog-shell.tsx apps/front/src/app/globals.css
git commit -m "feat: add admin product manager UI"
```

### Task 7: Product Attributes And Images Panels

**Files:**
- Create: `apps/front/src/components/admin/catalog/admin-product-images-panel.tsx`
- Create: `apps/front/src/components/admin/catalog/admin-product-images-panel.test.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-product-manager.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-product-manager.test.tsx`
- Modify: `apps/front/src/app/globals.css`

- [ ] **Step 1: Add failing panel tests**

Cover:

- attributes tab renders controls by attribute type: text, number, boolean, select;
- saving attributes calls `updateAdminProductAttributes` with CSRF;
- images tab loads product images;
- multiple file upload appends every file under `files`;
- alt/title update calls image metadata endpoint;
- main image action calls main endpoint;
- reorder action sends ordered image ids;
- delete image calls delete endpoint and refreshes images.

- [ ] **Step 2: Run panel tests to verify failure**

```powershell
npm.cmd test -- admin-product-images-panel.test.tsx admin-product-manager.test.tsx
```

Expected: FAIL because image panel and attribute save flow are not complete.

- [ ] **Step 3: Implement attributes tab**

Use product detail `Attributes` rows to render:

- text input for `text`;
- number input for `number`;
- checkbox for `boolean`;
- select for `select` using active options loaded from category attributes endpoint.

Save with `PUT /api/admin/catalog/products/{id}/attributes`.

- [ ] **Step 4: Implement images panel**

Build image grid:

- thumbnail;
- original filename;
- `alt`;
- `title`;
- main marker/action;
- sort controls;
- delete action;
- file input for multiple upload.

Do not implement drag-and-drop unless it is small and tested; up/down controls are acceptable for this slice.

- [ ] **Step 5: Run panel tests**

```powershell
npm.cmd test -- admin-product-images-panel.test.tsx admin-product-manager.test.tsx
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add apps/front/src/components/admin/catalog/admin-product-images-panel.tsx apps/front/src/components/admin/catalog/admin-product-images-panel.test.tsx apps/front/src/components/admin/catalog/admin-product-manager.tsx apps/front/src/components/admin/catalog/admin-product-manager.test.tsx apps/front/src/app/globals.css
git commit -m "feat: add admin product attributes and images UI"
```

### Task 8: Frontend Verification And Browser QA

**Files:**
- Verify all frontend files touched by Tasks 1-7.
- Modify only if verification reveals a real issue.

- [ ] **Step 1: Run focused frontend tests**

```powershell
npm.cmd test -- admin-catalog
```

Expected: all admin catalog tests pass.

- [ ] **Step 2: Run full frontend tests**

```powershell
npm.cmd test
```

Expected: PASS.

- [ ] **Step 3: Run frontend lint/build**

```powershell
npm.cmd run lint
npm.cmd run build
```

Expected: PASS with 0 errors.

- [ ] **Step 4: Run solution verification**

```powershell
dotnet test .\LineCom.sln
dotnet build .\LineCom.sln
```

Expected: PASS with 0 failures and 0 errors. NU1900 warnings from unavailable NuGet vulnerability feed are acceptable if the commands succeed.

- [ ] **Step 5: Browser QA**

Start the frontend and API if needed, then use Browser Use / Playwright to inspect:

- `/admin/catalog` at desktop width;
- `/admin/catalog` at mobile width;
- auth redirect path;
- forbidden state;
- product editor tabs;
- image upload panel layout;
- brand logo panel layout.

Expected:

- no overlapping text;
- controls remain inside containers;
- tables/cards are readable;
- image previews render;
- no blank main panel;
- no console errors from admin catalog interactions.

- [ ] **Step 6: Hygiene checks**

```powershell
git diff --check
git status --short --branch
rg -n "TODO|TBD|temporary|hack|EntityFramework|DbContext" apps/front apps/api tests docs vault
```

Expected:

- no whitespace errors;
- only intended frontend docs/files changed;
- `admin-catalog-homepage-slice.png` remains untracked and unstaged;
- no EF/DbContext usage introduced;
- no unresolved work markers in changed implementation files.

- [ ] **Step 7: Commit verification fixes only if changed**

```powershell
git add apps/front
git commit -m "fix: polish admin catalog frontend"
```

If no files changed, do not create an empty commit.

## Multi-Agent Execution Order

Use one worker per task and run review before moving on:

1. Worker Task 1: API client.
2. Spec review Task 1.
3. Quality review Task 1.
4. Push Task 1 if accepted.
5. Repeat for Tasks 2-7.
6. Task 8 can be run locally because it is verification-heavy; use a reviewer for any fix loop.

Each worker must be told:

- it is not alone in the codebase;
- it must not revert edits made by others;
- it must only stage files in its task scope;
- it must not touch `admin-catalog-homepage-slice.png`;
- it must follow TDD and show RED/GREEN evidence;
- it must commit at the end of its task only after tests pass.

## Continuation Prompt After Context Cleanup

Use this prompt after compaction or in a new session:

```text
Продолжаем LineCom в D:\Projects\FL\LineCom.

Обязательные правила:
- Все ответы пользователю на русском.
- Соблюдать AGENTS.md.
- Использовать Context7 для вопросов по библиотекам, SDK, API, CLI.
- Backend только PostgreSQL/Npgsql/Dapper, без Entity Framework.
- Миграции только SQL через DbUp.
- Local FileStorage — целевой file-storage подход.
- SEO/GEO учитывать при товарах, брендах, категориях, slug, metadata.
- Не трогать untracked admin-catalog-homepage-slice.png.

Текущий статус:
- main синхронизирован с origin/main.
- Последний backend commit: 7338949 fix: harden admin catalog image safety.
- Backend admin catalog CRUD/API и image/logo endpoints готовы и запушены.
- Последняя backend verification: focused image/admin 115 passed, AdminCatalog 226 passed, full suite 670 passed, build 0 errors.
- Ожидаемые предупреждения: NU1900 из-за недоступного NuGet vulnerability feed.
- Единственный ожидаемый untracked файл: admin-catalog-homepage-slice.png.

Следующий план:
- docs/superpowers/plans/2026-05-12-admin-catalog-frontend.md
- Scope: frontend-only admin catalog UI in apps/front over existing backend endpoints.
- Out of scope: backend endpoints, homepage mutation endpoints/UI, import/export, audit log, LLM duplicate checking, crop/resize/media library, prices/stock/checkout.

Исполнение:
- Использовать superpowers:subagent-driven-development.
- Работать task-by-task из плана.
- После каждого task: spec review, quality review, fix loop при необходимости, затем commit/push.
- Начать с Task 1: Admin Catalog API Client.
- Перед кодом открыть план и релевантные files:
  - apps/front/src/lib/api/http.ts
  - apps/front/src/lib/routes.ts
  - apps/front/src/lib/api/admin-requests.ts
  - apps/front/src/lib/api/admin-requests.test.ts
  - apps/api/Modules/Catalog/DTOs

Требования к Task 1:
- добавить multipart helper apiForm без ручного Content-Type;
- создать apps/front/src/lib/api/admin-catalog.ts с типами и функциями для всех admin catalog endpoints;
- добавить apps/front/src/lib/api/admin-catalog.test.ts;
- расширить routes.adminCatalog();
- TDD: сначала RED, затем implementation, затем GREEN;
- не stage/commit admin-catalog-homepage-slice.png.
```

