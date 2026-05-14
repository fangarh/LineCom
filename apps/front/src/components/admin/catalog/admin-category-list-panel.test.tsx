import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import type { AdminCategoryListItem } from "@/lib/api/admin-catalog";
import { AdminCategoryListPanel } from "./admin-category-list-panel";

const rootCategory: AdminCategoryListItem = {
  id: "cat-root",
  parentId: null,
  name: "Кабели",
  slug: "kabeli",
  sortOrder: 10,
  isActive: true,
  isVisibleInMenu: true,
  productsCount: 4,
  childrenCount: 1,
};

const childCategory: AdminCategoryListItem = {
  id: "cat-child",
  parentId: "cat-root",
  name: "Силовые кабели",
  slug: "silovye-kabeli",
  sortOrder: 20,
  isActive: false,
  isVisibleInMenu: false,
  productsCount: 2,
  childrenCount: 0,
};

describe("AdminCategoryListPanel", () => {
  it("renders filters and category tree with selected category", () => {
    render(
      <AdminCategoryListPanel
        activeFilter="false"
        allCategories={[rootCategory, childCategory]}
        isLoadingList={false}
        onActiveFilterChange={vi.fn()}
        onCategorySelect={vi.fn()}
        onCreateCategory={vi.fn()}
        onParentFilterChange={vi.fn()}
        onSearchChange={vi.fn()}
        parentFilter="cat-root"
        search="кабель"
        selectedCategoryId="cat-child"
        treeCategories={[rootCategory, childCategory]}
      />,
    );

    expect(screen.getByLabelText("Поиск")).toHaveValue("кабель");
    expect(screen.getByLabelText("Родитель")).toHaveValue("cat-root");
    expect(screen.getByLabelText("Активность")).toHaveValue("false");
    expect(screen.getByRole("treeitem", { name: /Кабели/ })).toHaveAttribute("aria-selected", "false");
    expect(screen.getByRole("treeitem", { name: /Силовые кабели/ })).toHaveAttribute("aria-selected", "true");
  });

  it("calls panel handlers from controls and tree", async () => {
    const user = userEvent.setup();
    const onActiveFilterChange = vi.fn();
    const onCategorySelect = vi.fn();
    const onCreateCategory = vi.fn();
    const onParentFilterChange = vi.fn();
    const onSearchChange = vi.fn();

    function ControlledPanel() {
      const [search, setSearch] = useState("");
      const [parentFilter, setParentFilter] = useState("");
      const [activeFilter, setActiveFilter] = useState("");

      return (
        <AdminCategoryListPanel
          activeFilter={activeFilter}
          allCategories={[rootCategory, childCategory]}
          isLoadingList={false}
          onActiveFilterChange={(nextActiveFilter) => {
            onActiveFilterChange(nextActiveFilter);
            setActiveFilter(nextActiveFilter);
          }}
          onCategorySelect={onCategorySelect}
          onCreateCategory={onCreateCategory}
          onParentFilterChange={(nextParentFilter) => {
            onParentFilterChange(nextParentFilter);
            setParentFilter(nextParentFilter);
          }}
          onSearchChange={(nextSearch) => {
            onSearchChange(nextSearch);
            setSearch(nextSearch);
          }}
          parentFilter={parentFilter}
          search={search}
          selectedCategoryId={null}
          treeCategories={[rootCategory, childCategory]}
        />
      );
    }

    render(<ControlledPanel />);

    await user.type(screen.getByLabelText("Поиск"), "кабель");
    await user.selectOptions(screen.getByLabelText("Родитель"), "cat-root");
    await user.selectOptions(screen.getByLabelText("Активность"), "true");
    await user.click(screen.getByRole("button", { name: "Новая категория" }));
    await user.click(screen.getByRole("treeitem", { name: /Силовые кабели/ }));

    expect(onSearchChange).toHaveBeenLastCalledWith("кабель");
    expect(onParentFilterChange).toHaveBeenCalledWith("cat-root");
    expect(onActiveFilterChange).toHaveBeenCalledWith("true");
    expect(onCreateCategory).toHaveBeenCalledTimes(1);
    expect(onCategorySelect).toHaveBeenCalledWith("cat-child");
  });

  it("passes loading and empty states to category tree", () => {
    render(
      <AdminCategoryListPanel
        activeFilter=""
        allCategories={[]}
        isLoadingList={true}
        onActiveFilterChange={vi.fn()}
        onCategorySelect={vi.fn()}
        onCreateCategory={vi.fn()}
        onParentFilterChange={vi.fn()}
        onSearchChange={vi.fn()}
        parentFilter=""
        search=""
        selectedCategoryId={null}
        treeCategories={[]}
      />,
    );

    expect(screen.getByRole("tree", { name: "Дерево категорий" })).toHaveAttribute("aria-busy", "true");
    expect(screen.getByText("Категории не найдены.")).toBeInTheDocument();
  });
});
