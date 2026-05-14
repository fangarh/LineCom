import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import type { AdminBrandListItem } from "@/lib/api/admin-catalog";
import { AdminBrandListPanel } from "./admin-brand-list-panel";

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

describe("AdminBrandListPanel", () => {
  it("renders filters and brand rows with selection state", () => {
    render(
      <AdminBrandListPanel
        activeFilter="true"
        brands={[activeBrand, inactiveBrand]}
        isLoadingList={false}
        onActiveFilterChange={vi.fn()}
        onCreateBrand={vi.fn()}
        onSearchChange={vi.fn()}
        onSelectBrand={vi.fn()}
        search="кабель"
        selectedBrandId="brand-active"
      />,
    );

    expect(screen.getByLabelText("Поиск")).toHaveValue("кабель");
    expect(screen.getByLabelText("Активность")).toHaveValue("true");
    expect(screen.getByRole("button", { name: /Кабельный завод/ })).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByRole("button", { name: /ПромСвет/ })).toHaveAttribute("aria-pressed", "false");
    expect(screen.getByText("Активен · 7 товаров")).toBeInTheDocument();
    expect(screen.getByText("Неактивен · 0 товаров")).toBeInTheDocument();
  });

  it("calls panel handlers from controls", async () => {
    const user = userEvent.setup();
    const onActiveFilterChange = vi.fn();
    const onCreateBrand = vi.fn();
    const onSearchChange = vi.fn();
    const onSelectBrand = vi.fn();

    function ControlledPanel() {
      const [search, setSearch] = useState("");
      const [activeFilter, setActiveFilter] = useState("");

      return (
        <AdminBrandListPanel
          activeFilter={activeFilter}
          brands={[activeBrand]}
          isLoadingList={false}
          onActiveFilterChange={(nextActiveFilter) => {
            onActiveFilterChange(nextActiveFilter);
            setActiveFilter(nextActiveFilter);
          }}
          onCreateBrand={onCreateBrand}
          onSearchChange={(nextSearch) => {
            onSearchChange(nextSearch);
            setSearch(nextSearch);
          }}
          onSelectBrand={onSelectBrand}
          search={search}
          selectedBrandId={null}
        />
      );
    }

    render(<ControlledPanel />);

    await user.type(screen.getByLabelText("Поиск"), "кабель");
    await user.selectOptions(screen.getByLabelText("Активность"), "false");
    await user.click(screen.getByRole("button", { name: "Новый бренд" }));
    await user.click(screen.getByRole("button", { name: /Кабельный завод/ }));

    expect(onSearchChange).toHaveBeenLastCalledWith("кабель");
    expect(onActiveFilterChange).toHaveBeenCalledWith("false");
    expect(onCreateBrand).toHaveBeenCalledTimes(1);
    expect(onSelectBrand).toHaveBeenCalledWith("brand-active");
  });

  it("renders empty state while preserving loading marker", () => {
    render(
      <AdminBrandListPanel
        activeFilter=""
        brands={[]}
        isLoadingList={true}
        onActiveFilterChange={vi.fn()}
        onCreateBrand={vi.fn()}
        onSearchChange={vi.fn()}
        onSelectBrand={vi.fn()}
        search=""
        selectedBrandId={null}
      />,
    );

    expect(screen.getByText("Бренды не найдены.")).toBeInTheDocument();
    expect(screen.getByLabelText("Список брендов")).toHaveAttribute("aria-busy", "true");
  });
});
