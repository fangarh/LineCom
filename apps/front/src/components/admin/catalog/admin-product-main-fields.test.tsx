import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { AdminBrandListItem, AdminCategoryListItem } from "@/lib/api/admin-catalog";
import type { ProductFormState } from "./admin-product-editor-helpers";
import { emptyProductForm } from "./admin-product-editor-helpers";
import { AdminProductMainFields } from "./admin-product-main-fields";

const parentCategory: AdminCategoryListItem = {
  id: "cat-cables",
  parentId: null,
  name: "Кабели",
  slug: "kabeli",
  sortOrder: 10,
  isActive: true,
  isVisibleInMenu: true,
  productsCount: 0,
  childrenCount: 1,
};

const leafCategory: AdminCategoryListItem = {
  id: "cat-power-cables",
  parentId: "cat-cables",
  name: "Силовые кабели",
  slug: "silovye-kabeli",
  sortOrder: 20,
  isActive: true,
  isVisibleInMenu: true,
  productsCount: 0,
  childrenCount: 0,
};

const brand: AdminBrandListItem = {
  id: "brand-cable",
  name: "Кабельный завод",
  slug: "kabelnyy-zavod",
  isActive: true,
  productsCount: 7,
};

function renderFields(form: ProductFormState = emptyProductForm) {
  const setForm = vi.fn();
  const view = render(
    <AdminProductMainFields
      brands={[brand]}
      categories={[parentCategory, leafCategory]}
      form={form}
      onNameChange={vi.fn()}
      onRegenerateSlug={vi.fn()}
      onSlugChange={vi.fn()}
      setForm={setForm}
    />,
  );

  return { ...view, setForm };
}

describe("AdminProductMainFields", () => {
  it("does not select a parent category for a product", async () => {
    const user = userEvent.setup();
    const { setForm } = renderFields();

    await user.click(screen.getByRole("button", { name: "Выбрать категорию" }));
    const listbox = screen.getByRole("listbox", { name: "Категория" });
    const parentOption = within(listbox).getByRole("option", { name: "Кабели" });

    expect(parentOption).toHaveAttribute("aria-disabled", "true");
    await user.click(parentOption);

    expect(setForm).not.toHaveBeenCalled();
  });

  it("selects a leaf category for a product and shows it when closed", async () => {
    const user = userEvent.setup();
    const { setForm, unmount } = renderFields();

    await user.click(screen.getByRole("button", { name: "Выбрать категорию" }));
    await user.click(within(screen.getByRole("listbox", { name: "Категория" })).getByRole("option", { name: "Силовые кабели" }));

    expect(setForm).toHaveBeenCalledTimes(1);
    const update = setForm.mock.calls[0][0] as (current: ProductFormState) => ProductFormState;
    expect(update(emptyProductForm).categoryId).toBe("cat-power-cables");

    unmount();
    renderFields({ ...emptyProductForm, categoryId: "cat-power-cables" });
    expect(screen.getByRole("button", { name: "Выбрать категорию" })).toHaveTextContent("Силовые кабели");
    expect(screen.getByRole("button", { name: "Выбрать категорию" })).toHaveTextContent("silovye-kabeli");
  });
});
