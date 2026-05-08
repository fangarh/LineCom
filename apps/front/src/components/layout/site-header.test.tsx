import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { SiteHeader } from "./site-header";

describe("SiteHeader", () => {
  afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it("renders request-oriented navigation", () => {
    render(<SiteHeader />);

    expect(screen.getByRole("link", { name: "LineCom" })).toHaveAttribute("href", "/");
    expect(screen.getByRole("img", { name: "LineCom - кабель и оптоволокно" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Главная" })).toHaveAttribute("href", "/");
    expect(screen.getByRole("link", { name: "Каталог" })).toHaveAttribute("href", "/catalog");
    expect(screen.getByRole("link", { name: "О нас" })).toHaveAttribute("href", "/about");
    expect(screen.getByRole("link", { name: "Доставка" })).toHaveAttribute("href", "/delivery");
    expect(screen.getByRole("link", { name: "Заявка" })).toHaveAttribute("href", "/request");
    expect(screen.getByRole("link", { name: "Мои заявки" })).toHaveAttribute("href", "/account/requests");
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
    render(<SiteHeader />);

    const brand = screen.getByRole("link", { name: "LineCom" });
    expect(brand).toHaveAttribute("aria-expanded", "false");

    await user.click(brand);
    expect(brand).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByRole("link", { name: "Главная" })).toHaveAttribute("href", "/");

    await user.click(screen.getByRole("link", { name: "Каталог" }));
    expect(brand).toHaveAttribute("aria-expanded", "false");
  });
});
