import type { PublicProductListItem } from "@/lib/api/catalog";

const FEATURED_LIMIT = 8;

const demandGroups = [
  ["utp", "ftp", "cat.5", "cat 5", "витая"],
  ["патчкорд", "patch"],
  ["адаптер", "пигтейл", "sc", "lc", "upc", "apc"],
  ["кросс", "шкаф", "панель", "19"],
  ["sfp", "медиаконвертер", "wdm"],
  ["стяжка", "крепеж", "изолента", "расход"],
] as const;

function haystack(product: PublicProductListItem) {
  return [
    product.name,
    product.sku,
    product.brand?.name,
    product.category.name,
    product.availability.label,
    product.saleUnit.label,
  ]
    .filter(Boolean)
    .join(" ")
    .toLowerCase();
}

function pushUnique(target: PublicProductListItem[], product: PublicProductListItem, seen: Set<string>, limit: number) {
  if (target.length >= limit || seen.has(product.id)) {
    return;
  }

  seen.add(product.id);
  target.push(product);
}

export function selectFeaturedProducts(products: PublicProductListItem[], limit = FEATURED_LIMIT) {
  const cappedLimit = Math.max(0, Math.min(limit, FEATURED_LIMIT));
  const selected: PublicProductListItem[] = [];
  const seen = new Set<string>();
  if (cappedLimit === 0) {
    return selected;
  }

  const withImages = products.filter((product) => product.mainImage);
  const primaryPool = withImages.length > 0 ? withImages : products;

  for (const keywords of demandGroups) {
    const match = primaryPool.find((product) => {
      if (seen.has(product.id)) {
        return false;
      }

      const text = haystack(product);
      return keywords.some((keyword) => text.includes(keyword));
    });

    if (match) {
      pushUnique(selected, match, seen, cappedLimit);
    }

    if (selected.length >= cappedLimit) {
      return selected;
    }
  }

  for (const product of withImages) {
    pushUnique(selected, product, seen, cappedLimit);
    if (selected.length >= cappedLimit) {
      return selected;
    }
  }

  for (const product of products) {
    pushUnique(selected, product, seen, cappedLimit);
    if (selected.length >= cappedLimit) {
      break;
    }
  }

  return selected;
}
