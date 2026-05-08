import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { SiteHeader } from "./site-header";

describe("SiteHeader", () => {
  it("renders request-oriented navigation", () => {
    render(<SiteHeader />);

    expect(screen.getByRole("link", { name: "LineCom" })).toHaveAttribute("href", "/");
    expect(screen.getByRole("img", { name: "LineCom - кабель и оптоволокно" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Каталог" })).toHaveAttribute("href", "/catalog");
    expect(screen.getByRole("link", { name: "Заявка" })).toHaveAttribute("href", "/request");
    expect(screen.getByRole("link", { name: "Мои заявки" })).toHaveAttribute("href", "/account/requests");
    expect(screen.getByRole("button", { name: "Включить темную тему" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Войти" })).toHaveAttribute("href", "/auth/login");
  });
});
