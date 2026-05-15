import { describe, expect, it } from "vitest";
import type { AdminCategoryListItem, AdminProductDetail } from "@/lib/api/admin-catalog";
import {
  buildProductCategoryChangeCommand,
  isLeafCategory,
  shouldShowCategoryAttributeWarning,
} from "./admin-product-category-change-helpers";

const leafCategory: AdminCategoryListItem = {
  id: "cat-leaf",
  parentId: "cat-root",
  name: "Leaf",
  slug: "leaf",
  sortOrder: 10,
  isActive: true,
  isVisibleInMenu: true,
  productsCount: 3,
  childrenCount: 0,
};

const parentCategory: AdminCategoryListItem = {
  ...leafCategory,
  id: "cat-parent",
  childrenCount: 2,
};

const productDetail: AdminProductDetail = {
  id: "product-1",
  categoryId: "cat-current",
  categoryName: "Current",
  brandId: "brand-1",
  brandName: "Brand",
  name: "  Product name  ",
  slug: " product-slug ",
  sku: " SKU-1 ",
  externalId: " ERP-1 ",
  description: " Description ",
  shortDescription: " Short ",
  availabilityStatus: "in_stock",
  saleUnit: " pcs ",
  unitQuantity: " 1 ",
  publishStatus: "draft",
  isActive: true,
  seoTitle: " SEO title ",
  seoDescription: " SEO description ",
  h1: " H1 ",
  sortOrder: 15,
  readiness: { canPublish: true, issues: [] },
  images: { imagesCount: 0, mainImageFileId: null },
  attributes: [
    {
      attributeId: "attr-1",
      code: "color",
      name: "Color",
      type: "text",
      unit: null,
      valueText: "Black",
      valueNumber: null,
      valueBoolean: null,
      attributeOptionId: null,
      optionValue: null,
    },
  ],
};

describe("admin product category change helpers", () => {
  it("builds a full product update command while changing only categoryId", () => {
    expect(buildProductCategoryChangeCommand(productDetail, "cat-target")).toEqual({
      categoryId: "cat-target",
      brandId: "brand-1",
      name: "Product name",
      slug: "product-slug",
      sku: "SKU-1",
      externalId: "ERP-1",
      description: "Description",
      shortDescription: "Short",
      availabilityStatus: "in_stock",
      saleUnit: "pcs",
      unitQuantity: "1",
      publishStatus: "draft",
      isActive: true,
      seoTitle: "SEO title",
      seoDescription: "SEO description",
      h1: "H1",
      sortOrder: 15,
    });
  });

  it("rejects categories with tree children or persisted children count", () => {
    expect(isLeafCategory(leafCategory)).toBe(true);
    expect(isLeafCategory(leafCategory, true)).toBe(false);
    expect(isLeafCategory(parentCategory)).toBe(false);
    expect(isLeafCategory(null)).toBe(false);
  });

  it("warns only for real category changes when the product has attribute values", () => {
    expect(shouldShowCategoryAttributeWarning(productDetail, "cat-target")).toBe(true);
    expect(shouldShowCategoryAttributeWarning(productDetail, "cat-current")).toBe(false);
    expect(shouldShowCategoryAttributeWarning({ ...productDetail, attributes: [] }, "cat-target")).toBe(false);
    expect(shouldShowCategoryAttributeWarning(productDetail, "")).toBe(false);
  });
});
