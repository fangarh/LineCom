import { act, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiClientError } from "@/lib/api/errors";
import { AdminProductManager } from "./admin-product-manager";
import type {
  AdminBrandListItem,
  AdminBrandListResponse,
  AdminCategoryAttribute,
  AdminCategoryAttributesResponse,
  AdminCategoryListItem,
  AdminCategoryListResponse,
  AdminProductDetail,
  AdminProductDuplicateCandidatesResponse,
  AdminProductImage,
  AdminProductImagesResponse,
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
  getAdminCategoryAttributes: vi.fn(),
  updateAdminProductAttributes: vi.fn(),
  getAdminProductImages: vi.fn(),
  uploadAdminProductImages: vi.fn(),
  updateAdminProductImage: vi.fn(),
  updateAdminProductImageOrder: vi.fn(),
  setAdminProductMainImage: vi.fn(),
  deleteAdminProductImage: vi.fn(),
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
    getAdminCategoryAttributes: adminCatalogApiMock.getAdminCategoryAttributes,
    updateAdminProductAttributes: adminCatalogApiMock.updateAdminProductAttributes,
    getAdminProductImages: adminCatalogApiMock.getAdminProductImages,
    uploadAdminProductImages: adminCatalogApiMock.uploadAdminProductImages,
    updateAdminProductImage: adminCatalogApiMock.updateAdminProductImage,
    updateAdminProductImageOrder: adminCatalogApiMock.updateAdminProductImageOrder,
    setAdminProductMainImage: adminCatalogApiMock.setAdminProductMainImage,
    deleteAdminProductImage: adminCatalogApiMock.deleteAdminProductImage,
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

const powerCablesCategory: AdminCategoryListItem = {
  id: "cat-power-cables",
  parentId: "cat-cables",
  name: "Силовые кабели",
  slug: "silovye-kabeli",
  sortOrder: 15,
  isActive: true,
  isVisibleInMenu: true,
  productsCount: 4,
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

const colorAttribute: AdminCategoryAttribute = {
  id: "attr-color",
  categoryId: "cat-cables",
  name: "Цвет",
  code: "color",
  type: "text",
  unit: null,
  isRequired: false,
  isFilterable: true,
  isComparable: true,
  isVisibleInProduct: true,
  isSeoImportant: false,
  isUsedInGeneratedName: false,
  sortOrder: 10,
  isActive: true,
  productValuesCount: 3,
  options: [],
};

const lengthAttribute: AdminCategoryAttribute = {
  id: "attr-length",
  categoryId: "cat-cables",
  name: "Длина",
  code: "length",
  type: "number",
  unit: "м",
  isRequired: false,
  isFilterable: true,
  isComparable: true,
  isVisibleInProduct: true,
  isSeoImportant: false,
  isUsedInGeneratedName: false,
  sortOrder: 20,
  isActive: true,
  productValuesCount: 4,
  options: [],
};

const kitAttribute: AdminCategoryAttribute = {
  id: "attr-kit",
  categoryId: "cat-cables",
  name: "Монтажный комплект",
  code: "kit",
  type: "boolean",
  unit: null,
  isRequired: false,
  isFilterable: true,
  isComparable: true,
  isVisibleInProduct: true,
  isSeoImportant: false,
  isUsedInGeneratedName: false,
  sortOrder: 30,
  isActive: true,
  productValuesCount: 2,
  options: [],
};

const materialAttribute: AdminCategoryAttribute = {
  id: "attr-material",
  categoryId: "cat-cables",
  name: "Материал",
  code: "material",
  type: "select",
  unit: null,
  isRequired: false,
  isFilterable: true,
  isComparable: true,
  isVisibleInProduct: true,
  isSeoImportant: true,
  isUsedInGeneratedName: false,
  sortOrder: 40,
  isActive: true,
  productValuesCount: 5,
  options: [
    {
      id: "option-copper",
      value: "Медь",
      slug: "copper",
      normalizedValue: "медь",
      sortOrder: 10,
      isActive: true,
      productValuesCount: 3,
    },
    {
      id: "option-aluminium",
      value: "Алюминий",
      slug: "aluminium",
      normalizedValue: "алюминий",
      sortOrder: 20,
      isActive: true,
      productValuesCount: 2,
    },
    {
      id: "option-inactive",
      value: "Серебро",
      slug: "silver",
      normalizedValue: "серебро",
      sortOrder: 30,
      isActive: false,
      productValuesCount: 0,
    },
  ],
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
  attributes: [
    {
      attributeId: "attr-color",
      code: "color",
      name: "Цвет",
      type: "text",
      unit: null,
      valueText: "Черный",
      valueNumber: null,
      valueBoolean: null,
      attributeOptionId: null,
      optionValue: null,
    },
    {
      attributeId: "attr-length",
      code: "length",
      name: "Длина",
      type: "number",
      unit: "м",
      valueText: null,
      valueNumber: 25,
      valueBoolean: null,
      attributeOptionId: null,
      optionValue: null,
    },
    {
      attributeId: "attr-kit",
      code: "kit",
      name: "Монтажный комплект",
      type: "boolean",
      unit: null,
      valueText: null,
      valueNumber: null,
      valueBoolean: true,
      attributeOptionId: null,
      optionValue: null,
    },
    {
      attributeId: "attr-material",
      code: "material",
      name: "Материал",
      type: "select",
      unit: null,
      valueText: null,
      valueNumber: null,
      valueBoolean: null,
      attributeOptionId: "option-copper",
      optionValue: "Медь",
    },
  ],
};

const publishedProductDetail: AdminProductDetail = {
  id: "product-published",
  categoryId: "cat-connectors",
  categoryName: "Разъемы",
  brandId: "brand-prom",
  brandName: "ПромСвет",
  name: "Разъем силовой РС",
  slug: "razem-silovoy-rs",
  sku: "RS-1",
  externalId: null,
  description: "Разъем для силовой линии",
  shortDescription: "Разъем силовой РС",
  availabilityStatus: "preorder",
  saleUnit: "шт",
  unitQuantity: "1",
  publishStatus: "published",
  isActive: false,
  seoTitle: "Разъем силовой РС купить",
  seoDescription: "SEO описание разъема",
  h1: "Разъем силовой РС",
  sortOrder: 20,
  readiness: { canPublish: true, issues: [] },
  images: { imagesCount: 1, mainImageFileId: "file-main" },
  attributes: [],
};

const savedProductDetail: AdminProductDetail = {
  ...activeProductDetail,
  name: "Кабель ВВГнг 3x2.5 обновленный",
};

function productListResponse(
  items: AdminProductListItem[] = [activeProduct, publishedProduct],
  meta: Partial<Omit<AdminProductListResponse, "items">> = {},
): AdminProductListResponse {
  return {
    items,
    page: meta.page ?? 1,
    pageSize: meta.pageSize ?? 60,
    totalItems: meta.totalItems ?? items.length,
    totalPages: meta.totalPages ?? 1,
  };
}

function categoryListResponse(): AdminCategoryListResponse {
  return {
    items: [cablesCategory, powerCablesCategory, connectorsCategory],
    page: 1,
    pageSize: 60,
    totalItems: 3,
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

const mainImage: AdminProductImage = {
  id: "image-main",
  storedFileId: "file-main",
  url: "/uploads/main.jpg",
  originalFileName: "main.jpg",
  contentType: "image/jpeg",
  sizeBytes: 1024,
  checksum: "checksum-main",
  alt: "Кабель на белом фоне",
  title: "Кабель ВВГнг",
  sortOrder: 10,
  isMain: true,
  createdAt: "2026-05-12T08:00:00Z",
};

function attributesResponse(): AdminCategoryAttributesResponse {
  return {
    items: [colorAttribute, lengthAttribute, kitAttribute, materialAttribute],
  };
}

function imagesResponse(): AdminProductImagesResponse {
  return {
    items: [mainImage],
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
  adminCatalogApiMock.getAdminCategoryAttributes.mockResolvedValue(attributesResponse());
  adminCatalogApiMock.updateAdminProductAttributes.mockResolvedValue(activeProductDetail);
  adminCatalogApiMock.getAdminProductImages.mockResolvedValue(imagesResponse());
  adminCatalogApiMock.uploadAdminProductImages.mockResolvedValue(imagesResponse());
  adminCatalogApiMock.updateAdminProductImage.mockResolvedValue(mainImage);
  adminCatalogApiMock.updateAdminProductImageOrder.mockResolvedValue(imagesResponse());
  adminCatalogApiMock.setAdminProductMainImage.mockResolvedValue(mainImage);
  adminCatalogApiMock.deleteAdminProductImage.mockResolvedValue(undefined);
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

  it("запрашивает первую страницу товаров с pageSize 60 по умолчанию", async () => {
    await renderManager();

    expect(adminCatalogApiMock.getAdminProducts).toHaveBeenCalledWith({ page: 1, pageSize: 60 });
  });

  it("показывает компактную таблицу товаров со статусами и диапазоном пагинации", async () => {
    adminCatalogApiMock.getAdminProducts.mockResolvedValue(productListResponse([activeProduct, publishedProduct], { totalItems: 135, totalPages: 3 }));
    await renderManager();

    const list = screen.getByLabelText("Список товаров");
    const table = within(list).getByRole("table");
    expect(within(list).getByRole("columnheader", { name: "Товар" })).toBeInTheDocument();
    expect(within(list).getByRole("columnheader", { name: "SKU / External ID" })).toBeInTheDocument();
    expect(within(list).getByRole("columnheader", { name: "Категория" })).toBeInTheDocument();
    expect(within(list).getByRole("columnheader", { name: "Бренд" })).toBeInTheDocument();
    expect(within(list).getByRole("columnheader", { name: "Статусы" })).toBeInTheDocument();
    expect(within(list).getByRole("columnheader", { name: "Проблемы" })).toBeInTheDocument();
    expect(within(list).getByText("1-60 из 135")).toBeInTheDocument();
    expect(within(table).getByText("Активен")).toBeInTheDocument();
    expect(within(table).getByText("Черновик")).toBeInTheDocument();
    expect(within(table).getByText("Нельзя публиковать")).toBeInTheDocument();
    expect(within(table).getByText("Неактивен")).toBeInTheDocument();
    expect(within(table).getByText("Опубликован")).toBeInTheDocument();
    expect(within(table).getByText("Готов к публикации")).toBeInTheDocument();
    expect(within(table).getByText("Добавьте основное изображение.")).toBeInTheDocument();
  });

  it("переходит вперед и назад по страницам, сохраняя фильтры", async () => {
    const user = userEvent.setup();
    adminCatalogApiMock.getAdminProducts.mockImplementation((params = {}) =>
      Promise.resolve(productListResponse([activeProduct, publishedProduct], { page: params.page ?? 1, totalItems: 135, totalPages: 3 })),
    );
    await renderManager();

    const list = screen.getByLabelText("Список товаров");
    await user.type(within(list).getByLabelText("Поиск"), "кабель");
    await user.click(within(list).getByRole("button", { name: "Дальше" }));

    await waitFor(() =>
      expect(adminCatalogApiMock.getAdminProducts).toHaveBeenLastCalledWith({
        page: 2,
        pageSize: 60,
        search: "кабель",
      }),
    );

    await user.click(within(list).getByRole("button", { name: "Назад" }));

    await waitFor(() =>
      expect(adminCatalogApiMock.getAdminProducts).toHaveBeenLastCalledWith({
        page: 1,
        pageSize: 60,
        search: "кабель",
      }),
    );
  });

  it("сбрасывает страницу на первую при изменении фильтра", async () => {
    const user = userEvent.setup();
    adminCatalogApiMock.getAdminProducts.mockResolvedValue(productListResponse([activeProduct], { page: 1, totalItems: 120, totalPages: 2 }));
    await renderManager();

    const list = screen.getByLabelText("Список товаров");
    await user.click(within(list).getByRole("button", { name: "Дальше" }));

    await waitFor(() => expect(adminCatalogApiMock.getAdminProducts).toHaveBeenLastCalledWith({ page: 2, pageSize: 60 }));

    await user.selectOptions(within(list).getByLabelText("Публикация"), "published");

    await waitFor(() =>
      expect(adminCatalogApiMock.getAdminProducts).toHaveBeenLastCalledWith({
        page: 1,
        pageSize: 60,
        publishStatus: "published",
      }),
    );
  });

  it("фильтрует список товаров по поиску, категории, бренду, активности и публикации", async () => {
    const user = userEvent.setup();
    await renderManager();

    expect(adminCatalogApiMock.getAdminProducts).toHaveBeenCalledWith({ page: 1, pageSize: 60 });

    const list = screen.getByLabelText("Список товаров");
    await user.type(within(list).getByLabelText("Поиск"), "кабель");
    await user.selectOptions(within(list).getByLabelText("Категория"), "cat-cables");
    await user.selectOptions(within(list).getByLabelText("Бренд"), "brand-cable");
    await user.selectOptions(within(list).getByLabelText("Активность"), "true");
    await user.selectOptions(within(list).getByLabelText("Публикация"), "published");

    expect(adminCatalogApiMock.getAdminProducts).toHaveBeenLastCalledWith({
      page: 1,
      pageSize: 60,
      search: "кабель",
      categoryId: "cat-cables",
      brandId: "brand-cable",
      isActive: true,
      publishStatus: "published",
    });
  });

  it("открывает редактор товара в диалоге из строки и из кнопки нового товара", async () => {
    const user = userEvent.setup();
    await renderManager();

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /Кабель ВВГнг 3x2.5/ }));
    const editDialog = await screen.findByRole("dialog", { name: /Редактирование товара/ });
    expect(within(editDialog).getByLabelText("Название")).toHaveValue("Кабель ВВГнг 3x2.5");

    await user.click(within(editDialog).getByRole("button", { name: "Закрыть редактор товара" }));
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());

    await user.click(screen.getByRole("button", { name: "Новый товар" }));
    const createDialog = await screen.findByRole("dialog", { name: /Новый товар/ });
    expect(within(createDialog).getByLabelText("Название")).toHaveValue("");
  });

  it("подтверждает закрытие диалога при несохраненных изменениях товара", async () => {
    const user = userEvent.setup();
    const confirmSpy = vi.spyOn(window, "confirm").mockReturnValueOnce(false).mockReturnValueOnce(true);
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Кабель ВВГнг 3x2.5/ }));
    const dialog = await screen.findByRole("dialog", { name: /Редактирование товара/ });
    await user.type(within(dialog).getByLabelText("Название"), " обновленный");

    await user.click(within(dialog).getByRole("button", { name: "Закрыть редактор товара" }));
    expect(confirmSpy).toHaveBeenCalledTimes(1);
    expect(screen.getByRole("dialog", { name: /Редактирование товара/ })).toBeInTheDocument();

    await user.keyboard("{Escape}");
    expect(confirmSpy).toHaveBeenCalledTimes(2);
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());

    confirmSpy.mockRestore();
  });

  it("оставляет диалог открытым после сохранения и блокирует закрытие во время мутации", async () => {
    const user = userEvent.setup();
    const saveRequest = deferred<AdminProductDetail>();
    adminCatalogApiMock.updateAdminProduct.mockReturnValueOnce(saveRequest.promise);
    const confirmSpy = vi.spyOn(window, "confirm");
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Кабель ВВГнг 3x2.5/ }));
    const dialog = await screen.findByRole("dialog", { name: /Редактирование товара/ });
    await user.click(within(dialog).getByRole("button", { name: "Сохранить" }));

    expect(within(dialog).getByRole("button", { name: "Закрыть редактор товара" })).toBeDisabled();
    await user.keyboard("{Escape}");
    expect(screen.getByRole("dialog", { name: /Редактирование товара/ })).toBeInTheDocument();
    expect(confirmSpy).not.toHaveBeenCalled();

    await act(async () => {
      saveRequest.resolve(savedProductDetail);
    });

    expect(await within(dialog).findByText("Товар сохранен.")).toBeInTheDocument();
    expect(screen.getByRole("dialog", { name: /Редактирование товара/ })).toBeInTheDocument();

    confirmSpy.mockRestore();
  });

  it("закрывает диалог после удаления товара и обновляет список", async () => {
    const user = userEvent.setup();
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Кабель ВВГнг 3x2.5/ }));
    const dialog = await screen.findByRole("dialog", { name: /Редактирование товара/ });
    await user.click(within(dialog).getByRole("button", { name: "Удалить" }));

    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    expect(adminCatalogApiMock.deleteAdminProduct).toHaveBeenCalledWith("product-active", "csrf-token");
    expect(adminCatalogApiMock.getAdminProducts).toHaveBeenLastCalledWith({ page: 1, pageSize: 60 });
  });

  it("не гидратирует закрытый диалог устаревшим ответом карточки товара", async () => {
    const user = userEvent.setup();
    const detailRequest = deferred<AdminProductDetail>();
    adminCatalogApiMock.getAdminProduct.mockReturnValueOnce(detailRequest.promise);
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Кабель ВВГнг 3x2.5/ }));
    const dialog = await screen.findByRole("dialog", { name: /Редактирование товара|Новый товар/ });
    await user.click(within(dialog).getByRole("button", { name: "Закрыть редактор товара" }));
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());

    await act(async () => {
      detailRequest.resolve(activeProductDetail);
    });

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Новый товар" }));
    const createDialog = await screen.findByRole("dialog", { name: /Новый товар/ });
    expect(within(createDialog).getByLabelText("Название")).toHaveValue("");
  });

  it("создает товар с обязательными полями и CSRF-токеном", async () => {
    const user = userEvent.setup();
    await renderManager();

    await user.click(screen.getByRole("button", { name: "Новый товар" }));
    const editor = screen.getByLabelText("Редактор товара");

    await user.click(within(editor).getByRole("button", { name: "Выбрать категорию" }));
    const categoryListbox = within(editor).getByRole("listbox", { name: "Категория" });
    expect(within(categoryListbox).getByRole("option", { name: "Кабели" })).toHaveAttribute("aria-disabled", "true");
    await user.click(within(categoryListbox).getByRole("option", { name: "Силовые кабели" }));
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
        categoryId: "cat-power-cables",
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

  it("показывает во вкладке характеристик контролы по типам text, number, boolean и select", async () => {
    const user = userEvent.setup();
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Кабель ВВГнг 3x2.5/ }));
    const editor = await screen.findByLabelText("Редактор товара");
    await user.click(within(editor).getByRole("tab", { name: "Характеристики" }));

    expect(adminCatalogApiMock.getAdminCategoryAttributes).toHaveBeenCalledWith("cat-cables");
    expect(within(editor).getByLabelText("Цвет")).toHaveAttribute("type", "text");
    expect(within(editor).getByLabelText("Цвет")).toHaveValue("Черный");
    expect(within(editor).getByLabelText("Длина")).toHaveAttribute("type", "number");
    expect(within(editor).getByLabelText("Длина")).toHaveValue(25);
    expect(within(editor).getByLabelText("Монтажный комплект")).toHaveAttribute("type", "checkbox");
    expect(within(editor).getByLabelText("Монтажный комплект")).toBeChecked();
    expect(within(editor).getByLabelText("Материал")).toHaveDisplayValue("Медь");
    expect(within(editor).getByRole("option", { name: "Алюминий" })).toBeInTheDocument();
    expect(within(editor).queryByRole("option", { name: "Серебро" })).not.toBeInTheDocument();
  });

  it("сохраняет характеристики товара через updateAdminProductAttributes с CSRF-токеном", async () => {
    const user = userEvent.setup();
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Кабель ВВГнг 3x2.5/ }));
    const editor = await screen.findByLabelText("Редактор товара");
    await user.click(within(editor).getByRole("tab", { name: "Характеристики" }));

    await user.clear(within(editor).getByLabelText("Цвет"));
    await user.type(within(editor).getByLabelText("Цвет"), "Синий");
    await user.clear(within(editor).getByLabelText("Длина"));
    await user.type(within(editor).getByLabelText("Длина"), "50");
    await user.click(within(editor).getByLabelText("Монтажный комплект"));
    await user.selectOptions(within(editor).getByLabelText("Материал"), "option-aluminium");
    await user.click(within(editor).getByRole("button", { name: "Сохранить характеристики" }));

    expect(adminCatalogApiMock.updateAdminProductAttributes).toHaveBeenCalledWith(
      "product-active",
      {
        values: [
          { attributeId: "attr-color", valueText: "Синий" },
          { attributeId: "attr-length", valueNumber: 50 },
          { attributeId: "attr-kit", valueBoolean: false },
          { attributeId: "attr-material", attributeOptionId: "option-aluminium" },
        ],
      },
      "csrf-token",
    );
  });

  it("позволяет добавить характеристики, когда у товара еще нет сохраненных значений", async () => {
    const user = userEvent.setup();
    adminCatalogApiMock.getAdminProduct.mockResolvedValueOnce({ ...activeProductDetail, attributes: [] });
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Кабель ВВГнг 3x2.5/ }));
    const editor = await screen.findByLabelText("Редактор товара");
    await user.click(within(editor).getByRole("tab", { name: "Характеристики" }));

    expect(await within(editor).findByLabelText("Цвет")).toHaveValue("");
    await user.type(within(editor).getByLabelText("Цвет"), "Белый");
    await user.type(within(editor).getByLabelText("Длина"), "15");
    await user.click(within(editor).getByLabelText("Монтажный комплект"));
    await user.selectOptions(within(editor).getByLabelText("Материал"), "option-copper");
    await user.click(within(editor).getByRole("button", { name: "Сохранить характеристики" }));

    expect(adminCatalogApiMock.updateAdminProductAttributes).toHaveBeenCalledWith(
      "product-active",
      {
        values: [
          { attributeId: "attr-color", valueText: "Белый" },
          { attributeId: "attr-length", valueNumber: 15 },
          { attributeId: "attr-kit", valueBoolean: true },
          { attributeId: "attr-material", attributeOptionId: "option-copper" },
        ],
      },
      "csrf-token",
    );
  });

  it("не отправляет очищенные text, number и select значения, но сохраняет boolean false", async () => {
    const user = userEvent.setup();
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Кабель ВВГнг 3x2.5/ }));
    const editor = await screen.findByLabelText("Редактор товара");
    await user.click(within(editor).getByRole("tab", { name: "Характеристики" }));

    await user.clear(within(editor).getByLabelText("Цвет"));
    await user.clear(within(editor).getByLabelText("Длина"));
    await user.click(within(editor).getByLabelText("Монтажный комплект"));
    await user.selectOptions(within(editor).getByLabelText("Материал"), "");
    await user.click(within(editor).getByRole("button", { name: "Сохранить характеристики" }));

    expect(adminCatalogApiMock.updateAdminProductAttributes).toHaveBeenCalledWith(
      "product-active",
      {
        values: [{ attributeId: "attr-kit", valueBoolean: false }],
      },
      "csrf-token",
    );
  });

  it("не перезаписывает выбранный товар устаревшим сохранением характеристик", async () => {
    const user = userEvent.setup();
    const attributesSave = deferred<AdminProductDetail>();
    adminCatalogApiMock.getAdminProduct.mockImplementation((productId: string) =>
      Promise.resolve(productId === "product-published" ? publishedProductDetail : activeProductDetail),
    );
    adminCatalogApiMock.updateAdminProductAttributes.mockReturnValueOnce(attributesSave.promise);
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Кабель ВВГнг 3x2.5/ }));
    const editor = await screen.findByLabelText("Редактор товара");
    await user.click(within(editor).getByRole("tab", { name: "Характеристики" }));
    await within(editor).findByLabelText("Цвет");
    await user.click(within(editor).getByRole("button", { name: "Сохранить характеристики" }));
    await user.click(screen.getByRole("button", { name: /Разъем силовой РС/ }));
    await waitFor(() => expect(within(editor).getByLabelText("Название")).toHaveValue("Разъем силовой РС"));

    await act(async () => {
      attributesSave.resolve(savedProductDetail);
    });

    expect(within(editor).getByLabelText("Название")).toHaveValue("Разъем силовой РС");
    expect(within(editor).getByLabelText("Slug")).toHaveValue("razem-silovoy-rs");
  });

  it("загружает изображения во вкладке изображений выбранного товара", async () => {
    const user = userEvent.setup();
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Кабель ВВГнг 3x2.5/ }));
    const editor = await screen.findByLabelText("Редактор товара");
    await user.click(within(editor).getByRole("tab", { name: "Изображения" }));

    expect(adminCatalogApiMock.getAdminProductImages).toHaveBeenCalledWith("product-active");
    expect(await within(editor).findByRole("article", { name: /main\.jpg/ })).toBeInTheDocument();
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

  it("автозаполняет slug для нового товара и уважает ручное переопределение", async () => {
    const user = userEvent.setup();
    await renderManager();

    await user.click(screen.getByRole("button", { name: "Новый товар" }));
    const editor = screen.getByLabelText("Редактор товара");

    await user.type(within(editor).getByLabelText("Название"), "Муфта кабельная 1кВ");
    expect(within(editor).getByLabelText("Slug")).toHaveValue("mufta-kabelnaya-1kv");

    await user.clear(within(editor).getByLabelText("Slug"));
    await user.type(within(editor).getByLabelText("Slug"), "manual-slug");
    await user.clear(within(editor).getByLabelText("Название"));
    await user.type(within(editor).getByLabelText("Название"), "Другое название");
    expect(within(editor).getByLabelText("Slug")).toHaveValue("manual-slug");

    await user.click(within(editor).getByRole("button", { name: "Сгенерировать заново" }));
    expect(within(editor).getByLabelText("Slug")).toHaveValue("drugoe-nazvanie");

    await user.clear(within(editor).getByLabelText("Название"));
    await user.type(within(editor).getByLabelText("Название"), "Не перезатирать");
    expect(within(editor).getByLabelText("Slug")).toHaveValue("drugoe-nazvanie");
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
