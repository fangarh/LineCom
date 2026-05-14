import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState, type FormEvent } from "react";
import { describe, expect, it, vi } from "vitest";
import type { AdminAttributeOption, AdminCategoryAttribute } from "@/lib/api/admin-catalog";
import { emptyOptionForm } from "./admin-attribute-manager-helpers";
import { AdminAttributeOptionEditor } from "./admin-attribute-option-editor";

const redOption: AdminAttributeOption = {
  id: "option-red",
  value: "Красный",
  slug: "krasnyy",
  normalizedValue: "red",
  sortOrder: 10,
  isActive: true,
  productValuesCount: 5,
};

const colorAttribute: AdminCategoryAttribute = {
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
  options: [redOption],
};

describe("AdminAttributeOptionEditor", () => {
  it("renders option rows and forwards editor actions", async () => {
    const user = userEvent.setup();
    const onDeleteOption = vi.fn();
    const onFormPatch = vi.fn();
    const onOptionSlugChange = vi.fn();
    const onOptionValueChange = vi.fn();
    const onRegenerateOptionSlug = vi.fn();
    const onSelectOption = vi.fn();
    const onStartCreateOption = vi.fn();
    const onSubmitOption = vi.fn((event: FormEvent<HTMLFormElement>) => event.preventDefault());

    function Harness() {
      const [optionForm, setOptionForm] = useState({
        ...emptyOptionForm,
        value: "Синий",
        slug: "siniy",
        normalizedValue: "blue",
      });

      return (
        <AdminAttributeOptionEditor
          isMutatingOption={false}
          onDeleteOption={onDeleteOption}
          onFormPatch={(patch) => {
            onFormPatch(patch);
            setOptionForm((current) => ({ ...current, ...patch }));
          }}
          onOptionSlugChange={(slug) => {
            onOptionSlugChange(slug);
            setOptionForm((current) => ({ ...current, slug }));
          }}
          onOptionValueChange={(value) => {
            onOptionValueChange(value);
            setOptionForm((current) => ({ ...current, value }));
          }}
          onRegenerateOptionSlug={onRegenerateOptionSlug}
          onSelectOption={onSelectOption}
          onStartCreateOption={onStartCreateOption}
          onSubmitOption={onSubmitOption}
          optionForm={optionForm}
          selectedAttribute={colorAttribute}
          selectedOption={redOption}
        />
      );
    }

    render(<Harness />);

    const editor = screen.getByLabelText("Редактор значения");
    await user.click(within(editor).getByRole("button", { name: "Новое значение" }));
    await user.click(within(editor).getByRole("button", { name: /Красный/ }));
    await user.clear(within(editor).getByLabelText("Значение"));
    await user.type(within(editor).getByLabelText("Значение"), "Темно-синий");
    await user.clear(within(editor).getByLabelText("Slug"));
    await user.type(within(editor).getByLabelText("Slug"), "manual-blue");
    await user.click(within(editor).getByRole("button", { name: "Сгенерировать заново" }));
    await user.clear(within(editor).getByLabelText("Нормализованное значение"));
    await user.type(within(editor).getByLabelText("Нормализованное значение"), "dark-blue");
    await user.clear(within(editor).getByLabelText("Сортировка значения"));
    await user.type(within(editor).getByLabelText("Сортировка значения"), "30");
    await user.click(within(editor).getByLabelText("Активно"));
    await user.click(within(editor).getByRole("button", { name: "Сохранить значение" }));
    await user.click(within(editor).getByRole("button", { name: "Удалить значение" }));

    expect(onStartCreateOption).toHaveBeenCalled();
    expect(onSelectOption).toHaveBeenCalledWith(redOption);
    expect(onOptionValueChange).toHaveBeenLastCalledWith("Темно-синий");
    expect(onOptionSlugChange).toHaveBeenLastCalledWith("manual-blue");
    expect(onRegenerateOptionSlug).toHaveBeenCalled();
    expect(onFormPatch).toHaveBeenCalledWith({ normalizedValue: "dark-blue" });
    expect(onFormPatch).toHaveBeenCalledWith({ sortOrder: "30" });
    expect(onFormPatch).toHaveBeenCalledWith({ isActive: false });
    expect(onSubmitOption).toHaveBeenCalled();
    expect(onDeleteOption).toHaveBeenCalled();
  });

  it("uses create wording and disables deletion without a selected option", () => {
    render(
      <AdminAttributeOptionEditor
        isMutatingOption={false}
        onDeleteOption={vi.fn()}
        onFormPatch={vi.fn()}
        onOptionSlugChange={vi.fn()}
        onOptionValueChange={vi.fn()}
        onRegenerateOptionSlug={vi.fn()}
        onSelectOption={vi.fn()}
        onStartCreateOption={vi.fn()}
        onSubmitOption={vi.fn()}
        optionForm={emptyOptionForm}
        selectedAttribute={{ ...colorAttribute, options: [] }}
        selectedOption={null}
      />,
    );

    expect(screen.getByText("Значения не найдены.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Создать значение" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Удалить значение" })).toBeDisabled();
  });
});
