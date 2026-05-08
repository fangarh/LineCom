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
          availabilityStatus: "in_stock",
          saleUnit: undefined,
          attributes: { connector: "lc" },
        }}
      />,
    );

    expect(screen.getByRole("link", { name: "В наличии" })).toHaveAttribute(
      "href",
      "/catalog/adapters?sort=name&attribute.connector=lc",
    );
    expect(screen.getByRole("link", { name: "Штука" })).toHaveAttribute(
      "href",
      "/catalog/adapters?sort=name&availabilityStatus=in_stock&saleUnit=piece&attribute.connector=lc",
    );
    expect(screen.getByRole("link", { name: "LC" })).toHaveAttribute(
      "href",
      "/catalog/adapters?sort=name&availabilityStatus=in_stock",
    );
    expect(screen.getByRole("link", { name: "Сбросить фильтры" })).toHaveAttribute("href", "/catalog/adapters");
  });
});
