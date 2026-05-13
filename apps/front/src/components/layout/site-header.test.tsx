import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactElement } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "@/components/auth/auth-provider";
import type { AuthSession } from "@/lib/api/auth";
import { SiteHeader } from "./site-header";

const authApiMock = vi.hoisted(() => ({
  getMe: vi.fn(),
  logout: vi.fn(),
}));

vi.mock("@/lib/api/auth", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/auth")>();

  return {
    ...actual,
    getMe: authApiMock.getMe,
    logout: authApiMock.logout,
  };
});

const customerSession: AuthSession = {
  csrfToken: "customer-csrf-token",
  user: {
    id: "customer-id",
    name: "Иван Петров",
    email: "ivan@example.com",
    phone: "+79000000000",
    role: "customer",
  },
};

const sellerSession: AuthSession = {
  csrfToken: "seller-csrf-token",
  user: {
    id: "seller-id",
    name: "Мария Селлер",
    email: "seller@example.com",
    phone: "+79000000001",
    role: "seller",
  },
};

const adminSession: AuthSession = {
  csrfToken: "admin-csrf-token",
  user: {
    id: "admin-id",
    name: "Анна Админ",
    email: "admin@example.com",
    phone: "+79000000002",
    role: "admin",
  },
};

function renderWithProviders(ui: ReactElement) {
  return render(<AuthProvider>{ui}</AuthProvider>);
}

describe("SiteHeader", () => {
  afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it("renders anonymous request-oriented navigation", async () => {
    authApiMock.getMe.mockRejectedValue({ code: "auth.unauthorized", message: "Unauthorized" });

    renderWithProviders(<SiteHeader />);

    await waitFor(() => expect(authApiMock.getMe).toHaveBeenCalledTimes(1));
    expect(screen.getByRole("link", { name: "LineCom" })).toHaveAttribute("href", "/");
    expect(screen.getByRole("img", { name: "LineCom - кабель и оптоволокно" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Главная" })).toHaveAttribute("href", "/");
    expect(screen.getByRole("link", { name: "Каталог" })).toHaveAttribute("href", "/catalog");
    expect(screen.getByRole("link", { name: "О нас" })).toHaveAttribute("href", "/about");
    expect(screen.getByRole("link", { name: "Доставка" })).toHaveAttribute("href", "/delivery");
    expect(screen.getByRole("link", { name: "Заявка" })).toHaveAttribute("href", "/request");
    expect(screen.queryByRole("link", { name: "Мои заявки" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Профиль" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Заявки клиентов" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Главная админки" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Каталог админки" })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Включить темную тему" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Войти" })).toHaveAttribute("href", "/auth/login");
    expect(screen.queryByRole("button", { name: "Выйти" })).not.toBeInTheDocument();
  });

  it("shows restored customer session and hides admin navigation", async () => {
    authApiMock.getMe.mockResolvedValue(customerSession);

    renderWithProviders(<SiteHeader />);

    expect(await screen.findByText("Иван Петров")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Мои заявки" })).toHaveAttribute("href", "/account/requests");
    expect(screen.getByRole("link", { name: "Профиль" })).toHaveAttribute("href", "/account/profile");
    expect(screen.queryByRole("link", { name: "Заявки клиентов" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Главная админки" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Каталог админки" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Войти" })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Выйти" })).toBeInTheDocument();
  });

  it.each([
    ["seller", sellerSession],
    ["admin", adminSession],
  ] satisfies Array<[string, AuthSession]>)("shows admin navigation for %s session", async (_, session) => {
    authApiMock.getMe.mockResolvedValue(session);

    renderWithProviders(<SiteHeader />);

    expect(await screen.findByText(session.user.name)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Заявки клиентов" })).toHaveAttribute("href", "/admin/requests");
    expect(screen.getByRole("link", { name: "Каталог админки" })).toHaveAttribute("href", "/admin/catalog");
    expect(screen.getByRole("link", { name: "Главная админки" })).toHaveAttribute("href", "/admin/homepage");
    expect(screen.getByRole("link", { name: "Мои заявки" })).toHaveAttribute("href", "/account/requests");
    expect(screen.getByRole("link", { name: "Профиль" })).toHaveAttribute("href", "/account/profile");
    expect(screen.queryByRole("link", { name: "Войти" })).not.toBeInTheDocument();
  });

  it("logs out with csrf token and returns to anonymous header", async () => {
    authApiMock.getMe.mockResolvedValue(customerSession);
    authApiMock.logout.mockResolvedValue(undefined);
    const user = userEvent.setup();

    renderWithProviders(<SiteHeader />);

    await screen.findByText("Иван Петров");
    await user.click(screen.getByRole("button", { name: "Выйти" }));

    expect(authApiMock.logout).toHaveBeenCalledWith(customerSession.csrfToken);
    expect(await screen.findByRole("link", { name: "Войти" })).toBeInTheDocument();
    expect(screen.queryByText("Иван Петров")).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Мои заявки" })).not.toBeInTheDocument();
  });

  it("toggles the mobile menu from the logo", async () => {
    authApiMock.getMe.mockRejectedValue({ code: "auth.unauthorized", message: "Unauthorized" });
    vi.stubGlobal("matchMedia", vi.fn().mockReturnValue({
      matches: true,
      media: "(max-width: 860px)",
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    }));

    const user = userEvent.setup();
    renderWithProviders(<SiteHeader />);

    await waitFor(() => expect(authApiMock.getMe).toHaveBeenCalledTimes(1));
    const brand = screen.getByRole("link", { name: "LineCom" });
    expect(brand).toHaveAttribute("aria-expanded", "false");

    await user.click(brand);
    expect(brand).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByRole("link", { name: "Главная" })).toHaveAttribute("href", "/");

    const catalogLink = screen.getByRole("link", { name: "Каталог" });
    catalogLink.addEventListener("click", (event) => event.preventDefault());

    await user.click(catalogLink);
    expect(brand).toHaveAttribute("aria-expanded", "false");
  });
});
