import { apiForm, apiJson } from "./http";

type Id = string;

type PageParams = {
  page?: number;
  pageSize?: number;
};

export type AdminCategoryListParams = PageParams & {
  parentId?: Id | null;
  search?: string | null;
  isActive?: boolean | null;
};

export type AdminCategoryListResponse = {
  items: AdminCategoryListItem[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

export type AdminCategoryListItem = {
  id: Id;
  parentId: Id | null;
  name: string;
  slug: string;
  sortOrder: number;
  isActive: boolean;
  isVisibleInMenu: boolean;
  productsCount: number;
  childrenCount: number;
};

export type AdminCategoryDetail = AdminCategoryListItem & {
  description: string | null;
  seoTitle: string | null;
  seoDescription: string | null;
  h1: string | null;
};

export type UpsertAdminCategoryCommand = {
  parentId?: Id | null;
  name?: string | null;
  slug?: string | null;
  description?: string | null;
  seoTitle?: string | null;
  seoDescription?: string | null;
  h1?: string | null;
  sortOrder?: number | null;
  isActive?: boolean | null;
  isVisibleInMenu?: boolean | null;
};

export type MoveAdminCategoryCommand = {
  parentId: Id | null;
};

export type SortAdminCategoryCommand = {
  sortOrder: number;
};

export type AdminBrandListParams = PageParams & {
  search?: string | null;
  isActive?: boolean | null;
};

export type AdminBrandListResponse = {
  items: AdminBrandListItem[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

export type AdminBrandListItem = {
  id: Id;
  name: string;
  slug: string;
  isActive: boolean;
  productsCount: number;
};

export type AdminBrandDetail = AdminBrandListItem & {
  description: string | null;
  seoTitle: string | null;
  seoDescription: string | null;
  logoFileId: Id | null;
};

export type UpsertAdminBrandCommand = {
  name?: string | null;
  slug?: string | null;
  description?: string | null;
  seoTitle?: string | null;
  seoDescription?: string | null;
  logoFileId?: Id | null;
  isActive?: boolean | null;
};

export type AdminCategoryAttributesResponse = {
  items: AdminCategoryAttribute[];
};

export type AdminCategoryAttribute = {
  id: Id;
  categoryId: Id;
  name: string;
  code: string;
  type: string;
  unit: string | null;
  isRequired: boolean;
  isFilterable: boolean;
  isComparable: boolean;
  isVisibleInProduct: boolean;
  isSeoImportant: boolean;
  isUsedInGeneratedName: boolean;
  sortOrder: number;
  isActive: boolean;
  productValuesCount: number;
  options: AdminAttributeOption[];
};

export type AdminAttributeOption = {
  id: Id;
  value: string;
  slug: string;
  normalizedValue: string;
  sortOrder: number;
  isActive: boolean;
  productValuesCount: number;
};

export type UpsertAdminCategoryAttributeCommand = {
  name?: string | null;
  code?: string | null;
  type?: string | null;
  unit?: string | null;
  isRequired?: boolean | null;
  isFilterable?: boolean | null;
  isComparable?: boolean | null;
  isVisibleInProduct?: boolean | null;
  isSeoImportant?: boolean | null;
  isUsedInGeneratedName?: boolean | null;
  sortOrder?: number | null;
  isActive?: boolean | null;
};

export type UpsertAdminAttributeOptionCommand = {
  value?: string | null;
  slug?: string | null;
  normalizedValue?: string | null;
  sortOrder?: number | null;
  isActive?: boolean | null;
};

export type InheritAdminCategoryAttributesResponse = {
  added: number;
  skipped: number;
};

export type AdminProductListParams = PageParams & {
  categoryId?: Id | null;
  brandId?: Id | null;
  isActive?: boolean | null;
  publishStatus?: string | null;
  search?: string | null;
};

export type AdminProductDuplicateCandidatesParams = {
  name?: string | null;
  categoryId?: Id | null;
  brandId?: Id | null;
  sku?: string | null;
  externalId?: string | null;
  slug?: string | null;
  excludeProductId?: Id | null;
  limit?: number | null;
  similarityThreshold?: number | null;
};

export type AdminProductListResponse = {
  items: AdminProductListItem[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

export type AdminProductListItem = {
  id: Id;
  name: string;
  slug: string;
  sku: string | null;
  externalId: string | null;
  categoryName: string;
  categorySlug: string;
  brandName: string | null;
  publishStatus: string;
  isActive: boolean;
  availabilityStatus: string;
  sortOrder: number;
  readiness: AdminProductReadiness;
};

export type AdminProductDetail = {
  id: Id;
  categoryId: Id;
  categoryName: string;
  brandId: Id | null;
  brandName: string | null;
  name: string;
  slug: string;
  sku: string | null;
  externalId: string | null;
  description: string | null;
  shortDescription: string | null;
  availabilityStatus: string;
  saleUnit: string;
  unitQuantity: string;
  publishStatus: string;
  isActive: boolean;
  seoTitle: string | null;
  seoDescription: string | null;
  h1: string | null;
  sortOrder: number;
  readiness: AdminProductReadiness;
  images: AdminProductImageSummary;
  attributes: AdminProductAttributeValue[];
};

export type AdminProductReadiness = {
  canPublish: boolean;
  issues: AdminProductReadinessIssue[];
};

export type AdminProductReadinessIssue = {
  code: string;
  message: string;
};

export type AdminProductImageSummary = {
  imagesCount: number;
  mainImageFileId: Id | null;
};

export type AdminProductAttributeValue = {
  attributeId: Id;
  code: string;
  name: string;
  type: string;
  unit: string | null;
  valueText: string | null;
  valueNumber: number | null;
  valueBoolean: boolean | null;
  attributeOptionId: Id | null;
  optionValue: string | null;
};

export type UpsertAdminProductCommand = {
  categoryId?: Id | null;
  brandId?: Id | null;
  name?: string | null;
  slug?: string | null;
  sku?: string | null;
  externalId?: string | null;
  description?: string | null;
  shortDescription?: string | null;
  availabilityStatus?: string | null;
  saleUnit?: string | null;
  unitQuantity?: string | null;
  publishStatus?: string | null;
  isActive?: boolean | null;
  seoTitle?: string | null;
  seoDescription?: string | null;
  h1?: string | null;
  sortOrder?: number | null;
};

export type UpdateAdminProductAttributesCommand = {
  values: UpsertAdminProductAttributeValueCommand[];
};

export type UpsertAdminProductAttributeValueCommand = {
  attributeId: Id;
  valueText?: string | null;
  valueNumber?: number | null;
  valueBoolean?: boolean | null;
  attributeOptionId?: Id | null;
};

export type AdminProductDuplicateCandidatesResponse = {
  items: AdminProductDuplicateCandidate[];
};

export type AdminProductDuplicateCandidate = {
  id: Id;
  name: string;
  slug: string;
  sku: string | null;
  externalId: string | null;
  categoryName: string;
  categorySlug: string;
  brandName: string | null;
  publishStatus: string;
  isActive: boolean;
  similarity: number;
};

export type AdminProductImagesResponse = {
  items: AdminProductImage[];
};

export type AdminProductImage = {
  id: Id;
  storedFileId: Id;
  url: string;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  checksum: string;
  alt: string;
  title: string | null;
  sortOrder: number;
  isMain: boolean;
  createdAt: string;
};

export type UpdateAdminProductImageCommand = {
  alt?: string | null;
  title?: string | null;
};

export type UpdateAdminProductImageOrderCommand = {
  imageIds: Id[];
};

export type AdminBrandLogo = {
  storedFileId: Id;
  url: string;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  checksum: string;
};

export function getAdminCategories(params: AdminCategoryListParams = {}) {
  return apiJson<AdminCategoryListResponse>(withQuery("/api/admin/catalog/categories", params), {
    cache: "no-store",
  });
}

export function createAdminCategory(command: UpsertAdminCategoryCommand, csrfToken: string) {
  return apiJson<AdminCategoryDetail>("/api/admin/catalog/categories", {
    method: "POST",
    body: command,
    csrfToken,
  });
}

export function getAdminCategory(id: Id) {
  return apiJson<AdminCategoryDetail>(`/api/admin/catalog/categories/${encodeURIComponent(id)}`, {
    cache: "no-store",
  });
}

export function updateAdminCategory(id: Id, command: UpsertAdminCategoryCommand, csrfToken: string) {
  return apiJson<AdminCategoryDetail>(`/api/admin/catalog/categories/${encodeURIComponent(id)}`, {
    method: "PUT",
    body: command,
    csrfToken,
  });
}

export function deleteAdminCategory(id: Id, csrfToken: string) {
  return apiJson<void>(`/api/admin/catalog/categories/${encodeURIComponent(id)}`, {
    method: "DELETE",
    csrfToken,
  });
}

export function moveAdminCategory(id: Id, parentId: Id | null, csrfToken: string) {
  const body: MoveAdminCategoryCommand = { parentId };

  return apiJson<AdminCategoryDetail>(`/api/admin/catalog/categories/${encodeURIComponent(id)}/move`, {
    method: "PUT",
    body,
    csrfToken,
  });
}

export function sortAdminCategory(id: Id, sortOrder: number, csrfToken: string) {
  const body: SortAdminCategoryCommand = { sortOrder };

  return apiJson<AdminCategoryDetail>(`/api/admin/catalog/categories/${encodeURIComponent(id)}/sort`, {
    method: "PUT",
    body,
    csrfToken,
  });
}

export function getAdminBrands(params: AdminBrandListParams = {}) {
  return apiJson<AdminBrandListResponse>(withQuery("/api/admin/catalog/brands", params), {
    cache: "no-store",
  });
}

export function createAdminBrand(command: UpsertAdminBrandCommand, csrfToken: string) {
  return apiJson<AdminBrandDetail>("/api/admin/catalog/brands", {
    method: "POST",
    body: command,
    csrfToken,
  });
}

export function getAdminBrand(id: Id) {
  return apiJson<AdminBrandDetail>(`/api/admin/catalog/brands/${encodeURIComponent(id)}`, {
    cache: "no-store",
  });
}

export function updateAdminBrand(id: Id, command: UpsertAdminBrandCommand, csrfToken: string) {
  return apiJson<AdminBrandDetail>(`/api/admin/catalog/brands/${encodeURIComponent(id)}`, {
    method: "PUT",
    body: command,
    csrfToken,
  });
}

export function deleteAdminBrand(id: Id, csrfToken: string) {
  return apiJson<void>(`/api/admin/catalog/brands/${encodeURIComponent(id)}`, {
    method: "DELETE",
    csrfToken,
  });
}

export function uploadAdminBrandLogo(id: Id, file: File, csrfToken: string) {
  const body = new FormData();
  body.append("file", file, file.name);

  return apiForm<AdminBrandLogo>(`/api/admin/catalog/brands/${encodeURIComponent(id)}/logo`, {
    method: "PUT",
    body,
    csrfToken,
  });
}

export function deleteAdminBrandLogo(id: Id, csrfToken: string) {
  return apiJson<void>(`/api/admin/catalog/brands/${encodeURIComponent(id)}/logo`, {
    method: "DELETE",
    csrfToken,
  });
}

export function getAdminCategoryAttributes(categoryId: Id) {
  return apiJson<AdminCategoryAttributesResponse>(
    `/api/admin/catalog/categories/${encodeURIComponent(categoryId)}/attributes`,
    { cache: "no-store" },
  );
}

export function createAdminCategoryAttribute(
  categoryId: Id,
  command: UpsertAdminCategoryAttributeCommand,
  csrfToken: string,
) {
  return apiJson<AdminCategoryAttribute>(`/api/admin/catalog/categories/${encodeURIComponent(categoryId)}/attributes`, {
    method: "POST",
    body: command,
    csrfToken,
  });
}

export function updateAdminCategoryAttribute(
  categoryId: Id,
  attributeId: Id,
  command: UpsertAdminCategoryAttributeCommand,
  csrfToken: string,
) {
  return apiJson<AdminCategoryAttribute>(
    `/api/admin/catalog/categories/${encodeURIComponent(categoryId)}/attributes/${encodeURIComponent(attributeId)}`,
    {
      method: "PUT",
      body: command,
      csrfToken,
    },
  );
}

export function deleteAdminCategoryAttribute(categoryId: Id, attributeId: Id, csrfToken: string) {
  return apiJson<void>(
    `/api/admin/catalog/categories/${encodeURIComponent(categoryId)}/attributes/${encodeURIComponent(attributeId)}`,
    {
      method: "DELETE",
      csrfToken,
    },
  );
}

export function inheritAdminCategoryAttributesFromParent(categoryId: Id, csrfToken: string) {
  return apiJson<InheritAdminCategoryAttributesResponse>(
    `/api/admin/catalog/categories/${encodeURIComponent(categoryId)}/attributes/inherit-from-parent`,
    {
      method: "POST",
      csrfToken,
    },
  );
}

export function createAdminAttributeOption(
  categoryId: Id,
  attributeId: Id,
  command: UpsertAdminAttributeOptionCommand,
  csrfToken: string,
) {
  return apiJson<AdminAttributeOption>(
    `/api/admin/catalog/categories/${encodeURIComponent(categoryId)}/attributes/${encodeURIComponent(attributeId)}/options`,
    {
      method: "POST",
      body: command,
      csrfToken,
    },
  );
}

export function updateAdminAttributeOption(
  categoryId: Id,
  attributeId: Id,
  optionId: Id,
  command: UpsertAdminAttributeOptionCommand,
  csrfToken: string,
) {
  return apiJson<AdminAttributeOption>(
    `/api/admin/catalog/categories/${encodeURIComponent(categoryId)}/attributes/${encodeURIComponent(attributeId)}/options/${encodeURIComponent(optionId)}`,
    {
      method: "PUT",
      body: command,
      csrfToken,
    },
  );
}

export function deleteAdminAttributeOption(categoryId: Id, attributeId: Id, optionId: Id, csrfToken: string) {
  return apiJson<void>(
    `/api/admin/catalog/categories/${encodeURIComponent(categoryId)}/attributes/${encodeURIComponent(attributeId)}/options/${encodeURIComponent(optionId)}`,
    {
      method: "DELETE",
      csrfToken,
    },
  );
}

export function getAdminProducts(params: AdminProductListParams = {}) {
  return apiJson<AdminProductListResponse>(withQuery("/api/admin/catalog/products", params), {
    cache: "no-store",
  });
}

export function createAdminProduct(command: UpsertAdminProductCommand, csrfToken: string) {
  return apiJson<AdminProductDetail>("/api/admin/catalog/products", {
    method: "POST",
    body: command,
    csrfToken,
  });
}

export function getAdminProduct(id: Id) {
  return apiJson<AdminProductDetail>(`/api/admin/catalog/products/${encodeURIComponent(id)}`, {
    cache: "no-store",
  });
}

export function updateAdminProduct(id: Id, command: UpsertAdminProductCommand, csrfToken: string) {
  return apiJson<AdminProductDetail>(`/api/admin/catalog/products/${encodeURIComponent(id)}`, {
    method: "PUT",
    body: command,
    csrfToken,
  });
}

export function deleteAdminProduct(id: Id, csrfToken: string) {
  return apiJson<void>(`/api/admin/catalog/products/${encodeURIComponent(id)}`, {
    method: "DELETE",
    csrfToken,
  });
}

export function updateAdminProductAttributes(
  id: Id,
  command: UpdateAdminProductAttributesCommand,
  csrfToken: string,
) {
  return apiJson<AdminProductDetail>(`/api/admin/catalog/products/${encodeURIComponent(id)}/attributes`, {
    method: "PUT",
    body: command,
    csrfToken,
  });
}

export function getAdminProductDuplicateCandidates(params: AdminProductDuplicateCandidatesParams = {}) {
  return apiJson<AdminProductDuplicateCandidatesResponse>(
    withQuery("/api/admin/catalog/products/duplicate-candidates", params),
    { cache: "no-store" },
  );
}

export function getAdminProductImages(productId: Id) {
  return apiJson<AdminProductImagesResponse>(`/api/admin/catalog/products/${encodeURIComponent(productId)}/images`, {
    cache: "no-store",
  });
}

export function uploadAdminProductImages(productId: Id, files: File[], csrfToken: string) {
  const body = new FormData();
  for (const file of files) {
    body.append("files", file, file.name);
  }

  return apiForm<AdminProductImagesResponse>(`/api/admin/catalog/products/${encodeURIComponent(productId)}/images`, {
    method: "POST",
    body,
    csrfToken,
  });
}

export function updateAdminProductImage(
  productId: Id,
  imageId: Id,
  command: UpdateAdminProductImageCommand,
  csrfToken: string,
) {
  return apiJson<AdminProductImage>(
    `/api/admin/catalog/products/${encodeURIComponent(productId)}/images/${encodeURIComponent(imageId)}`,
    {
      method: "PUT",
      body: command,
      csrfToken,
    },
  );
}

export function updateAdminProductImageOrder(productId: Id, imageIds: Id[], csrfToken: string) {
  return apiJson<AdminProductImagesResponse>(`/api/admin/catalog/products/${encodeURIComponent(productId)}/images/order`, {
    method: "PUT",
    body: { imageIds },
    csrfToken,
  });
}

export function setAdminProductMainImage(productId: Id, imageId: Id, csrfToken: string) {
  return apiJson<AdminProductImage>(
    `/api/admin/catalog/products/${encodeURIComponent(productId)}/images/${encodeURIComponent(imageId)}/main`,
    {
      method: "PUT",
      csrfToken,
    },
  );
}

export function deleteAdminProductImage(productId: Id, imageId: Id, csrfToken: string) {
  return apiJson<void>(
    `/api/admin/catalog/products/${encodeURIComponent(productId)}/images/${encodeURIComponent(imageId)}`,
    {
      method: "DELETE",
      csrfToken,
    },
  );
}

function withQuery(path: string, params: Record<string, string | number | boolean | null | undefined>) {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== "") {
      search.set(key, String(value));
    }
  }

  const suffix = search.toString();
  return suffix ? `${path}?${suffix}` : path;
}
