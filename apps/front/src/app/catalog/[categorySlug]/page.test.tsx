import { describe, expect, it, vi } from "vitest";
import { render } from "@testing-library/react";
import type { PublicCategoryDetail, PublicProductListItem } from "@/lib/api/catalog";
import { getCategory, getCategoryFilters, getCategoryTree, getProducts } from "@/lib/api/catalog";
import CategoryPage, { generateMetadata } from "./page";

vi.mock("@/components/catalog/catalog-filters", () => ({
  CatalogFilters: () => <div data-testid="catalog-filters" />,
}));

vi.mock("@/components/catalog/category-nav", () => ({
  CategoryNav: () => <nav aria-label="Категории каталога" />,
}));

vi.mock("@/components/catalog/product-card", () => ({
  ProductCard: ({ product }: { product: PublicProductListItem }) => <article>{product.name}</article>,
}));

vi.mock("@/lib/api/catalog", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/catalog")>();
  return {
    ...actual,
    getCategory: vi.fn(),
    getCategoryFilters: vi.fn(),
    getCategoryTree: vi.fn(),
    getProducts: vi.fn(),
  };
});

const getCategoryMock = vi.mocked(getCategory);
const getCategoryFiltersMock = vi.mocked(getCategoryFilters);
const getCategoryTreeMock = vi.mocked(getCategoryTree);
const getProductsMock = vi.mocked(getProducts);

function category(overrides: Partial<PublicCategoryDetail> = {}): PublicCategoryDetail {
  return {
    id: "6f830f45-0502-4cbf-8cda-f0ac8c74e7f1",
    parentId: null,
    name: "Витая пара",
    slug: "vitaya-para",
    description: "Кабель витая пара для СКС и сетевой инфраструктуры.",
    h1: "Витая пара",
    seo: {
      title: "Витая пара купить",
      description: "Каталог витой пары для сетей связи.",
      canonicalPath: "/catalog/vitaya-para",
    },
    breadcrumbs: [{ name: "Витая пара", slug: "vitaya-para" }],
    ...overrides,
  };
}

describe("category route metadata", () => {
  it("uses API SEO fields for indexable category metadata", async () => {
    getCategoryMock.mockResolvedValue(category());

    const metadata = await generateMetadata({
      params: Promise.resolve({ categorySlug: "vitaya-para" }),
    });

    expect(getCategoryMock).toHaveBeenCalledWith("vitaya-para");
    expect(metadata).toMatchObject({
      title: "Витая пара купить",
      description: "Каталог витой пары для сетей связи.",
      alternates: {
        canonical: "/catalog/vitaya-para",
      },
      robots: {
        index: true,
        follow: true,
      },
    });
  });
});

describe("category route page", () => {
  it("renders BreadcrumbList JSON-LD with absolute public category URLs", async () => {
    process.env.LINECOM_PUBLIC_SITE_ORIGIN = "https://linecom.example.ru/";
    getCategoryMock.mockResolvedValue(
      category({
        breadcrumbs: [
          { name: "Кабель", slug: "cable" },
          { name: "Витая пара", slug: "vitaya-para" },
        ],
      }),
    );
    getCategoryTreeMock.mockResolvedValue({ items: [] });
    getCategoryFiltersMock.mockResolvedValue({ category: { name: "Витая пара", slug: "vitaya-para" }, filters: [] });
    getProductsMock.mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 24,
      totalItems: 0,
      totalPages: 0,
    });

    const { container } = render(
      await CategoryPage({
        params: Promise.resolve({ categorySlug: "vitaya-para" }),
      }),
    );
    const scripts = [...container.querySelectorAll('script[type="application/ld+json"]')].map((script) =>
      JSON.parse(script.textContent ?? "{}"),
    );

    expect(scripts).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          "@context": "https://schema.org",
          "@type": "BreadcrumbList",
          itemListElement: [
            expect.objectContaining({
              position: 1,
              name: "Кабель",
              item: "https://linecom.example.ru/catalog/cable",
            }),
            expect.objectContaining({
              position: 2,
              name: "Витая пара",
              item: "https://linecom.example.ru/catalog/vitaya-para",
            }),
          ],
        }),
      ]),
    );
  });
});
