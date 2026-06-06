import Link from "next/link";
import Image from "next/image";
import type { Metadata } from "next";
import { getCategoryTree, getProduct, getProducts } from "@/lib/api/catalog";
import { getHomepageSections } from "@/lib/api/homepage";
import { applyCuratedHomepageSections } from "@/lib/homepage/curated-homepage";
import { resolveCuratedHomepageProducts } from "@/lib/homepage/curated-product-resolver";
import { formatSku } from "@/lib/format";
import { PRODUCT_IMAGE_FALLBACK, PRODUCT_IMAGE_FALLBACK_ALT } from "@/lib/product-images";
import { routes } from "@/lib/routes";
import { indexablePageMetadata } from "@/lib/seo/metadata";
import { ContactCtaButton } from "@/components/contact/contact-cta-button";
import { HomeHeroProducts } from "@/components/home/home-hero-products";

export const metadata: Metadata = indexablePageMetadata({
  title: "LineCom - кабель и сетевые компоненты для B2B-поставок",
  description: "Подбор кабеля, СКС, ВОЛС и сетевых компонентов для B2B-поставок без публичных цен и онлайн-оплаты.",
  canonicalPath: "/",
});

export default async function Home() {
  const [categoryResult, productResult, homepageResult] = await Promise.allSettled([
    getCategoryTree(),
    getProducts({ pageSize: 60, sort: "category" }),
    getHomepageSections(),
  ]);

  const categories = categoryResult.status === "fulfilled" ? categoryResult.value.items : [];
  const products = productResult.status === "fulfilled" ? productResult.value.items : [];
  const homepageSections = homepageResult.status === "fulfilled" ? homepageResult.value : null;
  const resolvedProducts = await resolveCuratedHomepageProducts({
    products,
    sections: homepageSections,
    getProduct,
  });
  const { heroProducts, featuredProducts, highlights } = applyCuratedHomepageSections({
    products: resolvedProducts,
    categories,
    sections: homepageSections,
  });

  return (
    <div className="home-page">
      <section className="home-hero" aria-labelledby="home-title">
        <div className="home-hero__content">
          <h1 id="home-title">Подберем кабель и сетевые компоненты под вашу задачу</h1>
          <p>
            Выберите позиции из каталога или опишите монтажную задачу: СКС, ВОЛС, шкаф, патчинг,
            расходники. LineCom поможет уточнить комплектность и собрать практичный набор позиций.
          </p>
          <div className="home-hero__actions">
            <Link className="button button--primary" href={routes.catalog()}>
              Перейти в каталог
            </Link>
            <Link className="button button--secondary" href={routes.contacts()}>
              Связаться с нами
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
            <p>Откройте товары в каталоге и передайте менеджеру нужные позиции удобным способом.</p>
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
                  <Image
                    src={product.mainImage?.url ?? PRODUCT_IMAGE_FALLBACK}
                    alt={product.mainImage?.alt ?? PRODUCT_IMAGE_FALLBACK_ALT}
                    width={360}
                    height={220}
                    sizes="(max-width: 720px) 100vw, (max-width: 1100px) 33vw, 360px"
                    fetchPriority="low"
                  />
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
                  <ContactCtaButton className="button button--primary" />
                </div>
              </article>
            ))}
          </div>
        ) : (
          <p className="empty-state">
            Популярные позиции временно недоступны. Откройте каталог или свяжитесь с менеджером.
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
            <p>Каталог помогает зафиксировать нужные позиции перед обсуждением поставки с менеджером.</p>
          </article>
          <article>
            <h3>Без лишних шагов</h3>
            <p>Публичная часть ведет к рабочему B2B-согласованию без корзины, оплаты и лишних сценариев.</p>
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
                <span>{category.description ?? "Откройте раздел и выберите подходящие позиции для обсуждения."}</span>
              </Link>
            ))}
          </div>
        </section>
      ) : null}
    </div>
  );
}
