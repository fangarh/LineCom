import type { MetadataRoute } from "next";
import { getCategoryTree, getProducts, type PublicProductListItem } from "@/lib/api/catalog";
import { buildPublicSitemapEntries } from "@/lib/seo/sitemap";
import { getPublicSiteOrigin } from "@/lib/seo/site";

const SITEMAP_PRODUCT_PAGE_SIZE = 60;
const SITEMAP_MAX_PRODUCT_PAGES = 10;
const SITEMAP_MAX_PRODUCT_URLS = 500;

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const [categoryResult, productResult] = await Promise.allSettled([getCategoryTree(), loadSitemapProducts()]);

  return buildPublicSitemapEntries({
    origin: getPublicSiteOrigin(),
    categories: categoryResult.status === "fulfilled" ? categoryResult.value.items : [],
    products: productResult.status === "fulfilled" ? productResult.value : [],
  });
}

async function loadSitemapProducts() {
  const firstPage = await getProducts({ page: 1, pageSize: SITEMAP_PRODUCT_PAGE_SIZE, sort: "category" });
  const products: PublicProductListItem[] = [];
  appendProductsWithinLimit(products, firstPage.items);

  const maxPage = Math.min(firstPage.totalPages, SITEMAP_MAX_PRODUCT_PAGES);
  for (let page = 2; page <= maxPage && products.length < SITEMAP_MAX_PRODUCT_URLS; page += 1) {
    const response = await getProducts({ page, pageSize: SITEMAP_PRODUCT_PAGE_SIZE, sort: "category" });
    appendProductsWithinLimit(products, response.items);
  }

  return products;
}

function appendProductsWithinLimit(products: PublicProductListItem[], nextItems: PublicProductListItem[]) {
  const remaining = SITEMAP_MAX_PRODUCT_URLS - products.length;
  if (remaining <= 0) {
    return;
  }

  products.push(...nextItems.slice(0, remaining));
}
