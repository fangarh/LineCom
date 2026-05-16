import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "@/components/auth/auth-provider";
import { RequestDraftProvider } from "@/components/request/request-draft-provider";
import { logout } from "@/lib/api/auth";
import type { AuthSession, CurrentUser } from "@/lib/api/auth";
import { SiteHeader } from "./site-header";

vi.mock("@/lib/api/auth", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api/auth")>();
  return {
    ...actual,
    logout: vi.fn(),
  };
});

const logoutMock = vi.mocked(logout);

function buildUser(role: string): CurrentUser {
  return {
    id: `${role}-user`,
    name: `${role} user`,
    email: `${role}@linecom.test`,
    phone: null,
    role,
  };
}

function renderHeader(session: AuthSession | null = null) {
  return render(
    <AuthProvider initialSession={session}>
      <RequestDraftProvider>
        <SiteHeader />
      </RequestDraftProvider>
    </AuthProvider>,
  );
}

describe("SiteHeader", () => {
  afterEach(() => {
    localStorage.clear();
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it("renders request-oriented navigation", () => {
    renderHeader();

    expect(screen.getByRole("link", { name: "LineCom" })).toHaveAttribute("href", "/");
    expect(screen.getByRole("img", { name: "LineCom - кабель и оптоволокно" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Главная" })).toHaveAttribute("href", "/");
    expect(screen.getByRole("link", { name: "Каталог" })).toHaveAttribute("href", "/catalog");
    expect(screen.getByRole("link", { name: "Контакты" })).toHaveAttribute("href", "/contacts");
    expect(screen.getByRole("link", { name: "Доставка" })).toHaveAttribute("href", "/delivery");
    expect(screen.getByRole("link", { name: "Заявка" })).toHaveAttribute("href", "/request");
    expect(screen.queryByRole("link", { name: "Мои заявки" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Профиль" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Администрирование" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Главная админки" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Каталог админки" })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Включить темную тему" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Войти" })).toHaveAttribute("href", "/auth/login");
  });

  it("toggles the mobile menu from the logo", async () => {
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
    renderHeader();

    const brand = screen.getByRole("link", { name: "LineCom" });
    expect(brand).toHaveAttribute("aria-expanded", "false");

    await user.click(brand);
    expect(brand).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByRole("link", { name: "Главная" })).toHaveAttribute("href", "/");

    await user.click(screen.getByRole("link", { name: "Каталог" }));
    expect(brand).toHaveAttribute("aria-expanded", "false");
  });

  it("shows the request draft items count", async () => {
    localStorage.setItem(
      "linecom.requestDraft.v1",
      JSON.stringify({
        customerComment: "",
        items: [
          {
            productId: "11111111-1111-1111-1111-111111111111",
            slug: "u-utp-cat-5e",
            productName: "Кабель U/UTP Cat 5e",
            productSku: "LC-UTP5E",
            saleUnit: { code: "coil", label: "бухта" },
            unitQuantity: "305 м",
            quantity: 2,
            customerComment: "",
          },
        ],
      }),
    );

    renderHeader();

    expect(await screen.findByRole("link", { name: /2/ })).toHaveAttribute("href", "/request");
  });

  it("shows account links without admin links for customers", () => {
    renderHeader({ user: buildUser("customer"), csrfToken: "csrf" });

    expect(screen.getByRole("link", { name: "Профиль" })).toHaveAttribute("href", "/account/profile");
    expect(screen.getByRole("link", { name: "Мои заявки" })).toHaveAttribute("href", "/account/requests");
    expect(screen.getByText("customer user")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Выйти" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Администрирование" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Заявки клиентов" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Каталог админки" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Главная админки" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Войти" })).not.toBeInTheDocument();
  });

  it("logs out from the header and returns to anonymous actions", async () => {
    logoutMock.mockResolvedValue(undefined);
    const user = userEvent.setup();
    renderHeader({ user: buildUser("customer"), csrfToken: "csrf" });

    await user.click(screen.getByRole("button", { name: "Выйти" }));

    expect(logoutMock).toHaveBeenCalledWith("csrf");
    expect(screen.queryByText("customer user")).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Войти" })).toHaveAttribute("href", "/auth/login");
  });

  it.each(["seller", "admin"])("shows one admin navigation group for %s users", async (role) => {
    const user = userEvent.setup();
    renderHeader({ user: buildUser(role), csrfToken: "csrf" });

    const adminGroup = screen.getByRole("button", { name: "Администрирование" });
    expect(screen.getAllByRole("button", { name: "Администрирование" })).toHaveLength(1);

    await user.click(adminGroup);

    expect(screen.getByRole("link", { name: "Заявки клиентов" })).toHaveAttribute("href", "/admin/requests");
    expect(screen.getByRole("link", { name: "Каталог админки" })).toHaveAttribute("href", "/admin/catalog");
    expect(screen.getByRole("link", { name: "Главная админки" })).toHaveAttribute("href", "/admin/homepage");
    expect(screen.getByRole("link", { name: "Профиль" })).toHaveAttribute("href", "/account/profile");
    expect(screen.getByRole("link", { name: "Мои заявки" })).toHaveAttribute("href", "/account/requests");
  });
});
