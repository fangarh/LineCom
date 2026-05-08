"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import type { PublicProductListItem } from "@/lib/api/catalog";
import { PRODUCT_IMAGE_FALLBACK, PRODUCT_IMAGE_FALLBACK_ALT } from "@/lib/product-images";
import { routes } from "@/lib/routes";

type HomeHeroProductsProps = {
  products: PublicProductListItem[];
};

export function HomeHeroProducts({ products }: HomeHeroProductsProps) {
  const [activeIndex, setActiveIndex] = useState(0);

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

  useEffect(() => {
    if (activeIndex >= products.length) {
      setActiveIndex(0);
    }
  }, [activeIndex, products.length]);

  if (products.length === 0) {
    return <p className="home-hero__empty">Добавьте позиции из каталога в заявку или опишите задачу.</p>;
  }

  return (
    <div className="home-hero-products" aria-live="polite">
      <div className="home-hero-products__stage" aria-hidden="true">
        {products.map((product, index) => (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            key={product.id}
            className={index === activeIndex ? "is-active" : undefined}
            src={product.mainImage?.url ?? PRODUCT_IMAGE_FALLBACK}
            alt=""
          />
        ))}
      </div>

      <div className="home-hero__product-stack">
        {products.map((product, index) => (
          <Link
            key={product.id}
            className={`home-hero-product${index === activeIndex ? " is-active" : ""}`}
            href={routes.product(product.slug)}
            onFocus={() => setActiveIndex(index)}
            onMouseEnter={() => setActiveIndex(index)}
          >
            <span className="home-hero-product__image">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={product.mainImage?.url ?? PRODUCT_IMAGE_FALLBACK}
                alt={product.mainImage?.alt ?? PRODUCT_IMAGE_FALLBACK_ALT}
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
