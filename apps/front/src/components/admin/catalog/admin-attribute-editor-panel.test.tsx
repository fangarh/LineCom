import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { AdminCategoryAttribute, AdminCategoryListItem } from "@/lib/api/admin-catalog";
import { emptyAttributeForm, emptyOptionForm } from "./admin-attribute-manager-helpers";
import { AdminAttributeEditorPanel } from "./admin-attribute-editor-panel";

const category: AdminCategoryListItem = {
  id: "cat-cables",
  parentId: null,
  name: "Кабели",
  slug: "kabeli",
  sortOrder: 10,
  isActive: true,
  isVisibleInMenu: true,
  productsCount: 8,
  childrenCount: 1,
};

const selectAttribute: AdminCategoryAttribute = {
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
};

function defaultProps() {
  return {
    alertMessage: null,
    attributeForm: emptyAttributeForm,
    isMutatingAttribute: false,
    isMutatingOption: false,
    isPersistedSelectAttribute: false,
    onAttributeFormPatch: vi.fn(),
    onAttributeTypeChange: vi.fn(),
    onDeleteAttribute: vi.fn(),
    onDeleteOption: vi.fn(),
    onOptionFormPatch: vi.fn(),
    onOptionSlugChange: vi.fn(),
    onOptionValueChange: vi.fn(),
    onRegenerateOptionSlug: vi.fn(),
    onSelectOption: vi.fn(),
    onStartCreateOption: vi.fn(),
    onSubmitAttribute: vi.fn(),
    onSubmitOption: vi.fn(),
    optionForm: emptyOptionForm,
    selectedAttribute: null,
    selectedCategory: category,
    selectedCategoryId: "cat-cables",
    selectedOption: null,
    statusMessage: null,
  };
}

describe("AdminAttributeEditorPanel", () => {
  it("renders selected category, messages, and create form state", () => {
    render(
      <AdminAttributeEditorPanel
        {...defaultProps()}
        alertMessage="Ошибка сохранения"
        statusMessage="Характеристика создана."
      />,
    );

    expect(screen.getByRole("heading", { name: "Новая характеристика" })).toBeInTheDocument();
    expect(screen.getByText("Кабели")).toBeInTheDocument();
    expect(screen.getByRole("alert")).toHaveTextContent("Ошибка сохранения");
    expect(screen.getByText("Характеристика создана.")).toBeInTheDocument();
  });

  it("renders option editor only for persisted select attributes", () => {
    const { rerender } = render(
      <AdminAttributeEditorPanel
        {...defaultProps()}
        attributeForm={{ ...emptyAttributeForm, type: "select" }}
        isPersistedSelectAttribute={false}
        selectedAttribute={null}
      />,
    );

    expect(screen.queryByRole("heading", { name: "Значения" })).not.toBeInTheDocument();

    rerender(
      <AdminAttributeEditorPanel
        {...defaultProps()}
        attributeForm={{ ...emptyAttributeForm, type: "select" }}
        isPersistedSelectAttribute
        selectedAttribute={selectAttribute}
      />,
    );

    expect(screen.getByRole("heading", { name: "Значения" })).toBeInTheDocument();
  });
});
