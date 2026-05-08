import type { Metadata } from "next";
import { notFound } from "next/navigation";
import Link from "next/link";
import { CategoryNav } from "@/components/catalog/category-nav";
import { ProductCard } from "@/components/catalog/product-card";
import { ApiClientError } from "@/lib/api/errors";
import { getCategory, getCategoryTree, getProducts } from "@/lib/api/catalog";
import { routes } from "@/lib/routes";

type CategoryPageProps = {
  params: Promise<{ categorySlug: string }>;
};

export async function generateMetadata({ params }: CategoryPageProps): Promise<Metadata> {
  const { categorySlug } = await params;

  try {
    const category = await getCategory(categorySlug);

    return {
      title: category.seo.title ?? category.h1 ?? category.name,
      description: category.seo.description ?? category.description ?? undefined,
      alternates: {
        canonical: category.seo.canonicalPath,
      },
    };
  } catch {
    return {
      title: "Категория каталога LineCom",
    };
  }
}

export default async function CategoryPage({ params }: CategoryPageProps) {
  const { categorySlug } = await params;
  const data = await loadCategoryPageData(categorySlug);

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

  const { category, categories, products } = data;

  return (
    <div className="catalog-page">
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

async function loadCategoryPageData(categorySlug: string) {
  try {
    const [category, categories, products] = await Promise.all([
      getCategory(categorySlug),
      getCategoryTree(),
      getProducts({ categorySlug, pageSize: 24, sort: "category" }),
    ]);

    return { status: "ready" as const, category, categories: categories.items, products };
  } catch (error) {
    if (error instanceof ApiClientError && error.status === 404) {
      notFound();
    }

    return { status: "unavailable" as const };
  }
}
