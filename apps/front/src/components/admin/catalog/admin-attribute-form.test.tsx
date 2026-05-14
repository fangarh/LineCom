import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState, type FormEvent } from "react";
import { describe, expect, it, vi } from "vitest";
import { AdminAttributeForm } from "./admin-attribute-form";
import { emptyAttributeForm } from "./admin-attribute-manager-helpers";

describe("AdminAttributeForm", () => {
  it("renders attribute fields and forwards edits as form patches", async () => {
    const user = userEvent.setup();
    const onDeleteAttribute = vi.fn();
    const onFormPatch = vi.fn();
    const onSubmitAttribute = vi.fn((event: FormEvent<HTMLFormElement>) => event.preventDefault());
    const onTypeChange = vi.fn();

    function Harness() {
      const [attributeForm, setAttributeForm] = useState({
        ...emptyAttributeForm,
        name: "Цвет",
        code: "color",
        type: "select",
      });

      return (
        <AdminAttributeForm
          attributeForm={attributeForm}
          isMutatingAttribute={false}
          isSelectedCategoryAvailable={true}
          isSelectedAttributeAvailable={true}
          onDeleteAttribute={onDeleteAttribute}
          onFormPatch={(patch) => {
            onFormPatch(patch);
            setAttributeForm((current) => ({ ...current, ...patch }));
          }}
          onSubmitAttribute={onSubmitAttribute}
          onTypeChange={(type) => {
            onTypeChange(type);
            setAttributeForm((current) => ({ ...current, type }));
          }}
        />
      );
    }

    render(<Harness />);

    await user.clear(screen.getByLabelText("Название"));
    await user.type(screen.getByLabelText("Название"), "Длина");
    await user.clear(screen.getByLabelText("Код"));
    await user.type(screen.getByLabelText("Код"), "length");
    await user.selectOptions(screen.getByLabelText("Тип"), "number");
    await user.type(screen.getByLabelText("Единица"), "м");
    await user.clear(screen.getByLabelText("Сортировка"));
    await user.type(screen.getByLabelText("Сортировка"), "20");
    await user.click(screen.getByLabelText("Обязательная"));
    await user.click(screen.getByLabelText("Сравниваемая"));
    await user.click(screen.getByRole("button", { name: "Сохранить характеристику" }));
    await user.click(screen.getByRole("button", { name: "Удалить характеристику" }));

    expect(onFormPatch).toHaveBeenCalledWith({ name: "Длина" });
    expect(onFormPatch).toHaveBeenCalledWith({ code: "length" });
    expect(onTypeChange).toHaveBeenCalledWith("number");
    expect(onFormPatch).toHaveBeenCalledWith({ unit: "м" });
    expect(onFormPatch).toHaveBeenCalledWith({ sortOrder: "20" });
    expect(onFormPatch).toHaveBeenCalledWith({ isRequired: true });
    expect(onFormPatch).toHaveBeenCalledWith({ isComparable: true });
    expect(onSubmitAttribute).toHaveBeenCalled();
    expect(onDeleteAttribute).toHaveBeenCalled();
  });

  it("uses create wording and disables controls without category", () => {
    render(
      <AdminAttributeForm
        attributeForm={emptyAttributeForm}
        isMutatingAttribute={false}
        isSelectedCategoryAvailable={false}
        isSelectedAttributeAvailable={false}
        onDeleteAttribute={vi.fn()}
        onFormPatch={vi.fn()}
        onSubmitAttribute={vi.fn()}
        onTypeChange={vi.fn()}
      />,
    );

    expect(screen.getByRole("button", { name: "Создать характеристику" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Удалить характеристику" })).toBeDisabled();
    expect(screen.getByLabelText("Название")).toBeDisabled();
  });
});
