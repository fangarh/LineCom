import { readFileSync } from "node:fs";
import path from "node:path";
import { describe, expect, it } from "vitest";

const publicCss = readFileSync(path.join(process.cwd(), "src", "styles", "public.css"), "utf8");

function cssBlock(selector: string) {
  const escapedSelector = selector.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  const matches = [...publicCss.matchAll(new RegExp(`${escapedSelector}\\s*\\{(?<body>[^}]*)\\}`, "g"))];
  return matches.at(-1)?.groups?.body ?? "";
}

describe("public css layout contracts", () => {
  it("keeps the active catalog category readable in dark theme", () => {
    const activeCategory = cssBlock(".category-nav__link--active");

    expect(activeCategory).toContain("color: #20262b");
  });

  it("keeps active homepage hero products readable on the light card state", () => {
    const activeProductName = cssBlock(".home-hero-product.is-active strong");
    const activeProductCategory = cssBlock(".home-hero-product.is-active small");

    expect(activeProductName).toContain("color: #20262b");
    expect(activeProductCategory).toContain("color: #62686f");
  });

  it("allows homepage direction descriptions to use the available card height", () => {
    const directionText = cssBlock(".home-direction span");

    expect(directionText).not.toContain("-webkit-line-clamp");
  });

  it("aligns the catalog card contact button to the lower right of the footer", () => {
    const productFooter = cssBlock(".product-card__footer");
    const productButton = cssBlock(".product-card__button");

    expect(productFooter).toContain("grid-template-columns: minmax(0, 1fr) auto");
    expect(productFooter).toContain("align-items: center");
    expect(productButton).toContain("justify-self: end");
    expect(productButton).toContain("transform: translateY(4px)");
  });

  it("aligns the product detail contact button to the right below the summary", () => {
    const productCta = cssBlock(".product-detail__cta");
    const productButton = cssBlock(".product-detail__button");

    expect(productCta).toContain("justify-items: end");
    expect(productCta).toContain("margin-top: 18px");
    expect(productButton).toContain("transform: translateY(4px)");
  });
});
