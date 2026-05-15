import { describe, expect, it } from "vitest";
import type { AdminHomepageSectionItem } from "@/lib/api/admin-homepage";
import { getAdminHomepageActiveTargetIds } from "./admin-homepage-section-editor-helpers";

function sectionItem(overrides: Partial<AdminHomepageSectionItem>): AdminHomepageSectionItem {
  return {
    id: overrides.id ?? "item",
    productId: overrides.productId ?? null,
    categoryId: overrides.categoryId ?? null,
    name: overrides.name ?? "Item",
    slug: overrides.slug ?? null,
    secondaryText: overrides.secondaryText ?? null,
    sortOrder: overrides.sortOrder ?? 0,
    isActive: overrides.isActive ?? true,
    visibilityStatus: overrides.visibilityStatus ?? "visible",
  };
}

describe("admin homepage section editor helpers", () => {
  it("returns active product and category target ids separately", () => {
    expect(
      getAdminHomepageActiveTargetIds([
        sectionItem({ id: "item-product", productId: "product-1" }),
        sectionItem({ id: "item-category", categoryId: "category-1" }),
      ]),
    ).toEqual({
      productIds: ["product-1"],
      categoryIds: ["category-1"],
    });
  });

  it("ignores null target fields", () => {
    expect(getAdminHomepageActiveTargetIds([sectionItem({ id: "item-empty" })])).toEqual({
      productIds: [],
      categoryIds: [],
    });
  });

  it("deduplicates repeated target ids", () => {
    expect(
      getAdminHomepageActiveTargetIds([
        sectionItem({ id: "item-product-1", productId: "product-1" }),
        sectionItem({ id: "item-product-2", productId: "product-1" }),
        sectionItem({ id: "item-category-1", categoryId: "category-1" }),
        sectionItem({ id: "item-category-2", categoryId: "category-1" }),
      ]),
    ).toEqual({
      productIds: ["product-1"],
      categoryIds: ["category-1"],
    });
  });
});
