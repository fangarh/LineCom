import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiClientError } from "@/lib/api/errors";
import { AdminProductManager } from "./admin-product-manager";
import type {
  AdminBrandListItem,
  AdminBrandListResponse,
  AdminCategoryListItem,
  AdminCategoryListResponse,
  AdminProductDetail,
  AdminProductDuplicateCandidatesResponse,
  AdminProductListItem,
  AdminProductListResponse,
} from "@/lib/api/admin-catalog";

const adminCatalogApiMock = vi.hoisted(() => ({
  getAdminProducts: vi.fn(),
  getAdminProduct: vi.fn(),
  createAdminProduct: vi.fn(),
  updateAdminProduct: vi.fn(),
  deleteAdminProduct: vi.fn(),
  getAdminProductDuplicateCandidates: vi.fn(),
  getAdminCategories: vi.fn(),
  getAdminBrands: vi.fn(),
}));

vi.mock("@/lib/api/admin-catalog", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/admin-catalog")>();
  return {
    ...actual,
    getAdminProducts: adminCatalogApiMock.getAdminProducts,
    getAdminProduct: adminCatalogApiMock.getAdminProduct,
    createAdminProduct: adminCatalogApiMock.createAdminProduct,
    updateAdminProduct: adminCatalogApiMock.updateAdminProduct,
    deleteAdminProduct: adminCatalogApiMock.deleteAdminProduct,
    getAdminProductDuplicateCandidates: adminCatalogApiMock.getAdminProductDuplicateCandidates,
    getAdminCategories: adminCatalogApiMock.getAdminCategories,
    getAdminBrands: adminCatalogApiMock.getAdminBrands,
  };
});

const cablesCategory: AdminCategoryListItem = {
  id: "cat-cables",
  parentId: null,
  name: "Кабели",
  slug: "kabeli",
  sortOrder: 10,
  isActive: true,
  isVisibleInMenu: true,
  productsCount: 8,
  childrenCount: 1,
};

const connectorsCategory: AdminCategoryListItem = {
  id: "cat-connectors",
  parentId: null,
  name: "Разъемы",
  slug: "razemy",
  sortOrder: 20,
  isActive: true,
  isVisibleInMenu: true,
  productsCount: 3,
  childrenCount: 0,
};

const cableBrand: AdminBrandListItem = {
  id: "brand-cable",
  name: "Кабельный завод",
  slug: "kabelnyy-zavod",
  isActive: true,
  productsCount: 7,
};

const promBrand: AdminBrandListItem = {
  id: "brand-prom",
  name: "ПромСвет",
  slug: "promsvet",
  isActive: true,
  productsCount: 2,
};

const activeProduct: AdminProductListItem = {
  id: "product-active",
  name: "Кабель ВВГнг 3x2.5",
  slug: "kabel-vvgng-3x25",
  sku: "VVG-325",
  externalId: "EXT-325",
  categoryName: "Кабели",
  categorySlug: "kabeli",
  brandName: "Кабельный завод",
  publishStatus: "draft",
  isActive: true,
  availabilityStatus: "in_stock",
  sortOrder: 10,
  readiness: { canPublish: false, issues: [{ code: "missing_image", message: "Добавьте основное изображение." }] },
};

const publishedProduct: AdminProductListItem = {
  id: "product-published",
  name: "Разъем силовой РС",
  slug: "razem-silovoy-rs",
  sku: "RS-1",
  externalId: null,
  categoryName: "Разъемы",
  categorySlug: "razemy",
  brandName: "ПромСвет",
  publishStatus: "published",
  isActive: false,
  availabilityStatus: "preorder",
  sortOrder: 20,
  readiness: { canPublish: true, issues: [] },
};

const activeProductDetail: AdminProductDetail = {
  id: "product-active",
  categoryId: "cat-cables",
  categoryName: "Кабели",
  brandId: "brand-cable",
  brandName: "Кабельный завод",
  name: "Кабель ВВГнг 3x2.5",
  slug: "kabel-vvgng-3x25",
  sku: "VVG-325",
  externalId: "EXT-325",
  description: "Силовой кабель для стационарной прокладки",
  shortDescription: "Кабель ВВГнг 3x2.5",
  availabilityStatus: "in_stock",
  saleUnit: "м",
  unitQuantity: "1",
  publishStatus: "draft",
  isActive: true,
  seoTitle: "Кабель ВВГнг 3x2.5 купить",
  seoDescription: "SEO описание кабеля",
  h1: "Кабель ВВГнг 3x2.5",
  sortOrder: 10,
  readiness: { canPublish: false, issues: [{ code: "missing_image", message: "Добавьте основное изображение." }] },
  images: { imagesCount: 0, mainImageFileId: null },
  attributes: [],
};

const savedProductDetail: AdminProductDetail = {
  ...activeProductDetail,
  name: "Кабель ВВГнг 3x2.5 обновленный",
};

function productListResponse(items: AdminProductListItem[] = [activeProduct, publishedProduct]): AdminProductListResponse {
  return {
    items,
    page: 1,
    pageSize: 50,
    totalItems: items.length,
    totalPages: 1,
  };
}

function categoryListResponse(): AdminCategoryListResponse {
  return {
    items: [cablesCategory, connectorsCategory],
    page: 1,
    pageSize: 60,
    totalItems: 2,
    totalPages: 1,
  };
}

function brandListResponse(): AdminBrandListResponse {
  return {
    items: [cableBrand, promBrand],
    page: 1,
    pageSize: 60,
    totalItems: 2,
    totalPages: 1,
  };
}

function duplicateResponse(): AdminProductDuplicateCandidatesResponse {
  return {
    items: [
      {
        id: "duplicate-product",
        name: "Кабель ВВГнг 3x2.5 похожий",
        slug: "kabel-vvgng-3x25-pohozhiy",
        sku: "VVG-325-DUP",
        externalId: "EXT-DUP",
        categoryName: "Кабели",
        categorySlug: "kabeli",
        brandName: "Кабельный завод",
        publishStatus: "draft",
        isActive: true,
        similarity: 0.92,
      },
    ],
  };
}

function mockDefaultApi() {
  adminCatalogApiMock.getAdminProducts.mockResolvedValue(productListResponse());
  adminCatalogApiMock.getAdminProduct.mockResolvedValue(activeProductDetail);
  adminCatalogApiMock.createAdminProduct.mockResolvedValue(activeProductDetail);
  adminCatalogApiMock.updateAdminProduct.mockResolvedValue(savedProductDetail);
  adminCatalogApiMock.deleteAdminProduct.mockResolvedValue(undefined);
  adminCatalogApiMock.getAdminProductDuplicateCandidates.mockResolvedValue(duplicateResponse());
  adminCatalogApiMock.getAdminCategories.mockResolvedValue(categoryListResponse());
  adminCatalogApiMock.getAdminBrands.mockResolvedValue(brandListResponse());
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  let reject!: (reason?: unknown) => void;
  const promise = new Promise<T>((promiseResolve, promiseReject) => {
    resolve = promiseResolve;
    reject = promiseReject;
  });

  return { promise, resolve, reject };
}

async function renderManager(csrfToken = "csrf-token") {
  render(<AdminProductManager csrfToken={csrfToken} />);

  await screen.findByRole("button", { name: /Кабель ВВГнг 3x2.5/ });
}

describe("AdminProductManager", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockDefaultApi();
  });

  it("фильтрует список товаров по поиску, категории, бренду, активности и публикации", async () => {
    const user = userEvent.setup();
    await renderManager();

    expect(adminCatalogApiMock.getAdminProducts).toHaveBeenCalledWith({});

    const list = screen.getByLabelText("Список товаров");
    await user.type(within(list).getByLabelText("Поиск"), "кабель");
    await user.selectOptions(within(list).getByLabelText("Категория"), "cat-cables");
    await user.selectOptions(within(list).getByLabelText("Бренд"), "brand-cable");
    await user.selectOptions(within(list).getByLabelText("Активность"), "true");
    await user.selectOptions(within(list).getByLabelText("Публикация"), "published");

    expect(adminCatalogApiMock.getAdminProducts).toHaveBeenLastCalledWith({
      search: "кабель",
      categoryId: "cat-cables",
      brandId: "brand-cable",
      isActive: true,
      publishStatus: "published",
    });
  });

  it("создает товар с обязательными полями и CSRF-токеном", async () => {
    const user = userEvent.setup();
    await renderManager();

    await user.click(screen.getByRole("button", { name: "Новый товар" }));
    const editor = screen.getByLabelText("Редактор товара");

    await user.selectOptions(within(editor).getByLabelText("Категория"), "cat-cables");
    await user.selectOptions(within(editor).getByLabelText("Бренд"), "brand-cable");
    await user.type(within(editor).getByLabelText("Название"), "Муфта кабельная 1кВ");
    await user.type(within(editor).getByLabelText("Slug"), "mufta-kabelnaya-1kv");
    await user.type(within(editor).getByLabelText("SKU"), "MUFTA-1KV");
    await user.type(within(editor).getByLabelText("External ID"), "ERP-1KV");
    await user.type(within(editor).getByLabelText("Краткое описание"), "Муфта для силового кабеля");
    await user.type(within(editor).getByLabelText("Описание"), "Полное описание муфты");
    await user.selectOptions(within(editor).getByLabelText("Наличие"), "preorder");
    await user.clear(within(editor).getByLabelText("Единица продажи"));
    await user.type(within(editor).getByLabelText("Единица продажи"), "шт");
    await user.clear(within(editor).getByLabelText("Количество в единице"));
    await user.type(within(editor).getByLabelText("Количество в единице"), "1");
    await user.clear(within(editor).getByLabelText("Сортировка"));
    await user.type(within(editor).getByLabelText("Сортировка"), "30");
    await user.click(within(editor).getByRole("button", { name: "Создать" }));

    expect(adminCatalogApiMock.createAdminProduct).toHaveBeenCalledWith(
      {
        categoryId: "cat-cables",
        brandId: "brand-cable",
        name: "Муфта кабельная 1кВ",
        slug: "mufta-kabelnaya-1kv",
        sku: "MUFTA-1KV",
        externalId: "ERP-1KV",
        description: "Полное описание муфты",
        shortDescription: "Муфта для силового кабеля",
        availabilityStatus: "preorder",
        saleUnit: "шт",
        unitQuantity: "1",
        publishStatus: "draft",
        isActive: true,
        seoTitle: null,
        seoDescription: null,
        h1: null,
        sortOrder: 30,
      },
      "csrf-token",
    );
  });

  it("редактирует поля товара во вкладках Основное, SEO и Публикация", async () => {
    const user = userEvent.setup();
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Кабель ВВГнг 3x2.5/ }));
    const editor = await screen.findByLabelText("Редактор товара");

    await user.clear(within(editor).getByLabelText("Название"));
    await user.type(within(editor).getByLabelText("Название"), "Кабель ВВГнг 3x2.5 обновленный");
    await user.selectOptions(within(editor).getByLabelText("Наличие"), "out_of_stock");
    await user.click(within(editor).getByRole("button", { name: "Сохранить" }));

    expect(adminCatalogApiMock.updateAdminProduct).toHaveBeenLastCalledWith(
      "product-active",
      expect.objectContaining({
        name: "Кабель ВВГнг 3x2.5 обновленный",
        availabilityStatus: "out_of_stock",
        h1: "Кабель ВВГнг 3x2.5",
      }),
      "csrf-token",
    );

    await user.click(within(editor).getByRole("tab", { name: "SEO" }));
    await user.clear(within(editor).getByLabelText("H1"));
    await user.type(within(editor).getByLabelText("H1"), "Кабель силовой ВВГнг");
    await user.clear(within(editor).getByLabelText("SEO title"));
    await user.type(within(editor).getByLabelText("SEO title"), "Кабель силовой ВВГнг купить");
    await user.clear(within(editor).getByLabelText("SEO description"));
    await user.type(within(editor).getByLabelText("SEO description"), "Купить кабель ВВГнг для монтажа");
    await user.click(within(editor).getByRole("button", { name: "Сохранить" }));

    expect(adminCatalogApiMock.updateAdminProduct).toHaveBeenLastCalledWith(
      "product-active",
      expect.objectContaining({
        h1: "Кабель силовой ВВГнг",
        seoTitle: "Кабель силовой ВВГнг купить",
        seoDescription: "Купить кабель ВВГнг для монтажа",
      }),
      "csrf-token",
    );

    await user.click(within(editor).getByRole("tab", { name: "Публикация" }));
    await user.selectOptions(within(editor).getByLabelText("Статус публикации"), "published");
    await user.click(within(editor).getByLabelText("Активен"));
    await user.click(within(editor).getByRole("button", { name: "Сохранить" }));

    expect(adminCatalogApiMock.updateAdminProduct).toHaveBeenLastCalledWith(
      "product-active",
      expect.objectContaining({
        publishStatus: "published",
        isActive: false,
      }),
      "csrf-token",
    );
  });

  it("показывает readiness issues из ответа backend", async () => {
    const user = userEvent.setup();
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Кабель ВВГнг 3x2.5/ }));
    const editor = await screen.findByLabelText("Редактор товара");
    await user.click(within(editor).getByRole("tab", { name: "Публикация" }));

    expect(within(editor).getByText("Добавьте основное изображение.")).toBeInTheDocument();
    expect(within(editor).getByText("Нельзя опубликовать")).toBeInTheDocument();
  });

  it("запрашивает кандидатов дублей из identity-полей товара и показывает строки кандидатов", async () => {
    const user = userEvent.setup();
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Кабель ВВГнг 3x2.5/ }));
    const editor = await screen.findByLabelText("Редактор товара");
    await user.click(within(editor).getByRole("button", { name: "Проверить дубли" }));

    expect(adminCatalogApiMock.getAdminProductDuplicateCandidates).toHaveBeenCalledWith({
      name: "Кабель ВВГнг 3x2.5",
      categoryId: "cat-cables",
      brandId: "brand-cable",
      sku: "VVG-325",
      externalId: "EXT-325",
      slug: "kabel-vvgng-3x25",
      excludeProductId: "product-active",
      limit: 5,
    });
    expect(await within(editor).findByRole("row", { name: /Кабель ВВГнг 3x2.5 похожий/ })).toBeInTheDocument();
  });

  it("показывает entity-in-use ошибку удаления товара", async () => {
    const user = userEvent.setup();
    adminCatalogApiMock.deleteAdminProduct.mockRejectedValueOnce(
      new ApiClientError(409, {
        code: "catalog.product_in_use",
        message: "Нельзя удалить товар, который используется в заявках.",
      }),
    );
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Кабель ВВГнг 3x2.5/ }));
    const editor = await screen.findByLabelText("Редактор товара");
    await user.click(within(editor).getByRole("button", { name: "Удалить" }));

    expect(adminCatalogApiMock.deleteAdminProduct).toHaveBeenCalledWith("product-active", "csrf-token");
    expect(await within(editor).findByRole("alert")).toHaveTextContent("Нельзя удалить товар, который используется в заявках.");
  });

  it("блокирует сохранение и удаление, пока загружается карточка другого товара", async () => {
    const user = userEvent.setup();
    const detailRequest = deferred<AdminProductDetail>();
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Кабель ВВГнг 3x2.5/ }));
    const editor = await screen.findByLabelText("Редактор товара");
    expect(await within(editor).findByLabelText("Название")).toHaveValue("Кабель ВВГнг 3x2.5");
    adminCatalogApiMock.getAdminProduct.mockReturnValueOnce(detailRequest.promise);

    await user.click(screen.getByRole("button", { name: /Разъем силовой РС/ }));

    expect(within(editor).getByRole("button", { name: "Сохранить" })).toBeDisabled();
    expect(within(editor).getByRole("button", { name: "Удалить" })).toBeDisabled();
    expect(adminCatalogApiMock.updateAdminProduct).not.toHaveBeenCalled();
    expect(adminCatalogApiMock.deleteAdminProduct).not.toHaveBeenCalled();
  });

  it("сбрасывает состояние проверки дублей при переходе к новому товару", async () => {
    const user = userEvent.setup();
    const duplicateRequest = deferred<AdminProductDuplicateCandidatesResponse>();
    adminCatalogApiMock.getAdminProductDuplicateCandidates.mockReturnValueOnce(duplicateRequest.promise);
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Кабель ВВГнг 3x2.5/ }));
    const editor = await screen.findByLabelText("Редактор товара");
    await user.click(within(editor).getByRole("button", { name: "Проверить дубли" }));
    await waitFor(() => expect(adminCatalogApiMock.getAdminProductDuplicateCandidates).toHaveBeenCalled());
    expect(within(editor).getByRole("button", { name: "Проверить дубли" })).toBeDisabled();

    await user.click(screen.getByRole("button", { name: "Новый товар" }));

    expect(within(editor).getByRole("button", { name: "Проверить дубли" })).toBeEnabled();
  });

  it("сбрасывает загрузку карточки при переходе к новому товару", async () => {
    const user = userEvent.setup();
    const detailRequest = deferred<AdminProductDetail>();
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Кабель ВВГнг 3x2.5/ }));
    const editor = await screen.findByLabelText("Редактор товара");
    await waitFor(() => expect(within(editor).getByRole("button", { name: "Сохранить" })).toBeEnabled());
    adminCatalogApiMock.getAdminProduct.mockReturnValueOnce(detailRequest.promise);

    await user.click(screen.getByRole("button", { name: /Разъем силовой РС/ }));
    expect(within(editor).getByRole("button", { name: "Сохранить" })).toBeDisabled();

    await user.click(screen.getByRole("button", { name: "Новый товар" }));

    expect(within(editor).getByRole("button", { name: "Создать" })).toBeEnabled();
    expect(within(editor).getByRole("button", { name: "Проверить дубли" })).toBeEnabled();
  });
});
