import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import type { PublicProductDetail } from "@/lib/api/catalog";
import { getCategoryTree, getProduct } from "@/lib/api/catalog";
import ProductPage, { generateMetadata } from "./page";

vi.mock("@/components/catalog/product-detail", () => ({
  ProductDetail: ({ product }: { product: PublicProductDetail }) => <article aria-label="Карточка товара">{product.name}</article>,
}));

vi.mock("@/components/catalog/category-nav", () => ({
  CategoryNav: ({ activeSlug, items }: { activeSlug?: string; items: Array<{ name: string; slug: string }> }) => (
    <nav aria-label="Категории каталога">
      <span>active:{activeSlug}</span>
      {items.map((item) => (
        <a href={`/catalog/${item.slug}`} key={item.slug}>
          {item.name}
        </a>
      ))}
    </nav>
  ),
}));

vi.mock("@/lib/api/catalog", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/catalog")>();
  return {
    ...actual,
    getCategoryTree: vi.fn(),
    getProduct: vi.fn(),
  };
});

const getCategoryTreeMock = vi.mocked(getCategoryTree);
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

  it("builds product canonical from the active route slug even if API returns a stale path", async () => {
    getProductMock.mockResolvedValue(
      product({
        seo: {
          title: "Кабель U/UTP Cat 5e 4 пары CU 305 м",
          description: "Купить кабель U/UTP Cat 5e для СКС.",
          canonicalPath: "/catalog/products/u-utp-cat-5e-cu-305m",
        },
      }),
    );

    const metadata = await generateMetadata({
      params: Promise.resolve({ slug: "u-utp-cat-5e-cu-305m" }),
    });

    expect(metadata.alternates).toMatchObject({
      canonical: "/products/u-utp-cat-5e-cu-305m",
    });
  });
});

describe("product route page", () => {
  it("renders category navigation beside the product card", async () => {
    getProductMock.mockResolvedValue(product());
    getCategoryTreeMock.mockResolvedValue({
      items: [
        {
          id: "cat-twisted-pair",
          parentId: null,
          name: "Витая пара",
          slug: "vitaya-para",
          h1: null,
          description: null,
          sortOrder: 10,
          isVisibleInMenu: true,
          children: [],
        },
      ],
    });

    render(
      await ProductPage({
        params: Promise.resolve({ slug: "u-utp-cat-5e-cu-305m" }),
      }),
    );

    expect(getCategoryTreeMock).toHaveBeenCalled();
    expect(screen.getByRole("heading", { name: "Категории" })).toBeInTheDocument();
    expect(screen.getByRole("navigation", { name: "Категории каталога" })).toHaveTextContent("active:vitaya-para");
    expect(screen.getByRole("article", { name: "Карточка товара" })).toBeInTheDocument();
  });

  it("renders Product and BreadcrumbList JSON-LD with absolute public URLs", async () => {
    process.env.LINECOM_PUBLIC_SITE_ORIGIN = "https://linecom.example.ru/";
    getProductMock.mockResolvedValue(
      product({
        images: [
          {
            url: "/storage/products/u-utp-cat-5e.png",
            alt: "Кабель U/UTP Cat 5e",
            title: null,
          },
        ],
      }),
    );
    getCategoryTreeMock.mockResolvedValue({ items: [] });

    const { container } = render(
      await ProductPage({
        params: Promise.resolve({ slug: "u-utp-cat-5e-cu-305m" }),
      }),
    );
    const scripts = [...container.querySelectorAll('script[type="application/ld+json"]')].map((script) =>
      JSON.parse(script.textContent ?? "{}"),
    );

    expect(scripts).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          "@context": "https://schema.org",
          "@type": "Product",
          name: "Кабель U/UTP Cat 5e 4 пары CU 305 м",
          sku: "LC-UTP5E-CU-305",
          url: "https://linecom.example.ru/products/u-utp-cat-5e-cu-305m",
          image: ["https://linecom.example.ru/storage/products/u-utp-cat-5e.png"],
        }),
        expect.objectContaining({
          "@context": "https://schema.org",
          "@type": "BreadcrumbList",
          itemListElement: expect.arrayContaining([
            expect.objectContaining({
              position: 1,
              name: "Витая пара",
              item: "https://linecom.example.ru/catalog/vitaya-para",
            }),
            expect.objectContaining({
              position: 2,
              name: "Кабель U/UTP Cat 5e 4 пары CU 305 м",
              item: "https://linecom.example.ru/products/u-utp-cat-5e-cu-305m",
            }),
          ]),
        }),
      ]),
    );
  });
});
