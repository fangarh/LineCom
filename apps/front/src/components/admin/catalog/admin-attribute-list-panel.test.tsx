import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { AdminCategoryAttribute, AdminCategoryListItem } from "@/lib/api/admin-catalog";
import { AdminAttributeListPanel } from "./admin-attribute-list-panel";

const categories: AdminCategoryListItem[] = [
  {
    id: "cat-cables",
    parentId: null,
    name: "Кабели",
    slug: "kabeli",
    sortOrder: 10,
    isActive: true,
    isVisibleInMenu: true,
    productsCount: 8,
    childrenCount: 1,
  },
  {
    id: "cat-connectors",
    parentId: null,
    name: "Разъемы",
    slug: "razemy",
    sortOrder: 20,
    isActive: true,
    isVisibleInMenu: true,
    productsCount: 3,
    childrenCount: 0,
  },
];

const attributes: AdminCategoryAttribute[] = [
  {
    id: "attr-color",
    categoryId: "cat-cables",
    name: "Цвет",
    code: "color",
    type: "select",
    unit: null,
    isRequired: true,
    isFilterable: true,
    isComparable: false,
    isVisibleInProduct: true,
    isSeoImportant: false,
    isUsedInGeneratedName: true,
    sortOrder: 10,
    isActive: true,
    productValuesCount: 3,
    options: [],
  },
];

describe("AdminAttributeListPanel", () => {
  it("renders category picker, actions, and attribute rows", async () => {
    const user = userEvent.setup();
    const onCategoryChange = vi.fn();
    const onCreateAttribute = vi.fn();
    const onInheritFromParent = vi.fn();
    const onSelectAttribute = vi.fn();

    render(
      <AdminAttributeListPanel
        attributes={attributes}
        categories={categories}
        isLoadingAttributes={false}
        isLoadingCategories={false}
        isMutatingAttribute={false}
        onCategoryChange={onCategoryChange}
        onCreateAttribute={onCreateAttribute}
        onInheritFromParent={onInheritFromParent}
        onSelectAttribute={onSelectAttribute}
        selectedAttributeId="attr-color"
        selectedCategory={categories[0]}
        selectedCategoryId="cat-cables"
      />,
    );

    await user.selectOptions(screen.getByLabelText("Категория"), "cat-connectors");
    await user.click(screen.getByRole("button", { name: "Новая характеристика" }));
    await user.click(screen.getByRole("button", { name: "Унаследовать от родителя" }));
    await user.click(screen.getByRole("button", { name: /Цвет/ }));

    expect(onCategoryChange).toHaveBeenCalledWith("cat-connectors");
    expect(onCreateAttribute).toHaveBeenCalled();
    expect(onInheritFromParent).toHaveBeenCalled();
    expect(onSelectAttribute).toHaveBeenCalledWith(attributes[0]);
    expect(screen.getByRole("button", { name: /color · select/ })).toHaveAttribute("aria-pressed", "true");
  });

  it("shows a category-specific empty state", () => {
    render(
      <AdminAttributeListPanel
        attributes={[]}
        categories={categories}
        isLoadingAttributes={false}
        isLoadingCategories={false}
        isMutatingAttribute={false}
        onCategoryChange={vi.fn()}
        onCreateAttribute={vi.fn()}
        onInheritFromParent={vi.fn()}
        onSelectAttribute={vi.fn()}
        selectedAttributeId={null}
        selectedCategory={categories[0]}
        selectedCategoryId="cat-cables"
      />,
    );

    expect(screen.getByText("Характеристики не найдены.")).toBeInTheDocument();
  });

  it("asks to select a category before showing rows", () => {
    render(
      <AdminAttributeListPanel
        attributes={[]}
        categories={categories}
        isLoadingAttributes={false}
        isLoadingCategories={false}
        isMutatingAttribute={false}
        onCategoryChange={vi.fn()}
        onCreateAttribute={vi.fn()}
        onInheritFromParent={vi.fn()}
        onSelectAttribute={vi.fn()}
        selectedAttributeId={null}
        selectedCategory={null}
        selectedCategoryId=""
      />,
    );

    expect(screen.getByText("Выберите категорию.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Новая характеристика" })).toBeDisabled();
  });
});
