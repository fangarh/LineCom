import { CategoryNav } from "@/components/catalog/category-nav";
import { ProductCard } from "@/components/catalog/product-card";
import { getCategoryTree, getProducts } from "@/lib/api/catalog";

export const metadata = {
  title: "Каталог кабеля и компонентов LineCom",
  description: "Каталог кабеля, СКС, ВОЛС и компонентов LineCom для заявок по запросу.",
};

export default async function CatalogPage() {
  const [categoryResult, productResult] = await Promise.allSettled([
    getCategoryTree(),
    getProducts({ pageSize: 24, sort: "category" }),
  ]);

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

          {products.length > 0 ? (
            <div className="product-grid">
              {products.map((product) => (
                <ProductCard key={product.id} product={product} />
              ))}
            </div>
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
