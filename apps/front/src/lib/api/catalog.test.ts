import { afterEach, describe, expect, it, vi } from "vitest";
import { getProduct, getProducts } from "./catalog";

function jsonResponse(payload: unknown) {
  return new Response(JSON.stringify(payload), {
    status: 200,
    headers: { "Content-Type": "application/json" },
  });
}

describe("public catalog API client", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("gets product details without cached catalog data", async () => {
    const payload = { id: "product-1", images: [] };
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse(payload));

    await expect(getProduct("u-utp")).resolves.toEqual(payload);

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/public/catalog/products/u-utp",
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      }),
    );
  });

  it("gets product lists without cached catalog data", async () => {
    const payload = { items: [], page: 1, pageSize: 24, totalItems: 0, totalPages: 0 };
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse(payload));

    await expect(getProducts({ categorySlug: "patch-kordy", page: 2 })).resolves.toEqual(payload);

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/public/catalog/products?categorySlug=patch-kordy&page=2",
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      }),
    );
  });
});
