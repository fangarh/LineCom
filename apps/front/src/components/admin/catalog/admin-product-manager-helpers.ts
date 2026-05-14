import type {
  AdminProductDuplicateCandidatesParams,
  AdminProductListParams,
  AdminProductListResponse,
} from "@/lib/api/admin-catalog";
import { normalizeOptionalText, type ProductFormState } from "./admin-product-editor-helpers";
import type { ProductListPageMeta } from "./admin-product-list-helpers";

type CatalogOptionPage<T> = {
  items: T[];
  totalPages: number;
};

type BuildProductListParamsInput = {
  activeFilter: string;
  brandFilter: string;
  categoryFilter: string;
  page: number;
  pageSize: number;
  publishStatusFilter: string;
  search: string;
};

export function buildProductListParams({
  activeFilter,
  brandFilter,
  categoryFilter,
  page,
  pageSize,
  publishStatusFilter,
  search,
}: BuildProductListParamsInput): AdminProductListParams {
  const params: AdminProductListParams = { page, pageSize };
  const normalizedSearch = search.trim();

  if (normalizedSearch) params.search = normalizedSearch;
  if (categoryFilter) params.categoryId = categoryFilter;
  if (brandFilter) params.brandId = brandFilter;
  if (activeFilter === "true") params.isActive = true;
  if (activeFilter === "false") params.isActive = false;
  if (publishStatusFilter) params.publishStatus = publishStatusFilter;

  return params;
}

export function productPageMetaFromResponse(response: AdminProductListResponse): ProductListPageMeta {
  return {
    page: response.page,
    pageSize: response.pageSize,
    totalItems: response.totalItems,
    totalPages: response.totalPages,
  };
}

export function buildDuplicateCandidateParams(
  form: ProductFormState,
  selectedProductId: string | null,
): AdminProductDuplicateCandidatesParams {
  return {
    name: normalizeOptionalText(form.name),
    categoryId: form.categoryId || null,
    brandId: form.brandId || null,
    sku: normalizeOptionalText(form.sku),
    externalId: normalizeOptionalText(form.externalId),
    slug: normalizeOptionalText(form.slug),
    excludeProductId: selectedProductId,
    limit: 5,
  };
}

export async function loadCatalogOptionPages<T>(
  fetchPage: (page: number, pageSize: number) => Promise<CatalogOptionPage<T>>,
  isCurrentRequest: () => boolean,
  pageSize: number,
) {
  const response = await fetchPage(1, pageSize);
  if (!isCurrentRequest()) return null;

  const items = [...response.items];

  for (let page = 2; page <= response.totalPages; page += 1) {
    const pageResponse = await fetchPage(page, pageSize);
    if (!isCurrentRequest()) return null;
    items.push(...pageResponse.items);
  }

  return items;
}
