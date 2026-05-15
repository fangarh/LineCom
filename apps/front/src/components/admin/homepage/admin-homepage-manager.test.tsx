import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AdminHomepageManager } from "./admin-homepage-manager";
import type { AdminHomepageSectionsResponse } from "@/lib/api/admin-homepage";

const adminHomepageApiMock = vi.hoisted(() => ({
  getAdminHomepageSections: vi.fn(),
  updateAdminHomepageSection: vi.fn(),
  addAdminHomepageSectionItem: vi.fn(),
  updateAdminHomepageSectionItemOrder: vi.fn(),
  updateAdminHomepageSectionItem: vi.fn(),
  deleteAdminHomepageSectionItem: vi.fn(),
}));

const adminCatalogApiMock = vi.hoisted(() => ({
  getAdminProducts: vi.fn(),
  getAdminCategories: vi.fn(),
}));

vi.mock("@/lib/api/admin-homepage", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/admin-homepage")>();
  return {
    ...actual,
    getAdminHomepageSections: adminHomepageApiMock.getAdminHomepageSections,
    updateAdminHomepageSection: adminHomepageApiMock.updateAdminHomepageSection,
    addAdminHomepageSectionItem: adminHomepageApiMock.addAdminHomepageSectionItem,
    updateAdminHomepageSectionItemOrder: adminHomepageApiMock.updateAdminHomepageSectionItemOrder,
    updateAdminHomepageSectionItem: adminHomepageApiMock.updateAdminHomepageSectionItem,
    deleteAdminHomepageSectionItem: adminHomepageApiMock.deleteAdminHomepageSectionItem,
  };
});

vi.mock("@/lib/api/admin-catalog", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/admin-catalog")>();
  return {
    ...actual,
    getAdminProducts: adminCatalogApiMock.getAdminProducts,
    getAdminCategories: adminCatalogApiMock.getAdminCategories,
  };
});

function deferred<T>() {
  let resolve!: (value: T) => void;
  let reject!: (reason?: unknown) => void;
  const promise = new Promise<T>((promiseResolve, promiseReject) => {
    resolve = promiseResolve;
    reject = promiseReject;
  });

  return { promise, resolve, reject };
}

function homepageSectionsResponse(): AdminHomepageSectionsResponse {
  return {
    sections: [
      {
        id: "section-products",
        code: "hero_products",
        title: "Главные товары",
        type: "product_list",
        itemLimit: 4,
        sortOrder: 10,
        isActive: true,
        items: [
          {
            id: "item-product-1",
            productId: "product-1",
            categoryId: null,
            name: "product_unpublished",
            slug: "product-unpublished",
            secondaryText: "Артикул LC-001",
            sortOrder: 20,
            isActive: true,
            visibilityStatus: "product_unpublished",
          },
        ],
      },
      {
        id: "section-categories",
        code: "popular_categories",
        title: "Популярные категории",
        type: "category_list",
        itemLimit: 6,
        sortOrder: 20,
        isActive: false,
        items: [],
      },
    ],
  };
}

describe("AdminHomepageManager", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    adminHomepageApiMock.getAdminHomepageSections.mockResolvedValue(homepageSectionsResponse());
    adminCatalogApiMock.getAdminProducts.mockResolvedValue({
      items: [
        {
          id: "product-1",
          name: "Кабель ВВГнг",
          slug: "kabel-vvgng",
          sku: "LC-001",
          externalId: null,
          categoryName: "Кабели",
          categorySlug: "kabeli",
          brandName: null,
          publishStatus: "published",
          isActive: true,
          availabilityStatus: "in_stock",
          sortOrder: 10,
          readiness: { canPublish: true, issues: [] },
        },
      ],
      page: 1,
      pageSize: 10,
      totalItems: 1,
      totalPages: 1,
    });
    adminCatalogApiMock.getAdminCategories.mockResolvedValue({
      items: [
        {
          id: "category-1",
          parentId: null,
          name: "Кабели",
          slug: "kabeli",
          sortOrder: 10,
          isActive: true,
          isVisibleInMenu: true,
          productsCount: 4,
          childrenCount: 1,
        },
      ],
      page: 1,
      pageSize: 10,
      totalItems: 1,
      totalPages: 1,
    });
    adminHomepageApiMock.updateAdminHomepageSection.mockImplementation((sectionId: string) =>
      Promise.resolve(homepageSectionsResponse().sections.find((section) => section.id === sectionId)),
    );
    adminHomepageApiMock.addAdminHomepageSectionItem.mockResolvedValue({
      id: "item-product-2",
      productId: "product-2",
      categoryId: null,
      name: "Новый товар",
      slug: "new-product",
      secondaryText: null,
      sortOrder: 30,
      isActive: true,
      visibilityStatus: "visible",
    });
    adminHomepageApiMock.updateAdminHomepageSectionItemOrder.mockResolvedValue(homepageSectionsResponse());
    adminHomepageApiMock.updateAdminHomepageSectionItem.mockResolvedValue({
      ...homepageSectionsResponse().sections[0].items[0],
      isActive: false,
    });
    adminHomepageApiMock.deleteAdminHomepageSectionItem.mockResolvedValue(undefined);
  });

  it("renders sections, item visibility statuses, and mutation controls", async () => {
    render(<AdminHomepageManager csrfToken="csrf" />);

    expect(await screen.findByRole("heading", { name: "Главная страница" })).toBeInTheDocument();
    expect(screen.getByText("hero_products")).toBeInTheDocument();
    expect(screen.getByText("product_unpublished")).toBeInTheDocument();
    expect(screen.getByLabelText("Поиск товара")).toBeInTheDocument();
    expect(screen.queryByLabelText("UUID товара")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Сохранить секцию" })).toBeEnabled();
  });

  it("saves section changes with csrf token", async () => {
    const user = userEvent.setup();
    render(<AdminHomepageManager csrfToken="csrf" />);

    const titleInput = await screen.findByLabelText("Заголовок секции");
    await user.clear(titleInput);
    await user.type(titleInput, "Подборка кабеля");
    await user.click(screen.getByLabelText("Секция активна"));
    await user.click(screen.getByRole("button", { name: "Сохранить секцию" }));

    await waitFor(() =>
      expect(adminHomepageApiMock.updateAdminHomepageSection).toHaveBeenCalledWith(
        "section-products",
        {
          title: "Подборка кабеля",
          itemLimit: 4,
          sortOrder: 10,
          isActive: false,
        },
        "csrf",
      ),
    );
    expect(adminHomepageApiMock.getAdminHomepageSections).toHaveBeenCalledTimes(2);
  });

  it("adds product item with csrf token", async () => {
    const user = userEvent.setup();
    adminCatalogApiMock.getAdminProducts.mockResolvedValue({
      items: [
        {
          id: "product-2",
          name: "Патч-корд LC",
          slug: "patch-cord-lc",
          sku: "LC-002",
          externalId: null,
          categoryName: "Кабели",
          categorySlug: "kabeli",
          brandName: null,
          publishStatus: "published",
          isActive: true,
          availabilityStatus: "in_stock",
          sortOrder: 20,
          readiness: { canPublish: true, issues: [] },
        },
      ],
      page: 1,
      pageSize: 10,
      totalItems: 1,
      totalPages: 1,
    });
    render(<AdminHomepageManager csrfToken="csrf" />);

    await user.type(await screen.findByLabelText("Поиск товара"), "кабель");
    await waitFor(() =>
      expect(adminCatalogApiMock.getAdminProducts).toHaveBeenCalledWith({ search: "кабель", page: 1, pageSize: 10 }),
    );
    await user.click(await screen.findByRole("button", { name: /Добавить Патч-корд LC/ }));

    await waitFor(() =>
      expect(adminHomepageApiMock.addAdminHomepageSectionItem).toHaveBeenCalledWith(
        "section-products",
        { productId: "product-2", categoryId: null, sortOrder: null, isActive: true },
        "csrf",
      ),
    );
    expect(adminHomepageApiMock.getAdminHomepageSections).toHaveBeenCalledTimes(2);
    expect(screen.queryByLabelText("UUID товара")).not.toBeInTheDocument();
  });

  it("does not post an item that is already present in the active section", async () => {
    const user = userEvent.setup();
    render(<AdminHomepageManager csrfToken="csrf" />);

    await user.type(await screen.findByLabelText("Поиск товара"), "кабель");
    const addButton = await screen.findByRole("button", { name: /Уже добавлен/ });

    expect(addButton).toBeDisabled();
    await user.click(addButton);

    expect(adminHomepageApiMock.addAdminHomepageSectionItem).not.toHaveBeenCalled();
  });

  it("keeps long product names out of the add button text while preserving accessible context", async () => {
    const user = userEvent.setup();
    const longProductName = "Кабель силовой бронированный негорючий морозостойкий для сложных монтажных трасс";
    adminCatalogApiMock.getAdminProducts.mockResolvedValue({
      items: [
        {
          id: "product-long",
          name: longProductName,
          slug: "long-power-cable",
          sku: "VERY-LONG-SKU-001",
          externalId: "ERP-LONG-001",
          categoryName: "Кабели для промышленных объектов",
          categorySlug: "industrial-cables",
          brandName: null,
          publishStatus: "published",
          isActive: true,
          availabilityStatus: "in_stock",
          sortOrder: 10,
          readiness: { canPublish: true, issues: [] },
        },
      ],
      page: 1,
      pageSize: 10,
      totalItems: 1,
      totalPages: 1,
    });
    render(<AdminHomepageManager csrfToken="csrf" />);

    fireEvent.change(await screen.findByLabelText("Поиск товара"), { target: { value: "бронированный" } });
    const addButton = await screen.findByRole("button", { name: `Добавить ${longProductName}` });

    expect(addButton).toHaveTextContent(/^Добавить$/);
    expect(addButton).not.toHaveTextContent(longProductName);
  });

  it("adds category item from category search with csrf token", async () => {
    const user = userEvent.setup();
    render(<AdminHomepageManager csrfToken="csrf" />);

    await user.click(await screen.findByRole("button", { name: /Популярные категории/ }));
    await user.type(await screen.findByLabelText("Поиск категории"), "кабели");
    await waitFor(() =>
      expect(adminCatalogApiMock.getAdminCategories).toHaveBeenCalledWith({ search: "кабели", page: 1, pageSize: 10 }),
    );
    await user.click(await screen.findByRole("button", { name: /Добавить Кабели/ }));

    await waitFor(() =>
      expect(adminHomepageApiMock.addAdminHomepageSectionItem).toHaveBeenCalledWith(
        "section-categories",
        { productId: null, categoryId: "category-1", sortOrder: null, isActive: true },
        "csrf",
      ),
    );
    expect(screen.queryByLabelText("UUID категории")).not.toBeInTheDocument();
  });

  it("does not submit duplicate section saves while a mutation is pending", async () => {
    const updateRequest = deferred<ReturnType<typeof homepageSectionsResponse>["sections"][number]>();
    adminHomepageApiMock.updateAdminHomepageSection.mockReturnValueOnce(updateRequest.promise);
    render(<AdminHomepageManager csrfToken="csrf" />);

    const saveButton = await screen.findByRole("button", { name: "Сохранить секцию" });
    const form = saveButton.closest("form");
    expect(form).not.toBeNull();

    fireEvent.submit(form!);
    await waitFor(() => expect(adminHomepageApiMock.updateAdminHomepageSection).toHaveBeenCalledTimes(1));
    fireEvent.submit(form!);

    expect(adminHomepageApiMock.updateAdminHomepageSection).toHaveBeenCalledTimes(1);
    updateRequest.resolve(homepageSectionsResponse().sections[0]);
  });

  it("does not submit duplicate item adds while a mutation is pending", async () => {
    const user = userEvent.setup();
    const addRequest = deferred<ReturnType<typeof homepageSectionsResponse>["sections"][number]["items"][number]>();
    adminHomepageApiMock.addAdminHomepageSectionItem.mockReturnValueOnce(addRequest.promise);
    adminCatalogApiMock.getAdminProducts.mockResolvedValue({
      items: [
        {
          id: "product-2",
          name: "Патч-корд LC",
          slug: "patch-cord-lc",
          sku: "LC-002",
          externalId: null,
          categoryName: "Кабели",
          categorySlug: "kabeli",
          brandName: null,
          publishStatus: "published",
          isActive: true,
          availabilityStatus: "in_stock",
          sortOrder: 20,
          readiness: { canPublish: true, issues: [] },
        },
      ],
      page: 1,
      pageSize: 10,
      totalItems: 1,
      totalPages: 1,
    });
    render(<AdminHomepageManager csrfToken="csrf" />);

    await user.type(await screen.findByLabelText("Поиск товара"), "кабель");
    const addButton = await screen.findByRole("button", { name: /Добавить Патч-корд LC/ });

    await user.click(addButton);
    await waitFor(() => expect(adminHomepageApiMock.addAdminHomepageSectionItem).toHaveBeenCalledTimes(1));
    await user.click(addButton);

    expect(adminHomepageApiMock.addAdminHomepageSectionItem).toHaveBeenCalledTimes(1);
    addRequest.resolve(homepageSectionsResponse().sections[0].items[0]);
  });
});
