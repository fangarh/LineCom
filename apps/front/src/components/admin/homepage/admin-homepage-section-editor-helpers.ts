import type { AdminHomepageSectionItem } from "@/lib/api/admin-homepage";

export type AdminHomepageActiveTargetIds = {
  productIds: string[];
  categoryIds: string[];
};

export function getAdminHomepageActiveTargetIds(items: AdminHomepageSectionItem[]): AdminHomepageActiveTargetIds {
  const productIds = new Set<string>();
  const categoryIds = new Set<string>();

  for (const item of items) {
    if (item.productId) {
      productIds.add(item.productId);
    }

    if (item.categoryId) {
      categoryIds.add(item.categoryId);
    }
  }

  return {
    productIds: [...productIds],
    categoryIds: [...categoryIds],
  };
}
