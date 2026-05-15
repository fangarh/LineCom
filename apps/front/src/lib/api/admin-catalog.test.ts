import { afterEach, describe, expect, it, vi } from "vitest";
import { routes } from "../routes";
import {
  type AdminProductDetail,
  type AdminProductListResponse,
  createAdminProduct,
  deleteAdminBrandLogo,
  deleteAdminProductImage,
  getAdminProduct,
  getAdminProducts,
  type UpdateAdminProductAttributesCommand,
  updateAdminProduct,
  updateAdminProductAttributes,
  type UpsertAdminProductCommand,
  uploadAdminBrandLogo,
  uploadAdminProductImages,
} from "./admin-catalog";

function jsonResponse(payload: unknown) {
  return new Response(JSON.stringify(payload), {
    status: 200,
    headers: { "Content-Type": "application/json" },
  });
}

function noContentResponse() {
  return new Response(null, { status: 204 });
}

function invalidResponse() {
  return new Response("<html>bad gateway</html>", {
    status: 500,
    headers: { "Content-Type": "text/html" },
  });
}

function expectJsonHeaders(headers: Headers, csrfToken?: string) {
  expect(headers.get("Accept")).toBe("application/json");
  expect(headers.get("Content-Type")).toBe("application/json");
  if (csrfToken) {
    expect(headers.get("X-CSRF-Token")).toBe(csrfToken);
  }
}

function expectFormHeaders(headers: Headers, csrfToken: string) {
  expect(headers.get("Accept")).toBe("application/json");
  expect(headers.get("Content-Type")).toBeNull();
  expect(headers.get("X-CSRF-Token")).toBe(csrfToken);
}

function expectCsrfHeaders(headers: Headers, csrfToken: string) {
  expect(headers.get("Accept")).toBe("application/json");
  expect(headers.get("X-CSRF-Token")).toBe(csrfToken);
}

describe("admin catalog API client", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("builds admin catalog route", () => {
    expect(routes.adminCatalog()).toBe("/admin/catalog");
  });

  it("builds product list query params and disables cache", async () => {
    const payload = {
      items: [
        {
          id: "product-id",
          name: "Cable",
          slug: "cable",
          sku: "SKU-1",
          externalId: null,
          categoryName: "Cables",
          categorySlug: "cables",
          brandName: "LineCom",
          publishStatus: "draft",
          isActive: false,
          availabilityStatus: "in_stock",
          sortOrder: 10,
          readiness: { canPublish: false, issues: [{ code: "missing_image", message: "Image required" }] },
        },
      ],
      page: 2,
      pageSize: 20,
      totalItems: 1,
      totalPages: 1,
    } satisfies AdminProductListResponse;
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(payload));
    vi.stubGlobal("fetch", fetchMock);

    await expect(getAdminProducts({ page: 2, pageSize: 20, search: "Cable" })).resolves.toEqual(payload);

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/admin/catalog/products?page=2&pageSize=20&search=Cable",
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      }),
    );
    expect(payload.items[0]).toMatchObject({
      id: "product-id",
      name: "Cable",
      slug: "cable",
      categoryName: "Cables",
      categorySlug: "cables",
      publishStatus: "draft",
      isActive: false,
      readiness: { canPublish: false },
    });
  });

  it("creates product as JSON with csrf token", async () => {
    const payload = productDetailFixture();
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(payload));
    vi.stubGlobal("fetch", fetchMock);

    const command = {
      categoryId: "category-id",
      brandId: "brand-id",
      name: "Cable",
      slug: "cable",
      sku: "SKU-1",
      externalId: null,
      description: "Long cable",
      shortDescription: "Cable",
      availabilityStatus: "in_stock",
      saleUnit: "pcs",
      unitQuantity: "1",
      publishStatus: "draft",
      isActive: true,
      seoTitle: "Cable",
      seoDescription: "Cable description",
      h1: "Cable",
      sortOrder: 10,
    } satisfies UpsertAdminProductCommand;

    await expect(createAdminProduct(command, "csrf-token")).resolves.toEqual(payload);

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(fetchMock.mock.calls[0][0]).toBe("/api/admin/catalog/products");
    expect(init.method).toBe("POST");
    expect(init.credentials).toBe("include");
    expect(init.body).toBe(JSON.stringify(command));
    expectJsonHeaders(init.headers as Headers, "csrf-token");
  });

  it("gets product detail with critical response contract fields", async () => {
    const payload = productDetailFixture();
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(payload));
    vi.stubGlobal("fetch", fetchMock);

    await expect(getAdminProduct("product/id")).resolves.toMatchObject({
      id: "product-id",
      categoryId: "category-id",
      name: "Cable",
      slug: "cable",
      publishStatus: "draft",
      isActive: true,
      readiness: { canPublish: false },
      images: { imagesCount: 1, mainImageFileId: "file-id" },
      attributes: [
        {
          attributeId: "attribute-id",
          code: "jacket",
          type: "select",
          attributeOptionId: "option-id",
        },
      ],
    });

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/admin/catalog/products/product%2Fid",
      expect.objectContaining({
        method: "GET",
        credentials: "include",
        cache: "no-store",
      }),
    );
  });

  it("updates product and attribute values as csrf-protected JSON contracts", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(jsonResponse(productDetailFixture({ name: "Updated cable", slug: "updated-cable" })))
      .mockResolvedValueOnce(jsonResponse(productDetailFixture()));
    vi.stubGlobal("fetch", fetchMock);
    const command = {
      categoryId: "category-id",
      brandId: null,
      name: "Updated cable",
      slug: "updated-cable",
      availabilityStatus: "in_stock",
      saleUnit: "pcs",
      unitQuantity: "1",
      publishStatus: "published",
      isActive: true,
      sortOrder: 20,
    } satisfies UpsertAdminProductCommand;
    const attributesCommand = {
      values: [
        {
          attributeId: "attribute-id",
          valueText: null,
          valueNumber: null,
          valueBoolean: null,
          attributeOptionId: "option-id",
        },
      ],
    } satisfies UpdateAdminProductAttributesCommand;

    await expect(updateAdminProduct("product/id", command, "csrf-token")).resolves.toMatchObject({
      name: "Updated cable",
      slug: "updated-cable",
    });
    await expect(updateAdminProductAttributes("product/id", attributesCommand, "csrf-token")).resolves.toMatchObject({
      attributes: [{ attributeId: "attribute-id", attributeOptionId: "option-id" }],
    });

    const [, updateInit] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(fetchMock.mock.calls[0][0]).toBe("/api/admin/catalog/products/product%2Fid");
    expect(updateInit.method).toBe("PUT");
    expect(updateInit.credentials).toBe("include");
    expect(updateInit.body).toBe(JSON.stringify(command));
    expectJsonHeaders(updateInit.headers as Headers, "csrf-token");

    const [, attributesInit] = fetchMock.mock.calls[1] as [string, RequestInit];
    expect(fetchMock.mock.calls[1][0]).toBe("/api/admin/catalog/products/product%2Fid/attributes");
    expect(attributesInit.method).toBe("PUT");
    expect(attributesInit.credentials).toBe("include");
    expect(attributesInit.body).toBe(JSON.stringify(attributesCommand));
    expectJsonHeaders(attributesInit.headers as Headers, "csrf-token");
  });

  it("uploads product images as multipart files with csrf token", async () => {
    const payload = { items: [] };
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(payload));
    vi.stubGlobal("fetch", fetchMock);
    const image = new File(["image"], "image.png", { type: "image/png" });

    await expect(uploadAdminProductImages("product/id", [image], "csrf-token")).resolves.toEqual(payload);

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const body = init.body as FormData;
    expect(fetchMock.mock.calls[0][0]).toBe("/api/admin/catalog/products/product%2Fid/images");
    expect(init.method).toBe("POST");
    expect(init.credentials).toBe("include");
    expect(body.getAll("files")).toHaveLength(1);
    expect((body.get("files") as File).name).toBe("image.png");
    expectFormHeaders(init.headers as Headers, "csrf-token");
  });

  it("uploads brand logo as multipart file with csrf token", async () => {
    const payload = { storedFileId: "file-id", url: "/logo.png" };
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(payload));
    vi.stubGlobal("fetch", fetchMock);
    const logo = new File(["logo"], "logo.png", { type: "image/png" });

    await expect(uploadAdminBrandLogo("brand/id", logo, "csrf-token")).resolves.toEqual(payload);

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const body = init.body as FormData;
    expect(fetchMock.mock.calls[0][0]).toBe("/api/admin/catalog/brands/brand%2Fid/logo");
    expect(init.method).toBe("PUT");
    expect(init.credentials).toBe("include");
    expect(body.getAll("file")).toHaveLength(1);
    expect((body.get("file") as File).name).toBe("logo.png");
    expectFormHeaders(init.headers as Headers, "csrf-token");
  });

  it("uses the shared invalid-response message for multipart transport errors", async () => {
    const fetchMock = vi.fn().mockResolvedValue(invalidResponse());
    vi.stubGlobal("fetch", fetchMock);
    const logo = new File(["logo"], "logo.png", { type: "image/png" });

    await expect(uploadAdminBrandLogo("brand-id", logo, "csrf-token")).rejects.toMatchObject({
      code: "transport.invalid_response",
      message: "Не удалось обработать ответ сервера. Попробуйте позже.",
    });
  });

  it("deletes product image and brand logo with csrf token", async () => {
    const fetchMock = vi.fn().mockResolvedValue(noContentResponse());
    vi.stubGlobal("fetch", fetchMock);

    await expect(deleteAdminProductImage("product/id", "image/id", "csrf-token")).resolves.toBeUndefined();
    await expect(deleteAdminBrandLogo("brand/id", "csrf-token")).resolves.toBeUndefined();

    const [, deleteImageInit] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(fetchMock.mock.calls[0][0]).toBe("/api/admin/catalog/products/product%2Fid/images/image%2Fid");
    expect(deleteImageInit.method).toBe("DELETE");
    expectCsrfHeaders(deleteImageInit.headers as Headers, "csrf-token");

    const [, deleteLogoInit] = fetchMock.mock.calls[1] as [string, RequestInit];
    expect(fetchMock.mock.calls[1][0]).toBe("/api/admin/catalog/brands/brand%2Fid/logo");
    expect(deleteLogoInit.method).toBe("DELETE");
    expectCsrfHeaders(deleteLogoInit.headers as Headers, "csrf-token");
  });
});

function productDetailFixture(overrides: Partial<AdminProductDetail> = {}): AdminProductDetail {
  return {
    id: "product-id",
    categoryId: "category-id",
    categoryName: "Cables",
    brandId: "brand-id",
    brandName: "LineCom",
    name: "Cable",
    slug: "cable",
    sku: "SKU-1",
    externalId: null,
    description: "Long cable",
    shortDescription: "Cable",
    availabilityStatus: "in_stock",
    saleUnit: "pcs",
    unitQuantity: "1",
    publishStatus: "draft",
    isActive: true,
    seoTitle: "Cable",
    seoDescription: "Cable description",
    h1: "Cable",
    sortOrder: 10,
    readiness: {
      canPublish: false,
      issues: [{ code: "missing_image", message: "Image required" }],
    },
    images: {
      imagesCount: 1,
      mainImageFileId: "file-id",
    },
    attributes: [
      {
        attributeId: "attribute-id",
        code: "jacket",
        name: "Jacket",
        type: "select",
        unit: null,
        valueText: null,
        valueNumber: null,
        valueBoolean: null,
        attributeOptionId: "option-id",
        optionValue: "PVC",
      },
    ],
    ...overrides,
  };
}
