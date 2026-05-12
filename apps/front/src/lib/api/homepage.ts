import { apiJson } from "./http";

type Id = string;

export type PublicHomepageSectionType = "product_list" | "category_list";

export type PublicHomepageSectionsResponse = {
  sections: PublicHomepageSection[];
};

export type PublicHomepageSection = {
  code: string;
  title: string;
  type: PublicHomepageSectionType;
  items: PublicHomepageSectionItem[];
};

export type PublicHomepageSectionItem = {
  id: Id;
  productId: Id | null;
  categoryId: Id | null;
  name: string;
  slug: string | null;
  secondaryText: string | null;
};

export function getHomepageSections() {
  return apiJson<PublicHomepageSectionsResponse>("/api/public/homepage/sections", {
    next: { revalidate: 60 },
  });
}
