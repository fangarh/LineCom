import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { LoginForm } from "./login-form";

describe("LoginForm", () => {
  it("calls onSubmit with login and password", async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn().mockResolvedValue(undefined);

    render(<LoginForm onSubmit={onSubmit} />);

    await user.type(screen.getByLabelText("Email или телефон"), "client@example.com");
    await user.type(screen.getByLabelText("Пароль"), "secure-password");
    await user.click(screen.getByRole("button", { name: "Войти" }));

    expect(onSubmit).toHaveBeenCalledWith({
      login: "client@example.com",
      password: "secure-password",
    });
  });

  it("shows backend error message", () => {
    render(<LoginForm onSubmit={vi.fn()} errorMessage="Неверный логин или пароль." />);

    expect(screen.getByRole("alert")).toHaveTextContent("Неверный логин или пароль.");
  });

  it("uses login submit wording", () => {
    render(<LoginForm onSubmit={vi.fn()} />);

    expect(screen.getByRole("button", { name: "Войти" })).toBeInTheDocument();
  });
});
