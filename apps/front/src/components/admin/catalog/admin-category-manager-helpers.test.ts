import { describe, expect, it } from "vitest";
import type { AdminCategoryDetail } from "@/lib/api/admin-catalog";
import {
  buildCategoryCommand,
  buildCategoryListParams,
  categoryFormFromDetail,
  emptyCategoryForm,
  parseCategorySortOrder,
} from "./admin-category-manager-helpers";

const categoryDetail: AdminCategoryDetail = {
  id: "cat-cables",
  parentId: null,
  name: "Кабели",
  slug: "kabeli",
  description: null,
  h1: "Купить кабели",
  seoTitle: null,
  seoDescription: "SEO описание",
  sortOrder: 20,
  isActive: true,
  isVisibleInMenu: false,
  productsCount: 4,
  childrenCount: 1,
};

describe("admin category manager helpers", () => {
  it("maps category details to form state and trims command payloads", () => {
    expect(categoryFormFromDetail(categoryDetail)).toEqual({
      name: "Кабели",
      slug: "kabeli",
      parentId: "",
      description: "",
      h1: "Купить кабели",
      seoTitle: "",
      seoDescription: "SEO описание",
      sortOrder: "20",
      isActive: true,
      isVisibleInMenu: false,
    });

    expect(
      buildCategoryCommand({
        ...emptyCategoryForm,
        name: "  Муфты  ",
        slug: " mufty ",
        parentId: "cat-cables",
        description: "   ",
        h1: " Муфты для кабеля ",
        seoTitle: " Муфты SEO ",
        seoDescription: " SEO описание ",
        sortOrder: "30",
        isActive: false,
      }),
    ).toEqual({
      name: "Муфты",
      slug: "mufty",
      parentId: "cat-cables",
      description: null,
      h1: "Муфты для кабеля",
      seoTitle: "Муфты SEO",
      seoDescription: "SEO описание",
      sortOrder: 30,
      isActive: false,
      isVisibleInMenu: true,
    });
  });

  it("builds list params from search, parent and active filters", () => {
    expect(buildCategoryListParams("  кабель  ", "cat-root", "true")).toEqual({
      search: "кабель",
      parentId: "cat-root",
      isActive: true,
    });
    expect(buildCategoryListParams("  ", "", "false")).toEqual({ isActive: false });
    expect(buildCategoryListParams("", "", "")).toEqual({});
  });

  it("parses invalid sort values as zero", () => {
    expect(parseCategorySortOrder("15")).toBe(15);
    expect(parseCategorySortOrder("not-number")).toBe(0);
    expect(parseCategorySortOrder("")).toBe(0);
  });
});
