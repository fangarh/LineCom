import Link from "next/link";
import type { PublicProductAttribute, PublicProductDetail as PublicProductDetailType } from "@/lib/api/catalog";
import { formatSku } from "@/lib/format";
import { PRODUCT_IMAGE_FALLBACK, PRODUCT_IMAGE_FALLBACK_ALT } from "@/lib/product-images";
import { routes } from "@/lib/routes";
import { AddToRequestButton } from "@/components/request/add-to-request-button";

type ProductDetailProps = {
  product: PublicProductDetailType;
};

export function ProductDetail({ product }: ProductDetailProps) {
  const leadImage = product.images[0] ?? null;

  return (
    <article className="product-detail">
      <nav className="breadcrumbs" aria-label="Хлебные крошки">
        <Link href={routes.catalog()}>Каталог</Link>
        {product.breadcrumbs.map((item) => (
          <Link key={item.slug} href={routes.category(item.slug)}>
            {item.name}
          </Link>
        ))}
      </nav>

      <div className="product-detail__grid">
        <div className="product-detail__media">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={leadImage?.url ?? PRODUCT_IMAGE_FALLBACK} alt={leadImage?.alt ?? PRODUCT_IMAGE_FALLBACK_ALT} />
        </div>

        <div className="product-detail__summary">
          <p className="eyebrow">{product.category.name}</p>
          <h1>{product.h1 ?? product.name}</h1>
          <p className="product-detail__sku">{formatSku(product.sku)}</p>
          {product.shortDescription ? <p className="lead-text">{product.shortDescription}</p> : null}
          {product.description ? <p className="muted-text">{product.description}</p> : null}

          <dl className="summary-grid">
            <div>
              <dt>Наличие</dt>
              <dd>{product.availability.label}</dd>
            </div>
            <div>
              <dt>Единица продажи</dt>
              <dd>{product.saleUnit.label}</dd>
            </div>
            <div>
              <dt>Количество в единице</dt>
              <dd>{product.unitQuantity}</dd>
            </div>
            <div>
              <dt>Стоимость</dt>
              <dd>Цена по запросу</dd>
            </div>
          </dl>

          <AddToRequestButton
            product={{
              productId: product.id,
              slug: product.slug,
              productName: product.name,
              productSku: product.sku,
              saleUnit: product.saleUnit,
              unitQuantity: product.unitQuantity,
            }}
          />
        </div>
      </div>

      <section className="product-detail__section" aria-labelledby="product-attributes-title">
        <h2 id="product-attributes-title">Характеристики</h2>
        {product.attributes.length > 0 ? (
          <table className="attributes-table">
            <tbody>
              {product.attributes.map((attribute) => (
                <tr key={attribute.code}>
                  <th scope="row">{attribute.name}</th>
                  <td>{formatAttributeValue(attribute)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <p className="empty-state">Характеристики для этой позиции пока не опубликованы.</p>
        )}
      </section>
    </article>
  );
}

function formatAttributeValue(attribute: PublicProductAttribute): string {
  const value =
    typeof attribute.value === "boolean" ? (attribute.value ? "да" : "нет") : String(attribute.value);

  return attribute.unit ? `${value} ${attribute.unit}` : value;
}
