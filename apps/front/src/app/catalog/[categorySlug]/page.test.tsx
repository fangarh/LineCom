import { describe, expect, it, vi } from "vitest";
import type { PublicCategoryDetail } from "@/lib/api/catalog";
import { getCategory } from "@/lib/api/catalog";
import { generateMetadata } from "./page";

vi.mock("@/lib/api/catalog", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/catalog")>();
  return {
    ...actual,
    getCategory: vi.fn(),
  };
});

const getCategoryMock = vi.mocked(getCategory);

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
