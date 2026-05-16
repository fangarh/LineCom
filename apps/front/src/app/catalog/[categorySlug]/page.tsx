import type { Metadata } from "next";
import { notFound } from "next/navigation";
import Link from "next/link";
import { CatalogFilters } from "@/components/catalog/catalog-filters";
import { CategoryNav } from "@/components/catalog/category-nav";
import { ProductCard } from "@/components/catalog/product-card";
import { ApiClientError } from "@/lib/api/errors";
import { getCategory, getCategoryFilters, getCategoryTree, getProducts } from "@/lib/api/catalog";
import { parseCatalogFilters, toProductListParams, type CatalogSearchParams } from "@/lib/catalog/filtering";
import { routes } from "@/lib/routes";
import { buildBreadcrumbListJsonLd, JsonLdScript } from "@/lib/seo/json-ld";
import { indexablePageMetadata, noindexPageMetadata } from "@/lib/seo/metadata";

type CategoryPageProps = {
  params: Promise<{ categorySlug: string }>;
  searchParams?: Promise<CatalogSearchParams>;
};

export async function generateMetadata({ params }: CategoryPageProps): Promise<Metadata> {
  const { categorySlug } = await params;

  try {
    const category = await getCategory(categorySlug);

    return indexablePageMetadata({
      title: category.seo.title ?? category.h1 ?? category.name,
      description: category.seo.description ?? category.description,
      canonicalPath: category.seo.canonicalPath,
    });
  } catch {
    return noindexPageMetadata("Категория каталога LineCom");
  }
}

export default async function CategoryPage({ params, searchParams }: CategoryPageProps) {
  const { categorySlug } = await params;
  const data = await loadCategoryPageData(categorySlug, await searchParams);

  if (data.status === "unavailable") {
    return (
      <div className="catalog-page">
        <section className="catalog-intro" aria-labelledby="category-error-title">
          <div>
            <p className="eyebrow">Категория</p>
            <h1 id="category-error-title">Категория временно недоступна</h1>
            <p className="lead-text">Не удалось получить данные из backend API. Попробуйте обновить страницу позже.</p>
          </div>
        </section>
      </div>
    );
  }

  const { category, categoryFilters, categories, filterState, products } = data;
  const breadcrumbItems = category.breadcrumbs.map((item) => ({
    name: item.name,
    path: routes.category(item.slug),
  }));

  return (
    <div className="catalog-page">
      <JsonLdScript data={buildBreadcrumbListJsonLd(breadcrumbItems)} />
      <section className="catalog-intro" aria-labelledby="category-title">
        <div>
          <nav className="breadcrumbs" aria-label="Хлебные крошки">
            <Link href={routes.catalog()}>Каталог</Link>
            {category.breadcrumbs.map((item, index) => {
              const isCurrent = index === category.breadcrumbs.length - 1;

              return isCurrent ? (
                <span key={item.slug} aria-current="page">
                  {item.name}
                </span>
              ) : (
                <Link key={item.slug} href={routes.category(item.slug)}>
                  {item.name}
                </Link>
              );
            })}
          </nav>
          <p className="eyebrow">Категория</p>
          <h1 id="category-title">{category.h1 ?? category.name}</h1>
          {category.description ? <p className="lead-text">{category.description}</p> : null}
        </div>
      </section>

      <div className="catalog-layout">
        <aside className="catalog-sidebar" aria-labelledby="catalog-categories-title">
          <h2 id="catalog-categories-title">Категории</h2>
          <CategoryNav items={categories} activeSlug={category.slug} />
        </aside>

        <section className="catalog-content" aria-labelledby="category-products-title">
          <div className="section-heading">
            <div>
              <p className="eyebrow">Позиции категории</p>
              <h2 id="category-products-title">Товары</h2>
            </div>
            <span className="muted-text">{products.totalItems} позиций</span>
          </div>

          <CatalogFilters
            attributeFilters={categoryFilters}
            basePath={routes.category(category.slug)}
            state={filterState}
            scopeLabel={category.name}
            totalItems={products.totalItems}
          />

          {products.items.length > 0 ? (
            <div className="product-grid">
              {products.items.map((product) => (
                <ProductCard key={product.id} product={product} />
              ))}
            </div>
          ) : (
            <p className="empty-state">В этой категории пока нет опубликованных товаров.</p>
          )}
        </section>
      </div>
    </div>
  );
}

async function loadCategoryPageData(categorySlug: string, searchParams: CatalogSearchParams = {}) {
  try {
    const [category, categories, categoryFilters] = await Promise.all([
      getCategory(categorySlug),
      getCategoryTree(),
      getCategoryFilters(categorySlug),
    ]);
    const filterState = parseCatalogFilters(searchParams, categoryFilters.filters);
    const products = await getProducts(toProductListParams(filterState, categorySlug));

    return {
      status: "ready" as const,
      category,
      categoryFilters: categoryFilters.filters,
      categories: categories.items,
      filterState,
      products,
    };
  } catch (error) {
    if (error instanceof ApiClientError && error.status === 404) {
      notFound();
    }

    return { status: "unavailable" as const };
  }
}
