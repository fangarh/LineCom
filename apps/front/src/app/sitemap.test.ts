import { beforeEach, describe, expect, it, vi } from "vitest";
import type { PublicProductListItem } from "@/lib/api/catalog";
import sitemap from "./sitemap";
import { getCategoryTree, getProducts } from "@/lib/api/catalog";

vi.mock("@/lib/api/catalog", () => ({
  getCategoryTree: vi.fn(),
  getProducts: vi.fn(),
}));

vi.mock("@/lib/seo/site", () => ({
  getPublicSiteOrigin: vi.fn(() => "https://linecom.example.ru"),
}));

const getCategoryTreeMock = vi.mocked(getCategoryTree);
const getProductsMock = vi.mocked(getProducts);

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

describe("sitemap route", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getCategoryTreeMock.mockResolvedValue({ items: [] });
  });

  it("loads sitemap products with the backend-supported page size", async () => {
    getProductsMock
      .mockResolvedValueOnce({
        items: [product({ id: "first-page", slug: "first-page-product" })],
        page: 1,
        pageSize: 60,
        totalItems: 2,
        totalPages: 2,
      })
      .mockResolvedValueOnce({
        items: [product({ id: "second-page", slug: "second-page-product" })],
        page: 2,
        pageSize: 60,
        totalItems: 2,
        totalPages: 2,
      });

    const entries = await sitemap();

    expect(getProductsMock).toHaveBeenNthCalledWith(1, { page: 1, pageSize: 60, sort: "category" });
    expect(getProductsMock).toHaveBeenNthCalledWith(2, { page: 2, pageSize: 60, sort: "category" });
    expect(entries.some((entry) => entry.url === "https://linecom.example.ru/products/second-page-product")).toBe(true);
  });

  it("returns static entries when sitemap products cannot be loaded", async () => {
    getProductsMock.mockRejectedValue(new Error("Invalid page size"));

    const entries = await sitemap();

    expect(entries.map((entry) => entry.url)).toEqual([
      "https://linecom.example.ru/",
      "https://linecom.example.ru/catalog",
      "https://linecom.example.ru/contacts",
      "https://linecom.example.ru/delivery",
    ]);
  });

  it("does not load product pages beyond the sitemap release page limit", async () => {
    getProductsMock.mockImplementation(({ page = 1 }) =>
      Promise.resolve({
        items: [product({ id: `page-${page}`, slug: `page-${page}` })],
        page,
        pageSize: 60,
        totalItems: 100,
        totalPages: 100,
      }),
    );

    const entries = await sitemap();

    expect(getProductsMock).toHaveBeenCalledTimes(10);
    expect(getProductsMock).toHaveBeenLastCalledWith({ page: 10, pageSize: 60, sort: "category" });
    expect(entries.some((entry) => entry.url === "https://linecom.example.ru/products/page-10")).toBe(true);
    expect(entries.some((entry) => entry.url === "https://linecom.example.ru/products/page-11")).toBe(false);
  });

  it("truncates product URLs at the sitemap release URL limit", async () => {
    getProductsMock.mockImplementation(({ page = 1 }) =>
      Promise.resolve({
        items: Array.from({ length: 60 }, (_, index) =>
          product({
            id: `page-${page}-product-${index}`,
            slug: `page-${page}-product-${index}`,
          }),
        ),
        page,
        pageSize: 60,
        totalItems: 1200,
        totalPages: 20,
      }),
    );

    const entries = await sitemap();
    const productUrls = entries
      .map((entry) => entry.url)
      .filter((url) => url.startsWith("https://linecom.example.ru/products/"));

    expect(productUrls).toHaveLength(500);
    expect(getProductsMock).toHaveBeenCalledTimes(9);
    expect(productUrls).toContain("https://linecom.example.ru/products/page-9-product-19");
    expect(productUrls).not.toContain("https://linecom.example.ru/products/page-9-product-20");
  });
});
