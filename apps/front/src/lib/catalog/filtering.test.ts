import { describe, expect, it } from "vitest";
import type { PublicFilter } from "@/lib/api/catalog";
import { countActiveFilters, parseCatalogFilters, toProductListParams } from "./filtering";

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

describe("catalog filtering", () => {
  it("parses supported query filters and allowed category attributes", () => {
    const filters = parseCatalogFilters(
      {
        sort: "name",
        availabilityStatus: "in_stock",
        saleUnit: "piece",
        "attribute.connector": "lc",
      },
      [connectorFilter],
    );

    expect(filters).toEqual({
      sort: "name",
      attributes: { connector: "lc" },
    });
    expect(countActiveFilters(filters)).toBe(1);
    expect(toProductListParams(filters, "adapters")).toEqual({
      categorySlug: "adapters",
      pageSize: 24,
      sort: "name",
      attributes: { connector: "lc" },
    });
  });

  it("ignores unsupported query values and attributes outside the category filter set", () => {
    const filters = parseCatalogFilters(
      {
        sort: "price",
        availabilityStatus: "soon",
        saleUnit: "meter",
        "attribute.connector": "fc",
        "attribute.color": "blue",
      },
      [connectorFilter],
    );

    expect(filters).toEqual({
      sort: "category",
      attributes: {},
    });
    expect(countActiveFilters(filters)).toBe(0);
  });
});
