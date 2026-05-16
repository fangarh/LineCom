import type { MetadataRoute } from "next";
import type { PublicCategoryTreeItem, PublicProductListItem } from "@/lib/api/catalog";
import { routes } from "@/lib/routes";

type BuildPublicSitemapEntriesInput = {
  origin: string;
  categories: PublicCategoryTreeItem[];
  products: PublicProductListItem[];
};

type SitemapChangeFrequency = MetadataRoute.Sitemap[number]["changeFrequency"];

const staticEntries = [
  { path: routes.home(), changeFrequency: "weekly" as const, priority: 1 },
  { path: routes.catalog(), changeFrequency: "daily" as const, priority: 0.9 },
  { path: routes.contacts(), changeFrequency: "monthly" as const, priority: 0.4 },
  { path: routes.delivery(), changeFrequency: "monthly" as const, priority: 0.4 },
];

export function buildPublicSitemapEntries({
  origin,
  categories,
  products,
}: BuildPublicSitemapEntriesInput): MetadataRoute.Sitemap {
  const normalizedOrigin = origin.replace(/\/+$/, "");
  const seen = new Set<string>();
  const entries: MetadataRoute.Sitemap = [];

  const push = (path: string, changeFrequency: SitemapChangeFrequency, priority: number) => {
    const normalizedPath = path.startsWith("/") ? path : `/${path}`;
    const url = `${normalizedOrigin}${normalizedPath}`;
    if (seen.has(url)) {
      return;
    }

    seen.add(url);
    entries.push({ url, changeFrequency, priority });
  };

  for (const entry of staticEntries) {
    push(entry.path, entry.changeFrequency, entry.priority);
  }

  for (const category of flattenVisibleCategories(categories)) {
    push(routes.category(category.slug), "weekly", 0.7);
  }

  for (const product of products) {
    push(routes.product(product.slug), "weekly", 0.6);
  }

  return entries;
}

function flattenVisibleCategories(categories: PublicCategoryTreeItem[]) {
  const result: PublicCategoryTreeItem[] = [];

  const visit = (category: PublicCategoryTreeItem) => {
    if (category.isVisibleInMenu) {
      result.push(category);
    }

    category.children.forEach(visit);
  };

  categories.forEach(visit);
  return result;
}
