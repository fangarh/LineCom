"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import Link from "next/link";
import type { PublicProductListItem } from "@/lib/api/catalog";
import { PRODUCT_IMAGE_FALLBACK, PRODUCT_IMAGE_FALLBACK_ALT } from "@/lib/product-images";
import { routes } from "@/lib/routes";

type HomeHeroProductsProps = {
  products: PublicProductListItem[];
};

export function HomeHeroProducts({ products }: HomeHeroProductsProps) {
  const [activeIndex, setActiveIndex] = useState(0);
  const visibleIndex = products.length === 0 ? 0 : Math.min(activeIndex, products.length - 1);

  useEffect(() => {
    const prefersReducedMotion =
      typeof window.matchMedia === "function" && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    if (products.length < 2 || prefersReducedMotion) {
      return;
    }

    const timerId = window.setInterval(() => {
      setActiveIndex((index) => (index + 1) % products.length);
    }, 3200);

    return () => window.clearInterval(timerId);
  }, [products.length]);

  if (products.length === 0) {
    return <p className="home-hero__empty">Добавьте позиции из каталога в заявку или опишите задачу.</p>;
  }

  return (
    <div className="home-hero-products" aria-live="polite">
      <div className="home-hero-products__stage" aria-hidden="true">
        <Image
          key={products[visibleIndex].id}
          className="is-active"
          src={products[visibleIndex].mainImage?.url ?? PRODUCT_IMAGE_FALLBACK}
          alt=""
          width={600}
          height={380}
          sizes="(max-width: 720px) 76vw, 300px"
          loading="eager"
        />
      </div>

      <div className="home-hero__product-stack">
        {products.map((product, index) => (
          <Link
            key={product.id}
            className={`home-hero-product${index === visibleIndex ? " is-active" : ""}`}
            href={routes.product(product.slug)}
            onFocus={() => setActiveIndex(index)}
            onMouseEnter={() => setActiveIndex(index)}
          >
            <span className="home-hero-product__image">
              <Image
                src={product.mainImage?.url ?? PRODUCT_IMAGE_FALLBACK}
                alt={product.mainImage?.alt ?? PRODUCT_IMAGE_FALLBACK_ALT}
                width={192}
                height={156}
                sizes="96px"
                fetchPriority="low"
              />
            </span>
            <span>
              <strong>{product.name}</strong>
              <small>{product.category.name}</small>
            </span>
          </Link>
        ))}
      </div>
    </div>
  );
}
