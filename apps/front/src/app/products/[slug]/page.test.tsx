import { describe, expect, it, vi } from "vitest";
import type { PublicProductDetail } from "@/lib/api/catalog";
import { getProduct } from "@/lib/api/catalog";
import { generateMetadata } from "./page";

vi.mock("@/lib/api/catalog", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/catalog")>();
  return {
    ...actual,
    getProduct: vi.fn(),
  };
});

const getProductMock = vi.mocked(getProduct);

function product(overrides: Partial<PublicProductDetail> = {}): PublicProductDetail {
  return {
    id: "e9c9e401-2f72-49a6-95bd-4e649cedeb3a",
    name: "Кабель U/UTP Cat 5e 4 пары CU 305 м",
    slug: "u-utp-cat-5e-cu-305m",
    sku: "LC-UTP5E-CU-305",
    description: "Описание товара.",
    shortDescription: "Кабель для структурированных кабельных систем.",
    h1: "Кабель U/UTP Cat 5e 4 пары CU 305 м",
    category: { name: "Витая пара", slug: "vitaya-para" },
    brand: { name: "LineCom", slug: "linecom" },
    availability: { code: "in_stock", label: "В наличии" },
    saleUnit: { code: "coil", label: "бухта" },
    unitQuantity: "305 м",
    images: [],
    attributes: [],
    seo: {
      title: "Кабель U/UTP Cat 5e 4 пары CU 305 м",
      description: "Купить кабель U/UTP Cat 5e для СКС.",
      canonicalPath: "/products/u-utp-cat-5e-cu-305m",
    },
    breadcrumbs: [
      { name: "Витая пара", slug: "vitaya-para" },
      { name: "Кабель U/UTP Cat 5e 4 пары CU 305 м", slug: "u-utp-cat-5e-cu-305m" },
    ],
    ...overrides,
  };
}

describe("product route metadata", () => {
  it("uses API SEO fields for indexable product metadata", async () => {
    getProductMock.mockResolvedValue(product());

    const metadata = await generateMetadata({
      params: Promise.resolve({ slug: "u-utp-cat-5e-cu-305m" }),
    });

    expect(getProductMock).toHaveBeenCalledWith("u-utp-cat-5e-cu-305m");
    expect(metadata).toMatchObject({
      title: "Кабель U/UTP Cat 5e 4 пары CU 305 м",
      description: "Купить кабель U/UTP Cat 5e для СКС.",
      alternates: {
        canonical: "/products/u-utp-cat-5e-cu-305m",
      },
      robots: {
        index: true,
        follow: true,
      },
    });
  });
});
