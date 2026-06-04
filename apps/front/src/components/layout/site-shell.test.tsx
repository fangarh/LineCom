import { render, screen, within } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { AuthProvider } from "@/components/auth/auth-provider";
import { RequestDraftProvider } from "@/components/request/request-draft-provider";
import { SiteShell } from "./site-shell";

function renderShell() {
  return render(
    <AuthProvider initialSession={null}>
      <RequestDraftProvider>
        <SiteShell>
          <div>Страница</div>
        </SiteShell>
      </RequestDraftProvider>
    </AuthProvider>,
  );
}

describe("SiteShell", () => {
  afterEach(() => {
    localStorage.clear();
  });

  it("places the login link in the footer before contacts", () => {
    renderShell();

    const footerNav = screen.getByRole("navigation", { name: "Правовая и служебная навигация" });
    const links = within(footerNav).getAllByRole("link");

    expect(links.map((link) => link.textContent)).toEqual(["Войти", "Контакты", "Доставка", "Cookie"]);
    expect(links[0]).toHaveAttribute("href", "/auth/login");
  });
});
