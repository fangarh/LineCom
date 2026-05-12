import { act, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiClientError } from "@/lib/api/errors";
import { AdminAttributeManager } from "./admin-attribute-manager";
import type {
  AdminAttributeOption,
  AdminCategoryAttribute,
  AdminCategoryAttributesResponse,
  AdminCategoryListItem,
  AdminCategoryListResponse,
} from "@/lib/api/admin-catalog";

const adminCatalogApiMock = vi.hoisted(() => ({
  getAdminCategories: vi.fn(),
  getAdminCategoryAttributes: vi.fn(),
  createAdminCategoryAttribute: vi.fn(),
  updateAdminCategoryAttribute: vi.fn(),
  deleteAdminCategoryAttribute: vi.fn(),
  inheritAdminCategoryAttributesFromParent: vi.fn(),
  createAdminAttributeOption: vi.fn(),
  updateAdminAttributeOption: vi.fn(),
  deleteAdminAttributeOption: vi.fn(),
}));

vi.mock("@/lib/api/admin-catalog", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/admin-catalog")>();
  return {
    ...actual,
    getAdminCategories: adminCatalogApiMock.getAdminCategories,
    getAdminCategoryAttributes: adminCatalogApiMock.getAdminCategoryAttributes,
    createAdminCategoryAttribute: adminCatalogApiMock.createAdminCategoryAttribute,
    updateAdminCategoryAttribute: adminCatalogApiMock.updateAdminCategoryAttribute,
    deleteAdminCategoryAttribute: adminCatalogApiMock.deleteAdminCategoryAttribute,
    inheritAdminCategoryAttributesFromParent: adminCatalogApiMock.inheritAdminCategoryAttributesFromParent,
    createAdminAttributeOption: adminCatalogApiMock.createAdminAttributeOption,
    updateAdminAttributeOption: adminCatalogApiMock.updateAdminAttributeOption,
    deleteAdminAttributeOption: adminCatalogApiMock.deleteAdminAttributeOption,
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

const secondPageCategory: AdminCategoryListItem = {
  id: "cat-second-page",
  parentId: null,
  name: "Категория со второй страницы",
  slug: "second-page",
  sortOrder: 30,
  isActive: true,
  isVisibleInMenu: true,
  productsCount: 0,
  childrenCount: 0,
};

const redOption: AdminAttributeOption = {
  id: "option-red",
  value: "Красный",
  slug: "krasnyy",
  normalizedValue: "red",
  sortOrder: 10,
  isActive: true,
  productValuesCount: 5,
};

const blackOption: AdminAttributeOption = {
  id: "option-black",
  value: "Черный",
  slug: "chernyy",
  normalizedValue: "black",
  sortOrder: 20,
  isActive: false,
  productValuesCount: 0,
};

const colorAttribute: AdminCategoryAttribute = {
  id: "attr-color",
  categoryId: "cat-cables",
  name: "Цвет",
  code: "color",
  type: "select",
  unit: null,
  isRequired: true,
  isFilterable: true,
  isComparable: false,
  isVisibleInProduct: true,
  isSeoImportant: false,
  isUsedInGeneratedName: true,
  sortOrder: 10,
  isActive: true,
  productValuesCount: 3,
  options: [redOption, blackOption],
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
  isSeoImportant: true,
  isUsedInGeneratedName: false,
  sortOrder: 20,
  isActive: true,
  productValuesCount: 0,
  options: [],
};

const connectorMaterialAttribute: AdminCategoryAttribute = {
  id: "attr-material",
  categoryId: "cat-connectors",
  name: "Материал",
  code: "material",
  type: "text",
  unit: null,
  isRequired: false,
  isFilterable: false,
  isComparable: false,
  isVisibleInProduct: true,
  isSeoImportant: false,
  isUsedInGeneratedName: false,
  sortOrder: 5,
  isActive: true,
  productValuesCount: 1,
  options: [],
};

function categoryListResponse(): AdminCategoryListResponse {
  return {
    items: [cablesCategory, connectorsCategory],
    page: 1,
    pageSize: 60,
    totalItems: 2,
    totalPages: 1,
  };
}

function attributesResponse(items: AdminCategoryAttribute[] = [colorAttribute, lengthAttribute]): AdminCategoryAttributesResponse {
  return { items };
}

function mockDefaultApi() {
  adminCatalogApiMock.getAdminCategories.mockResolvedValue(categoryListResponse());
  adminCatalogApiMock.getAdminCategoryAttributes.mockImplementation((categoryId: string) =>
    Promise.resolve(categoryId === "cat-connectors" ? attributesResponse([connectorMaterialAttribute]) : attributesResponse()),
  );
  adminCatalogApiMock.createAdminCategoryAttribute.mockResolvedValue(lengthAttribute);
  adminCatalogApiMock.updateAdminCategoryAttribute.mockResolvedValue(colorAttribute);
  adminCatalogApiMock.deleteAdminCategoryAttribute.mockResolvedValue(undefined);
  adminCatalogApiMock.inheritAdminCategoryAttributesFromParent.mockResolvedValue({ added: 2, skipped: 1 });
  adminCatalogApiMock.createAdminAttributeOption.mockResolvedValue(redOption);
  adminCatalogApiMock.updateAdminAttributeOption.mockResolvedValue(redOption);
  adminCatalogApiMock.deleteAdminAttributeOption.mockResolvedValue(undefined);
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
  render(<AdminAttributeManager csrfToken={csrfToken} />);

  await screen.findByLabelText("Категория");
}

describe("AdminAttributeManager", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockDefaultApi();
  });

  it("loads attributes for the selected category", async () => {
    const user = userEvent.setup();
    await renderManager();

    expect(adminCatalogApiMock.getAdminCategories).toHaveBeenCalledWith({ page: 1, pageSize: 60 });
    await user.selectOptions(screen.getByLabelText("Категория"), "cat-cables");

    expect(adminCatalogApiMock.getAdminCategoryAttributes).toHaveBeenCalledWith("cat-cables");
    expect(await screen.findByRole("button", { name: /Цвет/ })).toBeInTheDocument();

    await user.selectOptions(screen.getByLabelText("Категория"), "cat-connectors");

    expect(adminCatalogApiMock.getAdminCategoryAttributes).toHaveBeenCalledWith("cat-connectors");
    expect(await screen.findByRole("button", { name: /Материал/ })).toBeInTheDocument();
  });

  it("loads every categories page for the category picker", async () => {
    adminCatalogApiMock.getAdminCategories.mockImplementation((params = {}) => {
      if (params.page === 2) {
        return Promise.resolve({
          items: [secondPageCategory],
          page: 2,
          pageSize: 60,
          totalItems: 61,
          totalPages: 2,
        });
      }

      return Promise.resolve({
        items: [cablesCategory, connectorsCategory],
        page: 1,
        pageSize: 60,
        totalItems: 61,
        totalPages: 2,
      });
    });

    await renderManager();

    await waitFor(() => expect(adminCatalogApiMock.getAdminCategories).toHaveBeenCalledWith({ page: 2, pageSize: 60 }));
    expect(within(screen.getByLabelText("Категория")).getByRole("option", { name: "Категория со второй страницы" })).toBeInTheDocument();
  });

  it("creates and updates attributes with boolean flags, sort order and CSRF token", async () => {
    const user = userEvent.setup();
    await renderManager();
    await user.selectOptions(screen.getByLabelText("Категория"), "cat-cables");
    await screen.findByRole("button", { name: /Цвет/ });

    await user.click(screen.getByRole("button", { name: "Новая характеристика" }));
    await user.type(screen.getByLabelText("Название"), "Напряжение");
    await user.type(screen.getByLabelText("Код"), "voltage");
    await user.selectOptions(screen.getByLabelText("Тип"), "number");
    await user.type(screen.getByLabelText("Единица"), "В");
    await user.clear(screen.getByLabelText("Сортировка"));
    await user.type(screen.getByLabelText("Сортировка"), "30");
    await user.click(screen.getByLabelText("Обязательная"));
    await user.click(screen.getByLabelText("Фильтруемая"));
    await user.click(screen.getByLabelText("Сравниваемая"));
    await user.click(screen.getByLabelText("В карточке товара"));
    await user.click(screen.getByLabelText("SEO-важная"));
    await user.click(screen.getByLabelText("В названии товара"));
    await user.click(screen.getByLabelText("Активна"));
    await user.click(screen.getByRole("button", { name: "Создать характеристику" }));

    expect(adminCatalogApiMock.createAdminCategoryAttribute).toHaveBeenCalledWith(
      "cat-cables",
      {
        name: "Напряжение",
        code: "voltage",
        type: "number",
        unit: "В",
        isRequired: true,
        isFilterable: true,
        isComparable: true,
        isVisibleInProduct: false,
        isSeoImportant: true,
        isUsedInGeneratedName: true,
        sortOrder: 30,
        isActive: false,
      },
      "csrf-token",
    );

    await user.click(screen.getByRole("button", { name: /Цвет/ }));
    await screen.findByDisplayValue("Цвет");
    await user.clear(screen.getByLabelText("Название"));
    await user.type(screen.getByLabelText("Название"), "Цвет оболочки");
    await user.clear(screen.getByLabelText("Сортировка"));
    await user.type(screen.getByLabelText("Сортировка"), "15");
    await user.click(screen.getByLabelText("Сравниваемая"));
    await user.click(screen.getByLabelText("SEO-важная"));
    await user.click(screen.getByRole("button", { name: "Сохранить характеристику" }));

    expect(adminCatalogApiMock.updateAdminCategoryAttribute).toHaveBeenCalledWith(
      "cat-cables",
      "attr-color",
      expect.objectContaining({
        name: "Цвет оболочки",
        code: "color",
        type: "select",
        unit: null,
        isRequired: true,
        isFilterable: true,
        isComparable: true,
        isVisibleInProduct: true,
        isSeoImportant: true,
        isUsedInGeneratedName: true,
        sortOrder: 15,
        isActive: true,
      }),
      "csrf-token",
    );
  });

  it("shows option editor for select attributes", async () => {
    const user = userEvent.setup();
    await renderManager();
    await user.selectOptions(screen.getByLabelText("Категория"), "cat-cables");

    await user.click(await screen.findByRole("button", { name: /Цвет/ }));

    expect(await screen.findByRole("heading", { name: "Значения" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Красный/ })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Черный/ })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /Длина/ }));
    await screen.findByDisplayValue("Длина");

    expect(screen.queryByRole("heading", { name: "Значения" })).not.toBeInTheDocument();
  });

  it("does not show option editor until the select type is saved", async () => {
    const user = userEvent.setup();
    await renderManager();
    await user.selectOptions(screen.getByLabelText("Категория"), "cat-cables");

    await user.click(await screen.findByRole("button", { name: /Длина/ }));
    await screen.findByDisplayValue("Длина");
    await user.selectOptions(screen.getByLabelText("Тип"), "select");

    expect(screen.queryByRole("heading", { name: "Значения" })).not.toBeInTheDocument();
  });

  it("keeps select options visible after attribute metadata update", async () => {
    const user = userEvent.setup();
    adminCatalogApiMock.updateAdminCategoryAttribute.mockResolvedValueOnce({
      ...colorAttribute,
      name: "Цвет оболочки",
      options: [],
    });
    await renderManager();
    await user.selectOptions(screen.getByLabelText("Категория"), "cat-cables");
    await user.click(await screen.findByRole("button", { name: /Цвет/ }));
    await screen.findByRole("button", { name: /Красный/ });

    await user.clear(screen.getByLabelText("Название"));
    await user.type(screen.getByLabelText("Название"), "Цвет оболочки");
    await user.click(screen.getByRole("button", { name: "Сохранить характеристику" }));

    expect(await screen.findByRole("button", { name: /Красный/ })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Черный/ })).toBeInTheDocument();
  });

  it("creates, updates and deletes options through nested routes", async () => {
    const user = userEvent.setup();
    await renderManager();
    await user.selectOptions(screen.getByLabelText("Категория"), "cat-cables");
    await user.click(await screen.findByRole("button", { name: /Цвет/ }));

    const optionEditor = await screen.findByLabelText("Редактор значения");
    await user.type(within(optionEditor).getByLabelText("Значение"), "Синий");
    await user.type(within(optionEditor).getByLabelText("Slug"), "siniy");
    await user.type(within(optionEditor).getByLabelText("Нормализованное значение"), "blue");
    await user.clear(within(optionEditor).getByLabelText("Сортировка значения"));
    await user.type(within(optionEditor).getByLabelText("Сортировка значения"), "30");
    await user.click(within(optionEditor).getByRole("button", { name: "Создать значение" }));

    expect(adminCatalogApiMock.createAdminAttributeOption).toHaveBeenCalledWith(
      "cat-cables",
      "attr-color",
      {
        value: "Синий",
        slug: "siniy",
        normalizedValue: "blue",
        sortOrder: 30,
        isActive: true,
      },
      "csrf-token",
    );

    await user.click(screen.getByRole("button", { name: /Красный/ }));
    await user.clear(within(optionEditor).getByLabelText("Значение"));
    await user.type(within(optionEditor).getByLabelText("Значение"), "Красный RAL");
    await user.click(within(optionEditor).getByLabelText("Активно"));
    await user.click(within(optionEditor).getByRole("button", { name: "Сохранить значение" }));

    expect(adminCatalogApiMock.updateAdminAttributeOption).toHaveBeenCalledWith(
      "cat-cables",
      "attr-color",
      "option-red",
      expect.objectContaining({
        value: "Красный RAL",
        slug: "krasnyy",
        normalizedValue: "red",
        sortOrder: 10,
        isActive: false,
      }),
      "csrf-token",
    );

    await user.click(within(optionEditor).getByRole("button", { name: "Удалить значение" }));

    expect(adminCatalogApiMock.deleteAdminAttributeOption).toHaveBeenCalledWith(
      "cat-cables",
      "attr-color",
      "option-red",
      "csrf-token",
    );
  });

  it("shows inherit-from-parent result", async () => {
    const user = userEvent.setup();
    await renderManager();
    await user.selectOptions(screen.getByLabelText("Категория"), "cat-cables");
    await screen.findByRole("button", { name: /Цвет/ });

    await user.click(screen.getByRole("button", { name: "Унаследовать от родителя" }));

    expect(adminCatalogApiMock.inheritAdminCategoryAttributesFromParent).toHaveBeenCalledWith("cat-cables", "csrf-token");
    expect(await screen.findByText("Добавлено: 2. Пропущено: 1.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Унаследовать от родителя" })).toBeEnabled();
  });

  it("ignores stale option mutation result after selecting another attribute", async () => {
    const user = userEvent.setup();
    const optionRequest = deferred<AdminAttributeOption>();
    adminCatalogApiMock.createAdminAttributeOption.mockReturnValueOnce(optionRequest.promise);
    await renderManager();
    await user.selectOptions(screen.getByLabelText("Категория"), "cat-cables");
    await user.click(await screen.findByRole("button", { name: /Цвет/ }));

    const optionEditor = await screen.findByLabelText("Редактор значения");
    await user.type(within(optionEditor).getByLabelText("Значение"), "Синий");
    await user.type(within(optionEditor).getByLabelText("Slug"), "siniy");
    await user.type(within(optionEditor).getByLabelText("Нормализованное значение"), "blue");
    await user.click(within(optionEditor).getByRole("button", { name: "Создать значение" }));
    await waitFor(() => expect(adminCatalogApiMock.createAdminAttributeOption).toHaveBeenCalled());

    await user.click(screen.getByRole("button", { name: /Длина/ }));
    await screen.findByDisplayValue("Длина");

    await act(async () => {
      optionRequest.resolve({
        id: "option-blue",
        value: "Синий",
        slug: "siniy",
        normalizedValue: "blue",
        sortOrder: 30,
        isActive: true,
        productValuesCount: 0,
      });
    });

    expect(screen.getByDisplayValue("Длина")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Синий/ })).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Значения" })).not.toBeInTheDocument();
  });

  it("ignores stale create result and error after starting a new attribute draft", async () => {
    const user = userEvent.setup();
    const firstCreateRequest = deferred<AdminCategoryAttribute>();
    const secondCreateRequest = deferred<AdminCategoryAttribute>();
    adminCatalogApiMock.createAdminCategoryAttribute
      .mockReturnValueOnce(firstCreateRequest.promise)
      .mockReturnValueOnce(secondCreateRequest.promise);
    await renderManager();
    await user.selectOptions(screen.getByLabelText("Категория"), "cat-cables");
    await screen.findByRole("button", { name: /Цвет/ });

    await user.click(screen.getByRole("button", { name: "Новая характеристика" }));
    await user.type(screen.getByLabelText("Название"), "Напряжение");
    await user.type(screen.getByLabelText("Код"), "voltage");
    await user.click(screen.getByRole("button", { name: "Создать характеристику" }));
    await waitFor(() => expect(adminCatalogApiMock.createAdminCategoryAttribute).toHaveBeenCalledTimes(1));

    await user.click(screen.getByRole("button", { name: "Новая характеристика" }));
    await user.type(screen.getByLabelText("Название"), "Ток");
    await user.type(screen.getByLabelText("Код"), "current");

    await act(async () => {
      firstCreateRequest.resolve({ ...lengthAttribute, id: "attr-voltage", name: "Напряжение", code: "voltage" });
    });

    expect(screen.queryByDisplayValue("Напряжение")).not.toBeInTheDocument();
    expect(screen.getByDisplayValue("Ток")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Создать характеристику" }));
    await waitFor(() => expect(adminCatalogApiMock.createAdminCategoryAttribute).toHaveBeenCalledTimes(2));
    await user.click(screen.getByRole("button", { name: "Новая характеристика" }));

    await act(async () => {
      secondCreateRequest.reject(
        new ApiClientError(400, {
          code: "catalog.validation",
          message: "Код характеристики уже используется.",
        }),
      );
    });

    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  it("keeps the current draft after a stale attribute request finishes", async () => {
    const user = userEvent.setup();
    const firstCreateRequest = deferred<AdminCategoryAttribute>();
    adminCatalogApiMock.createAdminCategoryAttribute.mockReturnValueOnce(firstCreateRequest.promise);
    await renderManager();
    await user.selectOptions(screen.getByLabelText("Категория"), "cat-cables");
    await screen.findByRole("button", { name: /Цвет/ });

    await user.click(screen.getByRole("button", { name: "Новая характеристика" }));
    await user.type(screen.getByLabelText("Название"), "Напряжение");
    await user.type(screen.getByLabelText("Код"), "voltage");
    await user.click(screen.getByRole("button", { name: "Создать характеристику" }));
    await waitFor(() => expect(adminCatalogApiMock.createAdminCategoryAttribute).toHaveBeenCalledTimes(1));

    await user.click(screen.getByRole("button", { name: "Новая характеристика" }));
    await user.type(screen.getByLabelText("Название"), "Ток");
    await user.type(screen.getByLabelText("Код"), "current");

    await act(async () => {
      firstCreateRequest.resolve({ ...lengthAttribute, id: "attr-voltage", name: "Напряжение", code: "voltage" });
    });

    expect(screen.getByDisplayValue("Ток")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Создать характеристику" })).toBeEnabled();
  });

  it("ignores stale inherit result after selecting another category", async () => {
    const user = userEvent.setup();
    const inheritRequest = deferred<{ added: number; skipped: number }>();
    adminCatalogApiMock.inheritAdminCategoryAttributesFromParent.mockReturnValueOnce(inheritRequest.promise);
    await renderManager();
    await user.selectOptions(screen.getByLabelText("Категория"), "cat-cables");
    await screen.findByRole("button", { name: /Цвет/ });

    await user.click(screen.getByRole("button", { name: "Унаследовать от родителя" }));
    await waitFor(() => expect(adminCatalogApiMock.inheritAdminCategoryAttributesFromParent).toHaveBeenCalledWith("cat-cables", "csrf-token"));

    await user.selectOptions(screen.getByLabelText("Категория"), "cat-connectors");
    expect(await screen.findByRole("button", { name: /Материал/ })).toBeInTheDocument();

    await act(async () => {
      inheritRequest.resolve({ added: 2, skipped: 1 });
    });

    expect(screen.getByLabelText("Категория")).toHaveValue("cat-connectors");
    expect(screen.getByRole("button", { name: /Материал/ })).toBeInTheDocument();
    expect(screen.queryByText("Добавлено: 2. Пропущено: 1.")).not.toBeInTheDocument();
  });

  it("shows product values counts for used attributes and options plus deletion errors", async () => {
    const user = userEvent.setup();
    adminCatalogApiMock.deleteAdminCategoryAttribute.mockRejectedValueOnce(
      new ApiClientError(409, {
        code: "catalog.attribute_in_use",
        message: "Нельзя удалить характеристику, которая используется в товарах.",
      }),
    );
    adminCatalogApiMock.deleteAdminAttributeOption.mockRejectedValueOnce(
      new ApiClientError(409, {
        code: "catalog.option_in_use",
        message: "Нельзя удалить значение, которое используется в товарах.",
      }),
    );
    await renderManager();
    await user.selectOptions(screen.getByLabelText("Категория"), "cat-cables");
    await user.click(await screen.findByRole("button", { name: /Цвет/ }));

    expect(screen.getByText(/3 значений в товарах/)).toBeInTheDocument();
    expect(screen.getByText(/5 значений в товарах/)).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Удалить характеристику" }));

    expect(adminCatalogApiMock.deleteAdminCategoryAttribute).toHaveBeenCalledWith("cat-cables", "attr-color", "csrf-token");
    expect(await screen.findByRole("alert")).toHaveTextContent("Нельзя удалить характеристику, которая используется в товарах.");

    await user.click(screen.getByRole("button", { name: /Красный/ }));
    await user.click(screen.getByRole("button", { name: "Удалить значение" }));

    expect(adminCatalogApiMock.deleteAdminAttributeOption).toHaveBeenCalledWith(
      "cat-cables",
      "attr-color",
      "option-red",
      "csrf-token",
    );
    expect(await screen.findByRole("alert")).toHaveTextContent("Нельзя удалить значение, которое используется в товарах.");
  });
});
