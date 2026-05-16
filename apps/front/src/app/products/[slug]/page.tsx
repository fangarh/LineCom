import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { CategoryNav } from "@/components/catalog/category-nav";
import { ProductDetail } from "@/components/catalog/product-detail";
import { ApiClientError } from "@/lib/api/errors";
import { getCategoryTree, getProduct } from "@/lib/api/catalog";
import { routes } from "@/lib/routes";
import { buildBreadcrumbListJsonLd, buildProductJsonLd, JsonLdScript } from "@/lib/seo/json-ld";
import { indexablePageMetadata, noindexPageMetadata } from "@/lib/seo/metadata";

type ProductPageProps = {
  params: Promise<{ slug: string }>;
};

export async function generateMetadata({ params }: ProductPageProps): Promise<Metadata> {
  const { slug } = await params;

  try {
    const product = await getProduct(slug);

    return indexablePageMetadata({
      title: product.seo.title ?? product.h1 ?? product.name,
      description: product.seo.description ?? product.shortDescription ?? product.description,
      canonicalPath: routes.product(slug),
    });
  } catch {
    return noindexPageMetadata("Товар каталога LineCom");
  }
}

export default async function ProductPage({ params }: ProductPageProps) {
  const { slug } = await params;
  const data = await loadProductPageData(slug);

  if (data.status === "unavailable") {
    return (
      <div className="catalog-page">
        <section className="catalog-intro" aria-labelledby="product-error-title">
          <div>
            <p className="eyebrow">Товар</p>
            <h1 id="product-error-title">Товар временно недоступен</h1>
            <p className="lead-text">Не удалось получить карточку товара из backend API.</p>
          </div>
        </section>
      </div>
    );
  }

  const breadcrumbItems = data.product.breadcrumbs.map((item, index) => ({
    name: item.name,
    path:
      index === data.product.breadcrumbs.length - 1
        ? routes.product(data.product.slug)
        : routes.category(item.slug),
  }));

  return (
    <div className="catalog-page">
      <JsonLdScript data={buildProductJsonLd(data.product)} />
      <JsonLdScript data={buildBreadcrumbListJsonLd(breadcrumbItems)} />
      <div className="catalog-layout">
        <aside className="catalog-sidebar" aria-labelledby="catalog-categories-title">
          <h2 id="catalog-categories-title">Категории</h2>
          <CategoryNav items={data.categories} activeSlug={data.product.category.slug} />
        </aside>

        <section className="catalog-content" aria-label="Карточка товара">
          <ProductDetail product={data.product} />
        </section>
      </div>
    </div>
  );
}

async function loadProductPageData(slug: string) {
  try {
    const [product, categoriesResult] = await Promise.all([
      getProduct(slug),
      getCategoryTree().then((response) => response.items, () => []),
    ]);

    return { status: "ready" as const, categories: categoriesResult, product };
  } catch (error) {
    if (error instanceof ApiClientError && error.status === 404) {
      notFound();
    }

    return { status: "unavailable" as const };
  }
}
