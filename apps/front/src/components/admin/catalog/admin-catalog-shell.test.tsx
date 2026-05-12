import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { AdminCatalogShell } from "./admin-catalog-shell";

describe("AdminCatalogShell", () => {
  it("renders one h1 and keeps a stable selected tab", () => {
    render(<AdminCatalogShell />);

    expect(screen.getAllByRole("heading", { level: 1 })).toHaveLength(1);
    expect(screen.getByRole("heading", { name: "Каталог" })).toBeInTheDocument();

    const tablist = screen.getByRole("tablist", { name: "Разделы каталога" });
    const selectedTabs = within(tablist).getAllByRole("tab", { selected: true });
    expect(selectedTabs).toHaveLength(1);
    expect(selectedTabs[0]).toHaveTextContent("Товары");
    expect(screen.getByRole("tabpanel", { name: "Товары" })).toBeVisible();
  });

  it("switches tabs without unmounting the shell", async () => {
    const user = userEvent.setup();
    render(<AdminCatalogShell />);

    const shell = screen.getByRole("region", { name: "Администрирование каталога" });
    const tablist = screen.getByRole("tablist", { name: "Разделы каталога" });
    const productsTab = within(tablist).getByRole("tab", { name: "Товары" });
    const categoriesTab = within(tablist).getByRole("tab", { name: "Категории" });
    const brandsTab = within(tablist).getByRole("tab", { name: "Бренды" });
    const attributesTab = within(tablist).getByRole("tab", { name: "Характеристики" });

    await user.click(categoriesTab);
    expect(screen.getByRole("region", { name: "Администрирование каталога" })).toBe(shell);
    expect(categoriesTab).toHaveAttribute("aria-selected", "true");
    expect(productsTab).toHaveAttribute("aria-selected", "false");
    expect(screen.getByRole("tabpanel", { name: "Категории" })).toBeVisible();

    await user.click(brandsTab);
    expect(screen.getByRole("region", { name: "Администрирование каталога" })).toBe(shell);
    expect(brandsTab).toHaveAttribute("aria-selected", "true");
    expect(screen.getByRole("tabpanel", { name: "Бренды" })).toBeVisible();

    await user.click(attributesTab);
    expect(screen.getByRole("region", { name: "Администрирование каталога" })).toBe(shell);
    expect(attributesTab).toHaveAttribute("aria-selected", "true");
    expect(screen.getByRole("tabpanel", { name: "Характеристики" })).toBeVisible();
    expect(screen.getAllByRole("heading", { level: 1 })).toHaveLength(1);
  });
});
