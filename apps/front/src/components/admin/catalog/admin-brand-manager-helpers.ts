import type {
  AdminBrandDetail,
  AdminBrandListParams,
  AdminBrandLogo,
  UpsertAdminBrandCommand,
} from "@/lib/api/admin-catalog";

export type BrandFormState = {
  name: string;
  slug: string;
  description: string;
  seoTitle: string;
  seoDescription: string;
  isActive: boolean;
};

export type LogoPreviewState = {
  url: string;
  originalFileName: string;
};

export const emptyBrandForm: BrandFormState = {
  name: "",
  slug: "",
  description: "",
  seoTitle: "",
  seoDescription: "",
  isActive: true,
};

export function buildBrandListParams(search: string, activeFilter: string): AdminBrandListParams {
  const params: AdminBrandListParams = {};
  const normalizedSearch = search.trim();

  if (normalizedSearch) {
    params.search = normalizedSearch;
  }

  if (activeFilter === "true") {
    params.isActive = true;
  } else if (activeFilter === "false") {
    params.isActive = false;
  }

  return params;
}

export function brandFormFromDetail(brand: AdminBrandDetail): BrandFormState {
  return {
    name: brand.name,
    slug: brand.slug,
    description: brand.description ?? "",
    seoTitle: brand.seoTitle ?? "",
    seoDescription: brand.seoDescription ?? "",
    isActive: brand.isActive,
  };
}

export function buildBrandCommand(form: BrandFormState): UpsertAdminBrandCommand {
  return {
    name: form.name.trim(),
    slug: form.slug.trim(),
    description: normalizeOptionalText(form.description),
    seoTitle: normalizeOptionalText(form.seoTitle),
    seoDescription: normalizeOptionalText(form.seoDescription),
    isActive: form.isActive,
  };
}

export function logoPreviewFromUpload(logo: AdminBrandLogo): LogoPreviewState {
  return {
    url: logo.url,
    originalFileName: logo.originalFileName,
  };
}

function normalizeOptionalText(value: string) {
  const normalized = value.trim();
  return normalized || null;
}
