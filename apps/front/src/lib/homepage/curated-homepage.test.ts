import { describe, expect, it } from "vitest";
import type { PublicCategoryTreeItem, PublicProductListItem } from "@/lib/api/catalog";
import type { PublicHomepageSectionsResponse } from "@/lib/api/homepage";
import { applyCuratedHomepageSections, categoryHighlights } from "./curated-homepage";
import { selectFeaturedProducts } from "./featured-products";

function productList(): PublicProductListItem[] {
  return [
    product("product-1", "UTP cable cat.5", "cable"),
    product("product-2", "Patch cord", "patch"),
    product("product-3", "SC adapter", "adapter"),
    product("product-4", "SFP module", "network"),
    product("product-5", "Cable tie", " расход"),
  ];
}

function product(id: string, name: string, categoryName: string): PublicProductListItem {
  return {
    id,
    name,
    slug: id,
    sku: id.toUpperCase(),
    brand: null,
    category: { name: categoryName, slug: categoryName },
    availability: { code: "in_stock", label: "В наличии" },
    saleUnit: { code: "piece", label: "шт" },
    unitQuantity: "1",
    mainImage: { url: `/${id}.jpg`, alt: name, title: null },
  };
}

function categoryTree(): PublicCategoryTreeItem[] {
  return [
    category("category-1", "Кабель", "cable"),
    category("category-2", "СКС", "sks"),
    category("category-3", "Шкафы", "cabinets"),
    category("category-4", "Инструмент", "tools"),
  ];
}

function category(
  id: string,
  name: string,
  slug: string,
  options: { isVisibleInMenu?: boolean; children?: PublicCategoryTreeItem[] } = {},
): PublicCategoryTreeItem {
  return {
    id,
    parentId: null,
    name,
    slug,
    h1: null,
    description: null,
    sortOrder: 0,
    isVisibleInMenu: options.isVisibleInMenu ?? true,
    children: options.children ?? [],
  };
}

function publicHomepageSectionsResponse(): PublicHomepageSectionsResponse {
  return {
    sections: [
      section("hero_products", "product_list", [
        { id: "hero-1", productId: "product-1" },
        { id: "hero-2", productId: "product-2" },
        { id: "hero-3", productId: "product-3" },
      ]),
      section("featured_products", "product_list", [
        { id: "featured-1", productId: "product-1" },
        { id: "featured-2", productId: "product-4" },
      ]),
      section("direction_categories", "category_list", [
        { id: "direction-1", categoryId: "category-1" },
        { id: "direction-2", categoryId: "category-2" },
      ]),
    ],
  };
}

function section(
  code: string,
  type: "product_list" | "category_list",
  items: Array<{ id: string; productId?: string | null; categoryId?: string | null }>,
) {
  return {
    code,
    title: code,
    type,
    items: items.map((item) => ({
      id: item.id,
      productId: item.productId ?? null,
      categoryId: item.categoryId ?? null,
      name: item.id,
      slug: item.id,
      secondaryText: null,
    })),
  };
}

describe("applyCuratedHomepageSections", () => {
  it("uses curated hero and featured products when public sections provide product ids", () => {
    const result = applyCuratedHomepageSections({
      products: productList(),
      categories: categoryTree(),
      sections: publicHomepageSectionsResponse(),
    });

    expect(result.heroProducts.map((product) => product.id)).toEqual(["product-1", "product-2", "product-3"]);
    expect(result.featuredProducts.map((product) => product.id)).toEqual(["product-1", "product-4"]);
    expect(result.highlights.map((category) => category.id)).toEqual(["category-1", "category-2"]);
  });

  it("preserves automatic selections when public sections are empty", () => {
    const products = productList();
    const categories = categoryTree();
    const result = applyCuratedHomepageSections({
      products,
      categories,
      sections: { sections: [] },
    });

    const automaticFeatured = selectFeaturedProducts(products);
    expect(result.featuredProducts).toEqual(automaticFeatured);
    expect(result.heroProducts).toEqual(automaticFeatured.slice(0, 3));
    expect(result.highlights).toEqual(categoryHighlights(categories));
  });

  it("ignores missing ids and falls back for a section with no usable curated items", () => {
    const products = productList();
    const categories = categoryTree();
    const result = applyCuratedHomepageSections({
      products,
      categories,
      sections: {
        sections: [
          section("hero_products", "product_list", [
            { id: "missing-hero", productId: "missing-product" },
            { id: "valid-hero", productId: "product-2" },
          ]),
          section("featured_products", "product_list", [
            { id: "missing-featured", productId: "missing-product" },
          ]),
          section("direction_categories", "category_list", [
            { id: "missing-direction", categoryId: "missing-category" },
          ]),
        ],
      },
    });

    const automaticFeatured = selectFeaturedProducts(products);
    expect(result.heroProducts.map((product) => product.id)).toEqual(["product-2"]);
    expect(result.featuredProducts).toEqual(automaticFeatured);
    expect(result.highlights).toEqual(categoryHighlights(categories));
  });

  it("uses only matching section codes for hero, featured, and highlights", () => {
    const result = applyCuratedHomepageSections({
      products: productList(),
      categories: categoryTree(),
      sections: {
        sections: [
          section("hero_products", "product_list", [
            { id: "hero", productId: "product-3" },
            { id: "hero-category", categoryId: "category-1" },
          ]),
          section("featured_products", "product_list", [
            { id: "featured", productId: "product-4" },
            { id: "featured-category", categoryId: "category-2" },
          ]),
          section("direction_categories", "category_list", [
            { id: "direction", categoryId: "category-3" },
            { id: "direction-product", productId: "product-1" },
          ]),
        ],
      },
    });

    expect(result.heroProducts.map((product) => product.id)).toEqual(["product-3"]);
    expect(result.featuredProducts.map((product) => product.id)).toEqual(["product-4"]);
    expect(result.highlights.map((category) => category.id)).toEqual(["category-3"]);
  });

  it("falls back to automatic highlights when direction categories only reference hidden categories", () => {
    const categories = [
      ...categoryTree(),
      category("category-hidden-parent", "Hidden parent", "hidden-parent", {
        children: [
          category("category-hidden-child", "Hidden child", "hidden-child", {
            isVisibleInMenu: false,
          }),
        ],
      }),
    ];

    const result = applyCuratedHomepageSections({
      products: productList(),
      categories,
      sections: {
        sections: [
          section("direction_categories", "category_list", [
            { id: "hidden-direction", categoryId: "category-hidden-child" },
          ]),
        ],
      },
    });

    expect(result.highlights).toEqual(categoryHighlights(categories));
  });

  it("ignores hidden direction categories and keeps visible curated categories in order", () => {
    const categories = [
      ...categoryTree(),
      category("category-hidden", "Hidden category", "hidden", {
        isVisibleInMenu: false,
      }),
    ];

    const result = applyCuratedHomepageSections({
      products: productList(),
      categories,
      sections: {
        sections: [
          section("direction_categories", "category_list", [
            { id: "hidden-direction", categoryId: "category-hidden" },
            { id: "visible-direction-1", categoryId: "category-2" },
            { id: "visible-direction-2", categoryId: "category-1" },
          ]),
        ],
      },
    });

    expect(result.highlights.map((category) => category.id)).toEqual(["category-2", "category-1"]);
  });
});
