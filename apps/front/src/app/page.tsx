import Link from "next/link";
import { CategoryNav } from "@/components/catalog/category-nav";
import { ProductCard } from "@/components/catalog/product-card";
import { getCategoryTree, getProducts } from "@/lib/api/catalog";
import { routes } from "@/lib/routes";

export default async function Home() {
  const [categoryResult, productResult] = await Promise.allSettled([
    getCategoryTree(),
    getProducts({ pageSize: 6, sort: "category" }),
  ]);

  const categories = categoryResult.status === "fulfilled" ? categoryResult.value.items : [];
  const products = productResult.status === "fulfilled" ? productResult.value.items : [];

  return (
    <div className="catalog-page">
      <section className="catalog-intro" aria-labelledby="home-title">
        <div>
          <p className="eyebrow">LineCom</p>
          <h1 id="home-title">Каталог кабеля и компонентов</h1>
          <p className="lead-text">
            Кабель, СКС, ВОЛС и сопутствующие позиции для заявок по запросу. Выберите категорию
            или добавьте товар в черновик заявки прямо из каталога.
          </p>
        </div>
        <div className="catalog-intro__actions">
          <Link className="button button--primary" href={routes.catalog()}>
            Перейти в каталог
          </Link>
          <Link className="button button--secondary" href={routes.request()}>
            Открыть заявку
          </Link>
        </div>
      </section>

      <div className="catalog-layout">
        <aside className="catalog-sidebar" aria-labelledby="home-categories-title">
          <h2 id="home-categories-title">Категории</h2>
          <CategoryNav items={categories} />
        </aside>

        <section className="catalog-content" aria-labelledby="home-products-title">
          <div className="section-heading">
            <div>
              <p className="eyebrow">Позиции</p>
              <h2 id="home-products-title">Товары для заявки</h2>
            </div>
            <Link className="text-link" href={routes.catalog()}>
              Все товары
            </Link>
          </div>

          {products.length > 0 ? (
            <div className="product-grid">
              {products.map((product) => (
                <ProductCard key={product.id} product={product} />
              ))}
            </div>
          ) : (
            <p className="empty-state">
              Каталог временно недоступен или пока не содержит опубликованных товаров.
            </p>
          )}
        </section>
      </div>
    </div>
  );
}
