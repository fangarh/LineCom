import { describe, expect, it } from "vitest";
import type { AdminProductListItem } from "@/lib/api/admin-catalog";
import {
  formatPageRange,
  getProductIssueLabels,
  getProductStatusLabels,
  getPublishStatusLabel,
} from "./admin-product-list-helpers";

function product(overrides: Partial<AdminProductListItem> = {}): AdminProductListItem {
  return {
    id: "product-1",
    name: "Кабель ВВГнг",
    slug: "kabel-vvgng",
    sku: "VVG",
    externalId: "ERP-1",
    categoryName: "Кабели",
    categorySlug: "kabeli",
    brandName: "Кабельный завод",
    publishStatus: "draft",
    isActive: true,
    availabilityStatus: "in_stock",
    sortOrder: 10,
    readiness: { canPublish: false, issues: [{ code: "missing_image", message: "Добавьте изображение." }] },
    ...overrides,
  };
}

describe("admin product list helpers", () => {
  it("formats a 1-based page range from response meta", () => {
    expect(formatPageRange({ page: 1, pageSize: 60, totalItems: 135, totalPages: 3 })).toBe("1-60 из 135");
    expect(formatPageRange({ page: 3, pageSize: 60, totalItems: 135, totalPages: 3 })).toBe("121-135 из 135");
    expect(formatPageRange({ page: 1, pageSize: 60, totalItems: 0, totalPages: 0 })).toBe("0 из 0");
  });

  it("maps publish status and product state labels", () => {
    expect(getPublishStatusLabel("draft")).toBe("Черновик");
    expect(getPublishStatusLabel("review")).toBe("Проверка");
    expect(getPublishStatusLabel("published")).toBe("Опубликован");
    expect(getPublishStatusLabel("archived")).toBe("Архив");
    expect(getPublishStatusLabel("custom")).toBe("custom");

    expect(getProductStatusLabels(product())).toEqual(["Активен", "Черновик", "Нельзя публиковать"]);
    expect(getProductStatusLabels(product({ isActive: false, publishStatus: "published", readiness: { canPublish: true, issues: [] } }))).toEqual([
      "Неактивен",
      "Опубликован",
      "Готов к публикации",
    ]);
  });

  it("shows category, slug and readiness issues as issue labels", () => {
    expect(getProductIssueLabels(product())).toEqual(["Добавьте изображение."]);
    expect(getProductIssueLabels(product({ categoryName: "", categorySlug: "", slug: "", readiness: { canPublish: false, issues: [] } }))).toEqual([
      "Нет категории",
      "Нет slug",
    ]);
  });
});
