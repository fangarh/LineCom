import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { ContactCtaButton } from "./contact-cta-button";

describe("ContactCtaButton", () => {
  it("opens and closes the contact dialog", async () => {
    const user = userEvent.setup();
    render(<ContactCtaButton />);

    await user.click(screen.getByRole("button", { name: "Связаться с нами" }));

    expect(screen.getByRole("dialog", { name: "Связаться с нами" })).toBeInTheDocument();
    expect(screen.getByText("Лопатин А.В.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "+7 931 306-43-50" })).toHaveAttribute("href", "tel:+79313064350");

    await user.click(screen.getByRole("button", { name: "Закрыть" }));

    expect(screen.queryByRole("dialog", { name: "Связаться с нами" })).not.toBeInTheDocument();
  });
});
