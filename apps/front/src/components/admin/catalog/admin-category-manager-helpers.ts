import type {
  AdminCategoryDetail,
  AdminCategoryListParams,
  UpsertAdminCategoryCommand,
} from "@/lib/api/admin-catalog";
import type { CategoryFormState } from "./admin-category-form";

export type { CategoryFormState } from "./admin-category-form";

export const emptyCategoryForm: CategoryFormState = {
  name: "",
  slug: "",
  parentId: "",
  description: "",
  h1: "",
  seoTitle: "",
  seoDescription: "",
  sortOrder: "0",
  isActive: true,
  isVisibleInMenu: true,
};

export function buildCategoryListParams(search: string, parentFilter: string, activeFilter: string): AdminCategoryListParams {
  const params: AdminCategoryListParams = {};
  const normalizedSearch = search.trim();

  if (normalizedSearch) {
    params.search = normalizedSearch;
  }

  if (parentFilter) {
    params.parentId = parentFilter;
  }

  if (activeFilter === "true") {
    params.isActive = true;
  } else if (activeFilter === "false") {
    params.isActive = false;
  }

  return params;
}

export function categoryFormFromDetail(category: AdminCategoryDetail): CategoryFormState {
  return {
    name: category.name,
    slug: category.slug,
    parentId: category.parentId ?? "",
    description: category.description ?? "",
    h1: category.h1 ?? "",
    seoTitle: category.seoTitle ?? "",
    seoDescription: category.seoDescription ?? "",
    sortOrder: String(category.sortOrder),
    isActive: category.isActive,
    isVisibleInMenu: category.isVisibleInMenu,
  };
}

export function buildCategoryCommand(form: CategoryFormState): UpsertAdminCategoryCommand {
  return {
    name: form.name.trim(),
    slug: form.slug.trim(),
    parentId: form.parentId || null,
    description: normalizeOptionalText(form.description),
    h1: normalizeOptionalText(form.h1),
    seoTitle: normalizeOptionalText(form.seoTitle),
    seoDescription: normalizeOptionalText(form.seoDescription),
    sortOrder: parseCategorySortOrder(form.sortOrder),
    isActive: form.isActive,
    isVisibleInMenu: form.isVisibleInMenu,
  };
}

export function parseCategorySortOrder(value: string) {
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : 0;
}

function normalizeOptionalText(value: string) {
  const normalized = value.trim();
  return normalized || null;
}
