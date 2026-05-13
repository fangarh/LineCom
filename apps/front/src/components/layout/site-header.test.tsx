import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { RequestDraftProvider } from "@/components/request/request-draft-provider";
import { SiteHeader } from "./site-header";

function renderHeader() {
  return render(
    <RequestDraftProvider>
      <SiteHeader />
    </RequestDraftProvider>,
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
    expect(screen.getByRole("link", { name: "О нас" })).toHaveAttribute("href", "/about");
    expect(screen.getByRole("link", { name: "Доставка" })).toHaveAttribute("href", "/delivery");
    expect(screen.getByRole("link", { name: "Заявка" })).toHaveAttribute("href", "/request");
    expect(screen.getByRole("link", { name: "Мои заявки" })).toHaveAttribute("href", "/account/requests");
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
});
