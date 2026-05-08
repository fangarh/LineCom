import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { PublicCategoryTreeItem } from "@/lib/api/catalog";
import { CategoryNav } from "./category-nav";

function category(overrides: Partial<PublicCategoryTreeItem> & Pick<PublicCategoryTreeItem, "id" | "name" | "slug">): PublicCategoryTreeItem {
  return {
    id: overrides.id,
    parentId: overrides.parentId ?? null,
    name: overrides.name,
    slug: overrides.slug,
    h1: overrides.h1 ?? null,
    description: overrides.description ?? null,
    sortOrder: overrides.sortOrder ?? 0,
    isVisibleInMenu: overrides.isVisibleInMenu ?? true,
    children: overrides.children ?? [],
  };
}

describe("CategoryNav", () => {
  it("keeps the category tree visible and marks the active category", () => {
    render(
      <CategoryNav
        activeSlug="sc-lc-adapters"
        items={[
          category({
            id: "fiber",
            name: "Оптические компоненты",
            slug: "fiber-optic-components",
            children: [
              category({ id: "adapters", name: "SC / LC адаптеры", slug: "sc-lc-adapters" }),
              category({ id: "pigtails", name: "Пигтейлы", slug: "pigtails" }),
            ],
          }),
          category({ id: "patch", name: "Патч-корды", slug: "patch-cords" }),
        ]}
      />,
    );

    expect(screen.getByRole("link", { name: "Оптические компоненты" })).toHaveAttribute(
      "href",
      "/catalog/fiber-optic-components",
    );
    expect(screen.getByRole("link", { name: "SC / LC адаптеры" })).toHaveAttribute("aria-current", "page");
    expect(screen.getByRole("link", { name: "Пигтейлы" })).toHaveAttribute("href", "/catalog/pigtails");
    expect(screen.getByRole("link", { name: "Патч-корды" })).toHaveAttribute("href", "/catalog/patch-cords");
  });
});
