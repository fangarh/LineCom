import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { RegisterForm } from "./register-form";

describe("RegisterForm", () => {
  it("calls onSubmit with customer registration fields", async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn().mockResolvedValue(undefined);

    render(<RegisterForm onSubmit={onSubmit} />);

    await user.type(screen.getByLabelText("Имя"), "Иван Петров");
    await user.type(screen.getByLabelText("Email"), "client@example.com");
    await user.type(screen.getByLabelText("Телефон"), "+7 900 000-00-00");
    await user.type(screen.getByLabelText("Пароль"), "secure-password");
    await user.click(screen.getByRole("button", { name: "Зарегистрироваться" }));

    expect(onSubmit).toHaveBeenCalledWith({
      name: "Иван Петров",
      email: "client@example.com",
      phone: "+7 900 000-00-00",
      password: "secure-password",
    });
  });

  it("shows backend error message", () => {
    render(<RegisterForm onSubmit={vi.fn()} errorMessage="Пользователь уже существует." />);

    expect(screen.getByRole("alert")).toHaveTextContent("Пользователь уже существует.");
  });
});
