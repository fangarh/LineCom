import { afterEach, describe, expect, it, vi } from "vitest";
import { routes } from "../routes";
import {
  addAdminHomepageSectionItem,
  type AdminHomepageSection,
  type AdminHomepageSectionsResponse,
  deleteAdminHomepageSectionItem,
  getAdminHomepageSections,
  type UpdateAdminHomepageSectionCommand,
  type UpdateAdminHomepageSectionItemCommand,
  updateAdminHomepageSection,
  updateAdminHomepageSectionItem,
  updateAdminHomepageSectionItemOrder,
} from "./admin-homepage";

function jsonResponse(payload: unknown) {
  return new Response(JSON.stringify(payload), {
    status: 200,
    headers: { "Content-Type": "application/json" },
  });
}

function noContentResponse() {
  return new Response(null, { status: 204 });
}

function expectJsonHeaders(headers: Headers, csrfToken?: string) {
  expect(headers.get("Accept")).toBe("application/json");
  expect(headers.get("Content-Type")).toBe("application/json");
  if (csrfToken) {
    expect(headers.get("X-CSRF-Token")).toBe(csrfToken);
  }
}

function expectCsrfHeaders(headers: Headers, csrfToken: string) {
  expect(headers.get("Accept")).toBe("application/json");
  expect(headers.get("X-CSRF-Token")).toBe(csrfToken);
}

describe("admin homepage API client", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("builds admin homepage route", () => {
    expect(routes.adminHomepage()).toBe("/admin/homepage");
  });

  it("gets homepage sections with credentials", async () => {
    const payload = { sections: [homepageSectionFixture()] } satisfies AdminHomepageSectionsResponse;
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse(payload));

    await expect(getAdminHomepageSections()).resolves.toMatchObject({
      sections: [
        {
          id: "section-1",
          code: "featured_products",
          title: "Featured products",
          type: "product_list",
          isActive: true,
          sortOrder: 10,
          items: [
            {
              id: "item-1",
              productId: "product-1",
              categoryId: null,
              name: "Cable",
              slug: "cable",
              secondaryText: "SKU-1",
              sortOrder: 20,
              isActive: true,
              visibilityStatus: "visible",
            },
          ],
        },
      ],
    });

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/admin/homepage/sections",
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      }),
    );
  });

  it("updates homepage section with csrf", async () => {
    const payload = homepageSectionFixture({ title: "Main products", itemLimit: 6, sortOrder: 10 });
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse(payload));
    const command = { title: "Main products", itemLimit: 6, sortOrder: 10, isActive: true } satisfies UpdateAdminHomepageSectionCommand;

    await expect(updateAdminHomepageSection("section-1", command, "csrf")).resolves.toMatchObject({
      id: "section-1",
      code: "featured_products",
      title: "Main products",
      type: "product_list",
      itemLimit: 6,
      sortOrder: 10,
      isActive: true,
      items: expect.any(Array),
    });

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(fetchMock).toHaveBeenCalledWith(
      "/api/admin/homepage/sections/section-1",
      expect.objectContaining({
        method: "PUT",
        credentials: "include",
      }),
    );
    expect(init.body).toBe(JSON.stringify(command));
    expectJsonHeaders(init.headers as Headers, "csrf");
  });

  it("adds homepage section item with csrf and payload", async () => {
    const payload = homepageSectionFixture().items[0];
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse(payload));
    const command = { productId: "product-1", categoryId: null, sortOrder: 20, isActive: true };

    await expect(addAdminHomepageSectionItem("section-1", command, "csrf-token")).resolves.toMatchObject({
      id: "item-1",
      productId: "product-1",
      categoryId: null,
      name: "Cable",
      slug: "cable",
      secondaryText: "SKU-1",
      sortOrder: 20,
      isActive: true,
      visibilityStatus: "visible",
    });

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(fetchMock.mock.calls[0][0]).toBe("/api/admin/homepage/sections/section-1/items");
    expect(init.method).toBe("POST");
    expect(init.credentials).toBe("include");
    expect(init.body).toBe(JSON.stringify(command));
    expectJsonHeaders(init.headers as Headers, "csrf-token");
  });

  it("updates homepage section item order with csrf and item ids", async () => {
    const payload = { sections: [homepageSectionFixture()] } satisfies AdminHomepageSectionsResponse;
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse(payload));

    await expect(
      updateAdminHomepageSectionItemOrder("section-1", ["item-2", "item-1"], "csrf-token"),
    ).resolves.toEqual(payload);

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(fetchMock.mock.calls[0][0]).toBe("/api/admin/homepage/sections/section-1/items/order");
    expect(init.method).toBe("PUT");
    expect(init.credentials).toBe("include");
    expect(init.body).toBe(JSON.stringify({ itemIds: ["item-2", "item-1"] }));
    expectJsonHeaders(init.headers as Headers, "csrf-token");
  });

  it("updates homepage section item with csrf and payload", async () => {
    const payload = { ...homepageSectionFixture().items[0], sortOrder: 30, isActive: false };
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse(payload));
    const command = { sortOrder: 30, isActive: false } satisfies UpdateAdminHomepageSectionItemCommand;

    await expect(updateAdminHomepageSectionItem("section-1", "item-1", command, "csrf-token")).resolves.toMatchObject({
      id: "item-1",
      productId: "product-1",
      name: "Cable",
      sortOrder: 30,
      isActive: false,
      visibilityStatus: "visible",
    });

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(fetchMock.mock.calls[0][0]).toBe("/api/admin/homepage/sections/section-1/items/item-1");
    expect(init.method).toBe("PUT");
    expect(init.credentials).toBe("include");
    expect(init.body).toBe(JSON.stringify(command));
    expectJsonHeaders(init.headers as Headers, "csrf-token");
  });

  it("deletes homepage section item with csrf", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(noContentResponse());

    await expect(deleteAdminHomepageSectionItem("section-1", "item-1", "csrf-token")).resolves.toBeUndefined();

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(fetchMock.mock.calls[0][0]).toBe("/api/admin/homepage/sections/section-1/items/item-1");
    expect(init.method).toBe("DELETE");
    expect(init.credentials).toBe("include");
    expectCsrfHeaders(init.headers as Headers, "csrf-token");
  });
});

function homepageSectionFixture(overrides: Partial<AdminHomepageSection> = {}): AdminHomepageSection {
  return {
    id: "section-1",
    code: "featured_products",
    title: "Featured products",
    type: "product_list",
    itemLimit: 8,
    sortOrder: 10,
    isActive: true,
    items: [
      {
        id: "item-1",
        productId: "product-1",
        categoryId: null,
        name: "Cable",
        slug: "cable",
        secondaryText: "SKU-1",
        sortOrder: 20,
        isActive: true,
        visibilityStatus: "visible",
      },
    ],
    ...overrides,
  };
}
