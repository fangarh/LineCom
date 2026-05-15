import type { PublicProductDetail, PublicProductListItem } from "@/lib/api/catalog";
import type { PublicHomepageSectionsResponse } from "@/lib/api/homepage";

const PRODUCT_SECTION_CODES = new Set(["hero_products", "featured_products"]);

type ResolveCuratedHomepageProductsInput = {
  products: PublicProductListItem[];
  sections: PublicHomepageSectionsResponse | null | undefined;
  getProduct: (slug: string) => Promise<PublicProductDetail>;
};

export async function resolveCuratedHomepageProducts({
  products,
  sections,
  getProduct,
}: ResolveCuratedHomepageProductsInput) {
  const productById = new Map(products.map((product) => [product.id, product]));
  const missingSlugs = curatedProductSlugs(sections, productById);
  if (missingSlugs.length === 0) {
    return products;
  }

  const loadedProducts = await Promise.all(
    missingSlugs.map(async (slug) => {
      try {
        return productDetailToListItem(await getProduct(slug));
      } catch {
        return null;
      }
    }),
  );

  const resolved = [...products];
  for (const product of loadedProducts) {
    if (product && !productById.has(product.id)) {
      productById.set(product.id, product);
      resolved.push(product);
    }
  }

  return resolved;
}

function curatedProductSlugs(
  sections: PublicHomepageSectionsResponse | null | undefined,
  productById: Map<string, PublicProductListItem>,
) {
  const slugs: string[] = [];
  const seen = new Set<string>();

  for (const section of sections?.sections ?? []) {
    if (section.type !== "product_list" || !PRODUCT_SECTION_CODES.has(section.code)) {
      continue;
    }

    for (const item of section.items) {
      if (!item.productId || !item.slug || productById.has(item.productId) || seen.has(item.slug)) {
        continue;
      }

      seen.add(item.slug);
      slugs.push(item.slug);
    }
  }

  return slugs;
}

function productDetailToListItem(product: PublicProductDetail): PublicProductListItem {
  return {
    id: product.id,
    name: product.name,
    slug: product.slug,
    sku: product.sku,
    brand: product.brand,
    category: product.category,
    availability: product.availability,
    saleUnit: product.saleUnit,
    unitQuantity: product.unitQuantity,
    mainImage: product.images[0] ?? null,
  };
}
