import { describe, expect, it } from "vitest";
import type { PublicProductListItem } from "@/lib/api/catalog";
import { selectFeaturedProducts } from "./featured-products";

function product(overrides: Partial<PublicProductListItem> & Pick<PublicProductListItem, "id" | "name">): PublicProductListItem {
  return {
    id: overrides.id,
    name: overrides.name,
    slug: overrides.slug ?? overrides.id,
    sku: overrides.sku ?? null,
    brand: overrides.brand ?? null,
    category: overrides.category ?? { name: "Каталог", slug: "catalog" },
    availability: overrides.availability ?? { code: "request", label: "По запросу" },
    saleUnit: overrides.saleUnit ?? { code: "piece", label: "шт" },
    unitQuantity: overrides.unitQuantity ?? "1",
    mainImage: overrides.mainImage ?? null,
  };
}

const image = {
  url: "/image.png",
  alt: "Изображение",
  title: null,
};

describe("selectFeaturedProducts", () => {
  it("prioritizes popular demand groups with images", () => {
    const selected = selectFeaturedProducts([
      product({ id: "dac", name: "SFP+ DAC модуль, медный кабель", mainImage: image }),
      product({ id: "utp", name: "Кабель UTP cat.5e Cu", mainImage: image }),
      product({ id: "patch", name: "Патчкорд SC UPC", mainImage: image }),
      product({ id: "misc", name: "Редкая позиция", mainImage: image }),
    ]);

    expect(selected.map((item) => item.id)).toEqual(["utp", "patch", "dac", "misc"]);
  });

  it("prefers image-bearing products before falling back to products without images", () => {
    const selected = selectFeaturedProducts([
      product({ id: "no-image-utp", name: "Кабель UTP cat.5e" }),
      product({ id: "image-rack", name: "Шкаф 19 дюймов", mainImage: image }),
      product({ id: "image-any", name: "Инструмент монтажный", mainImage: image }),
    ]);

    expect(selected.map((item) => item.id)).toEqual(["image-rack", "image-any", "no-image-utp"]);
  });

  it("deduplicates products and respects the requested limit", () => {
    const duplicate = product({ id: "same", name: "Кабель UTP cat.5e", mainImage: image });
    const selected = selectFeaturedProducts(
      [
        duplicate,
        duplicate,
        product({ id: "patch", name: "Патчкорд LC UPC", mainImage: image }),
        product({ id: "sfp", name: "SFP модуль", mainImage: image }),
      ],
      2,
    );

    expect(selected.map((item) => item.id)).toEqual(["same", "patch"]);
  });

  it("returns an empty list for an empty catalog", () => {
    expect(selectFeaturedProducts([])).toEqual([]);
  });
});
