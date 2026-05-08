import Link from "next/link";
import type { PublicCategoryTreeItem, PublicProductListItem } from "@/lib/api/catalog";
import { getCategoryTree, getProducts } from "@/lib/api/catalog";
import { selectFeaturedProducts } from "@/lib/homepage/featured-products";
import { formatSku } from "@/lib/format";
import { PRODUCT_IMAGE_FALLBACK, PRODUCT_IMAGE_FALLBACK_ALT } from "@/lib/product-images";
import { routes } from "@/lib/routes";
import { HomeHeroProducts } from "@/components/home/home-hero-products";
import { AddToRequestButton } from "@/components/request/add-to-request-button";

function flattenCategories(items: PublicCategoryTreeItem[]) {
  const result: PublicCategoryTreeItem[] = [];
  const visit = (item: PublicCategoryTreeItem) => {
    result.push(item);
    item.children.forEach(visit);
  };

  items.forEach(visit);
  return result;
}

function categoryHighlights(categories: PublicCategoryTreeItem[]) {
  const flattened = flattenCategories(categories).filter((category) => category.isVisibleInMenu);
  const preferred = ["кабель", "опт", "скс", "шкаф", "кросс", "инструмент", "расход"];
  const selected: PublicCategoryTreeItem[] = [];
  const seen = new Set<string>();

  for (const keyword of preferred) {
    const match = flattened.find((category) => {
      const text = `${category.name} ${category.description ?? ""}`.toLowerCase();
      return !seen.has(category.id) && text.includes(keyword);
    });

    if (match) {
      seen.add(match.id);
      selected.push(match);
    }
  }

  for (const category of flattened) {
    if (selected.length >= 4) {
      break;
    }

    if (!seen.has(category.id)) {
      seen.add(category.id);
      selected.push(category);
    }
  }

  return selected.slice(0, 4);
}

function requestProduct(product: PublicProductListItem) {
  return {
    productId: product.id,
    slug: product.slug,
    productName: product.name,
    productSku: product.sku,
    saleUnit: product.saleUnit,
    unitQuantity: product.unitQuantity,
  };
}

export default async function Home() {
  const [categoryResult, productResult] = await Promise.allSettled([
    getCategoryTree(),
    getProducts({ pageSize: 60, sort: "category" }),
  ]);

  const categories = categoryResult.status === "fulfilled" ? categoryResult.value.items : [];
  const products = productResult.status === "fulfilled" ? productResult.value.items : [];
  const featuredProducts = selectFeaturedProducts(products);
  const heroProducts = featuredProducts.slice(0, 3);
  const highlights = categoryHighlights(categories);

  return (
    <div className="home-page">
      <section className="home-hero" aria-labelledby="home-title">
        <div className="home-hero__content">
          <h1 id="home-title">Подберем кабель и сетевые компоненты под вашу задачу</h1>
          <p>
            Соберите заявку из каталога или опишите монтажную задачу: СКС, ВОЛС, шкаф, патчинг,
            расходники. LineCom поможет уточнить комплектность и собрать практичный набор позиций.
          </p>
          <div className="home-hero__actions">
            <Link className="button button--primary" href={routes.catalog()}>
              Перейти в каталог
            </Link>
            <Link className="button button--secondary" href={routes.request()}>
              Собрать заявку
            </Link>
          </div>
        </div>

        <div className="home-hero__visual" aria-label="Ходовые позиции LineCom">
          <div className="home-hero__visual-head">
            <span>Ходовые позиции</span>
            <strong>СКС · ВОЛС · монтаж</strong>
          </div>
          <HomeHeroProducts products={heroProducts} />
        </div>
      </section>

      <section className="home-task" aria-labelledby="home-task-title">
        <div>
          <p className="eyebrow">Подбор под задачу</p>
          <h2 id="home-task-title">Не нужно заранее знать каждый компонент</h2>
        </div>
        <div className="home-task__grid">
          <article>
            <span>01</span>
            <h3>Опишите объект или узел</h3>
            <p>Линия СКС, оптическое подключение, шкаф, коммутация, расходники для монтажной бригады.</p>
          </article>
          <article>
            <span>02</span>
            <h3>Добавьте известные позиции</h3>
            <p>Выберите товары из каталога, укажите количество и комментарии прямо в заявке.</p>
          </article>
          <article>
            <span>03</span>
            <h3>Уточним комплектность</h3>
            <p>Поможем проверить состав и недостающие компоненты перед согласованием поставки.</p>
          </article>
        </div>
      </section>

      <section className="home-section" aria-labelledby="featured-title">
        <div className="section-heading">
          <div>
            <p className="eyebrow">Популярные позиции</p>
            <h2 id="featured-title">Часто запрашивают для сетевых и монтажных задач</h2>
          </div>
          <Link className="text-link" href={routes.catalog()}>
            Все товары
          </Link>
        </div>

        {featuredProducts.length > 0 ? (
          <div className="featured-products">
            {featuredProducts.map((product) => (
              <article key={product.id} className="featured-product">
                <Link className="featured-product__image" href={routes.product(product.slug)} aria-label={product.name}>
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img src={product.mainImage?.url ?? PRODUCT_IMAGE_FALLBACK} alt={product.mainImage?.alt ?? PRODUCT_IMAGE_FALLBACK_ALT} />
                </Link>
                <div className="featured-product__body">
                  <div className="product-card__meta">
                    <span>{product.category.name}</span>
                    {product.brand ? <span>{product.brand.name}</span> : null}
                  </div>
                  <h3>
                    <Link href={routes.product(product.slug)}>{product.name}</Link>
                  </h3>
                  <p>{formatSku(product.sku)}</p>
                </div>
                <div className="featured-product__actions">
                  <Link className="text-link" href={routes.product(product.slug)}>
                    Подробнее
                  </Link>
                  <AddToRequestButton
                    className="button button--primary"
                    product={requestProduct(product)}
                  />
                </div>
              </article>
            ))}
          </div>
        ) : (
          <p className="empty-state">
            Популярные позиции временно недоступны. Откройте каталог или опишите задачу в заявке.
          </p>
        )}
      </section>

      <section className="home-why" aria-labelledby="why-title">
        <div>
          <p className="eyebrow">Почему LineCom</p>
          <h2 id="why-title">Каталог, который работает как начало подбора</h2>
        </div>
        <div className="home-why__list">
          <article>
            <h3>Комплектуем по задаче</h3>
            <p>Смотрим на назначение: линия связи, кроссировка, шкаф, патчинг, расходники.</p>
          </article>
          <article>
            <h3>Подходит для организаций</h3>
            <p>Заявка хранит позиции, количество и комментарии, чтобы быстро вернуться к закупке.</p>
          </article>
          <article>
            <h3>Без лишних шагов</h3>
            <p>Публичная страница ведет к рабочему B2B-согласованию и не отвлекает от состава заявки.</p>
          </article>
        </div>
      </section>

      {highlights.length > 0 ? (
        <section className="home-section" aria-labelledby="directions-title">
          <div className="section-heading">
            <div>
              <p className="eyebrow">Направления</p>
              <h2 id="directions-title">С чего начать подбор</h2>
            </div>
          </div>
          <div className="home-directions">
            {highlights.map((category) => (
              <Link key={category.id} className="home-direction" href={routes.category(category.slug)}>
                <strong>{category.h1 ?? category.name}</strong>
                <span>{category.description ?? "Откройте раздел и добавьте подходящие позиции в заявку."}</span>
              </Link>
            ))}
          </div>
        </section>
      ) : null}
    </div>
  );
}
