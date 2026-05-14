import { act, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiClientError } from "@/lib/api/errors";
import { AdminBrandManager } from "./admin-brand-manager";
import type { AdminBrandDetail, AdminBrandListItem, AdminBrandListResponse, AdminBrandLogo } from "@/lib/api/admin-catalog";

const adminCatalogApiMock = vi.hoisted(() => ({
  getAdminBrands: vi.fn(),
  getAdminBrand: vi.fn(),
  createAdminBrand: vi.fn(),
  updateAdminBrand: vi.fn(),
  deleteAdminBrand: vi.fn(),
  uploadAdminBrandLogo: vi.fn(),
  deleteAdminBrandLogo: vi.fn(),
}));

vi.mock("@/lib/api/admin-catalog", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/admin-catalog")>();
  return {
    ...actual,
    getAdminBrands: adminCatalogApiMock.getAdminBrands,
    getAdminBrand: adminCatalogApiMock.getAdminBrand,
    createAdminBrand: adminCatalogApiMock.createAdminBrand,
    updateAdminBrand: adminCatalogApiMock.updateAdminBrand,
    deleteAdminBrand: adminCatalogApiMock.deleteAdminBrand,
    uploadAdminBrandLogo: adminCatalogApiMock.uploadAdminBrandLogo,
    deleteAdminBrandLogo: adminCatalogApiMock.deleteAdminBrandLogo,
  };
});

const activeBrand: AdminBrandListItem = {
  id: "brand-active",
  name: "Кабельный завод",
  slug: "kabelnyy-zavod",
  isActive: true,
  productsCount: 7,
};

const inactiveBrand: AdminBrandListItem = {
  id: "brand-inactive",
  name: "ПромСвет",
  slug: "promsvet",
  isActive: false,
  productsCount: 0,
};

const activeBrandDetail: AdminBrandDetail = {
  ...activeBrand,
  description: "Производитель кабельной продукции",
  seoTitle: "Кабельный завод SEO",
  seoDescription: "SEO описание бренда",
  logoFileId: null,
};

const inactiveBrandDetail: AdminBrandDetail = {
  ...inactiveBrand,
  description: "Светотехнический бренд",
  seoTitle: "ПромСвет SEO",
  seoDescription: "Описание ПромСвет",
  logoFileId: "stored-logo",
};

const uploadedLogo: AdminBrandLogo = {
  storedFileId: "stored-logo",
  url: "/files/brands/promsvet.png",
  originalFileName: "promsvet.png",
  contentType: "image/png",
  sizeBytes: 1024,
  checksum: "checksum",
};

function listResponse(items: AdminBrandListItem[] = [activeBrand, inactiveBrand]): AdminBrandListResponse {
  return {
    items,
    page: 1,
    pageSize: 50,
    totalItems: items.length,
    totalPages: 1,
  };
}

function mockDefaultApi() {
  adminCatalogApiMock.getAdminBrands.mockResolvedValue(listResponse());
  adminCatalogApiMock.getAdminBrand.mockResolvedValue(activeBrandDetail);
  adminCatalogApiMock.createAdminBrand.mockResolvedValue(activeBrandDetail);
  adminCatalogApiMock.updateAdminBrand.mockResolvedValue(activeBrandDetail);
  adminCatalogApiMock.deleteAdminBrand.mockResolvedValue(undefined);
  adminCatalogApiMock.uploadAdminBrandLogo.mockResolvedValue(uploadedLogo);
  adminCatalogApiMock.deleteAdminBrandLogo.mockResolvedValue(undefined);
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
  render(<AdminBrandManager csrfToken={csrfToken} />);

  await screen.findByRole("button", { name: /Кабельный завод/ });
}

describe("AdminBrandManager", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockDefaultApi();
  });

  it("загружает список и фильтрует бренды по поиску и активности", async () => {
    const user = userEvent.setup();
    await renderManager();

    expect(adminCatalogApiMock.getAdminBrands).toHaveBeenCalledWith({});

    await user.type(screen.getByLabelText("Поиск"), "кабель");
    await user.selectOptions(screen.getByLabelText("Активность"), "true");

    expect(adminCatalogApiMock.getAdminBrands).toHaveBeenLastCalledWith({
      search: "кабель",
      isActive: true,
    });
  });

  it("загружает выбранный бренд в редактор", async () => {
    const user = userEvent.setup();
    adminCatalogApiMock.getAdminBrand.mockResolvedValueOnce(inactiveBrandDetail);
    await renderManager();

    await user.click(screen.getByRole("button", { name: /ПромСвет/ }));

    expect(adminCatalogApiMock.getAdminBrand).toHaveBeenCalledWith("brand-inactive");
    expect(await screen.findByDisplayValue("ПромСвет")).toBeInTheDocument();
    expect(screen.getByDisplayValue("promsvet")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Светотехнический бренд")).toBeInTheDocument();
    expect(screen.getByDisplayValue("ПромСвет SEO")).toBeInTheDocument();
  });

  it("создает бренд с описанием, SEO-полями, активностью и CSRF-токеном", async () => {
    const user = userEvent.setup();
    await renderManager();

    await user.click(screen.getByRole("button", { name: "Новый бренд" }));
    await user.type(screen.getByLabelText("Название"), "ЭлектроКомплект");
    await user.type(screen.getByLabelText("Слаг"), "elektrokomplekt");
    await user.type(screen.getByLabelText("Описание"), "Поставщик электромонтажной продукции");
    await user.type(screen.getByLabelText("SEO-заголовок"), "ЭлектроКомплект SEO");
    await user.type(screen.getByLabelText("SEO-описание"), "SEO описание ЭлектроКомплект");
    await user.click(screen.getByLabelText("Активен"));
    await user.click(screen.getByRole("button", { name: "Создать" }));

    expect(adminCatalogApiMock.createAdminBrand).toHaveBeenCalledWith(
      {
        name: "ЭлектроКомплект",
        slug: "elektrokomplekt",
        description: "Поставщик электромонтажной продукции",
        seoTitle: "ЭлектроКомплект SEO",
        seoDescription: "SEO описание ЭлектроКомплект",
        isActive: false,
      },
      "csrf-token",
    );
  });

  it("автозаполняет слаг для нового бренда и не меняет слаг выбранного бренда без явного действия", async () => {
    const user = userEvent.setup();
    await renderManager();

    await user.click(screen.getByRole("button", { name: "Новый бренд" }));
    await user.type(screen.getByLabelText("Название"), "ЭлектроКомплект");
    expect(screen.getByLabelText("Слаг")).toHaveValue("elektrokomplekt");

    await user.clear(screen.getByLabelText("Слаг"));
    await user.type(screen.getByLabelText("Слаг"), "manual-brand");
    await user.clear(screen.getByLabelText("Название"));
    await user.type(screen.getByLabelText("Название"), "ПромСвет новый");
    expect(screen.getByLabelText("Слаг")).toHaveValue("manual-brand");

    await user.click(screen.getByRole("button", { name: "Сгенерировать заново" }));
    expect(screen.getByLabelText("Слаг")).toHaveValue("promsvet-novyy");

    adminCatalogApiMock.getAdminBrand.mockResolvedValueOnce(inactiveBrandDetail);
    await user.click(screen.getByRole("button", { name: /ПромСвет/ }));
    await screen.findByDisplayValue("ПромСвет");
    await user.clear(screen.getByLabelText("Название"));
    await user.type(screen.getByLabelText("Название"), "ПромСвет обновленный");
    expect(screen.getByLabelText("Слаг")).toHaveValue("promsvet");
  });

  it("обновляет выбранный бренд с описанием, SEO-полями, активностью и CSRF-токеном", async () => {
    const user = userEvent.setup();
    adminCatalogApiMock.getAdminBrand.mockResolvedValueOnce(inactiveBrandDetail);
    await renderManager();

    await user.click(screen.getByRole("button", { name: /ПромСвет/ }));
    await screen.findByDisplayValue("ПромСвет");
    await user.clear(screen.getByLabelText("Название"));
    await user.type(screen.getByLabelText("Название"), "ПромСвет обновленный");
    await user.clear(screen.getByLabelText("SEO-заголовок"));
    await user.type(screen.getByLabelText("SEO-заголовок"), "ПромСвет обновленный SEO");
    await user.click(screen.getByLabelText("Активен"));
    await user.click(screen.getByRole("button", { name: "Сохранить" }));

    expect(adminCatalogApiMock.updateAdminBrand).toHaveBeenCalledWith(
      "brand-inactive",
      {
        name: "ПромСвет обновленный",
        slug: "promsvet",
        description: "Светотехнический бренд",
        seoTitle: "ПромСвет обновленный SEO",
        seoDescription: "Описание ПромСвет",
        isActive: true,
      },
      "csrf-token",
    );
  });

  it("показывает ошибку удаления, если бренд используется товарами", async () => {
    const user = userEvent.setup();
    adminCatalogApiMock.getAdminBrand.mockResolvedValueOnce(activeBrandDetail);
    adminCatalogApiMock.deleteAdminBrand.mockRejectedValueOnce(
      new ApiClientError(409, {
        code: "catalog.brand_in_use",
        message: "Нельзя удалить бренд, который используется в товарах.",
      }),
    );
    await renderManager();

    await user.click(screen.getByRole("button", { name: /Кабельный завод/ }));
    await screen.findByDisplayValue("Кабельный завод");
    await user.click(screen.getByRole("button", { name: "Удалить" }));

    expect(adminCatalogApiMock.deleteAdminBrand).toHaveBeenCalledWith("brand-active", "csrf-token");
    expect(await screen.findByRole("alert")).toHaveTextContent("Нельзя удалить бренд, который используется в товарах.");
  });

  it("загружает логотип выбранного бренда через отдельный endpoint и показывает preview с alt по имени бренда", async () => {
    const user = userEvent.setup();
    adminCatalogApiMock.getAdminBrand.mockResolvedValueOnce(inactiveBrandDetail);
    await renderManager();

    await user.click(screen.getByRole("button", { name: /ПромСвет/ }));
    await screen.findByDisplayValue("ПромСвет");
    const file = new File(["logo"], "promsvet.png", { type: "image/png" });

    await user.upload(screen.getByLabelText("Файл логотипа"), file);
    await user.click(screen.getByRole("button", { name: "Заменить логотип" }));

    expect(adminCatalogApiMock.uploadAdminBrandLogo).toHaveBeenCalledWith("brand-inactive", file, "csrf-token");
    const preview = await screen.findByRole("img", { name: "Логотип ПромСвет" });
    expect(preview).toHaveAttribute("src", uploadedLogo.url);
    expect(preview).toHaveAttribute("width", "240");
    expect(preview).toHaveAttribute("height", "96");
  });

  it("игнорирует результат загрузки логотипа, если пользователь выбрал другой бренд до ответа", async () => {
    const user = userEvent.setup();
    const logoRequest = deferred<AdminBrandLogo>();
    const activeDetailRequest = deferred<AdminBrandDetail>();
    adminCatalogApiMock.getAdminBrand
      .mockResolvedValueOnce(inactiveBrandDetail)
      .mockReturnValueOnce(activeDetailRequest.promise);
    adminCatalogApiMock.uploadAdminBrandLogo.mockReturnValueOnce(logoRequest.promise);
    await renderManager();

    await user.click(screen.getByRole("button", { name: /ПромСвет/ }));
    await screen.findByDisplayValue("ПромСвет");
    const file = new File(["logo"], "promsvet.png", { type: "image/png" });
    await user.upload(screen.getByLabelText("Файл логотипа"), file);
    await user.click(screen.getByRole("button", { name: "Заменить логотип" }));
    await waitFor(() => expect(adminCatalogApiMock.uploadAdminBrandLogo).toHaveBeenCalledWith("brand-inactive", file, "csrf-token"));

    await user.click(screen.getByRole("button", { name: /Кабельный завод/ }));
    await waitFor(() => expect(adminCatalogApiMock.getAdminBrand).toHaveBeenCalledWith("brand-active"));

    await act(async () => {
      logoRequest.resolve(uploadedLogo);
    });

    expect(screen.queryByRole("img", { name: /Логотип/ })).not.toBeInTheDocument();
    expect(screen.queryByText("Логотип загружен.")).not.toBeInTheDocument();

    await act(async () => {
      activeDetailRequest.resolve(activeBrandDetail);
    });
    expect(await screen.findByDisplayValue("Кабельный завод")).toBeInTheDocument();
    expect(screen.getByText("Логотип пока не загружен.")).toBeInTheDocument();
  });

  it("очищает DOM input после успешной загрузки логотипа", async () => {
    const user = userEvent.setup();
    adminCatalogApiMock.getAdminBrand.mockResolvedValueOnce(inactiveBrandDetail);
    await renderManager();

    await user.click(screen.getByRole("button", { name: /ПромСвет/ }));
    await screen.findByDisplayValue("ПромСвет");
    const file = new File(["logo"], "promsvet.png", { type: "image/png" });
    const input = screen.getByLabelText("Файл логотипа") as HTMLInputElement;

    await user.upload(input, file);
    expect(input.files).toHaveLength(1);
    await user.click(screen.getByRole("button", { name: "Заменить логотип" }));

    await waitFor(() => expect(input.files).toHaveLength(0));
    expect(input.value).toBe("");
  });

  it("удаляет логотип выбранного бренда через отдельный endpoint и очищает preview", async () => {
    const user = userEvent.setup();
    adminCatalogApiMock.getAdminBrand.mockResolvedValueOnce(inactiveBrandDetail);
    await renderManager();

    await user.click(screen.getByRole("button", { name: /ПромСвет/ }));
    await screen.findByDisplayValue("ПромСвет");
    const file = new File(["logo"], "promsvet.png", { type: "image/png" });
    await user.upload(screen.getByLabelText("Файл логотипа"), file);
    await user.click(screen.getByRole("button", { name: "Заменить логотип" }));
    await screen.findByRole("img", { name: "Логотип ПромСвет" });

    await user.click(screen.getByRole("button", { name: "Удалить логотип" }));

    expect(adminCatalogApiMock.deleteAdminBrandLogo).toHaveBeenCalledWith("brand-inactive", "csrf-token");
    await waitFor(() => expect(screen.queryByRole("img", { name: "Логотип ПромСвет" })).not.toBeInTheDocument());
  });
});
