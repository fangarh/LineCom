import type { MetadataRoute } from "next";
import { getCategoryTree, getProducts, type PublicProductListItem } from "@/lib/api/catalog";
import { buildPublicSitemapEntries } from "@/lib/seo/sitemap";
import { getPublicSiteOrigin } from "@/lib/seo/site";

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const [categoryResult, productResult] = await Promise.allSettled([getCategoryTree(), loadSitemapProducts()]);

  return buildPublicSitemapEntries({
    origin: getPublicSiteOrigin(),
    categories: categoryResult.status === "fulfilled" ? categoryResult.value.items : [],
    products: productResult.status === "fulfilled" ? productResult.value : [],
  });
}

async function loadSitemapProducts() {
  const firstPage = await getProducts({ page: 1, pageSize: 100, sort: "category" });
  const products: PublicProductListItem[] = [...firstPage.items];

  for (let page = 2; page <= firstPage.totalPages; page += 1) {
    const response = await getProducts({ page, pageSize: firstPage.pageSize, sort: "category" });
    products.push(...response.items);
  }

  return products;
}
