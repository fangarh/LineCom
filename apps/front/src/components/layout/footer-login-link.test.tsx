import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { AuthProvider } from "@/components/auth/auth-provider";
import type { AuthSession } from "@/lib/api/auth";
import { FooterLoginLink } from "./footer-login-link";

function renderFooterLoginLink(session: AuthSession | null = null) {
  return render(
    <AuthProvider initialSession={session}>
      <FooterLoginLink />
    </AuthProvider>,
  );
}

describe("FooterLoginLink", () => {
  it("shows a footer login link for anonymous users", () => {
    renderFooterLoginLink();

    expect(screen.getByRole("link", { name: "Войти" })).toHaveAttribute("href", "/auth/login");
  });

  it("hides the login link when the user is already authenticated", () => {
    renderFooterLoginLink({
      csrfToken: "csrf",
      user: {
        id: "customer-user",
        name: "Customer user",
        email: "customer@linecom.test",
        phone: null,
        role: "customer",
      },
    });

    expect(screen.queryByRole("link", { name: "Войти" })).not.toBeInTheDocument();
  });
});
