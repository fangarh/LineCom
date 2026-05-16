import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { PublicProductDetail } from "@/lib/api/catalog";
import { ProductDetail } from "./product-detail";

vi.mock("@/components/request/add-to-request-button", () => ({
  AddToRequestButton: () => <button type="button">Добавить в заявку</button>,
}));

function product(overrides: Partial<PublicProductDetail> = {}): PublicProductDetail {
  return {
    id: "product-1",
    name: "Кабель U/UTP Cat 5e",
    slug: "u-utp-cat-5e",
    sku: "LC-UTP5E",
    description: "Полное описание товара для карточки.",
    shortDescription: "Краткое описание товара.",
    h1: null,
    category: { name: "Витая пара", slug: "vitaya-para" },
    brand: null,
    availability: { code: "in_stock", label: "В наличии" },
    saleUnit: { code: "coil", label: "бухта" },
    unitQuantity: "305 м",
    images: [],
    attributes: [],
    seo: {
      title: null,
      description: "SEO описание товара.",
      canonicalPath: "/products/u-utp-cat-5e",
    },
    breadcrumbs: [{ name: "Витая пара", slug: "vitaya-para" }],
    ...overrides,
  };
}

describe("ProductDetail", () => {
  it("shows only the short description when both product descriptions are present", () => {
    render(<ProductDetail product={product()} />);

    expect(screen.getByText("Краткое описание товара.")).toBeInTheDocument();
    expect(screen.queryByText("Полное описание товара для карточки.")).not.toBeInTheDocument();
  });

  it("falls back to the full description when the short description is empty", () => {
    render(<ProductDetail product={product({ shortDescription: null })} />);

    expect(screen.getByText("Полное описание товара для карточки.")).toBeInTheDocument();
  });
});
