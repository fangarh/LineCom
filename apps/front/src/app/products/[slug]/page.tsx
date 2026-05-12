import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ProductDetail } from "@/components/catalog/product-detail";
import { ApiClientError } from "@/lib/api/errors";
import { getProduct } from "@/lib/api/catalog";
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
      canonicalPath: product.seo.canonicalPath,
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

  return (
    <div className="catalog-page">
      <ProductDetail product={data.product} />
    </div>
  );
}

async function loadProductPageData(slug: string) {
  try {
    const product = await getProduct(slug);

    return { status: "ready" as const, product };
  } catch (error) {
    if (error instanceof ApiClientError && error.status === 404) {
      notFound();
    }

    return { status: "unavailable" as const };
  }
}
