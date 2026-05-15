import { describe, expect, it, vi } from "vitest";
import type { PublicProductDetail, PublicProductListItem } from "@/lib/api/catalog";
import type { PublicHomepageSectionsResponse } from "@/lib/api/homepage";
import { resolveCuratedHomepageProducts } from "./curated-product-resolver";

function listProduct(id: string, name = id): PublicProductListItem {
  return {
    id,
    name,
    slug: id,
    sku: id.toUpperCase(),
    brand: null,
    category: { name: "Category", slug: "category" },
    availability: { code: "in_stock", label: "In stock" },
    saleUnit: { code: "piece", label: "piece" },
    unitQuantity: "1",
    mainImage: null,
  };
}

function detailProduct(id: string, name = id): PublicProductDetail {
  return {
    id,
    name,
    slug: id,
    sku: id.toUpperCase(),
    description: null,
    shortDescription: null,
    h1: null,
    category: { name: "Category", slug: "category" },
    brand: null,
    availability: { code: "in_stock", label: "In stock" },
    saleUnit: { code: "piece", label: "piece" },
    unitQuantity: "1",
    images: [{ url: `/${id}.jpg`, alt: name, title: null }],
    attributes: [],
    seo: { title: null, description: null, canonicalPath: `/products/${id}` },
    breadcrumbs: [],
  };
}

function homepageSections(): PublicHomepageSectionsResponse {
  return {
    sections: [
      {
        code: "hero_products",
        title: "Hero",
        type: "product_list",
        items: [
          {
            id: "hero-existing",
            productId: "product-existing",
            categoryId: null,
            name: "Existing product",
            slug: "product-existing",
            secondaryText: null,
          },
          {
            id: "hero-missing",
            productId: "product-missing",
            categoryId: null,
            name: "Missing product",
            slug: "product-missing",
            secondaryText: null,
          },
        ],
      },
      {
        code: "featured_products",
        title: "Featured",
        type: "product_list",
        items: [
          {
            id: "featured-missing",
            productId: "product-featured",
            categoryId: null,
            name: "Featured product",
            slug: "product-featured",
            secondaryText: null,
          },
        ],
      },
    ],
  };
}

describe("resolveCuratedHomepageProducts", () => {
  it("loads curated products that are absent from the first catalog page", async () => {
    const getProduct = vi.fn(async (slug: string) => detailProduct(slug));

    const products = await resolveCuratedHomepageProducts({
      products: [listProduct("product-existing")],
      sections: homepageSections(),
      getProduct,
    });

    expect(products.map((product) => product.id)).toEqual([
      "product-existing",
      "product-missing",
      "product-featured",
    ]);
    expect(products.find((product) => product.id === "product-missing")?.mainImage?.url).toBe("/product-missing.jpg");
    expect(getProduct).toHaveBeenCalledWith("product-missing");
    expect(getProduct).toHaveBeenCalledWith("product-featured");
  });
});
