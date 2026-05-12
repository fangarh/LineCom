import { render, screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "@/components/auth/auth-provider";
import { ApiClientError } from "@/lib/api/errors";
import { CatalogPageClient } from "./catalog-page-client";

const routerMock = vi.hoisted(() => ({
  push: vi.fn(),
}));

const authApiMock = vi.hoisted(() => ({
  getMe: vi.fn(),
}));

const adminCatalogApiMock = vi.hoisted(() => ({
  getAdminProducts: vi.fn(),
  getAdminCategories: vi.fn(),
  getAdminBrands: vi.fn(),
  getAdminCategoryAttributes: vi.fn(),
}));

vi.mock("next/navigation", () => ({
  useRouter: () => routerMock,
}));

vi.mock("@/lib/api/auth", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/auth")>();
  return {
    ...actual,
    getMe: authApiMock.getMe,
  };
});

vi.mock("@/lib/api/admin-catalog", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/admin-catalog")>();
  return {
    ...actual,
    getAdminProducts: adminCatalogApiMock.getAdminProducts,
    getAdminCategories: adminCatalogApiMock.getAdminCategories,
    getAdminBrands: adminCatalogApiMock.getAdminBrands,
    getAdminCategoryAttributes: adminCatalogApiMock.getAdminCategoryAttributes,
  };
});

function renderPage() {
  return render(
    <AuthProvider>
      <CatalogPageClient />
    </AuthProvider>,
  );
}

function expectNoCatalogListCalls() {
  expect(adminCatalogApiMock.getAdminProducts).not.toHaveBeenCalled();
  expect(adminCatalogApiMock.getAdminCategories).not.toHaveBeenCalled();
  expect(adminCatalogApiMock.getAdminBrands).not.toHaveBeenCalled();
  expect(adminCatalogApiMock.getAdminCategoryAttributes).not.toHaveBeenCalled();
}

describe("Admin CatalogPageClient", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    authApiMock.getMe.mockResolvedValue({
      user: {
        id: "11111111-1111-1111-1111-111111111111",
        name: "Анна",
        email: "anna@example.com",
        phone: null,
        role: "seller",
      },
      csrfToken: "csrf",
    });
    adminCatalogApiMock.getAdminProducts.mockResolvedValue({ items: [], page: 1, pageSize: 50, totalItems: 0, totalPages: 1 });
    adminCatalogApiMock.getAdminCategories.mockResolvedValue({ items: [], page: 1, pageSize: 60, totalItems: 0, totalPages: 1 });
    adminCatalogApiMock.getAdminBrands.mockResolvedValue({ items: [], page: 1, pageSize: 60, totalItems: 0, totalPages: 1 });
    adminCatalogApiMock.getAdminCategoryAttributes.mockResolvedValue({ items: [] });
  });

  it("loads catalog shell for seller", async () => {
    renderPage();

    expect(await screen.findByRole("heading", { name: "Каталог" })).toBeInTheDocument();
    const tablist = screen.getByRole("tablist", { name: "Разделы каталога" });
    expect(within(tablist).getByRole("tab", { name: "Товары" })).toBeInTheDocument();
    expect(within(tablist).getByRole("tab", { name: "Категории" })).toBeInTheDocument();
    expect(within(tablist).getByRole("tab", { name: "Бренды" })).toBeInTheDocument();
    expect(within(tablist).getByRole("tab", { name: "Характеристики" })).toBeInTheDocument();
    expect(authApiMock.getMe).toHaveBeenCalledTimes(1);
    expect(adminCatalogApiMock.getAdminProducts).toHaveBeenCalledWith({});
  });

  it("shows forbidden state for customer without catalog list calls", async () => {
    authApiMock.getMe.mockResolvedValue({
      user: {
        id: "22222222-2222-2222-2222-222222222222",
        name: "Иван",
        email: "ivan@example.com",
        phone: null,
        role: "customer",
      },
      csrfToken: "csrf",
    });

    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent("У вас нет доступа к управлению каталогом.");
    expect(screen.queryByRole("tab", { name: "Товары" })).not.toBeInTheDocument();
    expectNoCatalogListCalls();
  });

  it("redirects unauthorized users to login with catalog returnTo", async () => {
    authApiMock.getMe.mockRejectedValue(
      new ApiClientError(401, { code: "auth.unauthorized", message: "Требуется вход." }),
    );

    renderPage();

    await waitFor(() => expect(routerMock.push).toHaveBeenCalledWith("/auth/login?returnTo=%2Fadmin%2Fcatalog"));
    expectNoCatalogListCalls();
  });
});
