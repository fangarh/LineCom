import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { PasswordForm } from "./password-form";

describe("PasswordForm", () => {
  it("does not submit when new password and repeat do not match", async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn().mockResolvedValue(undefined);

    render(<PasswordForm onSubmit={onSubmit} />);

    await user.type(screen.getByLabelText("Текущий пароль"), "old-password");
    await user.type(screen.getByLabelText("Новый пароль"), "new-password");
    await user.type(screen.getByLabelText("Повтор нового пароля"), "different-password");
    await user.click(screen.getByRole("button", { name: "Сменить пароль" }));

    expect(onSubmit).not.toHaveBeenCalled();
    expect(screen.getByRole("alert")).toHaveTextContent("Новый пароль и повтор не совпадают.");
  });

  it("submits current and new passwords, shows success and clears fields", async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn().mockResolvedValue(undefined);

    render(<PasswordForm onSubmit={onSubmit} />);

    await user.type(screen.getByLabelText("Текущий пароль"), "old-password");
    await user.type(screen.getByLabelText("Новый пароль"), "new-password");
    await user.type(screen.getByLabelText("Повтор нового пароля"), "new-password");
    await user.click(screen.getByRole("button", { name: "Сменить пароль" }));

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith({
        currentPassword: "old-password",
        newPassword: "new-password",
      }),
    );

    expect(screen.getByText("Пароль изменен.")).toBeInTheDocument();
    expect(screen.getByLabelText("Текущий пароль")).toHaveValue("");
    expect(screen.getByLabelText("Новый пароль")).toHaveValue("");
    expect(screen.getByLabelText("Повтор нового пароля")).toHaveValue("");
  });
});
