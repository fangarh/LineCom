# Frontend Auth + Request Flow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first vertical LineCom frontend flow: catalog -> product -> request draft -> auth/profile -> submit request -> view own requests.

**Architecture:** Use Next.js App Router with server-rendered public catalog pages and client components for interactive auth/account/request draft flows. Proxy browser API calls through Next.js rewrites to avoid CORS and keep cookie-auth same-origin from the browser perspective. Keep domain logic in small frontend modules, not inside route components.

**Tech Stack:** Next.js 16 App Router, React 19, TypeScript, CSS Modules/plain CSS, Vitest, React Testing Library, jsdom, ASP.NET Core backend API, HTTP-only cookie auth, CSRF header.

---

## Source Context

Read these before implementation:

- Design spec: `docs/superpowers/specs/2026-05-07-frontend-auth-request-flow-design.md`
- Public catalog contract: `vault/Человекочитаемое/Public Catalog API.md`
- Auth/request contract: `vault/Человекочитаемое/Auth Request Core API.md`
- Frontend app root: `apps/front`
- Backend public catalog DTOs:
  - `apps/api/Modules/Catalog/DTOs/PublicCategoryDtos.cs`
  - `apps/api/Modules/Catalog/DTOs/PublicProductDtos.cs`
  - `apps/api/Modules/Catalog/DTOs/PublicCatalogSharedDtos.cs`
- Backend auth/account/request DTOs:
  - `apps/api/Modules/Auth/DTOs/AuthDtos.cs`
  - `apps/api/Modules/Account/DTOs/AccountDtos.cs`
  - `apps/api/Modules/Requests/DTOs/CustomerRequestDtos.cs`

Use current Next.js docs for App Router behavior when uncertain. The design already confirmed: pages/layouts are Server Components by default, data can be fetched in async Server Components, metadata is server-defined, and rewrites can proxy `/api/:path*` to a backend destination.

## File Structure

Create or modify these frontend files:

- `apps/front/package.json` - add test scripts and testing dependencies.
- `apps/front/vitest.config.ts` - Vitest config with jsdom and setup file.
- `apps/front/src/test/setup.ts` - testing-library setup.
- `apps/front/next.config.ts` - proxy `/api/:path*` to ASP.NET backend using `LINECOM_API_ORIGIN`.
- `apps/front/.env.example` - document `LINECOM_API_ORIGIN`.
- `apps/front/src/app/layout.tsx` - Russian locale, metadata defaults, shell providers.
- `apps/front/src/app/page.tsx` - working storefront entry.
- `apps/front/src/app/globals.css` - global layout tokens and base UI styles.
- `apps/front/src/app/catalog/page.tsx` - catalog overview.
- `apps/front/src/app/catalog/[categorySlug]/page.tsx` - category listing.
- `apps/front/src/app/products/[slug]/page.tsx` - product detail.
- `apps/front/src/app/request/page.tsx` - request draft page.
- `apps/front/src/app/auth/login/page.tsx` - login form.
- `apps/front/src/app/auth/register/page.tsx` - register form.
- `apps/front/src/app/account/profile/page.tsx` - profile and organization.
- `apps/front/src/app/account/requests/page.tsx` - own request list.
- `apps/front/src/app/account/requests/[number]/page.tsx` - own request detail.
- `apps/front/src/components/layout/site-header.tsx` - shared header/navigation.
- `apps/front/src/components/layout/site-shell.tsx` - root visual shell.
- `apps/front/src/components/catalog/category-nav.tsx` - category tree display.
- `apps/front/src/components/catalog/product-card.tsx` - product summary card.
- `apps/front/src/components/catalog/product-detail.tsx` - product detail UI.
- `apps/front/src/components/request/add-to-request-button.tsx` - product add action.
- `apps/front/src/components/request/request-draft-provider.tsx` - client state provider.
- `apps/front/src/components/request/request-draft-view.tsx` - request draft UI.
- `apps/front/src/components/auth/auth-provider.tsx` - current user and csrf runtime state.
- `apps/front/src/components/auth/login-form.tsx` - login form UI.
- `apps/front/src/components/auth/register-form.tsx` - register form UI.
- `apps/front/src/components/account/profile-form.tsx` - profile form.
- `apps/front/src/components/account/organization-form.tsx` - organization form.
- `apps/front/src/components/account/request-list.tsx` - account request list UI.
- `apps/front/src/components/account/request-detail.tsx` - account request detail UI.
- `apps/front/src/lib/api/errors.ts` - `ApiErrorResponse` parsing and helpers.
- `apps/front/src/lib/api/http.ts` - fetch wrapper with JSON and credentials.
- `apps/front/src/lib/api/catalog.ts` - typed public catalog client.
- `apps/front/src/lib/api/auth.ts` - typed auth client.
- `apps/front/src/lib/api/account.ts` - typed account client.
- `apps/front/src/lib/api/requests.ts` - typed request client.
- `apps/front/src/lib/request-draft/types.ts` - draft item and state types.
- `apps/front/src/lib/request-draft/storage.ts` - localStorage persistence.
- `apps/front/src/lib/request-draft/reducer.ts` - pure reducer/actions.
- `apps/front/src/lib/request-draft/selectors.ts` - derived draft values.
- `apps/front/src/lib/routes.ts` - central route builders.
- `apps/front/src/lib/format.ts` - small display helpers.

Test files:

- `apps/front/src/lib/api/errors.test.ts`
- `apps/front/src/lib/request-draft/reducer.test.ts`
- `apps/front/src/lib/request-draft/storage.test.ts`
- `apps/front/src/components/request/request-draft-view.test.tsx`
- `apps/front/src/components/auth/login-form.test.tsx`

Do not add a global state library in this plan. Start with React context, reducer, and localStorage. Add a dedicated state library only if this plan later proves the local model is insufficient.

## Task 1: Frontend Tooling And Backend Proxy

**Files:**

- Modify: `apps/front/package.json`
- Create: `apps/front/vitest.config.ts`
- Create: `apps/front/src/test/setup.ts`
- Modify: `apps/front/next.config.ts`
- Create: `apps/front/.env.example`

- [x] **Step 1: Install test dependencies**

Run from `apps/front`:

```powershell
npm.cmd install -D vitest jsdom @testing-library/react @testing-library/jest-dom @testing-library/user-event
```

Expected: `package.json` and `package-lock.json` update.

- [x] **Step 2: Add scripts to `apps/front/package.json`**

Set scripts to:

```json
{
  "scripts": {
    "dev": "next dev",
    "build": "next build",
    "start": "next start",
    "lint": "eslint",
    "test": "vitest run --passWithNoTests",
    "test:watch": "vitest"
  }
}
```

Keep existing dependencies, devDependencies, and `overrides.postcss`.

- [x] **Step 3: Create Vitest config**

Create `apps/front/vitest.config.ts`:

```ts
import { defineConfig } from "vitest/config";

export default defineConfig({
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./src/test/setup.ts"],
    include: ["src/**/*.{test,spec}.{ts,tsx}"],
    clearMocks: true,
    restoreMocks: true,
  },
});
```

- [x] **Step 4: Create test setup**

Create `apps/front/src/test/setup.ts`:

```ts
import "@testing-library/jest-dom/vitest";
```

- [x] **Step 5: Configure backend rewrite**

Modify `apps/front/next.config.ts`:

```ts
import type { NextConfig } from "next";

const apiOrigin = process.env.LINECOM_API_ORIGIN ?? "http://127.0.0.1:8080";

const nextConfig: NextConfig = {
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: `${apiOrigin}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;
```

- [x] **Step 6: Document environment**

Create `apps/front/.env.example`:

```env
LINECOM_API_ORIGIN=http://127.0.0.1:8080
```

- [x] **Step 7: Verify tooling**

Run from `apps/front`:

```powershell
npm.cmd run lint
npm.cmd test
```

Expected:

- lint passes;
- tests pass with no tests found because the script uses `--passWithNoTests`.

- [x] **Step 8: Commit**

```powershell
git add apps/front/package.json apps/front/package-lock.json apps/front/vitest.config.ts apps/front/src/test/setup.ts apps/front/next.config.ts apps/front/.env.example
git commit -m "test: add frontend test tooling and api proxy"
```

## Task 2: Shared API Types, Error Parsing, And Fetch Wrapper

**Files:**

- Create: `apps/front/src/lib/api/errors.ts`
- Create: `apps/front/src/lib/api/http.ts`
- Create: `apps/front/src/lib/api/catalog.ts`
- Create: `apps/front/src/lib/api/auth.ts`
- Create: `apps/front/src/lib/api/account.ts`
- Create: `apps/front/src/lib/api/requests.ts`
- Create: `apps/front/src/lib/api/errors.test.ts`
- Create: `apps/front/src/lib/routes.ts`
- Create: `apps/front/src/lib/format.ts`

- [ ] **Step 1: Write failing error parsing tests**

Create `apps/front/src/lib/api/errors.test.ts`:

```ts
import { describe, expect, it } from "vitest";
import { isApiErrorResponse, normalizeApiError } from "./errors";

describe("api errors", () => {
  it("accepts backend ApiErrorResponse shape", () => {
    expect(isApiErrorResponse({ code: "auth.unauthorized", message: "Требуется вход." })).toBe(true);
  });

  it("rejects unknown payloads", () => {
    expect(isApiErrorResponse({ error: "nope" })).toBe(false);
    expect(isApiErrorResponse(null)).toBe(false);
  });

  it("normalizes non-api failures to internal_error", () => {
    expect(normalizeApiError(new Error("network")).code).toBe("internal_error");
  });
});
```

- [ ] **Step 2: Run failing test**

Run from `apps/front`:

```powershell
npm.cmd test -- src/lib/api/errors.test.ts
```

Expected: fail because `errors.ts` does not exist.

- [ ] **Step 3: Implement `errors.ts`**

Create `apps/front/src/lib/api/errors.ts`:

```ts
export type ApiErrorResponse = {
  code: string;
  message: string;
};

export class ApiClientError extends Error {
  readonly status: number;
  readonly code: string;

  constructor(status: number, error: ApiErrorResponse) {
    super(error.message);
    this.name = "ApiClientError";
    this.status = status;
    this.code = error.code;
  }
}

export function isApiErrorResponse(value: unknown): value is ApiErrorResponse {
  if (!value || typeof value !== "object") {
    return false;
  }

  const candidate = value as Record<string, unknown>;
  return typeof candidate.code === "string" && typeof candidate.message === "string";
}

export function normalizeApiError(error: unknown): ApiErrorResponse {
  if (error instanceof ApiClientError) {
    return { code: error.code, message: error.message };
  }

  if (isApiErrorResponse(error)) {
    return error;
  }

  return {
    code: "internal_error",
    message: "Внутренняя ошибка сервера.",
  };
}
```

- [ ] **Step 4: Implement fetch wrapper**

Create `apps/front/src/lib/api/http.ts`:

```ts
import { ApiClientError, type ApiErrorResponse, isApiErrorResponse } from "./errors";

type JsonRequestOptions = {
  method?: "GET" | "POST" | "PUT" | "DELETE";
  body?: unknown;
  csrfToken?: string | null;
  cache?: RequestCache;
  next?: NextFetchRequestConfig;
};

export async function apiJson<T>(path: string, options: JsonRequestOptions = {}): Promise<T> {
  const headers = new Headers();
  headers.set("Accept", "application/json");

  if (options.body !== undefined) {
    headers.set("Content-Type", "application/json");
  }

  if (options.csrfToken) {
    headers.set("X-CSRF-Token", options.csrfToken);
  }

  const response = await fetch(path, {
    method: options.method ?? "GET",
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
    credentials: "include",
    cache: options.cache,
    next: options.next,
  });

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  const payload = text ? (JSON.parse(text) as unknown) : null;

  if (!response.ok) {
    const apiError: ApiErrorResponse = isApiErrorResponse(payload)
      ? payload
      : { code: "internal_error", message: "Внутренняя ошибка сервера." };
    throw new ApiClientError(response.status, apiError);
  }

  return payload as T;
}
```

- [ ] **Step 5: Implement shared route builders**

Create `apps/front/src/lib/routes.ts`:

```ts
export const routes = {
  home: () => "/",
  catalog: () => "/catalog",
  category: (slug: string) => `/catalog/${encodeURIComponent(slug)}`,
  product: (slug: string) => `/products/${encodeURIComponent(slug)}`,
  request: () => "/request",
  login: (returnTo?: string) => `/auth/login${returnTo ? `?returnTo=${encodeURIComponent(returnTo)}` : ""}`,
  register: (returnTo?: string) => `/auth/register${returnTo ? `?returnTo=${encodeURIComponent(returnTo)}` : ""}`,
  accountProfile: () => "/account/profile",
  accountRequests: () => "/account/requests",
  accountRequest: (number: string) => `/account/requests/${encodeURIComponent(number)}`,
};
```

- [ ] **Step 6: Implement display helpers**

Create `apps/front/src/lib/format.ts`:

```ts
export function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat("ru-RU", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

export function formatSku(sku: string | null): string {
  return sku ? `Артикул: ${sku}` : "Артикул не указан";
}
```

- [ ] **Step 7: Implement typed catalog client**

Create `apps/front/src/lib/api/catalog.ts` with TypeScript types matching `Public Catalog API.md`:

```ts
import { apiJson } from "./http";

export type PublicSeo = {
  title: string | null;
  description: string | null;
  canonicalPath: string;
};

export type PublicBreadcrumb = {
  name: string;
  slug: string;
};

export type PublicCodeLabel = {
  code: string;
  label: string;
};

export type PublicImage = {
  url: string;
  alt: string;
  title: string | null;
};

export type PublicCategorySummary = {
  name: string;
  slug: string;
};

export type PublicBrandSummary = {
  name: string;
  slug: string;
};

export type PublicCategoryTreeItem = {
  id: string;
  parentId: string | null;
  name: string;
  slug: string;
  h1: string | null;
  description: string | null;
  sortOrder: number;
  isVisibleInMenu: boolean;
  children: PublicCategoryTreeItem[];
};

export type PublicCategoryTreeResponse = {
  items: PublicCategoryTreeItem[];
};

export type PublicCategoryDetail = {
  id: string;
  parentId: string | null;
  name: string;
  slug: string;
  description: string | null;
  h1: string | null;
  seo: PublicSeo;
  breadcrumbs: PublicBreadcrumb[];
};

export type PublicProductListItem = {
  id: string;
  name: string;
  slug: string;
  sku: string | null;
  brand: PublicBrandSummary | null;
  category: PublicCategorySummary;
  availability: PublicCodeLabel;
  saleUnit: PublicCodeLabel;
  unitQuantity: string;
  mainImage: PublicImage | null;
};

export type PublicProductListResponse = {
  items: PublicProductListItem[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

export type PublicProductAttribute = {
  code: string;
  name: string;
  type: string;
  unit: string | null;
  value: string | number | boolean;
  sortOrder: number;
};

export type PublicProductDetail = {
  id: string;
  name: string;
  slug: string;
  sku: string | null;
  description: string | null;
  shortDescription: string | null;
  h1: string | null;
  category: PublicCategorySummary;
  brand: PublicBrandSummary | null;
  availability: PublicCodeLabel;
  saleUnit: PublicCodeLabel;
  unitQuantity: string;
  images: PublicImage[];
  attributes: PublicProductAttribute[];
  seo: PublicSeo;
  breadcrumbs: PublicBreadcrumb[];
};

export function getCategoryTree() {
  return apiJson<PublicCategoryTreeResponse>("/api/public/catalog/categories", {
    next: { revalidate: 60 },
  });
}

export function getCategory(slug: string) {
  return apiJson<PublicCategoryDetail>(`/api/public/catalog/categories/${encodeURIComponent(slug)}`, {
    next: { revalidate: 60 },
  });
}

export function getProducts(params: { categorySlug?: string; page?: number; pageSize?: number } = {}) {
  const search = new URLSearchParams();
  if (params.categorySlug) search.set("categorySlug", params.categorySlug);
  if (params.page) search.set("page", String(params.page));
  if (params.pageSize) search.set("pageSize", String(params.pageSize));

  const suffix = search.toString();
  return apiJson<PublicProductListResponse>(`/api/public/catalog/products${suffix ? `?${suffix}` : ""}`, {
    next: { revalidate: 60 },
  });
}

export function getProduct(slug: string) {
  return apiJson<PublicProductDetail>(`/api/public/catalog/products/${encodeURIComponent(slug)}`, {
    next: { revalidate: 60 },
  });
}
```

- [ ] **Step 8: Implement typed auth/account/request clients**

Create `apps/front/src/lib/api/auth.ts`, `account.ts`, and `requests.ts` using the DTO names from `Auth Request Core API.md`. Include these exported functions:

```ts
// auth.ts
export type CurrentUser = {
  id: string;
  name: string;
  email: string | null;
  phone: string | null;
  role: string;
};

export type AuthSession = {
  user: CurrentUser;
  csrfToken: string;
};
```

```ts
// account.ts
import type { CurrentUser } from "./auth";

export type AccountOrganization = {
  name: string;
  inn: string | null;
  contactPerson: string | null;
  phone: string | null;
  email: string | null;
  comment: string | null;
};

export type AccountProfile = {
  user: CurrentUser;
  organization: AccountOrganization | null;
};
```

```ts
// requests.ts
import type { PublicCodeLabel } from "./catalog";

export type CreateCustomerRequestPayload = {
  source: "cart" | "quick_order";
  customerComment: string | null;
  items: Array<{
    productId: string;
    quantity: number;
    customerComment: string | null;
  }>;
};

export type CustomerRequestItem = {
  productId: string;
  productName: string;
  productSku: string | null;
  saleUnit: PublicCodeLabel;
  unitQuantity: string;
  quantity: number;
  customerComment: string | null;
};
```

Add corresponding functions:

- `register(payload)`
- `login(payload)`
- `getMe()`
- `getAccountProfile()`
- `updateAccountProfile(payload, csrfToken)`
- `upsertOrganization(payload, csrfToken)`
- `createCustomerRequest(payload, csrfToken)`
- `getCustomerRequests(params)`
- `getCustomerRequest(number)`

Use `cache: "no-store"` for auth/account/request reads.

- [ ] **Step 9: Run tests**

```powershell
npm.cmd test -- src/lib/api/errors.test.ts
npm.cmd run lint
```

Expected: tests and lint pass.

- [ ] **Step 10: Commit**

```powershell
git add apps/front/src/lib apps/front/src/lib/api apps/front/src/lib/routes.ts apps/front/src/lib/format.ts
git commit -m "feat: add frontend api clients"
```

## Task 3: Request Draft State

**Files:**

- Create: `apps/front/src/lib/request-draft/types.ts`
- Create: `apps/front/src/lib/request-draft/reducer.ts`
- Create: `apps/front/src/lib/request-draft/storage.ts`
- Create: `apps/front/src/lib/request-draft/selectors.ts`
- Create: `apps/front/src/lib/request-draft/reducer.test.ts`
- Create: `apps/front/src/lib/request-draft/storage.test.ts`

- [ ] **Step 1: Write reducer tests**

Create `apps/front/src/lib/request-draft/reducer.test.ts`:

```ts
import { describe, expect, it } from "vitest";
import { requestDraftReducer } from "./reducer";
import type { RequestDraftState } from "./types";

const empty: RequestDraftState = { items: [], customerComment: "" };

const product = {
  productId: "11111111-1111-1111-1111-111111111111",
  slug: "u-utp-cat-5e",
  productName: "Кабель U/UTP Cat 5e",
  productSku: "LC-UTP5E",
  saleUnit: { code: "coil", label: "бухта" },
  unitQuantity: "305 м",
};

describe("requestDraftReducer", () => {
  it("adds product as one sale unit", () => {
    const state = requestDraftReducer(empty, { type: "addProduct", product });
    expect(state.items).toHaveLength(1);
    expect(state.items[0].quantity).toBe(1);
  });

  it("increments existing product instead of duplicating it", () => {
    const one = requestDraftReducer(empty, { type: "addProduct", product });
    const two = requestDraftReducer(one, { type: "addProduct", product });
    expect(two.items).toHaveLength(1);
    expect(two.items[0].quantity).toBe(2);
  });

  it("does not allow quantity below one", () => {
    const one = requestDraftReducer(empty, { type: "addProduct", product });
    const updated = requestDraftReducer(one, {
      type: "setQuantity",
      productId: product.productId,
      quantity: 0,
    });
    expect(updated.items[0].quantity).toBe(1);
  });

  it("removes product", () => {
    const one = requestDraftReducer(empty, { type: "addProduct", product });
    const updated = requestDraftReducer(one, { type: "removeItem", productId: product.productId });
    expect(updated.items).toEqual([]);
  });
});
```

- [ ] **Step 2: Run failing reducer tests**

```powershell
npm.cmd test -- src/lib/request-draft/reducer.test.ts
```

Expected: fail because reducer files do not exist.

- [ ] **Step 3: Implement draft types**

Create `apps/front/src/lib/request-draft/types.ts`:

```ts
import type { PublicCodeLabel } from "../api/catalog";

export type RequestDraftProduct = {
  productId: string;
  slug: string;
  productName: string;
  productSku: string | null;
  saleUnit: PublicCodeLabel;
  unitQuantity: string;
};

export type RequestDraftItem = RequestDraftProduct & {
  quantity: number;
  customerComment: string;
};

export type RequestDraftState = {
  items: RequestDraftItem[];
  customerComment: string;
};

export type RequestDraftAction =
  | { type: "hydrate"; state: RequestDraftState }
  | { type: "addProduct"; product: RequestDraftProduct }
  | { type: "setQuantity"; productId: string; quantity: number }
  | { type: "setItemComment"; productId: string; customerComment: string }
  | { type: "setCustomerComment"; customerComment: string }
  | { type: "removeItem"; productId: string }
  | { type: "clear" };

export const emptyRequestDraft: RequestDraftState = {
  items: [],
  customerComment: "",
};
```

- [ ] **Step 4: Implement reducer**

Create `apps/front/src/lib/request-draft/reducer.ts`:

```ts
import { emptyRequestDraft, type RequestDraftAction, type RequestDraftState } from "./types";

export function requestDraftReducer(
  state: RequestDraftState,
  action: RequestDraftAction,
): RequestDraftState {
  switch (action.type) {
    case "hydrate":
      return action.state;
    case "addProduct": {
      const existing = state.items.find((item) => item.productId === action.product.productId);
      if (existing) {
        return {
          ...state,
          items: state.items.map((item) =>
            item.productId === action.product.productId
              ? { ...item, quantity: item.quantity + 1 }
              : item,
          ),
        };
      }

      return {
        ...state,
        items: [
          ...state.items,
          {
            ...action.product,
            quantity: 1,
            customerComment: "",
          },
        ],
      };
    }
    case "setQuantity":
      return {
        ...state,
        items: state.items.map((item) =>
          item.productId === action.productId
            ? { ...item, quantity: Math.max(1, Math.floor(action.quantity || 1)) }
            : item,
        ),
      };
    case "setItemComment":
      return {
        ...state,
        items: state.items.map((item) =>
          item.productId === action.productId
            ? { ...item, customerComment: action.customerComment }
            : item,
        ),
      };
    case "setCustomerComment":
      return { ...state, customerComment: action.customerComment };
    case "removeItem":
      return { ...state, items: state.items.filter((item) => item.productId !== action.productId) };
    case "clear":
      return emptyRequestDraft;
    default:
      return state;
  }
}
```

- [ ] **Step 5: Write storage tests**

Create `apps/front/src/lib/request-draft/storage.test.ts`:

```ts
import { beforeEach, describe, expect, it } from "vitest";
import { loadRequestDraft, saveRequestDraft } from "./storage";
import type { RequestDraftState } from "./types";

describe("request draft storage", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("loads empty draft when localStorage is empty", () => {
    expect(loadRequestDraft()).toEqual({ items: [], customerComment: "" });
  });

  it("round-trips draft state", () => {
    const state: RequestDraftState = {
      customerComment: "Позвоните перед счетом",
      items: [
        {
          productId: "11111111-1111-1111-1111-111111111111",
          slug: "u-utp-cat-5e",
          productName: "Кабель U/UTP Cat 5e",
          productSku: "LC-UTP5E",
          saleUnit: { code: "coil", label: "бухта" },
          unitQuantity: "305 м",
          quantity: 2,
          customerComment: "",
        },
      ],
    };

    saveRequestDraft(state);
    expect(loadRequestDraft()).toEqual(state);
  });
});
```

- [ ] **Step 6: Implement storage**

Create `apps/front/src/lib/request-draft/storage.ts`:

```ts
import { emptyRequestDraft, type RequestDraftState } from "./types";

const STORAGE_KEY = "linecom.requestDraft.v1";

export function loadRequestDraft(): RequestDraftState {
  if (typeof window === "undefined") {
    return emptyRequestDraft;
  }

  const raw = window.localStorage.getItem(STORAGE_KEY);
  if (!raw) {
    return emptyRequestDraft;
  }

  try {
    const parsed = JSON.parse(raw) as RequestDraftState;
    if (!Array.isArray(parsed.items)) {
      return emptyRequestDraft;
    }

    return {
      items: parsed.items,
      customerComment: typeof parsed.customerComment === "string" ? parsed.customerComment : "",
    };
  } catch {
    return emptyRequestDraft;
  }
}

export function saveRequestDraft(state: RequestDraftState): void {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
}
```

- [ ] **Step 7: Implement selectors**

Create `apps/front/src/lib/request-draft/selectors.ts`:

```ts
import type { RequestDraftState } from "./types";

export function getDraftItemsCount(state: RequestDraftState): number {
  return state.items.reduce((sum, item) => sum + item.quantity, 0);
}

export function isDraftEmpty(state: RequestDraftState): boolean {
  return state.items.length === 0;
}
```

- [ ] **Step 8: Run tests**

```powershell
npm.cmd test -- src/lib/request-draft
npm.cmd run lint
```

Expected: tests and lint pass.

- [ ] **Step 9: Commit**

```powershell
git add apps/front/src/lib/request-draft
git commit -m "feat: add request draft state"
```

## Task 4: App Shell And Base Visual System

**Files:**

- Modify: `apps/front/src/app/layout.tsx`
- Modify: `apps/front/src/app/globals.css`
- Delete or stop using: `apps/front/src/app/page.module.css`
- Create: `apps/front/src/components/layout/site-shell.tsx`
- Create: `apps/front/src/components/layout/site-header.tsx`
- Create: `apps/front/src/components/auth/auth-provider.tsx`
- Create: `apps/front/src/components/request/request-draft-provider.tsx`

- [ ] **Step 1: Implement AuthProvider**

Create `apps/front/src/components/auth/auth-provider.tsx`:

```tsx
"use client";

import { createContext, useContext, useMemo, useState, type ReactNode } from "react";
import type { AuthSession, CurrentUser } from "@/lib/api/auth";

type AuthContextValue = {
  user: CurrentUser | null;
  csrfToken: string | null;
  setSession: (session: AuthSession) => void;
  clearSession: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [csrfToken, setCsrfToken] = useState<string | null>(null);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      csrfToken,
      setSession: (session) => {
        setUser(session.user);
        setCsrfToken(session.csrfToken);
      },
      clearSession: () => {
        setUser(null);
        setCsrfToken(null);
      },
    }),
    [csrfToken, user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) {
    throw new Error("useAuth must be used inside AuthProvider");
  }
  return value;
}
```

- [ ] **Step 2: Implement RequestDraftProvider**

Create `apps/front/src/components/request/request-draft-provider.tsx`:

```tsx
"use client";

import { createContext, useContext, useEffect, useMemo, useReducer, type ReactNode } from "react";
import { requestDraftReducer } from "@/lib/request-draft/reducer";
import { loadRequestDraft, saveRequestDraft } from "@/lib/request-draft/storage";
import { emptyRequestDraft, type RequestDraftAction, type RequestDraftState } from "@/lib/request-draft/types";

type RequestDraftContextValue = {
  state: RequestDraftState;
  dispatch: React.Dispatch<RequestDraftAction>;
};

const RequestDraftContext = createContext<RequestDraftContextValue | null>(null);

export function RequestDraftProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(requestDraftReducer, emptyRequestDraft);

  useEffect(() => {
    dispatch({ type: "hydrate", state: loadRequestDraft() });
  }, []);

  useEffect(() => {
    saveRequestDraft(state);
  }, [state]);

  const value = useMemo(() => ({ state, dispatch }), [state]);

  return <RequestDraftContext.Provider value={value}>{children}</RequestDraftContext.Provider>;
}

export function useRequestDraft() {
  const value = useContext(RequestDraftContext);
  if (!value) {
    throw new Error("useRequestDraft must be used inside RequestDraftProvider");
  }
  return value;
}
```

- [ ] **Step 3: Implement shell components**

Create `site-shell.tsx` and `site-header.tsx`. Header must link to:

- `/`
- `/catalog`
- `/request`
- `/account/requests`
- `/auth/login`

Use `routes` from `src/lib/routes.ts`. Keep text Russian and commercial language request-oriented.

- [ ] **Step 4: Update root layout**

Modify `apps/front/src/app/layout.tsx`:

```tsx
import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import { AuthProvider } from "@/components/auth/auth-provider";
import { RequestDraftProvider } from "@/components/request/request-draft-provider";
import { SiteShell } from "@/components/layout/site-shell";
import "./globals.css";

const geistSans = Geist({ variable: "--font-geist-sans", subsets: ["latin", "cyrillic"] });
const geistMono = Geist_Mono({ variable: "--font-geist-mono", subsets: ["latin", "cyrillic"] });

export const metadata: Metadata = {
  title: "LineCom - каталог кабеля и компонентов",
  description: "Каталог кабеля, СКС, ВОЛС и сопутствующих компонентов с заявками по запросу.",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="ru" className={`${geistSans.variable} ${geistMono.variable}`}>
      <body>
        <AuthProvider>
          <RequestDraftProvider>
            <SiteShell>{children}</SiteShell>
          </RequestDraftProvider>
        </AuthProvider>
      </body>
    </html>
  );
}
```

- [ ] **Step 5: Replace starter global styles**

Modify `globals.css` to establish a restrained B2B catalog style:

- white/off-white background;
- dark neutral text;
- restrained red or graphite accent compatible with cable/industrial subject;
- no large decorative gradient/orbs;
- stable button heights;
- responsive content width.

Keep all colors varied enough that the UI does not read as a one-hue palette.

- [ ] **Step 6: Verify shell**

```powershell
npm.cmd run lint
npm.cmd test
npm.cmd run build
```

Expected: all pass.

- [ ] **Step 7: Commit**

```powershell
git add apps/front/src/app apps/front/src/components apps/front/src/lib
git commit -m "feat: add frontend app shell"
```

## Task 5: Public Catalog Pages

**Files:**

- Modify: `apps/front/src/app/page.tsx`
- Create: `apps/front/src/app/catalog/page.tsx`
- Create: `apps/front/src/app/catalog/[categorySlug]/page.tsx`
- Create: `apps/front/src/app/products/[slug]/page.tsx`
- Create: `apps/front/src/components/catalog/category-nav.tsx`
- Create: `apps/front/src/components/catalog/product-card.tsx`
- Create: `apps/front/src/components/catalog/product-detail.tsx`
- Create: `apps/front/src/components/request/add-to-request-button.tsx`

- [ ] **Step 1: Implement `AddToRequestButton`**

Create `apps/front/src/components/request/add-to-request-button.tsx` as a client component that accepts product draft fields and dispatches `addProduct`.

Button text: `Добавить в заявку`.

Do not use `Купить`, `Оформить заказ`, or prices.

- [ ] **Step 2: Implement product card**

Create `apps/front/src/components/catalog/product-card.tsx`:

- show name, sku, brand, availability label, sale unit label, unit quantity;
- show `Цена по запросу`;
- link to product detail;
- include `AddToRequestButton`.

- [ ] **Step 3: Implement product detail**

Create `apps/front/src/components/catalog/product-detail.tsx`:

- show breadcrumbs;
- H1 from `product.h1 ?? product.name`;
- description/shortDescription;
- attributes table;
- availability;
- saleUnit and unitQuantity;
- image or no-image state;
- `Цена по запросу`;
- `AddToRequestButton`.

- [ ] **Step 4: Implement category nav**

Create `apps/front/src/components/catalog/category-nav.tsx`:

- recursively render category tree;
- show only returned categories;
- use slug links through `routes.category`.

- [ ] **Step 5: Implement home page**

Modify `apps/front/src/app/page.tsx`:

- fetch category tree and first product page;
- render useful storefront entry immediately;
- no starter Next.js content;
- no marketing-only hero.

- [ ] **Step 6: Implement catalog overview**

Create `apps/front/src/app/catalog/page.tsx`:

- fetch category tree and product list;
- render categories and products.

- [ ] **Step 7: Implement category page with metadata**

Create `apps/front/src/app/catalog/[categorySlug]/page.tsx`:

- `generateMetadata` calls `getCategory(params.categorySlug)`;
- use `category.seo.title`, `category.seo.description`, and canonical path;
- page fetches category, filters if used, and products for `categorySlug`;
- render product list and empty state.

- [ ] **Step 8: Implement product page with metadata**

Create `apps/front/src/app/products/[slug]/page.tsx`:

- `generateMetadata` calls `getProduct(params.slug)`;
- use product SEO fields;
- page renders `ProductDetail`.

- [ ] **Step 9: Verify catalog routes**

Run with backend API available:

```powershell
npm.cmd run lint
npm.cmd run build
```

Then use browser QA:

- open `/`;
- open `/catalog`;
- open at least one category URL from visible links;
- open at least one product URL from visible links;
- verify visible text says `Цена по запросу`;
- verify no visible text says `Купить` or `Оформить заказ`.

- [ ] **Step 10: Commit**

```powershell
git add apps/front/src/app apps/front/src/components/catalog apps/front/src/components/request apps/front/src/lib
git commit -m "feat: add public catalog frontend"
```

## Task 6: Request Draft Page

**Files:**

- Create: `apps/front/src/components/request/request-draft-view.tsx`
- Create: `apps/front/src/components/request/request-draft-view.test.tsx`
- Modify: `apps/front/src/app/request/page.tsx`

- [ ] **Step 1: Write request draft view tests**

Create `request-draft-view.test.tsx` to verify:

- empty state says `В заявке пока нет товаров`;
- item quantity can be changed;
- remove button removes an item;
- visible text uses `Отправить заявку`, not `Оформить заказ`.

- [ ] **Step 2: Run failing component test**

```powershell
npm.cmd test -- src/components/request/request-draft-view.test.tsx
```

Expected: fail because component does not exist.

- [ ] **Step 3: Implement request draft view**

Create `request-draft-view.tsx`:

- read state from `useRequestDraft`;
- render items;
- inputs for quantity and comments;
- remove buttons;
- customerComment textarea;
- submit button disabled when empty;
- submit action placeholder calls an `onSubmit` prop.

Keep submission side effect out of this presentational component until Task 8.

- [ ] **Step 4: Implement `/request` page**

Create or modify `apps/front/src/app/request/page.tsx` to render `RequestDraftView` with an `onSubmit` handler that routes the user to `routes.login(routes.request())`. Task 8 replaces this handler with real authenticated submission, but the visible behavior is already coherent: a user who wants to send a request is asked to sign in first.

- [ ] **Step 5: Run tests and lint**

```powershell
npm.cmd test -- src/components/request/request-draft-view.test.tsx
npm.cmd run lint
```

Expected: pass.

- [ ] **Step 6: Commit**

```powershell
git add apps/front/src/components/request apps/front/src/app/request
git commit -m "feat: add request draft page"
```

## Task 7: Auth And Account Profile UI

**Files:**

- Create: `apps/front/src/components/auth/login-form.tsx`
- Create: `apps/front/src/components/auth/register-form.tsx`
- Create: `apps/front/src/components/auth/login-form.test.tsx`
- Create: `apps/front/src/app/auth/login/page.tsx`
- Create: `apps/front/src/app/auth/register/page.tsx`
- Create: `apps/front/src/components/account/profile-form.tsx`
- Create: `apps/front/src/components/account/organization-form.tsx`
- Create: `apps/front/src/app/account/profile/page.tsx`

- [ ] **Step 1: Write login form tests**

Test:

- login form calls `onSubmit` with login and password;
- backend error message is shown;
- submit button text is `Войти`.

- [ ] **Step 2: Implement login/register forms**

Components accept injected submit handlers:

```ts
type LoginFormProps = {
  onSubmit: (payload: { login: string; password: string }) => Promise<void>;
  errorMessage?: string | null;
};
```

Register form fields:

- name;
- email;
- phone;
- password.

Do not ask for organization during registration.

- [ ] **Step 3: Implement login/register pages**

Pages:

- parse `returnTo` from search params;
- call `login` or `register`;
- call `useAuth().setSession`;
- navigate to `returnTo ?? routes.request()` after success.

- [ ] **Step 4: Implement profile and organization forms**

Profile form:

- name;
- email;
- phone.

Organization form:

- name;
- inn;
- contactPerson;
- phone;
- email;
- comment.

Both use `csrfToken` from `useAuth`.

- [ ] **Step 5: Implement account profile page**

On client mount:

- call `getMe`;
- set auth session;
- call `getAccountProfile`;
- render profile and organization forms;
- on `auth.unauthorized`, route to login with `returnTo=/account/profile`.

- [ ] **Step 6: Run tests and lint**

```powershell
npm.cmd test -- src/components/auth/login-form.test.tsx
npm.cmd run lint
npm.cmd run build
```

Expected: all pass.

- [ ] **Step 7: Commit**

```powershell
git add apps/front/src/components/auth apps/front/src/components/account apps/front/src/app/auth apps/front/src/app/account/profile
git commit -m "feat: add auth and profile frontend"
```

## Task 8: Request Submission Flow

**Files:**

- Modify: `apps/front/src/app/request/page.tsx`
- Modify: `apps/front/src/components/request/request-draft-view.tsx`
- Modify: `apps/front/src/lib/api/requests.ts`
- Modify: `apps/front/src/components/auth/auth-provider.tsx` if `getMe` bootstrap is needed.

- [ ] **Step 1: Add submission behavior to request page**

Behavior:

- if draft is empty, do not submit;
- if auth state is missing, route to `routes.login(routes.request())`;
- create payload:

```ts
{
  source: "cart",
  customerComment: state.customerComment || null,
  items: state.items.map((item) => ({
    productId: item.productId,
    quantity: item.quantity,
    customerComment: item.customerComment || null,
  })),
}
```

- call `createCustomerRequest(payload, csrfToken)`;
- on success, clear draft and route to `routes.accountRequest(result.number)`;
- on `request.product_not_available`, keep draft and show backend message;
- on `auth.forbidden`, call `getMe`, refresh csrf, retry once.

- [ ] **Step 2: Ensure submit is explicit after login**

After login/register redirects back to `/request`, do not auto-submit. User must press `Отправить заявку`.

- [ ] **Step 3: Verify manually**

With backend running and at least one published product:

- add product to request;
- open `/request`;
- change quantity;
- go to login/register if not authenticated;
- return to `/request`;
- submit;
- verify created request number is shown via redirect.

- [ ] **Step 4: Run checks**

```powershell
npm.cmd run lint
npm.cmd run build
dotnet test LineCom.sln -m:1
```

Expected: all pass.

- [ ] **Step 5: Commit**

```powershell
git add apps/front/src/app/request apps/front/src/components/request apps/front/src/lib/api apps/front/src/components/auth
git commit -m "feat: submit customer requests from frontend"
```

## Task 9: Account Request List And Detail

**Files:**

- Create: `apps/front/src/components/account/request-list.tsx`
- Create: `apps/front/src/components/account/request-detail.tsx`
- Create: `apps/front/src/app/account/requests/page.tsx`
- Create: `apps/front/src/app/account/requests/[number]/page.tsx`

- [ ] **Step 1: Implement request list component**

Show:

- number;
- status label;
- source;
- itemsCount;
- customerComment;
- createdAt formatted through `formatDateTime`;
- link to detail.

Add status filter with values:

- all;
- `new`;
- `in_progress`;
- `completed`;
- `canceled`.

- [ ] **Step 2: Implement account request list page**

On client mount:

- call `getMe` and `getCustomerRequests`;
- on unauthorized, route to login with `returnTo=/account/requests`;
- render empty state if no requests.

- [ ] **Step 3: Implement request detail component**

Show:

- number;
- status;
- customer snapshot;
- organization snapshot if present;
- customer comment;
- item snapshots;
- history.

Do not show prices.

- [ ] **Step 4: Implement request detail page**

Use route param `number`; call `getCustomerRequest(number)`. For `request.not_found`, show controlled not-found state.

- [ ] **Step 5: Run checks**

```powershell
npm.cmd run lint
npm.cmd run build
```

Expected: pass.

- [ ] **Step 6: Commit**

```powershell
git add apps/front/src/components/account apps/front/src/app/account/requests
git commit -m "feat: add account request views"
```

## Task 10: Browser QA, Accessibility Pass, And Debt Check

**Files:**

- Modify frontend files only if QA finds issues.
- Modify: `vault/Человекочитаемое/Auth Request Core API.md` only if API usage notes need clarification.
- Modify or create frontend docs only if a dev setup command changed.

- [ ] **Step 1: Run full verification**

From repo root:

```powershell
dotnet build LineCom.sln -m:1
dotnet test LineCom.sln -m:1
```

From `apps/front`:

```powershell
npm.cmd run lint
npm.cmd test
npm.cmd run build
```

Expected: all pass.

- [ ] **Step 2: Start backend and frontend**

Backend:

```powershell
dotnet run --project apps/api/LineCom.Api --urls http://127.0.0.1:8080
```

Frontend:

```powershell
npm.cmd run dev
```

Set `LINECOM_API_ORIGIN=http://127.0.0.1:8080` if not already set.

- [ ] **Step 3: Browser QA happy path**

Use Browser/Playwright:

- open `/`;
- navigate to catalog;
- open category;
- open product;
- add product to request;
- open request draft;
- register or login;
- submit request;
- open created request detail;
- open request list.

Expected:

- no blank pages;
- no overlapping text;
- request language is used consistently;
- prices are absent;
- request number appears after submission;
- account list includes the created request.

- [ ] **Step 4: Browser QA responsive pass**

Check desktop and mobile widths:

- `/catalog`;
- product detail;
- `/request`;
- `/account/requests`;
- request detail.

Expected:

- header does not overlap content;
- controls remain tappable;
- cards/forms do not overflow horizontally;
- long product names wrap cleanly.

- [ ] **Step 5: Search for forbidden language and debt markers**

Run:

```powershell
rg -n "Купить|Оформить заказ|оплат|цена \\d|TODO|TBD|FIXME|заглуш|костыл" apps/front docs/superpowers/specs docs/superpowers/plans
```

Expected:

- no forbidden commerce language in `apps/front`;
- no accidental TODO/TBD/FIXME markers in changed implementation files;
- matches in plans/specs are only intentional rule text.

- [ ] **Step 6: Final commit**

If QA fixes were needed:

```powershell
git add apps/front vault docs
git commit -m "fix: polish frontend request flow"
```

If no QA fixes were needed, do not create an empty commit.

## Implementation Notes

- Keep frontend copy Russian.
- Keep code comments English only if the surrounding file uses English comments; otherwise avoid comments unless they explain non-obvious behavior.
- Do not add public prices.
- Do not add anonymous request submission.
- Do not change backend contracts unless a test proves the frontend cannot satisfy the existing documented contract.
- Use `dotnet build/test -m:1` until the parallel MSBuild issue is resolved separately.

## Self-Review

Spec coverage:

- Vertical catalog-to-request flow: Tasks 5, 6, 8, 9.
- SEO/GEO server-rendered catalog pages and metadata: Task 5.
- Auth, HTTP-only cookie, CSRF runtime token: Tasks 2, 7, 8.
- Request draft persistence: Task 3 and Task 6.
- Controlled backend errors: Tasks 2, 6, 8, 9.
- No prices/payment/order language: Tasks 5, 6, 8, 10.
- Verification commands: Task 10.

No placeholders are intentionally left in implementation steps. Open decisions from the design are resolved for this plan as:

- dev API integration uses Next.js rewrites;
- state uses React context/reducer/localStorage;
- test runner is Vitest with jsdom and React Testing Library;
- visual style is restrained B2B catalog styling based on global CSS and existing assets, without a marketing landing page.
