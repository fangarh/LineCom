import type { AdminCategoryListItem, AdminProductDetail } from "@/lib/api/admin-catalog";
import { buildAdminProductCommand, formFromAdminProductDetail } from "./admin-product-editor-helpers";

export function buildProductCategoryChangeCommand(product: AdminProductDetail, targetCategoryId: string) {
  return buildAdminProductCommand({
    ...formFromAdminProductDetail(product),
    categoryId: targetCategoryId,
  });
}

export function isLeafCategory(category: AdminCategoryListItem | null | undefined, hasTreeChildren = false) {
  return Boolean(category) && !hasTreeChildren && category.childrenCount <= 0;
}

export function findCategoryById(categories: AdminCategoryListItem[], categoryId: string) {
  return categories.find((category) => category.id === categoryId) ?? null;
}

export function shouldShowCategoryAttributeWarning(product: AdminProductDetail | null | undefined, targetCategoryId: string) {
  return Boolean(product && targetCategoryId && product.categoryId !== targetCategoryId && product.attributes.length > 0);
}
