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

export type PublicFilterOption = {
  value: string;
  slug: string;
  sortOrder: number;
};

export type PublicFilter = {
  code: string;
  name: string;
  type: string;
  unit: string | null;
  sortOrder: number;
  options: PublicFilterOption[];
};

export type PublicCategoryFiltersResponse = {
  category: PublicCategorySummary;
  filters: PublicFilter[];
};

export type PublicCatalogFiltersResponse = {
  filters: PublicFilter[];
};

export type ProductListParams = {
  categorySlug?: string;
  page?: number;
  pageSize?: number;
  sort?: "category" | "name" | "newest";
  brandSlug?: string;
  availabilityStatus?: string;
  saleUnit?: string;
  attributes?: Record<string, string>;
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

export function getCategoryFilters(slug: string) {
  return apiJson<PublicCategoryFiltersResponse>(
    `/api/public/catalog/categories/${encodeURIComponent(slug)}/filters`,
    {
      next: { revalidate: 60 },
    },
  );
}

export function getCatalogFilters() {
  return apiJson<PublicCatalogFiltersResponse>("/api/public/catalog/filters", {
    next: { revalidate: 60 },
  });
}

export function getProducts(params: ProductListParams = {}) {
  const search = new URLSearchParams();
  if (params.categorySlug) search.set("categorySlug", params.categorySlug);
  if (params.page) search.set("page", String(params.page));
  if (params.pageSize) search.set("pageSize", String(params.pageSize));
  if (params.sort) search.set("sort", params.sort);
  if (params.brandSlug) search.set("brandSlug", params.brandSlug);
  if (params.availabilityStatus) search.set("availabilityStatus", params.availabilityStatus);
  if (params.saleUnit) search.set("saleUnit", params.saleUnit);

  for (const [code, value] of Object.entries(params.attributes ?? {})) {
    search.set(`attribute.${code}`, value);
  }

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
