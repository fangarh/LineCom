import type { Metadata } from "next";
import { CatalogFilters } from "@/components/catalog/catalog-filters";
import { CategoryNav } from "@/components/catalog/category-nav";
import { ProductCard } from "@/components/catalog/product-card";
import { getCatalogFilters, getCategoryTree, getProducts } from "@/lib/api/catalog";
import { parseCatalogFilters, toProductListParams, type CatalogSearchParams } from "@/lib/catalog/filtering";
import { routes } from "@/lib/routes";
import { indexablePageMetadata } from "@/lib/seo/metadata";

export const metadata: Metadata = indexablePageMetadata({
  title: "Каталог кабеля и компонентов LineCom",
  description: "Каталог кабеля, СКС, ВОЛС и компонентов LineCom для заявок по запросу.",
  canonicalPath: "/catalog",
});

type CatalogPageProps = {
  searchParams?: Promise<CatalogSearchParams>;
};

export default async function CatalogPage({ searchParams }: CatalogPageProps) {
  const rawSearchParams = await searchParams;
  const [categoryResult, catalogFiltersResult] = await Promise.allSettled([
    getCategoryTree(),
    getCatalogFilters(),
  ]);
  const catalogFilters = catalogFiltersResult.status === "fulfilled" ? catalogFiltersResult.value.filters : [];
  const filterState = parseCatalogFilters(rawSearchParams, catalogFilters);
  const [productResult] = await Promise.allSettled([getProducts(toProductListParams(filterState))]);

  const categories = categoryResult.status === "fulfilled" ? categoryResult.value.items : [];
  const products = productResult.status === "fulfilled" ? productResult.value.items : [];

  return (
    <div className="catalog-page">
      <section className="catalog-intro" aria-labelledby="catalog-title">
        <div>
          <h1 id="catalog-title">Кабель и компоненты LineCom</h1>
          <p className="lead-text">
            Категории, характеристики и единицы продажи для подготовки заявки. Стоимость
            рассчитывается после проверки наличия и подбора.
          </p>
        </div>
      </section>

      <div className="catalog-layout">
        <aside className="catalog-sidebar" aria-labelledby="catalog-categories-title">
          <h2 id="catalog-categories-title">Категории</h2>
          <CategoryNav items={categories} />
        </aside>

        <section className="catalog-content" aria-labelledby="catalog-products-title">
          <div className="section-heading">
            <div>
              <p className="eyebrow">Все позиции</p>
              <h2 id="catalog-products-title">Товары</h2>
            </div>
            {productResult.status === "fulfilled" ? (
              <span className="muted-text">{productResult.value.totalItems} позиций</span>
            ) : null}
          </div>

          <CatalogFilters
            attributeFilters={catalogFilters}
            basePath={routes.catalog()}
            state={filterState}
            scopeLabel="Все категории"
            totalItems={productResult.status === "fulfilled" ? productResult.value.totalItems : undefined}
          />

          {products.length > 0 ? (
            <div className="product-grid">
              {products.map((product) => (
                <ProductCard key={product.id} product={product} />
              ))}
            </div>
          ) : productResult.status === "fulfilled" ? (
            <p className="empty-state">В каталоге пока нет опубликованных товаров.</p>
          ) : (
            <p className="empty-state">
              Не удалось загрузить опубликованные товары. Проверьте доступность backend API.
            </p>
          )}
        </section>
      </div>
    </div>
  );
}
