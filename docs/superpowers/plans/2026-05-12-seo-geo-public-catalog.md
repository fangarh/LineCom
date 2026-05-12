# SEO/GEO Public Catalog Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add production-ready SEO/GEO infrastructure for LineCom public pages: stable site origin config, metadataBase, canonical metadata, robots, and sitemap coverage for homepage, catalog, active categories, and published products.

**Architecture:** Keep this as a frontend-first Next.js App Router slice in `apps/front`. Reuse existing public catalog API clients and route helpers; do not add backend tables or migrations unless implementation proves that current public DTOs are insufficient. Keep internal/auth/admin pages out of public indexing.

**Tech Stack:** Next.js 16 App Router, React 19, TypeScript, Vitest, Testing Library where needed, ASP.NET Core public catalog API as existing data source.

---

## Context7 Documentation Check

Before this plan was written, current Next.js docs were fetched with Context7 for `/vercel/next.js`.

Relevant docs signals:

- `generateMetadata` is the App Router mechanism for dynamic route metadata and supports async data loading from route params.
- `metadataBase` lets relative `alternates.canonical` and Open Graph URLs resolve into absolute URLs.
- `app/sitemap.ts` exports a function returning `MetadataRoute.Sitemap`.
- `app/robots.ts` exports a function returning `MetadataRoute.Robots`.
- Robots metadata can be set per page through `robots` in `Metadata`.

Primary source URLs from Context7:

- `https://github.com/vercel/next.js/blob/canary/docs/01-app/03-api-reference/04-functions/generate-metadata.mdx`
- `https://github.com/vercel/next.js/blob/canary/docs/01-app/03-api-reference/03-file-conventions/01-metadata/sitemap.mdx`
- `https://github.com/vercel/next.js/blob/canary/docs/01-app/03-api-reference/03-file-conventions/01-metadata/robots.mdx`

## Current Baseline

- Branch expected at start: `main`, synced with `origin/main`.
- Expected untracked file: `admin-catalog-homepage-slice.png`. Do not stage, edit, delete, or commit it.
- Frontend package is `apps/front`.
- Next.js version in `apps/front/package.json`: `16.2.4`.
- Existing metadata:
  - `apps/front/src/app/layout.tsx` has root `metadata` with title, description, icon.
  - `apps/front/src/app/catalog/page.tsx` has static metadata but no canonical.
  - `apps/front/src/app/catalog/[categorySlug]/page.tsx` has `generateMetadata` with category canonical from API.
  - `apps/front/src/app/products/[slug]/page.tsx` has `generateMetadata` with product canonical from API.
- Existing public API clients:
  - `apps/front/src/lib/api/catalog.ts`
  - `apps/front/src/lib/api/homepage.ts`
  - `apps/front/src/lib/api/http.ts`
- Existing route helper: `apps/front/src/lib/routes.ts`.
- `vault/Человекочитаемое` remains source of truth. Relevant files:
  - `Технический стек.md`: App Router metadata, sitemap, canonical URL are required for SEO/GEO.
  - `Сквозные требования.md`: indexing, sitemap, robots.txt, canonical URL are cross-cutting requirements.
  - `Продуктовая модель.md`: SEO/GEO matters for catalog URL structure, metadata, sitemap, canonical URL.
  - `Public Catalog API.md`: category/product SEO DTOs and canonical paths.

## Scope

In scope:

- Site origin configuration for absolute SEO URLs.
- Root `metadataBase`.
- Canonical metadata for `/`, `/catalog`, `/about`, `/delivery`, category pages, and product pages.
- `robots` metadata for public pages and noindex behavior for internal/auth/account/admin pages where practical.
- `app/robots.ts`.
- `app/sitemap.ts`.
- Unit tests for URL helpers, metadata helpers, sitemap builder, and robots output.
- Browser/build QA for public routes and generated `/robots.txt` and `/sitemap.xml`.

Out of scope:

- SEO landing pages by filters.
- Multiple segmented sitemaps.
- Open Graph images beyond using existing product/category data where already available.
- Backend database schema changes.
- Prices, checkout, payment, public stock counts.
- Rewriting public catalog visual layout.

## File Map

Expected new files:

- `apps/front/src/lib/seo/site.ts` - site origin normalization and absolute URL helpers.
- `apps/front/src/lib/seo/site.test.ts` - tests for origin and URL helpers.
- `apps/front/src/lib/seo/metadata.ts` - shared metadata builders for canonical, robots, and title fallbacks.
- `apps/front/src/lib/seo/metadata.test.ts` - tests for metadata builders.
- `apps/front/src/lib/seo/sitemap.ts` - pure sitemap entry builder from categories/products.
- `apps/front/src/lib/seo/sitemap.test.ts` - tests for sitemap builder.
- `apps/front/src/app/sitemap.ts` - Next.js sitemap route.
- `apps/front/src/app/robots.ts` - Next.js robots route.
- `apps/front/src/app/robots.test.ts` - tests for robots route output.
- `vault/Человекочитаемое/SEO GEO Public Catalog.md` - human-readable contract after implementation.

Expected modified files:

- `apps/front/src/app/layout.tsx`
- `apps/front/src/app/page.tsx`
- `apps/front/src/app/catalog/page.tsx`
- `apps/front/src/app/catalog/[categorySlug]/page.tsx`
- `apps/front/src/app/products/[slug]/page.tsx`
- `apps/front/src/app/about/page.tsx`
- `apps/front/src/app/delivery/page.tsx`
- Optional internal pages if metadata noindex is added directly:
  - `apps/front/src/app/auth/login/page.tsx`
  - `apps/front/src/app/auth/register/page.tsx`
  - `apps/front/src/app/account/profile/page.tsx`
  - `apps/front/src/app/account/requests/page.tsx`
  - `apps/front/src/app/account/requests/[number]/page.tsx`
  - `apps/front/src/app/admin/catalog/page.tsx`
  - `apps/front/src/app/admin/homepage/page.tsx`
  - `apps/front/src/app/admin/requests/page.tsx`
  - `apps/front/src/app/admin/requests/[number]/page.tsx`

## Task 1: Site Origin And SEO Metadata Helpers

**Files:**
- Create: `apps/front/src/lib/seo/site.ts`
- Create: `apps/front/src/lib/seo/site.test.ts`
- Create: `apps/front/src/lib/seo/metadata.ts`
- Create: `apps/front/src/lib/seo/metadata.test.ts`

- [ ] **Step 1: Write failing tests for site URL helpers**

Create `apps/front/src/lib/seo/site.test.ts`:

```ts
import { afterEach, describe, expect, it } from "vitest";
import { absoluteSiteUrl, getPublicSiteOrigin, normalizeSiteOrigin } from "./site";

const originalOrigin = process.env.LINECOM_PUBLIC_SITE_ORIGIN;

afterEach(() => {
  process.env.LINECOM_PUBLIC_SITE_ORIGIN = originalOrigin;
});

describe("site SEO URL helpers", () => {
  it("uses localhost fallback when public origin is not configured", () => {
    delete process.env.LINECOM_PUBLIC_SITE_ORIGIN;

    expect(getPublicSiteOrigin()).toBe("http://127.0.0.1:3000");
  });

  it("normalizes configured public origin by trimming trailing slashes", () => {
    process.env.LINECOM_PUBLIC_SITE_ORIGIN = "https://linecom.example.ru///";

    expect(getPublicSiteOrigin()).toBe("https://linecom.example.ru");
  });

  it("falls back when configured public origin is not an absolute http URL", () => {
    expect(normalizeSiteOrigin("linecom.example.ru")).toBe("http://127.0.0.1:3000");
    expect(normalizeSiteOrigin("ftp://linecom.example.ru")).toBe("http://127.0.0.1:3000");
  });

  it("builds absolute URLs from relative public paths", () => {
    process.env.LINECOM_PUBLIC_SITE_ORIGIN = "https://linecom.example.ru/";

    expect(absoluteSiteUrl("/catalog/vitaya-para")).toBe("https://linecom.example.ru/catalog/vitaya-para");
    expect(absoluteSiteUrl("products/u-utp")).toBe("https://linecom.example.ru/products/u-utp");
  });
});
```

- [ ] **Step 2: Run RED for site helper tests**

Run:

```powershell
npm.cmd test -- site
```

Expected: FAIL because `apps/front/src/lib/seo/site.ts` does not exist yet.

- [ ] **Step 3: Implement site URL helpers**

Create `apps/front/src/lib/seo/site.ts`:

```ts
const DEFAULT_PUBLIC_SITE_ORIGIN = "http://127.0.0.1:3000";

export function normalizeSiteOrigin(value: string | null | undefined) {
  const trimmed = value?.trim();
  if (!trimmed) {
    return DEFAULT_PUBLIC_SITE_ORIGIN;
  }

  try {
    const parsed = new URL(trimmed);
    if (parsed.protocol !== "http:" && parsed.protocol !== "https:") {
      return DEFAULT_PUBLIC_SITE_ORIGIN;
    }

    return parsed.origin;
  } catch {
    return DEFAULT_PUBLIC_SITE_ORIGIN;
  }
}

export function getPublicSiteOrigin() {
  return normalizeSiteOrigin(process.env.LINECOM_PUBLIC_SITE_ORIGIN);
}

export function siteMetadataBase() {
  return new URL(getPublicSiteOrigin());
}

export function absoluteSiteUrl(path: string) {
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return `${getPublicSiteOrigin()}${normalizedPath}`;
}
```

- [ ] **Step 4: Write failing tests for metadata helpers**

Create `apps/front/src/lib/seo/metadata.test.ts`:

```ts
import { describe, expect, it } from "vitest";
import { indexablePageMetadata, noindexPageMetadata } from "./metadata";

describe("SEO metadata helpers", () => {
  it("creates canonical metadata for indexable public pages", () => {
    expect(indexablePageMetadata({
      title: "Каталог LineCom",
      description: "Кабель и компоненты.",
      canonicalPath: "/catalog",
    })).toEqual({
      title: "Каталог LineCom",
      description: "Кабель и компоненты.",
      alternates: {
        canonical: "/catalog",
      },
      robots: {
        index: true,
        follow: true,
        googleBot: {
          index: true,
          follow: true,
          "max-image-preview": "large",
          "max-snippet": -1,
          "max-video-preview": -1,
        },
      },
    });
  });

  it("omits empty optional descriptions", () => {
    expect(indexablePageMetadata({
      title: "LineCom",
      canonicalPath: "/",
    }).description).toBeUndefined();
  });

  it("creates noindex metadata for internal and unavailable pages", () => {
    expect(noindexPageMetadata("Админка LineCom")).toEqual({
      title: "Админка LineCom",
      robots: {
        index: false,
        follow: false,
      },
    });
  });
});
```

- [ ] **Step 5: Run RED for metadata helper tests**

Run:

```powershell
npm.cmd test -- metadata
```

Expected: FAIL because `apps/front/src/lib/seo/metadata.ts` does not exist yet.

- [ ] **Step 6: Implement metadata helpers**

Create `apps/front/src/lib/seo/metadata.ts`:

```ts
import type { Metadata } from "next";

type IndexablePageMetadataInput = {
  title: string;
  description?: string | null;
  canonicalPath: string;
};

export function indexablePageMetadata({
  title,
  description,
  canonicalPath,
}: IndexablePageMetadataInput): Metadata {
  return {
    title,
    description: description || undefined,
    alternates: {
      canonical: canonicalPath,
    },
    robots: {
      index: true,
      follow: true,
      googleBot: {
        index: true,
        follow: true,
        "max-image-preview": "large",
        "max-snippet": -1,
        "max-video-preview": -1,
      },
    },
  };
}

export function noindexPageMetadata(title: string): Metadata {
  return {
    title,
    robots: {
      index: false,
      follow: false,
    },
  };
}
```

- [ ] **Step 7: Run GREEN for helper tests**

Run:

```powershell
npm.cmd test -- site metadata
```

Expected: PASS.

- [ ] **Step 8: Commit helpers**

```powershell
git add apps/front/src/lib/seo/site.ts apps/front/src/lib/seo/site.test.ts apps/front/src/lib/seo/metadata.ts apps/front/src/lib/seo/metadata.test.ts
git commit -m "feat: add public SEO helpers"
```

## Task 2: Root And Public Page Metadata

**Files:**
- Modify: `apps/front/src/app/layout.tsx`
- Modify: `apps/front/src/app/page.tsx`
- Modify: `apps/front/src/app/catalog/page.tsx`
- Modify: `apps/front/src/app/about/page.tsx`
- Modify: `apps/front/src/app/delivery/page.tsx`
- Modify: `apps/front/src/app/catalog/[categorySlug]/page.tsx`
- Modify: `apps/front/src/app/products/[slug]/page.tsx`
- Test through existing and new helper tests.

- [ ] **Step 1: Add `metadataBase` and root canonical metadata**

Modify `apps/front/src/app/layout.tsx`:

```ts
import { indexablePageMetadata } from "@/lib/seo/metadata";
import { siteMetadataBase } from "@/lib/seo/site";
```

Replace the current `metadata` export with:

```ts
export const metadata: Metadata = {
  ...indexablePageMetadata({
    title: "LineCom - каталог кабеля и компонентов",
    description: "Каталог кабеля, СКС, ВОЛС и сопутствующих компонентов с заявками по запросу.",
    canonicalPath: "/",
  }),
  applicationName: "LineCom",
  metadataBase: siteMetadataBase(),
  icons: {
    icon: [
      {
        url: "/linecom-tab-icon.svg",
        type: "image/svg+xml",
      },
    ],
  },
};
```

- [ ] **Step 2: Add static homepage metadata**

Modify `apps/front/src/app/page.tsx`:

```ts
import type { Metadata } from "next";
import { indexablePageMetadata } from "@/lib/seo/metadata";
```

Add before `function requestProduct`:

```ts
export const metadata: Metadata = indexablePageMetadata({
  title: "LineCom - кабель и сетевые компоненты по заявке",
  description: "Подбор кабеля, СКС, ВОЛС и сетевых компонентов для заявок без публичных цен и онлайн-оплаты.",
  canonicalPath: "/",
});
```

- [ ] **Step 3: Add canonical metadata to static public pages**

Modify `apps/front/src/app/catalog/page.tsx` metadata:

```ts
import type { Metadata } from "next";
import { indexablePageMetadata } from "@/lib/seo/metadata";
```

Replace metadata export:

```ts
export const metadata: Metadata = indexablePageMetadata({
  title: "Каталог кабеля и компонентов LineCom",
  description: "Каталог кабеля, СКС, ВОЛС и компонентов LineCom для заявок по запросу.",
  canonicalPath: "/catalog",
});
```

Modify `apps/front/src/app/about/page.tsx` and `apps/front/src/app/delivery/page.tsx` to use `indexablePageMetadata` with canonical paths `/about` and `/delivery`.

- [ ] **Step 4: Use metadata helper in category and product pages**

In `apps/front/src/app/catalog/[categorySlug]/page.tsx`, import:

```ts
import { indexablePageMetadata, noindexPageMetadata } from "@/lib/seo/metadata";
```

Replace successful category metadata return:

```ts
return indexablePageMetadata({
  title: category.seo.title ?? category.h1 ?? category.name,
  description: category.seo.description ?? category.description,
  canonicalPath: category.seo.canonicalPath,
});
```

Replace catch return:

```ts
return noindexPageMetadata("Категория каталога LineCom");
```

In `apps/front/src/app/products/[slug]/page.tsx`, import the same helpers.

Replace successful product metadata return:

```ts
return indexablePageMetadata({
  title: product.seo.title ?? product.h1 ?? product.name,
  description: product.seo.description ?? product.shortDescription ?? product.description,
  canonicalPath: product.seo.canonicalPath,
});
```

Replace catch return:

```ts
return noindexPageMetadata("Товар каталога LineCom");
```

- [ ] **Step 5: Run metadata-related tests and build check**

Run:

```powershell
npm.cmd test -- metadata site
npm.cmd run build
```

Expected: tests pass; build succeeds.

- [ ] **Step 6: Commit metadata changes**

```powershell
git add apps/front/src/app/layout.tsx apps/front/src/app/page.tsx apps/front/src/app/catalog/page.tsx apps/front/src/app/about/page.tsx apps/front/src/app/delivery/page.tsx apps/front/src/app/catalog/[categorySlug]/page.tsx apps/front/src/app/products/[slug]/page.tsx
git commit -m "feat: add public canonical metadata"
```

## Task 3: Internal Page Noindex Metadata

**Files:**
- Modify internal route page files listed in File Map.
- Optional create: `apps/front/src/lib/seo/internal-pages.test.ts`

- [ ] **Step 1: Add noindex metadata to auth pages**

In `apps/front/src/app/auth/login/page.tsx` and `apps/front/src/app/auth/register/page.tsx`, add:

```ts
import type { Metadata } from "next";
import { noindexPageMetadata } from "@/lib/seo/metadata";
```

Use page-specific exports:

```ts
export const metadata: Metadata = noindexPageMetadata("Вход в LineCom");
```

```ts
export const metadata: Metadata = noindexPageMetadata("Регистрация LineCom");
```

- [ ] **Step 2: Add noindex metadata to account pages**

Add `noindexPageMetadata` exports to:

- `apps/front/src/app/account/profile/page.tsx` - title `Профиль LineCom`
- `apps/front/src/app/account/requests/page.tsx` - title `Мои заявки LineCom`
- `apps/front/src/app/account/requests/[number]/page.tsx` - title `Заявка LineCom`

- [ ] **Step 3: Add noindex metadata to admin pages**

Add `noindexPageMetadata` exports to:

- `apps/front/src/app/admin/catalog/page.tsx` - title `Админка каталога LineCom`
- `apps/front/src/app/admin/homepage/page.tsx` - title `Админка главной LineCom`
- `apps/front/src/app/admin/requests/page.tsx` - title `Админка заявок LineCom`
- `apps/front/src/app/admin/requests/[number]/page.tsx` - title `Админка заявки LineCom`

- [ ] **Step 4: Run noindex import/build check**

Run:

```powershell
npm.cmd test -- metadata
npm.cmd run build
```

Expected: helper tests pass; build succeeds.

- [ ] **Step 5: Commit noindex metadata**

```powershell
git add apps/front/src/app/auth apps/front/src/app/account apps/front/src/app/admin
git commit -m "feat: noindex internal frontend pages"
```

## Task 4: Sitemap Builder And `app/sitemap.ts`

**Files:**
- Create: `apps/front/src/lib/seo/sitemap.ts`
- Create: `apps/front/src/lib/seo/sitemap.test.ts`
- Create: `apps/front/src/app/sitemap.ts`

- [ ] **Step 1: Write failing sitemap builder tests**

Create `apps/front/src/lib/seo/sitemap.test.ts`:

```ts
import { describe, expect, it } from "vitest";
import type { PublicCategoryTreeItem, PublicProductListItem } from "@/lib/api/catalog";
import { buildPublicSitemapEntries } from "./sitemap";

function category(overrides: Partial<PublicCategoryTreeItem>): PublicCategoryTreeItem {
  return {
    id: "category-1",
    parentId: null,
    name: "Витая пара",
    slug: "vitaya-para",
    h1: "Витая пара",
    description: null,
    sortOrder: 10,
    isVisibleInMenu: true,
    children: [],
    ...overrides,
  };
}

function product(overrides: Partial<PublicProductListItem>): PublicProductListItem {
  return {
    id: "product-1",
    name: "Кабель U/UTP",
    slug: "u-utp",
    sku: "LC-UTP",
    brand: null,
    category: { name: "Витая пара", slug: "vitaya-para" },
    availability: { code: "in_stock", label: "В наличии" },
    saleUnit: { code: "coil", label: "бухта" },
    unitQuantity: "305 м",
    mainImage: null,
    ...overrides,
  };
}

describe("public sitemap builder", () => {
  it("includes static public pages, visible categories, and products", () => {
    const entries = buildPublicSitemapEntries({
      origin: "https://linecom.example.ru",
      categories: [
        category({
          id: "root",
          slug: "kabel",
          children: [category({ id: "child", parentId: "root", slug: "vitaya-para" })],
        }),
      ],
      products: [product({ slug: "u-utp-cat-5e" })],
    });

    expect(entries.map((entry) => entry.url)).toEqual([
      "https://linecom.example.ru/",
      "https://linecom.example.ru/catalog",
      "https://linecom.example.ru/about",
      "https://linecom.example.ru/delivery",
      "https://linecom.example.ru/catalog/kabel",
      "https://linecom.example.ru/catalog/vitaya-para",
      "https://linecom.example.ru/products/u-utp-cat-5e",
    ]);
  });

  it("excludes categories hidden from menu from sitemap", () => {
    const entries = buildPublicSitemapEntries({
      origin: "https://linecom.example.ru",
      categories: [category({ slug: "hidden", isVisibleInMenu: false })],
      products: [],
    });

    expect(entries.some((entry) => entry.url.endsWith("/catalog/hidden"))).toBe(false);
  });

  it("deduplicates category and product URLs", () => {
    const entries = buildPublicSitemapEntries({
      origin: "https://linecom.example.ru/",
      categories: [category({ id: "a", slug: "vitaya-para" }), category({ id: "b", slug: "vitaya-para" })],
      products: [product({ id: "a", slug: "u-utp" }), product({ id: "b", slug: "u-utp" })],
    });

    expect(entries.filter((entry) => entry.url.endsWith("/catalog/vitaya-para"))).toHaveLength(1);
    expect(entries.filter((entry) => entry.url.endsWith("/products/u-utp"))).toHaveLength(1);
  });
});
```

- [ ] **Step 2: Run RED for sitemap builder tests**

Run:

```powershell
npm.cmd test -- sitemap
```

Expected: FAIL because `apps/front/src/lib/seo/sitemap.ts` does not exist.

- [ ] **Step 3: Implement sitemap builder**

Create `apps/front/src/lib/seo/sitemap.ts`:

```ts
import type { MetadataRoute } from "next";
import type { PublicCategoryTreeItem, PublicProductListItem } from "@/lib/api/catalog";
import { routes } from "@/lib/routes";

type BuildPublicSitemapEntriesInput = {
  origin: string;
  categories: PublicCategoryTreeItem[];
  products: PublicProductListItem[];
};

const staticEntries = [
  { path: routes.home(), changeFrequency: "weekly" as const, priority: 1 },
  { path: routes.catalog(), changeFrequency: "daily" as const, priority: 0.9 },
  { path: routes.about(), changeFrequency: "monthly" as const, priority: 0.4 },
  { path: routes.delivery(), changeFrequency: "monthly" as const, priority: 0.4 },
];

export function buildPublicSitemapEntries({
  origin,
  categories,
  products,
}: BuildPublicSitemapEntriesInput): MetadataRoute.Sitemap {
  const normalizedOrigin = origin.replace(/\/+$/, "");
  const seen = new Set<string>();
  const entries: MetadataRoute.Sitemap = [];

  const push = (path: string, changeFrequency: MetadataRoute.Sitemap[number]["changeFrequency"], priority: number) => {
    const normalizedPath = path.startsWith("/") ? path : `/${path}`;
    const url = `${normalizedOrigin}${normalizedPath}`;
    if (seen.has(url)) {
      return;
    }

    seen.add(url);
    entries.push({ url, changeFrequency, priority });
  };

  for (const entry of staticEntries) {
    push(entry.path, entry.changeFrequency, entry.priority);
  }

  for (const category of flattenVisibleCategories(categories)) {
    push(routes.category(category.slug), "weekly", 0.7);
  }

  for (const product of products) {
    push(routes.product(product.slug), "weekly", 0.6);
  }

  return entries;
}

function flattenVisibleCategories(categories: PublicCategoryTreeItem[]) {
  const result: PublicCategoryTreeItem[] = [];
  const visit = (category: PublicCategoryTreeItem) => {
    if (category.isVisibleInMenu) {
      result.push(category);
    }

    category.children.forEach(visit);
  };

  categories.forEach(visit);
  return result;
}
```

- [ ] **Step 4: Add Next.js sitemap route**

Create `apps/front/src/app/sitemap.ts`:

```ts
import type { MetadataRoute } from "next";
import { getCategoryTree, getProducts, type PublicProductListItem } from "@/lib/api/catalog";
import { buildPublicSitemapEntries } from "@/lib/seo/sitemap";
import { getPublicSiteOrigin } from "@/lib/seo/site";

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const [categoryResult, productResult] = await Promise.allSettled([
    getCategoryTree(),
    loadSitemapProducts(),
  ]);

  return buildPublicSitemapEntries({
    origin: getPublicSiteOrigin(),
    categories: categoryResult.status === "fulfilled" ? categoryResult.value.items : [],
    products: productResult.status === "fulfilled" ? productResult.value : [],
  });
}

async function loadSitemapProducts() {
  const firstPage = await getProducts({ page: 1, pageSize: 100, sort: "category" });
  const products: PublicProductListItem[] = [...firstPage.items];

  for (let page = 2; page <= firstPage.totalPages; page += 1) {
    const response = await getProducts({ page, pageSize: firstPage.pageSize, sort: "category" });
    products.push(...response.items);
  }

  return products;
}
```

- [ ] **Step 5: Run GREEN for sitemap tests**

Run:

```powershell
npm.cmd test -- sitemap
npm.cmd run build
```

Expected: tests pass; build succeeds.

- [ ] **Step 6: Commit sitemap**

```powershell
git add apps/front/src/lib/seo/sitemap.ts apps/front/src/lib/seo/sitemap.test.ts apps/front/src/app/sitemap.ts
git commit -m "feat: add public catalog sitemap"
```

## Task 5: Robots Route

**Files:**
- Create: `apps/front/src/app/robots.ts`
- Create: `apps/front/src/app/robots.test.ts`

- [ ] **Step 1: Write failing robots tests**

Create `apps/front/src/app/robots.test.ts`:

```ts
import { afterEach, describe, expect, it } from "vitest";
import robots from "./robots";

const originalOrigin = process.env.LINECOM_PUBLIC_SITE_ORIGIN;

afterEach(() => {
  process.env.LINECOM_PUBLIC_SITE_ORIGIN = originalOrigin;
});

describe("robots route", () => {
  it("allows public pages and blocks internal authenticated surfaces", () => {
    process.env.LINECOM_PUBLIC_SITE_ORIGIN = "https://linecom.example.ru";

    expect(robots()).toEqual({
      rules: {
        userAgent: "*",
        allow: "/",
        disallow: ["/admin/", "/account/", "/auth/"],
      },
      sitemap: "https://linecom.example.ru/sitemap.xml",
      host: "https://linecom.example.ru",
    });
  });
});
```

- [ ] **Step 2: Run RED for robots tests**

Run:

```powershell
npm.cmd test -- robots
```

Expected: FAIL because `apps/front/src/app/robots.ts` does not exist.

- [ ] **Step 3: Implement robots route**

Create `apps/front/src/app/robots.ts`:

```ts
import type { MetadataRoute } from "next";
import { absoluteSiteUrl, getPublicSiteOrigin } from "@/lib/seo/site";

export default function robots(): MetadataRoute.Robots {
  return {
    rules: {
      userAgent: "*",
      allow: "/",
      disallow: ["/admin/", "/account/", "/auth/"],
    },
    sitemap: absoluteSiteUrl("/sitemap.xml"),
    host: getPublicSiteOrigin(),
  };
}
```

- [ ] **Step 4: Run GREEN for robots tests**

Run:

```powershell
npm.cmd test -- robots site
npm.cmd run build
```

Expected: tests pass; build succeeds.

- [ ] **Step 5: Commit robots**

```powershell
git add apps/front/src/app/robots.ts apps/front/src/app/robots.test.ts
git commit -m "feat: add public robots route"
```

## Task 6: Documentation, Browser QA, And Final Verification

**Files:**
- Create: `vault/Человекочитаемое/SEO GEO Public Catalog.md`
- Modify if needed: `vault/Человекочитаемое/README.md`
- Verify all frontend files touched by Tasks 1-5.

- [ ] **Step 1: Document implemented SEO/GEO contract**

Create `vault/Человекочитаемое/SEO GEO Public Catalog.md` with:

- site origin env var `LINECOM_PUBLIC_SITE_ORIGIN`;
- indexed public pages;
- blocked internal surfaces;
- canonical rules;
- sitemap source data and fallback behavior;
- out-of-scope items: SEO filter landing pages, segmented sitemaps, Open Graph image generation.

Add one README bullet if the README does not already mention this contract.

- [ ] **Step 2: Run focused frontend tests**

Run:

```powershell
npm.cmd test -- seo metadata sitemap robots
```

Expected: all focused tests pass.

- [ ] **Step 3: Run full frontend verification**

Run:

```powershell
npm.cmd test
npm.cmd run lint
npm.cmd run build
```

Expected: all commands pass. Existing `@next/next/no-img-element` warnings are acceptable only if lint exits successfully.

- [ ] **Step 4: Run solution sanity verification**

Run from repository root:

```powershell
dotnet test .\LineCom.sln -m:1
dotnet build .\LineCom.sln -m:1
```

Expected: all tests/build pass. NU1900 warnings are acceptable if commands exit successfully.

- [ ] **Step 5: Browser QA**

Start API/frontend or use mocks if no local API is available. Verify:

- `/` has a canonical link to the configured origin root.
- `/catalog` has a canonical link ending in `/catalog`.
- `/catalog/{categorySlug}` uses category API canonical path.
- `/products/{slug}` uses product API canonical path.
- `/robots.txt` includes `Disallow: /admin/`, `Disallow: /account/`, `Disallow: /auth/`, and a sitemap URL.
- `/sitemap.xml` includes homepage, catalog, visible categories, and published products.
- `/admin/catalog` and `/auth/login` include noindex metadata or are excluded by robots rules.
- No console errors in public happy paths.

- [ ] **Step 6: Hygiene checks**

Run:

```powershell
git diff --check
git status --short --branch
rg -n "TODO|TBD|temporary|hack|EntityFramework|DbContext" apps/front apps/api tests docs vault
```

Expected:

- no whitespace errors;
- only intended files changed;
- `admin-catalog-homepage-slice.png` remains untracked and unstaged;
- no EF/DbContext usage introduced;
- no unresolved work markers in changed implementation files.

- [ ] **Step 7: Commit docs and final fixes**

If documentation or verification fixes changed files:

```powershell
git add vault/Человекочитаемое/SEO\ GEO\ Public\ Catalog.md vault/Человекочитаемое/README.md apps/front
git commit -m "docs: document public SEO metadata"
```

If no files changed after verification, do not create an empty commit.

## Multi-Agent Execution Order

Use one worker per task and run review before moving on:

1. Task 1 worker.
2. Review Task 1 for spec coverage and code quality.
3. Run local Task 1 verification.
4. Commit Task 1 only.
5. Repeat the same loop for Tasks 2-6.

Every worker must know:

- You are not alone in the codebase.
- Do not revert edits made by others.
- Do not stage or commit `admin-catalog-homepage-slice.png`.
- Do not touch files outside the task scope without a concrete reason.
- Follow TDD: RED, implementation, GREEN.
- Use Context7 for Next.js docs if you need API details.
- Keep backend unchanged unless a reviewed issue proves it is necessary.

## Resume Prompt After Context Compaction

Use this prompt after compaction or in a new session:

```text
Продолжаем LineCom в D:\Projects\FL\LineCom.

Обязательные правила:
- Все ответы пользователю на русском.
- Соблюдать AGENTS.md.
- vault/Человекочитаемое — source of truth.
- Backend: только PostgreSQL + Npgsql + Dapper, без Entity Framework.
- Миграции только SQL через DbUp.
- Local FileStorage — целевой file-storage подход.
- Context7 использовать для вопросов по библиотекам/framework/API/CLI.
- Не трогать untracked admin-catalog-homepage-slice.png.

Текущее ожидаемое состояние:
- Branch: main, synced with origin/main.
- Ожидаемый untracked файл: admin-catalog-homepage-slice.png.
- Admin catalog and admin homepage management are completed and pushed.
- Следующий план: docs/superpowers/plans/2026-05-12-seo-geo-public-catalog.md.

Новая задача:
- Выполнять именно docs/superpowers/plans/2026-05-12-seo-geo-public-catalog.md.
- Использовать superpowers:subagent-driven-development.
- Идти task-by-task.
- Для каждого task: worker -> review/fix loop -> локальная проверка -> commit только scoped файлов.
- Начать с Task 1: Site Origin And SEO Metadata Helpers.

Перед стартом:
- Открыть и прочитать docs/superpowers/plans/2026-05-12-seo-geo-public-catalog.md.
- Проверить git status --short --branch.
- Начать Task 1 через subagent worker.
```
