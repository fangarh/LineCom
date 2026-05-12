import type { PublicCategoryTreeItem, PublicProductListItem } from "@/lib/api/catalog";
import type { PublicHomepageSectionsResponse } from "@/lib/api/homepage";
import { selectFeaturedProducts } from "./featured-products";

const HERO_PRODUCTS_SECTION = "hero_products";
const FEATURED_PRODUCTS_SECTION = "featured_products";
const DIRECTION_CATEGORIES_SECTION = "direction_categories";

type ApplyCuratedHomepageSectionsInput = {
  products: PublicProductListItem[];
  categories: PublicCategoryTreeItem[];
  sections?: PublicHomepageSectionsResponse | null;
};

type CuratedHomepageSections = {
  heroProducts: PublicProductListItem[];
  featuredProducts: PublicProductListItem[];
  highlights: PublicCategoryTreeItem[];
};

function flattenCategories(items: PublicCategoryTreeItem[]) {
  const result: PublicCategoryTreeItem[] = [];
  const visit = (item: PublicCategoryTreeItem) => {
    result.push(item);
    item.children.forEach(visit);
  };

  items.forEach(visit);
  return result;
}

export function categoryHighlights(categories: PublicCategoryTreeItem[]) {
  const flattened = flattenCategories(categories).filter((category) => category.isVisibleInMenu);
  const preferred = ["кабель", "опт", "скс", "шкаф", "кросс", "инструмент", "расход"];
  const selected: PublicCategoryTreeItem[] = [];
  const seen = new Set<string>();

  for (const keyword of preferred) {
    const match = flattened.find((category) => {
      const text = `${category.name} ${category.description ?? ""}`.toLowerCase();
      return !seen.has(category.id) && text.includes(keyword);
    });

    if (match) {
      seen.add(match.id);
      selected.push(match);
    }
  }

  for (const category of flattened) {
    if (selected.length >= 4) {
      break;
    }

    if (!seen.has(category.id)) {
      seen.add(category.id);
      selected.push(category);
    }
  }

  return selected.slice(0, 4);
}

export function applyCuratedHomepageSections({
  products,
  categories,
  sections,
}: ApplyCuratedHomepageSectionsInput): CuratedHomepageSections {
  const automaticFeatured = selectFeaturedProducts(products);
  const automaticHighlights = categoryHighlights(categories);
  const productById = new Map(products.map((product) => [product.id, product]));
  const categoryById = new Map(
    flattenCategories(categories)
      .filter((category) => category.isVisibleInMenu)
      .map((category) => [category.id, category]),
  );

  const curatedHeroProducts = productsForSection(sections, HERO_PRODUCTS_SECTION, productById);
  const curatedFeaturedProducts = productsForSection(sections, FEATURED_PRODUCTS_SECTION, productById);
  const curatedHighlights = categoriesForSection(sections, DIRECTION_CATEGORIES_SECTION, categoryById);

  return {
    heroProducts: curatedHeroProducts.length > 0 ? curatedHeroProducts : automaticFeatured.slice(0, 3),
    featuredProducts: curatedFeaturedProducts.length > 0 ? curatedFeaturedProducts : automaticFeatured,
    highlights: curatedHighlights.length > 0 ? curatedHighlights : automaticHighlights,
  };
}

function productsForSection(
  response: PublicHomepageSectionsResponse | null | undefined,
  code: string,
  productById: Map<string, PublicProductListItem>,
) {
  const section = response?.sections.find((candidate) => candidate.code === code && candidate.type === "product_list");
  if (!section) {
    return [];
  }

  const selected: PublicProductListItem[] = [];
  const seen = new Set<string>();
  for (const item of section.items) {
    if (!item.productId || seen.has(item.productId)) {
      continue;
    }

    const product = productById.get(item.productId);
    if (product) {
      seen.add(item.productId);
      selected.push(product);
    }
  }

  return selected;
}

function categoriesForSection(
  response: PublicHomepageSectionsResponse | null | undefined,
  code: string,
  categoryById: Map<string, PublicCategoryTreeItem>,
) {
  const section = response?.sections.find((candidate) => candidate.code === code && candidate.type === "category_list");
  if (!section) {
    return [];
  }

  const selected: PublicCategoryTreeItem[] = [];
  const seen = new Set<string>();
  for (const item of section.items) {
    if (!item.categoryId || seen.has(item.categoryId)) {
      continue;
    }

    const category = categoryById.get(item.categoryId);
    if (category) {
      seen.add(item.categoryId);
      selected.push(category);
    }
  }

  return selected;
}
