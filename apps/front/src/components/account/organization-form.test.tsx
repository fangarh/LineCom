import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { OrganizationForm } from "./organization-form";

describe("OrganizationForm", () => {
  it("calls onSubmit with organization fields", async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn().mockResolvedValue(undefined);

    render(<OrganizationForm initialValue={null} onSubmit={onSubmit} />);

    await user.type(screen.getByLabelText("Название организации"), "ООО Сеть");
    await user.type(screen.getByLabelText("ИНН"), "7700000000");
    await user.type(screen.getByLabelText("Контактное лицо"), "Иван Петров");
    await user.type(screen.getByLabelText("Телефон организации"), "+7 900 000-00-00");
    await user.type(screen.getByLabelText("Email организации"), "sales@example.com");
    await user.type(screen.getByLabelText("Комментарий"), "Основная организация");
    await user.click(screen.getByRole("button", { name: "Сохранить организацию" }));

    expect(onSubmit).toHaveBeenCalledWith({
      name: "ООО Сеть",
      inn: "7700000000",
      contactPerson: "Иван Петров",
      phone: "+7 900 000-00-00",
      email: "sales@example.com",
      comment: "Основная организация",
    });
  });
});
