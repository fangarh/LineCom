import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ProductCard } from "@/components/catalog/product-card";
import { ApiClientError } from "@/lib/api/errors";
import { getCategory, getProducts } from "@/lib/api/catalog";

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

  const { category, products } = data;

  return (
    <div className="catalog-page">
      <section className="catalog-intro" aria-labelledby="category-title">
        <div>
          <p className="eyebrow">Категория</p>
          <h1 id="category-title">{category.h1 ?? category.name}</h1>
          {category.description ? <p className="lead-text">{category.description}</p> : null}
        </div>
      </section>

      <section className="catalog-content catalog-content--wide" aria-labelledby="category-products-title">
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
  );
}

async function loadCategoryPageData(categorySlug: string) {
  try {
    const [category, products] = await Promise.all([
      getCategory(categorySlug),
      getProducts({ categorySlug, pageSize: 24, sort: "category" }),
    ]);

    return { status: "ready" as const, category, products };
  } catch (error) {
    if (error instanceof ApiClientError && error.status === 404) {
      notFound();
    }

    return { status: "unavailable" as const };
  }
}
