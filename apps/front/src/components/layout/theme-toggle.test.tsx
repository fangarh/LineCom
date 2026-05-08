import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it } from "vitest";
import { ThemeToggle } from "./theme-toggle";

describe("ThemeToggle", () => {
  afterEach(() => {
    document.documentElement.removeAttribute("data-theme");
    document.documentElement.style.colorScheme = "";
    localStorage.clear();
  });

  it("toggles and persists the selected theme", async () => {
    const user = userEvent.setup();
    render(<ThemeToggle />);

    const button = await screen.findByRole("button", { name: "Включить темную тему" });
    expect(document.documentElement.dataset.theme).toBe("light");

    await user.click(button);

    expect(document.documentElement.dataset.theme).toBe("dark");
    expect(localStorage.getItem("linecom.theme")).toBe("dark");
    expect(screen.getByRole("button", { name: "Включить светлую тему" })).toBeInTheDocument();
  });
});
