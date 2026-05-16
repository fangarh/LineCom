import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { PublicFilter } from "@/lib/api/catalog";
import { CatalogFilters } from "./catalog-filters";

const connectorFilter: PublicFilter = {
  code: "connector",
  name: "Коннектор",
  type: "string",
  unit: null,
  sortOrder: 10,
  options: [
    { value: "SC", slug: "sc", sortOrder: 10 },
    { value: "LC", slug: "lc", sortOrder: 20 },
  ],
};

describe("CatalogFilters", () => {
  it("builds stable filter links for the current catalog scope", () => {
    render(
      <CatalogFilters
        attributeFilters={[connectorFilter]}
        basePath="/catalog/adapters"
        scopeLabel="Адаптеры"
        totalItems={12}
        state={{
          sort: "name",
          attributes: { connector: "lc" },
        }}
      />,
    );

    expect(screen.queryByRole("link", { name: "В наличии" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Штука" })).not.toBeInTheDocument();
    expect(screen.getByText("Фильтры товаров").closest("summary")).not.toBeNull();
    expect(screen.getByRole("link", { name: "LC" })).toHaveAttribute(
      "href",
      "/catalog/adapters?sort=name",
    );
    expect(screen.getByRole("link", { name: "Сбросить фильтры" })).toHaveAttribute("href", "/catalog/adapters");
  });

  it("does not render attribute filters without multiple selectable options", () => {
    render(
      <CatalogFilters
        attributeFilters={[
          connectorFilter,
          {
            code: "series",
            name: "Серия",
            type: "select",
            unit: null,
            sortOrder: 15,
            options: [{ value: "Кабель", slug: "kabel", sortOrder: 10 }],
          },
          {
            code: "low_smoke",
            name: "Низкое дымовыделение",
            type: "boolean",
            unit: null,
            sortOrder: 20,
            options: [],
          },
        ]}
        basePath="/catalog/dlya-vneshnej-prokladki"
        scopeLabel="Для внешней прокладки"
        totalItems={18}
        state={{
          sort: "category",
          attributes: {},
        }}
      />,
    );

    expect(screen.getByRole("heading", { name: "Коннектор" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Серия" })).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Низкое дымовыделение" })).not.toBeInTheDocument();
  });
});
