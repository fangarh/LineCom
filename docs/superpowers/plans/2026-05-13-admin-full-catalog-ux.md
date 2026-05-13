# Admin Full Catalog UX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Реализовать согласованный срез Admin Full Catalog UX: role-aware меню администрирования, рабочий каталог товаров и категорий, поиск для главной, автогенерацию slug и смену пароля текущего пользователя.

**Architecture:** Сначала выполняется узкая декомпозиция крупных frontend-компонентов, затем задачи можно раздать независимым worker-агентам с непересекающимися зонами записи. Backend меняется только для смены пароля текущего пользователя и минимальной проверки существующей slug-валидации; публичные SEO/GEO маршруты, sitemap, robots, canonical и metadata не меняются.

**Tech Stack:** Next.js/React/Vitest/Testing Library, ASP.NET Core Web API, PostgreSQL через Npgsql и Dapper, SQL-миграции через DbUp, cookie-auth с CSRF.

---

## Current Context

- Ветка: `main...origin/main`.
- Последние релевантные коммиты: `553a145 docs: add admin ux start prompt`, `92337b8 docs: design admin full catalog ux`.
- Untracked PNG не относятся к задаче и не stage/commit: `admin-catalog-homepage-slice.png`, `dns-master-current.png`, `old_cite.png`.
- Свежего плана `docs/superpowers/plans/2026-05-13-admin-full-catalog-ux.md` до этой записи не было.
- Источник истины: `vault/Человекочитаемое`.

## Write Boundaries For Workers

- `Navigation/Auth worker`: `apps/front/src/components/layout/site-header.tsx`, `apps/front/src/components/auth/auth-provider.tsx`, `apps/front/src/lib/api/auth.ts`, связанные frontend tests и CSS header.
- `Catalog Products worker`: `apps/front/src/components/admin/catalog/admin-product-manager.tsx`, `admin-product-list-panel.tsx`, новые product-list helpers/components/tests, CSS product list.
- `Category Tree worker`: `apps/front/src/components/admin/catalog/admin-category-manager.tsx`, новые category tree helpers/components/tests, CSS category tree.
- `Homepage Search worker`: `apps/front/src/components/admin/homepage/*`, `apps/front/src/lib/api/admin-homepage.ts`, read-only imports from `admin-catalog.ts`, homepage tests, CSS homepage.
- `Slug worker`: shared slug helper/tests and slug field integration in catalog managers: product, category, brand, attribute option.
- `Password worker`: `apps/api/Modules/Account/*`, `tests/LineCom.Api.Tests/Modules/Account/*`, `apps/front/src/lib/api/account.ts`, account profile components/tests.

Every worker must be told: you are not alone in the codebase, do not revert edits made by others, and adapt your implementation to current workspace changes.

## Task 1: Navigation And Auth Session Restore

**Files:**
- Modify: `apps/front/src/components/auth/auth-provider.tsx`
- Modify: `apps/front/src/components/layout/site-header.tsx`
- Modify: `apps/front/src/lib/routes.ts`
- Test: existing or new `apps/front/src/components/layout/site-header.test.tsx`
- Test: existing or new `apps/front/src/components/auth/auth-provider.test.tsx`
- Modify: `apps/front/src/app/globals.css`

- [ ] **Step 1: Write failing header/auth tests**

Add tests proving:

```tsx
render(
  <AuthProvider initialSession={{ user: sellerUser, csrfToken: "csrf" }}>
    <SiteHeader />
  </AuthProvider>,
);

await user.click(screen.getByRole("button", { name: "Администрирование" }));

expect(screen.getByRole("link", { name: "Заявки клиентов" })).toHaveAttribute("href", "/admin/requests");
expect(screen.getByRole("link", { name: "Каталог админки" })).toHaveAttribute("href", "/admin/catalog");
expect(screen.getByRole("link", { name: "Главная админки" })).toHaveAttribute("href", "/admin/homepage");
```

Also assert `customer` and anonymous users do not see `Администрирование`, and authenticated users see profile/request links without exposing admin links to customers.

- [ ] **Step 2: Run failing tests**

Run: `npm.cmd test -- apps/front/src/components/layout/site-header.test.tsx apps/front/src/components/auth/auth-provider.test.tsx`

Expected: FAIL because `AuthProvider` does not restore sessions and `SiteHeader` renders only anonymous actions.

- [ ] **Step 3: Extend auth provider session restore**

Extend provider state:

```ts
type AuthStatus = "idle" | "restoring" | "authenticated" | "anonymous";

type AuthContextValue = {
  user: CurrentUser | null;
  csrfToken: string | null;
  status: AuthStatus;
  setSession: (session: AuthSession) => void;
  clearSession: () => void;
  restoreSession: () => Promise<void>;
};
```

Implementation must call existing `getMe()` once on mount, ignore `auth.unauthorized` as anonymous, clear state on inactive sessions, and never expose cookie values to JavaScript.

- [ ] **Step 4: Implement role-aware header**

Use `useAuth()` in `SiteHeader`. Render:

```ts
const isStaff = user?.role === "seller" || user?.role === "admin";
```

Desktop and mobile menu both contain one `Администрирование` group for staff with:

- `routes.adminRequests()` -> `Заявки клиентов`
- `routes.adminCatalog()` -> `Каталог админки`
- `routes.adminHomepage()` -> `Главная админки`

Customer/account links remain under account navigation. Anonymous users keep `Войти`.

- [ ] **Step 5: Style menu without overflow**

Add bounded menu styles in `globals.css`: iconless button or text button for `Администрирование`, dropdown list width with `min-width: 220px`, mobile in-flow expansion, no absolute dropdown on mobile.

- [ ] **Step 6: Run focused tests**

Run: `npm.cmd test -- apps/front/src/components/layout/site-header.test.tsx apps/front/src/components/auth/auth-provider.test.tsx`

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add apps/front/src/components/auth/auth-provider.tsx apps/front/src/components/layout/site-header.tsx apps/front/src/lib/routes.ts apps/front/src/app/globals.css apps/front/src/components/layout/site-header.test.tsx apps/front/src/components/auth/auth-provider.test.tsx
git commit -m "feat: add role-aware admin navigation"
```

## Task 2: Compact Product Table With Pagination

**Files:**
- Modify: `apps/front/src/components/admin/catalog/admin-product-manager.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-product-list-panel.tsx`
- Create: `apps/front/src/components/admin/catalog/admin-product-list-helpers.ts`
- Create: `apps/front/src/components/admin/catalog/admin-product-list-helpers.test.ts`
- Modify: `apps/front/src/components/admin/catalog/admin-product-manager.test.tsx`
- Modify: `apps/front/src/app/globals.css`

- [ ] **Step 1: Write failing helper tests**

Create tests for product row badges:

```ts
expect(getProductIssueLabels({
  slug: "",
  categoryName: "",
  isActive: false,
  publishStatus: "published",
  readiness: { canPublish: false, issues: [{ code: "missing_seo", message: "Нет SEO." }] },
})).toEqual(["нет категории", "нет slug", "неактивен", "Нет SEO."]);
```

Add pagination range tests:

```ts
expect(formatPageRange({ page: 2, pageSize: 60, totalItems: 145 })).toBe("61-120 из 145");
expect(formatPageRange({ page: 1, pageSize: 60, totalItems: 0 })).toBe("0 из 0");
```

- [ ] **Step 2: Write failing component tests**

Extend `admin-product-manager.test.tsx`:

```tsx
expect(adminCatalogApiMock.getAdminProducts).toHaveBeenCalledWith({ page: 1, pageSize: 60 });
expect(screen.getByText("1-2 из 2")).toBeInTheDocument();
expect(screen.getByRole("columnheader", { name: "Категория" })).toBeInTheDocument();
expect(screen.getByText("Черновик")).toBeInTheDocument();
```

Add next-page assertion:

```tsx
await user.click(screen.getByRole("button", { name: "Дальше" }));
expect(adminCatalogApiMock.getAdminProducts).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2, pageSize: 60 }));
```

- [ ] **Step 3: Run failing tests**

Run: `npm.cmd test -- apps/front/src/components/admin/catalog/admin-product-list-helpers.test.ts apps/front/src/components/admin/catalog/admin-product-manager.test.tsx`

Expected: FAIL because helpers and pagination are absent.

- [ ] **Step 4: Implement list state and API params**

In `AdminProductManager`, add:

```ts
const defaultProductPageSize = 60;
const [page, setPage] = useState(1);
const [pageSize, setPageSize] = useState(defaultProductPageSize);
const [productListMeta, setProductListMeta] = useState({
  page: 1,
  pageSize: defaultProductPageSize,
  totalItems: 0,
  totalPages: 1,
});
```

Include `page` and `pageSize` in `listParams`, reset page to `1` when search/filter values change, and set meta from `getAdminProducts` response.

- [ ] **Step 5: Implement compact table component**

Replace button rows with a real table in `AdminProductListPanel`:

```tsx
<table className="admin-product-table">
  <thead>
    <tr>
      <th>Товар</th>
      <th>SKU / externalId</th>
      <th>Категория</th>
      <th>Бренд</th>
      <th>Статусы</th>
      <th>Проблемы</th>
    </tr>
  </thead>
  <tbody>
    {products.map((product) => (
      <tr key={product.id} data-selected={selectedProductId === product.id}>
        <td>
          <button type="button" onClick={() => onProductSelect(product.id)}>
            <strong>{product.name}</strong>
            <small>{product.slug || "нет slug"}</small>
          </button>
        </td>
        <td>{product.sku ?? product.externalId ?? "без артикула"}</td>
        <td>{product.categoryName || "без категории"}</td>
        <td>{product.brandName ?? "без бренда"}</td>
        <td>{renderStatusBadges(product)}</td>
        <td>{renderIssueBadges(product)}</td>
      </tr>
    ))}
  </tbody>
</table>
```

Use helper maps for publish status labels: `draft -> Черновик`, `review -> Проверка`, `published -> Опубликован`, `archived -> Архив`.

- [ ] **Step 6: Add pagination controls**

Props in `AdminProductListPanel`:

```ts
page: number;
pageSize: number;
totalItems: number;
totalPages: number;
onPageChange: (page: number) => void;
onPageSizeChange: (pageSize: number) => void;
```

Render `Назад`, `Дальше`, `1-60 из N`, page size select with `20`, `40`, `60`.

- [ ] **Step 7: Run focused tests**

Run: `npm.cmd test -- apps/front/src/components/admin/catalog/admin-product-list-helpers.test.ts apps/front/src/components/admin/catalog/admin-product-manager.test.tsx`

Expected: PASS.

- [ ] **Step 8: Commit**

```powershell
git add apps/front/src/components/admin/catalog/admin-product-manager.tsx apps/front/src/components/admin/catalog/admin-product-list-panel.tsx apps/front/src/components/admin/catalog/admin-product-list-helpers.ts apps/front/src/components/admin/catalog/admin-product-list-helpers.test.ts apps/front/src/components/admin/catalog/admin-product-manager.test.tsx apps/front/src/app/globals.css
git commit -m "feat: add compact admin product table"
```

## Task 3: Category Tree And Parent Picker

**Files:**
- Modify: `apps/front/src/components/admin/catalog/admin-category-manager.tsx`
- Create: `apps/front/src/components/admin/catalog/admin-category-tree-helpers.ts`
- Create: `apps/front/src/components/admin/catalog/admin-category-tree-helpers.test.ts`
- Create: `apps/front/src/components/admin/catalog/admin-category-tree.tsx`
- Create: `apps/front/src/components/admin/catalog/admin-category-parent-picker.tsx`
- Create: `apps/front/src/components/admin/catalog/admin-category-form.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-category-manager.test.tsx`
- Modify: `apps/front/src/app/globals.css`

- [ ] **Step 1: Write failing tree helper tests**

Create tests:

```ts
const tree = buildCategoryTree([rootCategory, childCategory, connectorCategory]);
expect(tree[0].children[0].id).toBe("cat-child");
expect(getBlockedParentIds(tree, "cat-root")).toEqual(new Set(["cat-root", "cat-child"]));
expect(flattenCategoryTree(tree).map((node) => `${node.depth}:${node.category.name}`)).toEqual([
  "0:Кабели",
  "1:Силовые кабели",
  "0:Разъемы",
]);
```

- [ ] **Step 2: Write failing manager tests**

Extend category tests:

```tsx
expect(screen.getByRole("tree", { name: "Дерево категорий" })).toBeInTheDocument();
expect(screen.getByRole("treeitem", { name: /Кабели.*4 товаров.*1 подкатегория/ })).toBeInTheDocument();
expect(screen.getByRole("treeitem", { name: /Силовые кабели.*2 товаров/ })).toHaveAttribute("aria-level", "2");
```

For selected category, parent picker must exclude itself and descendants:

```tsx
await user.click(screen.getByRole("treeitem", { name: /Кабели/ }));
await user.click(screen.getByRole("button", { name: "Выбрать родителя" }));
expect(screen.queryByRole("option", { name: "Кабели" })).not.toBeInTheDocument();
expect(screen.queryByRole("option", { name: "Силовые кабели" })).not.toBeInTheDocument();
expect(screen.getByRole("option", { name: "Разъемы" })).toBeInTheDocument();
```

- [ ] **Step 3: Run failing tests**

Run: `npm.cmd test -- apps/front/src/components/admin/catalog/admin-category-tree-helpers.test.ts apps/front/src/components/admin/catalog/admin-category-manager.test.tsx`

Expected: FAIL because tree helpers/components do not exist.

- [ ] **Step 4: Implement helpers**

Use pure helpers:

```ts
export type CategoryTreeNode = {
  category: AdminCategoryListItem;
  children: CategoryTreeNode[];
};

export function buildCategoryTree(categories: AdminCategoryListItem[]): CategoryTreeNode[] {
  const byId = new Map(categories.map((category) => [category.id, { category, children: [] as CategoryTreeNode[] }]));
  const roots: CategoryTreeNode[] = [];

  for (const node of byId.values()) {
    const parent = node.category.parentId ? byId.get(node.category.parentId) : null;
    if (parent) parent.children.push(node);
    else roots.push(node);
  }

  sortTree(roots);
  return roots;
}
```

Sort by `sortOrder`, then `name`. `getBlockedParentIds` includes selected id and all descendants.

- [ ] **Step 5: Decompose category manager**

Move presentational rendering into:

- `AdminCategoryTree`
- `AdminCategoryParentPicker`
- `AdminCategoryForm`

Keep `AdminCategoryManager` responsible for loading, filters, selected detail, mutations, stale response guards, and CSRF handling.

- [ ] **Step 6: Replace flat select parent UX**

Use `AdminCategoryParentPicker` for:

- `form.parentId`
- `moveParentId`
- parent filter if a compact tree picker fits without breaking current tests

The picker shows `Без родителя`, category depth indentation, active/menu badges, product count, child count, and disables blocked ids.

- [ ] **Step 7: Run focused tests**

Run: `npm.cmd test -- apps/front/src/components/admin/catalog/admin-category-tree-helpers.test.ts apps/front/src/components/admin/catalog/admin-category-manager.test.tsx`

Expected: PASS.

- [ ] **Step 8: Commit**

```powershell
git add apps/front/src/components/admin/catalog/admin-category-manager.tsx apps/front/src/components/admin/catalog/admin-category-tree-helpers.ts apps/front/src/components/admin/catalog/admin-category-tree-helpers.test.ts apps/front/src/components/admin/catalog/admin-category-tree.tsx apps/front/src/components/admin/catalog/admin-category-parent-picker.tsx apps/front/src/components/admin/catalog/admin-category-form.tsx apps/front/src/components/admin/catalog/admin-category-manager.test.tsx apps/front/src/app/globals.css
git commit -m "feat: add admin category tree"
```

## Task 4: Homepage Product And Category Search

**Files:**
- Modify: `apps/front/src/components/admin/homepage/admin-homepage-manager.tsx`
- Create: `apps/front/src/components/admin/homepage/admin-homepage-section-list.tsx`
- Create: `apps/front/src/components/admin/homepage/admin-homepage-section-editor.tsx`
- Create: `apps/front/src/components/admin/homepage/admin-homepage-target-search.tsx`
- Create: `apps/front/src/components/admin/homepage/admin-homepage-visibility.ts`
- Create: `apps/front/src/components/admin/homepage/admin-homepage-visibility.test.ts`
- Modify: `apps/front/src/components/admin/homepage/admin-homepage-manager.test.tsx`
- Modify: `apps/front/src/lib/api/admin-homepage.ts`
- Modify: `apps/front/src/app/globals.css`

- [ ] **Step 1: Write failing visibility/search tests**

Add helper tests:

```ts
expect(describeHomepageTargetVisibility({
  type: "product",
  isActive: true,
  publishStatus: "published",
  slug: "kabel",
  categoryName: "Кабели",
})).toBe("Попадет на витрину");

expect(describeHomepageTargetVisibility({
  type: "product",
  isActive: false,
  publishStatus: "draft",
  slug: "",
  categoryName: "",
})).toBe("Не попадет: товар неактивен, не опубликован, нет slug, нет категории");
```

- [ ] **Step 2: Write failing manager tests**

Change test from UUID input to search:

```tsx
await user.type(await screen.findByLabelText("Поиск товара"), "кабель");
expect(adminCatalogApiMock.getAdminProducts).toHaveBeenCalledWith({ search: "кабель", page: 1, pageSize: 10 });
await user.click(await screen.findByRole("button", { name: /Добавить Кабель ВВГнг/ }));
expect(adminHomepageApiMock.addAdminHomepageSectionItem).toHaveBeenCalledWith(
  "section-products",
  { productId: "product-1", categoryId: null, sortOrder: null, isActive: true },
  "csrf",
);
expect(screen.queryByLabelText("UUID товара")).not.toBeInTheDocument();
```

Add category section test using `getAdminCategories({ search, page: 1, pageSize: 10 })`.

- [ ] **Step 3: Run failing tests**

Run: `npm.cmd test -- apps/front/src/components/admin/homepage/admin-homepage-visibility.test.ts apps/front/src/components/admin/homepage/admin-homepage-manager.test.tsx`

Expected: FAIL because search UI is absent.

- [ ] **Step 4: Decompose homepage manager**

Move rendering into:

- `AdminHomepageSectionList`
- `AdminHomepageSectionEditor`
- `AdminHomepageTargetSearch`
- existing `AdminHomepageItemList`

Keep manager responsible for section loading, active section selection, draft sync, pending mutation guard, and refresh.

- [ ] **Step 5: Implement target search**

For `product_list`, call existing catalog API:

```ts
getAdminProducts({ search: query.trim(), page: 1, pageSize: 10 });
```

For `category_list`:

```ts
getAdminCategories({ search: query.trim(), page: 1, pageSize: 10 });
```

Render result rows with type, name, slug, `sku ?? externalId`, active state, publish status for products, and visibility text from helper. Add button sends internal ID through existing `addAdminHomepageSectionItem`.

- [ ] **Step 6: Keep CSRF and duplicate-submit guard**

Search is GET and needs no CSRF. Adding still uses existing CSRF token and existing `beginPendingAction("add-item")`.

- [ ] **Step 7: Run focused tests**

Run: `npm.cmd test -- apps/front/src/components/admin/homepage/admin-homepage-visibility.test.ts apps/front/src/components/admin/homepage/admin-homepage-manager.test.tsx`

Expected: PASS.

- [ ] **Step 8: Commit**

```powershell
git add apps/front/src/components/admin/homepage/admin-homepage-manager.tsx apps/front/src/components/admin/homepage/admin-homepage-section-list.tsx apps/front/src/components/admin/homepage/admin-homepage-section-editor.tsx apps/front/src/components/admin/homepage/admin-homepage-target-search.tsx apps/front/src/components/admin/homepage/admin-homepage-visibility.ts apps/front/src/components/admin/homepage/admin-homepage-visibility.test.ts apps/front/src/components/admin/homepage/admin-homepage-manager.test.tsx apps/front/src/lib/api/admin-homepage.ts apps/front/src/app/globals.css
git commit -m "feat: add homepage item search"
```

## Task 5: Shared Slug Generation And Manual Override

**Files:**
- Create: `apps/front/src/lib/catalog/slug.ts`
- Create: `apps/front/src/lib/catalog/slug.test.ts`
- Modify: `apps/front/src/components/admin/catalog/admin-product-editor-helpers.ts`
- Modify: `apps/front/src/components/admin/catalog/admin-product-main-fields.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-product-manager.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-product-manager.test.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-category-manager.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-category-manager.test.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-brand-manager.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-brand-manager.test.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-attribute-manager.tsx`
- Modify: `apps/front/src/components/admin/catalog/admin-attribute-manager.test.tsx`
- Review only: `apps/api/Modules/Catalog/Services/AdminCatalogInput.cs`
- Review only: `apps/api/Modules/Catalog/Services/AdminCatalogProductService.cs`

- [ ] **Step 1: Write failing slug tests**

Create tests:

```ts
expect(generateSlug("Кабель ВВГнг 3x2.5")).toBe("kabel-vvgng-3x2-5");
expect(generateSlug("  Муфта---кабельная 1кВ  ")).toBe("mufta-kabelnaya-1kv");
expect(generateSlug("LC/UPC адаптер")).toBe("lc-upc-adapter");
expect(generateSlug("!!!")).toBe("");
```

- [ ] **Step 2: Write failing UI tests**

Product:

```tsx
await user.click(screen.getByRole("button", { name: "Новый товар" }));
await user.type(within(editor).getByLabelText("Название"), "Муфта кабельная 1кВ");
expect(within(editor).getByLabelText("Slug")).toHaveValue("mufta-kabelnaya-1kv");
await user.clear(within(editor).getByLabelText("Slug"));
await user.type(within(editor).getByLabelText("Slug"), "manual-slug");
await user.clear(within(editor).getByLabelText("Название"));
await user.type(within(editor).getByLabelText("Название"), "Другое название");
expect(within(editor).getByLabelText("Slug")).toHaveValue("manual-slug");
await user.click(within(editor).getByRole("button", { name: "Сгенерировать заново" }));
expect(within(editor).getByLabelText("Slug")).toHaveValue("drugoe-nazvanie");
```

Repeat for category and brand. For select option in `AdminAttributeManager`, apply the same rule to option `value -> slug`.

- [ ] **Step 3: Run failing tests**

Run: `npm.cmd test -- apps/front/src/lib/catalog/slug.test.ts apps/front/src/components/admin/catalog/admin-product-manager.test.tsx apps/front/src/components/admin/catalog/admin-category-manager.test.tsx apps/front/src/components/admin/catalog/admin-brand-manager.test.tsx apps/front/src/components/admin/catalog/admin-attribute-manager.test.tsx`

Expected: FAIL because helper and auto-fill behavior are absent.

- [ ] **Step 4: Implement transliteration helper**

Use explicit Russian map:

```ts
const transliterationMap: Record<string, string> = {
  а: "a", б: "b", в: "v", г: "g", д: "d", е: "e", ё: "e", ж: "zh", з: "z", и: "i", й: "y",
  к: "k", л: "l", м: "m", н: "n", о: "o", п: "p", р: "r", с: "s", т: "t", у: "u", ф: "f",
  х: "h", ц: "c", ч: "ch", ш: "sh", щ: "sch", ы: "y", э: "e", ю: "yu", я: "ya",
  ь: "", ъ: "",
};
```

Normalize to lowercase, transliterate, replace non-alphanumeric sequences with `-`, collapse dashes, trim dashes.

- [ ] **Step 5: Add dirty-slug state per editor**

For each form, track `isSlugManual` or equivalent in the container:

```ts
function handleNameChange(name: string) {
  setForm((current) => ({
    ...current,
    name,
    slug: current.isSlugManual ? current.slug : generateSlug(name),
  }));
}
```

When loading existing entity details, mark slug manual as `true` so changing a published/existing entity name does not silently rewrite a public URL. For create form, mark manual as `false`.

- [ ] **Step 6: Add regenerate action and conflict messaging**

Add button near slug:

```tsx
<button type="button" className="button button--ghost" onClick={onRegenerateSlug}>
  Сгенерировать заново
</button>
```

Existing `normalizeApiError` already shows `admin_catalog.slug_already_exists`; keep that path and ensure tests assert the public message.

- [ ] **Step 7: Backend validation review**

Read `AdminCatalogInput.RequireText` and upsert service methods. Confirm backend still requires non-empty slug and maps unique conflicts to `admin_catalog.slug_already_exists`. If backend accepts uppercase or separators unchanged, add normalization in `AdminCatalogInput` with unit tests in `tests/LineCom.Api.Tests/Modules/Catalog/AdminCatalog*ServiceTests.cs`; otherwise record in commit message that backend validation already enforces current contract.

- [ ] **Step 8: Run focused tests**

Run the same command from Step 3.

Expected: PASS.

- [ ] **Step 9: Commit**

```powershell
git add apps/front/src/lib/catalog/slug.ts apps/front/src/lib/catalog/slug.test.ts apps/front/src/components/admin/catalog/admin-product-editor-helpers.ts apps/front/src/components/admin/catalog/admin-product-main-fields.tsx apps/front/src/components/admin/catalog/admin-product-manager.tsx apps/front/src/components/admin/catalog/admin-product-manager.test.tsx apps/front/src/components/admin/catalog/admin-category-manager.tsx apps/front/src/components/admin/catalog/admin-category-manager.test.tsx apps/front/src/components/admin/catalog/admin-brand-manager.tsx apps/front/src/components/admin/catalog/admin-brand-manager.test.tsx apps/front/src/components/admin/catalog/admin-attribute-manager.tsx apps/front/src/components/admin/catalog/admin-attribute-manager.test.tsx
git commit -m "feat: add admin slug generation"
```

## Task 6: Current User Password Change

**Files:**
- Modify: `apps/api/Modules/Account/DTOs/AccountDtos.cs`
- Modify: `apps/api/Modules/Account/Controllers/AccountProfileController.cs`
- Modify: `apps/api/Modules/Account/Services/IAccountProfileService.cs`
- Modify: `apps/api/Modules/Account/Services/AccountProfileService.cs`
- Modify: `apps/api/Modules/Account/Repositories/IAccountProfileRepository.cs`
- Modify: `apps/api/Modules/Account/Repositories/DapperAccountProfileRepository.cs`
- Modify: `apps/api/Modules/Account/AccountServiceCollectionExtensions.cs` if a separate service is introduced
- Modify: `tests/LineCom.Api.Tests/Modules/Account/AccountProfileServiceTests.cs`
- Modify: `tests/LineCom.Api.Tests/Modules/Account/AccountProfileEndpointTests.cs`
- Modify: `apps/front/src/lib/api/account.ts`
- Create: `apps/front/src/components/account/password-form.tsx`
- Create: `apps/front/src/components/account/password-form.test.tsx`
- Modify: `apps/front/src/app/account/profile/profile-page-client.tsx`

- [ ] **Step 1: Write failing backend service tests**

Add tests:

```csharp
[Fact]
public async Task ChangePasswordAsync_VerifiesCurrentPasswordAndStoresNewHash()
{
    var user = TestUser();
    var repository = new CapturingAccountProfileRepository { PasswordHash = "old-hash" };
    var hasher = new CapturingPasswordHasher(verified: true, hash: "new-hash");
    var service = new AccountProfileService(new ReturningCurrentUserService(user), repository, hasher);

    await service.ChangePasswordAsync(
        new DefaultHttpContext(),
        new ChangeAccountPasswordRequest("old-password", "new-password"),
        CancellationToken.None);

    Assert.Equal(user.Id, repository.LastPasswordUserId);
    Assert.Equal("old-hash", hasher.LastVerifiedHash);
    Assert.Equal("old-password", hasher.LastVerifiedPassword);
    Assert.Equal("new-password", hasher.LastHashedPassword);
    Assert.Equal("new-hash", repository.LastPasswordHash);
}
```

Add tests for invalid current password and new password length `< 8` and `> 128`.

- [ ] **Step 2: Write failing endpoint tests**

Add:

```csharp
using var request = new HttpRequestMessage(HttpMethod.Put, "/api/account/password")
{
    Content = JsonContent.Create(new ChangeAccountPasswordRequest("old-password", "new-password"))
};
request.Headers.Add("X-CSRF-Token", "csrf-token");

using var response = await client.SendAsync(request);

Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
```

Also assert missing CSRF is forbidden and unauthenticated request is unauthorized.

- [ ] **Step 3: Write failing frontend tests**

`PasswordForm` tests:

```tsx
await user.type(screen.getByLabelText("Текущий пароль"), "old-password");
await user.type(screen.getByLabelText("Новый пароль"), "new-password");
await user.type(screen.getByLabelText("Повтор нового пароля"), "different-password");
await user.click(screen.getByRole("button", { name: "Сменить пароль" }));
expect(onSubmit).not.toHaveBeenCalled();
expect(screen.getByRole("alert")).toHaveTextContent("Новый пароль и повтор не совпадают.");
```

Add success submit sends only `{ currentPassword, newPassword }`.

- [ ] **Step 4: Run failing tests**

Run:

```powershell
dotnet test .\tests\LineCom.Api.Tests\LineCom.Api.Tests.csproj --filter "FullyQualifiedName~Account"
npm.cmd test -- apps/front/src/components/account/password-form.test.tsx
```

Expected: FAIL because endpoint, repository methods and form do not exist.

- [ ] **Step 5: Implement DTO, repository and service**

DTO:

```csharp
public sealed record ChangeAccountPasswordRequest(
    string? CurrentPassword,
    string? NewPassword);
```

Repository methods:

```csharp
Task<string?> FindPasswordHashAsync(Guid userId, CancellationToken cancellationToken = default);
Task UpdatePasswordHashAsync(Guid userId, string passwordHash, CancellationToken cancellationToken = default);
```

SQL:

```sql
SELECT password_hash FROM users WHERE id = @UserId AND is_active = TRUE LIMIT 1;

UPDATE users
SET password_hash = @PasswordHash
WHERE id = @UserId;
```

Service rules:

- require active current user via existing `IAuthCurrentUserService`
- require current password text
- require new password length 8..128
- verify current password via `IPasswordHasher.VerifyPassword`
- hash new password via `IPasswordHasher.HashPassword`
- throw controlled error for wrong current password, for example `account.invalid_current_password`

- [ ] **Step 6: Implement endpoint**

In controller:

```csharp
[RequireCsrfToken]
[HttpPut("password")]
public async Task<IActionResult> ChangePassword(
    ChangeAccountPasswordRequest request,
    CancellationToken cancellationToken)
{
    await _profileService.ChangePasswordAsync(HttpContext, request, cancellationToken);
    return NoContent();
}
```

- [ ] **Step 7: Implement frontend API and form**

API:

```ts
export type ChangePasswordPayload = {
  currentPassword: string;
  newPassword: string;
};

export function changePassword(payload: ChangePasswordPayload, csrfToken: string) {
  return apiJson<void>("/api/account/password", {
    method: "PUT",
    body: payload,
    csrfToken,
  });
}
```

Render `PasswordForm` in `ProfilePageClient` next to contact and organization sections. The form has current, new and repeat fields, client-side mismatch check, success text `Пароль изменен.`, and clears password fields after success.

- [ ] **Step 8: Run focused tests**

Run the same commands from Step 4.

Expected: PASS.

- [ ] **Step 9: Commit**

```powershell
git add apps/api/Modules/Account/DTOs/AccountDtos.cs apps/api/Modules/Account/Controllers/AccountProfileController.cs apps/api/Modules/Account/Services/IAccountProfileService.cs apps/api/Modules/Account/Services/AccountProfileService.cs apps/api/Modules/Account/Repositories/IAccountProfileRepository.cs apps/api/Modules/Account/Repositories/DapperAccountProfileRepository.cs apps/api/Modules/Account/AccountServiceCollectionExtensions.cs tests/LineCom.Api.Tests/Modules/Account/AccountProfileServiceTests.cs tests/LineCom.Api.Tests/Modules/Account/AccountProfileEndpointTests.cs apps/front/src/lib/api/account.ts apps/front/src/components/account/password-form.tsx apps/front/src/components/account/password-form.test.tsx apps/front/src/app/account/profile/profile-page-client.tsx
git commit -m "feat: add current user password change"
```

## Task 7: Browser QA And Full Verification

**Files:**
- Modify only if QA reveals concrete defects in files already touched by earlier tasks.

- [ ] **Step 1: Run full frontend verification**

Run:

```powershell
npm.cmd test
npm.cmd run build
```

Expected: PASS.

- [ ] **Step 2: Run full backend verification**

Run:

```powershell
dotnet test .\LineCom.sln
dotnet build .\LineCom.sln
```

Expected: PASS.

- [ ] **Step 3: Run diff hygiene**

Run:

```powershell
git diff --check
git status --short --branch
```

Expected: no whitespace errors. The known untracked PNG may remain untracked and must not be staged.

- [ ] **Step 4: Start local app for browser QA**

Use the existing project scripts. If a server is already running on the default port, use the next free port approved by the environment. Keep the server running until browser QA is complete.

- [ ] **Step 5: Browser QA desktop and mobile**

Use the in-app browser to verify:

- desktop and mobile header show `Администрирование` only for `seller` and `admin`
- `/admin/catalog` products: filters, pagination, row selection, compact table, no horizontal overflow
- `/admin/catalog` categories: tree, edit form, parent picker excludes self and descendants
- `/admin/homepage`: product/category search, add item, no UUID field in user flow
- `/account/profile`: password form mismatch validation and successful submit state
- no text overlap in buttons, badges, table rows or compact rows

- [ ] **Step 6: Technical debt check**

Search:

```powershell
rg "T[O]DO|T[B]D|temporar[y]|времен[н]о|hac[k]|FIXM[E]" apps tests docs
```

For every hit in changed files, either remove it or replace it with a completed implementation decision. Do not touch unrelated historical hits.

- [ ] **Step 7: Final commit for QA fixes if needed**

If QA produced code changes:

```powershell
git add <changed-files-from-qa>
git commit -m "fix: polish admin catalog ux"
```

If QA produced no code changes, do not create an empty commit.

## Spec Coverage Check

- `Администрирование` dropdown for `seller`/`admin`: Task 1.
- Compact products table with activity, publication, readiness, category and explicit pagination: Task 2.
- Category tree and tree parent selection: Task 3.
- Homepage item add by product/category search, no manual UUID UX: Task 4.
- Automatic slug with manual override and regenerate action: Task 5.
- Current-user password change through protected endpoint: Task 6.
- SEO/GEO public routes, sitemap, robots, canonical and metadata unchanged: enforced in architecture and Task 7.
- No user management, admin password reset, recovery, one-time codes, audit log, bulk operations, Excel import/export, or product moves on category move: not included in any task.

## Execution Notes

- Recommended execution mode: subagent-driven after this plan is accepted as ready.
- Do not run two workers against the same implementation file at the same time.
- Complete Task 1 and the decomposition parts of Tasks 3 and 4 before parallelizing deeper UI changes if the same files are still large.
- Focused tests are required after each task; full verification and browser QA are required before final completion.
