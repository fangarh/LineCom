import { act, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiClientError } from "@/lib/api/errors";
import { AdminCategoryManager } from "./admin-category-manager";
import type {
  AdminCategoryDetail,
  AdminCategoryListItem,
  AdminCategoryListResponse,
} from "@/lib/api/admin-catalog";

const adminCatalogApiMock = vi.hoisted(() => ({
  getAdminCategories: vi.fn(),
  getAdminCategory: vi.fn(),
  createAdminCategory: vi.fn(),
  updateAdminCategory: vi.fn(),
  deleteAdminCategory: vi.fn(),
  moveAdminCategory: vi.fn(),
  sortAdminCategory: vi.fn(),
}));

vi.mock("@/lib/api/admin-catalog", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/admin-catalog")>();
  return {
    ...actual,
    getAdminCategories: adminCatalogApiMock.getAdminCategories,
    getAdminCategory: adminCatalogApiMock.getAdminCategory,
    createAdminCategory: adminCatalogApiMock.createAdminCategory,
    updateAdminCategory: adminCatalogApiMock.updateAdminCategory,
    deleteAdminCategory: adminCatalogApiMock.deleteAdminCategory,
    moveAdminCategory: adminCatalogApiMock.moveAdminCategory,
    sortAdminCategory: adminCatalogApiMock.sortAdminCategory,
  };
});

const rootCategory: AdminCategoryListItem = {
  id: "cat-root",
  parentId: null,
  name: "Кабели",
  slug: "kabeli",
  sortOrder: 10,
  isActive: true,
  isVisibleInMenu: true,
  productsCount: 4,
  childrenCount: 1,
};

const childCategory: AdminCategoryListItem = {
  id: "cat-child",
  parentId: "cat-root",
  name: "Силовые кабели",
  slug: "silovye-kabeli",
  sortOrder: 20,
  isActive: false,
  isVisibleInMenu: false,
  productsCount: 2,
  childrenCount: 0,
};

const connectorCategory: AdminCategoryListItem = {
  id: "cat-connector",
  parentId: null,
  name: "Разъемы",
  slug: "razemy",
  sortOrder: 30,
  isActive: true,
  isVisibleInMenu: true,
  productsCount: 1,
  childrenCount: 0,
};

const secondPageCategory: AdminCategoryListItem = {
  id: "cat-page-2",
  parentId: null,
  name: "Категория со второй страницы",
  slug: "page-2-category",
  sortOrder: 70,
  isActive: true,
  isVisibleInMenu: true,
  productsCount: 0,
  childrenCount: 0,
};

const rootDetail: AdminCategoryDetail = {
  ...rootCategory,
  description: "Описание категории",
  h1: "Купить кабели",
  seoTitle: "SEO title",
  seoDescription: "SEO description",
};

const childDetail: AdminCategoryDetail = {
  ...childCategory,
  description: "Описание силовых кабелей",
  h1: "Купить силовые кабели",
  seoTitle: "Силовые кабели SEO",
  seoDescription: "Описание SEO силовых кабелей",
};

function listResponse(items: AdminCategoryListItem[] = [rootCategory, childCategory]): AdminCategoryListResponse {
  return {
    items,
    page: 1,
    pageSize: 50,
    totalItems: items.length,
    totalPages: 1,
  };
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((promiseResolve) => {
    resolve = promiseResolve;
  });

  return { promise, resolve };
}

function mockDefaultApi() {
  adminCatalogApiMock.getAdminCategories.mockResolvedValue(listResponse());
  adminCatalogApiMock.getAdminCategory.mockResolvedValue(rootDetail);
  adminCatalogApiMock.createAdminCategory.mockResolvedValue(rootDetail);
  adminCatalogApiMock.updateAdminCategory.mockResolvedValue(rootDetail);
  adminCatalogApiMock.deleteAdminCategory.mockResolvedValue(undefined);
  adminCatalogApiMock.moveAdminCategory.mockResolvedValue(rootDetail);
  adminCatalogApiMock.sortAdminCategory.mockResolvedValue(rootDetail);
}

async function renderManager(csrfToken = "csrf-token") {
  render(<AdminCategoryManager csrfToken={csrfToken} />);

  await screen.findByRole("button", { name: /Кабели/ });
}

describe("AdminCategoryManager", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockDefaultApi();
  });

  it("loads categories on initial render", async () => {
    render(<AdminCategoryManager csrfToken="csrf-token" />);

    await waitFor(() => expect(adminCatalogApiMock.getAdminCategories).toHaveBeenCalledTimes(2));
    expect(adminCatalogApiMock.getAdminCategories).toHaveBeenNthCalledWith(1, { page: 1, pageSize: 60 });
    expect(adminCatalogApiMock.getAdminCategories).toHaveBeenNthCalledWith(2, {});
  });

  it("filters categories by search, parent and active state", async () => {
    const user = userEvent.setup();
    await renderManager();

    await user.type(screen.getByLabelText("Поиск"), "кабель");
    await user.selectOptions(screen.getByLabelText("Родитель"), "cat-root");
    await user.selectOptions(screen.getByLabelText("Активность"), "true");

    expect(adminCatalogApiMock.getAdminCategories).toHaveBeenLastCalledWith({
      search: "кабель",
      parentId: "cat-root",
      isActive: true,
    });
  });

  it("ignores stale category list responses", async () => {
    const user = userEvent.setup();
    const initialList = deferred<AdminCategoryListResponse>();
    const filteredList = deferred<AdminCategoryListResponse>();
    let unfilteredCalls = 0;
    adminCatalogApiMock.getAdminCategories.mockImplementation((params = {}) => {
      if (params.search) {
        return filteredList.promise;
      }

      unfilteredCalls += 1;
      return unfilteredCalls === 1 ? Promise.resolve(listResponse([rootCategory, childCategory])) : initialList.promise;
    });

    render(<AdminCategoryManager csrfToken="csrf-token" />);

    await waitFor(() => expect(adminCatalogApiMock.getAdminCategories).toHaveBeenCalledTimes(2));
    await user.type(screen.getByLabelText("Поиск"), "с");
    await waitFor(() => expect(adminCatalogApiMock.getAdminCategories).toHaveBeenCalledWith({ search: "с" }));

    await act(async () => {
      filteredList.resolve(listResponse([childCategory]));
    });
    expect(await screen.findByRole("button", { name: /Силовые кабели/ })).toBeInTheDocument();

    await act(async () => {
      initialList.resolve(listResponse([rootCategory]));
    });
    expect(screen.getByRole("button", { name: /Силовые кабели/ })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /^Кабели.*kabeli/ })).not.toBeInTheDocument();
  });

  it("ignores stale category detail responses", async () => {
    const user = userEvent.setup();
    const rootDetailRequest = deferred<AdminCategoryDetail>();
    const childDetailRequest = deferred<AdminCategoryDetail>();
    await renderManager();

    adminCatalogApiMock.getAdminCategory
      .mockReturnValueOnce(rootDetailRequest.promise)
      .mockReturnValueOnce(childDetailRequest.promise);

    await user.click(screen.getByRole("button", { name: /^Кабели.*kabeli/ }));
    await user.click(screen.getByRole("button", { name: /Силовые кабели/ }));

    await act(async () => {
      childDetailRequest.resolve(childDetail);
    });
    expect(await screen.findByDisplayValue("Силовые кабели")).toBeInTheDocument();

    await act(async () => {
      rootDetailRequest.resolve(rootDetail);
    });
    expect(screen.getByDisplayValue("Силовые кабели")).toBeInTheDocument();
    expect(screen.queryByDisplayValue("kabeli")).not.toBeInTheDocument();
  });

  it("refreshes rows with the latest filters when a mutation completes after filters changed", async () => {
    const user = userEvent.setup();
    const updateRequest = deferred<AdminCategoryDetail>();
    adminCatalogApiMock.getAdminCategories.mockImplementation((params = {}) =>
      Promise.resolve(params.isActive === false ? listResponse([childCategory]) : listResponse([rootCategory, childCategory, connectorCategory])),
    );
    adminCatalogApiMock.getAdminCategory.mockResolvedValueOnce(childDetail);
    adminCatalogApiMock.updateAdminCategory.mockReturnValueOnce(updateRequest.promise);

    render(<AdminCategoryManager csrfToken="csrf-token" />);

    await screen.findByRole("button", { name: /Разъемы/ });
    await user.click(screen.getByRole("button", { name: /Силовые кабели/ }));
    await screen.findByDisplayValue("Силовые кабели");
    await user.click(screen.getByRole("button", { name: "Сохранить" }));
    await user.selectOptions(screen.getByLabelText("Активность"), "false");

    await waitFor(() =>
      expect(adminCatalogApiMock.getAdminCategories).toHaveBeenCalledWith({ isActive: false }),
    );
    expect(await screen.findByRole("button", { name: /Силовые кабели/ })).toBeInTheDocument();

    await act(async () => {
      updateRequest.resolve(childDetail);
    });

    await waitFor(() =>
      expect(adminCatalogApiMock.getAdminCategories).toHaveBeenLastCalledWith({ isActive: false }),
    );
    expect(screen.getByRole("button", { name: /Силовые кабели/ })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /^Кабели.*kabeli/ })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Разъемы/ })).not.toBeInTheDocument();
  });

  it("keeps unfiltered parent options when filters change before initial unfiltered response resolves", async () => {
    const user = userEvent.setup();
    const initialRowsRequest = deferred<AdminCategoryListResponse>();
    const allOptionsRequest = deferred<AdminCategoryListResponse>();
    const filteredRowsRequest = deferred<AdminCategoryListResponse>();
    let unfilteredCalls = 0;
    adminCatalogApiMock.getAdminCategories.mockImplementation((params = {}) => {
      if (params.isActive === false) {
        return filteredRowsRequest.promise;
      }

      unfilteredCalls += 1;
      return unfilteredCalls === 1 ? initialRowsRequest.promise : allOptionsRequest.promise;
    });
    adminCatalogApiMock.getAdminCategory.mockResolvedValueOnce(childDetail);

    render(<AdminCategoryManager csrfToken="csrf-token" />);

    await waitFor(() => expect(adminCatalogApiMock.getAdminCategories).toHaveBeenCalledWith({}));
    await user.selectOptions(screen.getByLabelText("Активность"), "false");
    await waitFor(() =>
      expect(adminCatalogApiMock.getAdminCategories).toHaveBeenCalledWith({ isActive: false }),
    );

    await act(async () => {
      filteredRowsRequest.resolve(listResponse([childCategory]));
    });
    expect(await screen.findByRole("button", { name: /Силовые кабели/ })).toBeInTheDocument();

    await act(async () => {
      initialRowsRequest.resolve(listResponse([rootCategory, childCategory, connectorCategory]));
      allOptionsRequest.resolve(listResponse([rootCategory, childCategory, connectorCategory]));
    });

    await user.click(screen.getByRole("button", { name: /Силовые кабели/ }));
    await screen.findByDisplayValue("Силовые кабели");

    const parentSelect = screen.getByLabelText("Родительская категория");
    const moveSelect = screen.getByLabelText("Новый родитель");
    expect(within(parentSelect).getByRole("option", { name: "Кабели" })).toBeInTheDocument();
    expect(within(parentSelect).getByRole("option", { name: "Разъемы" })).toBeInTheDocument();
    expect(within(moveSelect).getByRole("option", { name: "Кабели" })).toBeInTheDocument();
    expect(within(moveSelect).getByRole("option", { name: "Разъемы" })).toBeInTheDocument();
  });

  it("loads every page for unfiltered parent options", async () => {
    const user = userEvent.setup();
    adminCatalogApiMock.getAdminCategories.mockImplementation((params = {}) => {
      if (params.pageSize === 60 && params.page === 2) {
        return Promise.resolve({
          ...listResponse([secondPageCategory]),
          page: 2,
          pageSize: 60,
          totalItems: 61,
          totalPages: 2,
        });
      }

      if (params.pageSize === 60) {
        return Promise.resolve({
          ...listResponse([rootCategory, childCategory]),
          page: 1,
          pageSize: 60,
          totalItems: 61,
          totalPages: 2,
        });
      }

      return Promise.resolve(listResponse([childCategory]));
    });
    adminCatalogApiMock.getAdminCategory.mockResolvedValueOnce(childDetail);

    render(<AdminCategoryManager csrfToken="csrf-token" />);

    expect(await screen.findByRole("button", { name: /Силовые кабели/ })).toBeInTheDocument();
    await waitFor(() => expect(adminCatalogApiMock.getAdminCategories).toHaveBeenCalledWith({ page: 2, pageSize: 60 }));
    await user.click(screen.getByRole("button", { name: /Силовые кабели/ }));
    await screen.findByDisplayValue("Силовые кабели");

    const parentSelect = screen.getByLabelText("Родительская категория");
    const moveSelect = screen.getByLabelText("Новый родитель");
    expect(within(parentSelect).getByRole("option", { name: "Категория со второй страницы" })).toBeInTheDocument();
    expect(within(moveSelect).getByRole("option", { name: "Категория со второй страницы" })).toBeInTheDocument();
  });

  it("keeps unfiltered parent options available while category rows are filtered", async () => {
    const user = userEvent.setup();
    adminCatalogApiMock.getAdminCategories.mockImplementation((params = {}) =>
      Promise.resolve(params.isActive === false ? listResponse([childCategory]) : listResponse([rootCategory, childCategory, connectorCategory])),
    );
    adminCatalogApiMock.getAdminCategory.mockResolvedValueOnce(childDetail);

    render(<AdminCategoryManager csrfToken="csrf-token" />);

    await screen.findByRole("button", { name: /Разъемы/ });
    await user.selectOptions(screen.getByLabelText("Активность"), "false");
    await waitFor(() =>
      expect(adminCatalogApiMock.getAdminCategories).toHaveBeenLastCalledWith({ isActive: false }),
    );
    expect(await screen.findByRole("button", { name: /Силовые кабели/ })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Разъемы/ })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /Силовые кабели/ }));
    await screen.findByDisplayValue("Силовые кабели");

    const parentSelect = screen.getByLabelText("Родительская категория");
    const moveSelect = screen.getByLabelText("Новый родитель");
    expect(within(parentSelect).getByRole("option", { name: "Кабели" })).toBeInTheDocument();
    expect(within(parentSelect).getByRole("option", { name: "Разъемы" })).toBeInTheDocument();
    expect(within(moveSelect).getByRole("option", { name: "Кабели" })).toBeInTheDocument();
    expect(within(moveSelect).getByRole("option", { name: "Разъемы" })).toBeInTheDocument();
    expect(within(parentSelect).queryByRole("option", { name: "Силовые кабели" })).not.toBeInTheDocument();
  });

  it("loads selected category details into the form", async () => {
    const user = userEvent.setup();
    adminCatalogApiMock.getAdminCategory.mockResolvedValueOnce(childDetail);
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Силовые кабели/ }));

    expect(adminCatalogApiMock.getAdminCategory).toHaveBeenCalledWith("cat-child");
    expect(await screen.findByDisplayValue("Силовые кабели")).toBeInTheDocument();
    expect(screen.getByDisplayValue("silovye-kabeli")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Описание силовых кабелей")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Силовые кабели SEO")).toBeInTheDocument();
  });

  it("creates a category with full catalog and SEO payload plus CSRF token", async () => {
    const user = userEvent.setup();
    await renderManager();

    await user.click(screen.getByRole("button", { name: "Новая категория" }));
    await user.type(screen.getByLabelText("Название"), "Муфты");
    await user.type(screen.getByLabelText("Slug"), "mufty");
    await user.selectOptions(screen.getByLabelText("Родительская категория"), "cat-root");
    await user.type(screen.getByLabelText("Описание"), "Описание муфт");
    await user.type(screen.getByLabelText("H1"), "Муфты для кабеля");
    await user.type(screen.getByLabelText("SEO title"), "Муфты SEO");
    await user.type(screen.getByLabelText("SEO description"), "SEO описание муфт");
    await user.clear(screen.getByLabelText("Сортировка"));
    await user.type(screen.getByLabelText("Сортировка"), "30");
    await user.click(screen.getByLabelText("Активна"));
    await user.click(screen.getByLabelText("Показывать в меню"));
    await user.click(screen.getByRole("button", { name: "Создать" }));

    expect(adminCatalogApiMock.createAdminCategory).toHaveBeenCalledWith(
      {
        name: "Муфты",
        slug: "mufty",
        parentId: "cat-root",
        description: "Описание муфт",
        h1: "Муфты для кабеля",
        seoTitle: "Муфты SEO",
        seoDescription: "SEO описание муфт",
        sortOrder: 30,
        isActive: false,
        isVisibleInMenu: false,
      },
      "csrf-token",
    );
  });

  it("updates a selected category with full catalog and SEO payload plus CSRF token", async () => {
    const user = userEvent.setup();
    adminCatalogApiMock.getAdminCategory.mockResolvedValueOnce(childDetail);
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Силовые кабели/ }));
    await screen.findByDisplayValue("Силовые кабели");
    await user.clear(screen.getByLabelText("Название"));
    await user.type(screen.getByLabelText("Название"), "Кабели силовые обновленные");
    await user.selectOptions(screen.getByLabelText("Родительская категория"), "");
    await user.click(screen.getByLabelText("Активна"));
    await user.click(screen.getByRole("button", { name: "Сохранить" }));

    expect(adminCatalogApiMock.updateAdminCategory).toHaveBeenCalledWith(
      "cat-child",
      expect.objectContaining({
        name: "Кабели силовые обновленные",
        slug: "silovye-kabeli",
        parentId: null,
        description: "Описание силовых кабелей",
        h1: "Купить силовые кабели",
        seoTitle: "Силовые кабели SEO",
        seoDescription: "Описание SEO силовых кабелей",
        sortOrder: 20,
        isActive: true,
        isVisibleInMenu: false,
      }),
      "csrf-token",
    );
  });

  it("deletes selected category with CSRF token and refreshes the list", async () => {
    const user = userEvent.setup();
    adminCatalogApiMock.getAdminCategory.mockResolvedValueOnce(childDetail);
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Силовые кабели/ }));
    await screen.findByDisplayValue("Силовые кабели");
    await user.click(screen.getByRole("button", { name: "Удалить" }));

    expect(adminCatalogApiMock.deleteAdminCategory).toHaveBeenCalledWith("cat-child", "csrf-token");
    await waitFor(() => expect(adminCatalogApiMock.getAdminCategories).toHaveBeenCalledTimes(4));
  });

  it("calls dedicated move and sort endpoints", async () => {
    const user = userEvent.setup();
    adminCatalogApiMock.getAdminCategory.mockResolvedValueOnce(childDetail);
    adminCatalogApiMock.moveAdminCategory.mockResolvedValueOnce({ ...childDetail, parentId: null });
    adminCatalogApiMock.sortAdminCategory.mockResolvedValueOnce({ ...childDetail, sortOrder: 5 });
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Силовые кабели/ }));
    await screen.findByDisplayValue("Силовые кабели");
    await user.selectOptions(screen.getByLabelText("Новый родитель"), "");
    await user.click(screen.getByRole("button", { name: "Переместить" }));
    await user.clear(screen.getByLabelText("Новый порядок"));
    await user.type(screen.getByLabelText("Новый порядок"), "5");
    await user.click(screen.getByRole("button", { name: "Обновить порядок" }));

    expect(adminCatalogApiMock.moveAdminCategory).toHaveBeenCalledWith("cat-child", null, "csrf-token");
    expect(adminCatalogApiMock.sortAdminCategory).toHaveBeenCalledWith("cat-child", 5, "csrf-token");
  });

  it("shows API errors as alerts", async () => {
    const user = userEvent.setup();
    adminCatalogApiMock.getAdminCategories.mockRejectedValueOnce(
      new ApiClientError(500, { code: "catalog.error", message: "Не удалось загрузить категории." }),
    );

    render(<AdminCategoryManager csrfToken="csrf-token" />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Не удалось загрузить категории.");
    adminCatalogApiMock.getAdminCategories.mockResolvedValue(listResponse());
    adminCatalogApiMock.createAdminCategory.mockRejectedValueOnce(
      new ApiClientError(400, { code: "catalog.validation", message: "Slug уже используется." }),
    );
    await user.click(screen.getByRole("button", { name: "Новая категория" }));
    await user.type(screen.getByLabelText("Название"), "Кабели");
    await user.type(screen.getByLabelText("Slug"), "kabeli");
    await user.click(screen.getByRole("button", { name: "Создать" }));

    const alerts = await screen.findAllByRole("alert");
    expect(within(alerts[alerts.length - 1]).getByText("Slug уже используется.")).toBeInTheDocument();
  });
});
