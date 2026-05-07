import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ProfileForm } from "./profile-form";

describe("ProfileForm", () => {
  it("calls onSubmit with profile contacts", async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn().mockResolvedValue(undefined);

    render(
      <ProfileForm
        initialValue={{
          name: "Иван",
          email: "ivan@example.com",
          phone: "+79000000000",
        }}
        onSubmit={onSubmit}
      />,
    );

    await user.clear(screen.getByLabelText("Имя"));
    await user.type(screen.getByLabelText("Имя"), "Иван Петров");
    await user.click(screen.getByRole("button", { name: "Сохранить профиль" }));

    expect(onSubmit).toHaveBeenCalledWith({
      name: "Иван Петров",
      email: "ivan@example.com",
      phone: "+79000000000",
    });
  });
});
