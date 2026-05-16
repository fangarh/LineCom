import { describe, expect, it } from "vitest";
import type { PublicCategoryTreeItem, PublicProductListItem } from "@/lib/api/catalog";
import { buildPublicSitemapEntries } from "./sitemap";

function category(overrides: Partial<PublicCategoryTreeItem>): PublicCategoryTreeItem {
  return {
    id: "category-1",
    parentId: null,
    name: "Витая пара",
    slug: "vitaya-para",
    h1: "Витая пара",
    description: null,
    sortOrder: 10,
    isVisibleInMenu: true,
    children: [],
    ...overrides,
  };
}

function product(overrides: Partial<PublicProductListItem>): PublicProductListItem {
  return {
    id: "product-1",
    name: "Кабель U/UTP",
    slug: "u-utp",
    sku: "LC-UTP",
    brand: null,
    category: { name: "Витая пара", slug: "vitaya-para" },
    availability: { code: "in_stock", label: "В наличии" },
    saleUnit: { code: "coil", label: "бухта" },
    unitQuantity: "305 м",
    mainImage: null,
    ...overrides,
  };
}

describe("public sitemap builder", () => {
  it("includes static public pages, visible categories, and products", () => {
    const entries = buildPublicSitemapEntries({
      origin: "https://linecom.example.ru",
      categories: [
        category({
          id: "root",
          slug: "kabel",
          children: [category({ id: "child", parentId: "root", slug: "vitaya-para" })],
        }),
      ],
      products: [product({ slug: "u-utp-cat-5e" })],
    });

    expect(entries.map((entry) => entry.url)).toEqual([
      "https://linecom.example.ru/",
      "https://linecom.example.ru/catalog",
      "https://linecom.example.ru/contacts",
      "https://linecom.example.ru/delivery",
      "https://linecom.example.ru/catalog/kabel",
      "https://linecom.example.ru/catalog/vitaya-para",
      "https://linecom.example.ru/products/u-utp-cat-5e",
    ]);
  });

  it("excludes categories hidden from menu from sitemap", () => {
    const entries = buildPublicSitemapEntries({
      origin: "https://linecom.example.ru",
      categories: [category({ slug: "hidden", isVisibleInMenu: false })],
      products: [],
    });

    expect(entries.some((entry) => entry.url.endsWith("/catalog/hidden"))).toBe(false);
  });

  it("still visits children of categories hidden from menu", () => {
    const entries = buildPublicSitemapEntries({
      origin: "https://linecom.example.ru",
      categories: [
        category({
          slug: "hidden",
          isVisibleInMenu: false,
          children: [category({ id: "visible-child", parentId: "category-1", slug: "visible-child" })],
        }),
      ],
      products: [],
    });

    expect(entries.some((entry) => entry.url.endsWith("/catalog/hidden"))).toBe(false);
    expect(entries.some((entry) => entry.url.endsWith("/catalog/visible-child"))).toBe(true);
  });

  it("deduplicates category and product URLs", () => {
    const entries = buildPublicSitemapEntries({
      origin: "https://linecom.example.ru/",
      categories: [category({ id: "a", slug: "vitaya-para" }), category({ id: "b", slug: "vitaya-para" })],
      products: [product({ id: "a", slug: "u-utp" }), product({ id: "b", slug: "u-utp" })],
    });

    expect(entries.filter((entry) => entry.url.endsWith("/catalog/vitaya-para"))).toHaveLength(1);
    expect(entries.filter((entry) => entry.url.endsWith("/products/u-utp"))).toHaveLength(1);
  });
});
