import Link from "next/link";
import type { PublicProductListItem } from "@/lib/api/catalog";
import { formatSku } from "@/lib/format";
import { PRODUCT_IMAGE_FALLBACK, PRODUCT_IMAGE_FALLBACK_ALT } from "@/lib/product-images";
import { routes } from "@/lib/routes";
import { AddToRequestButton } from "@/components/request/add-to-request-button";

type ProductCardProps = {
  product: PublicProductListItem;
};

export function ProductCard({ product }: ProductCardProps) {
  return (
    <article className="product-card">
      <Link className="product-card__media" href={routes.product(product.slug)} aria-label={product.name}>
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src={product.mainImage?.url ?? PRODUCT_IMAGE_FALLBACK} alt={product.mainImage?.alt ?? PRODUCT_IMAGE_FALLBACK_ALT} />
      </Link>

      <div className="product-card__body">
        <div className="product-card__meta">
          <span>{product.category.name}</span>
          {product.brand ? <span>{product.brand.name}</span> : null}
        </div>

        <h2>
          <Link href={routes.product(product.slug)}>{product.name}</Link>
        </h2>

        <dl className="spec-list">
          <div>
            <dt>Артикул</dt>
            <dd>{formatSku(product.sku).replace("Артикул: ", "")}</dd>
          </div>
          <div>
            <dt>Наличие</dt>
            <dd>{product.availability.label}</dd>
          </div>
          <div>
            <dt>Единица</dt>
            <dd>
              {product.saleUnit.label}, {product.unitQuantity}
            </dd>
          </div>
        </dl>
      </div>

      <div className="product-card__footer">
        <strong>Цена по запросу</strong>
        <AddToRequestButton
          className="button button--primary product-card__button"
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
    </article>
  );
}
