import { describe, expect, it, vi } from "vitest";
import type { AdminProductListResponse } from "@/lib/api/admin-catalog";
import { emptyProductForm } from "./admin-product-editor-helpers";
import {
  buildDuplicateCandidateParams,
  buildProductListParams,
  loadCatalogOptionPages,
  productPageMetaFromResponse,
} from "./admin-product-manager-helpers";

describe("admin product manager helpers", () => {
  it("builds product list params from pagination and filters", () => {
    expect(
      buildProductListParams({
        activeFilter: "true",
        brandFilter: "brand-cable",
        categoryFilter: "cat-cables",
        page: 2,
        pageSize: 30,
        publishStatusFilter: "published",
        search: "  кабель  ",
      }),
    ).toEqual({
      page: 2,
      pageSize: 30,
      search: "кабель",
      categoryId: "cat-cables",
      brandId: "brand-cable",
      isActive: true,
      publishStatus: "published",
    });

    expect(
      buildProductListParams({
        activeFilter: "false",
        brandFilter: "",
        categoryFilter: "",
        page: 1,
        pageSize: 60,
        publishStatusFilter: "",
        search: " ",
      }),
    ).toEqual({ page: 1, pageSize: 60, isActive: false });
  });

  it("maps list response pagination into page meta", () => {
    const response: AdminProductListResponse = {
      items: [],
      page: 3,
      pageSize: 60,
      totalItems: 135,
      totalPages: 3,
    };

    expect(productPageMetaFromResponse(response)).toEqual({
      page: 3,
      pageSize: 60,
      totalItems: 135,
      totalPages: 3,
    });
  });

  it("builds duplicate candidate params from trimmed identity fields", () => {
    expect(
      buildDuplicateCandidateParams(
        {
          ...emptyProductForm,
          name: "  Кабель ВВГнг  ",
          categoryId: "cat-cables",
          brandId: "",
          sku: " VVG-325 ",
          externalId: "   ",
          slug: " kabel-vvgng ",
        },
        "product-active",
      ),
    ).toEqual({
      name: "Кабель ВВГнг",
      categoryId: "cat-cables",
      brandId: null,
      sku: "VVG-325",
      externalId: null,
      slug: "kabel-vvgng",
      excludeProductId: "product-active",
      limit: 5,
    });
  });

  it("loads every catalog option page while request stays current", async () => {
    const fetchPage = vi.fn((page: number, pageSize: number) =>
      Promise.resolve({
        items: [`page-${page}`],
        page,
        pageSize,
        totalItems: 2,
        totalPages: 2,
      }),
    );

    await expect(loadCatalogOptionPages(fetchPage, () => true, 60)).resolves.toEqual(["page-1", "page-2"]);
    expect(fetchPage).toHaveBeenNthCalledWith(1, 1, 60);
    expect(fetchPage).toHaveBeenNthCalledWith(2, 2, 60);
  });

  it("returns null when a paged catalog option request becomes stale", async () => {
    const fetchPage = vi.fn((page: number, pageSize: number) =>
      Promise.resolve({
        items: [`page-${page}`],
        page,
        pageSize,
        totalItems: 2,
        totalPages: 2,
      }),
    );
    let isCurrent = true;

    const result = await loadCatalogOptionPages(
      async (page, pageSize) => {
        const response = await fetchPage(page, pageSize);
        isCurrent = false;
        return response;
      },
      () => isCurrent,
      60,
    );

    expect(result).toBeNull();
    expect(fetchPage).toHaveBeenCalledTimes(1);
  });
});
