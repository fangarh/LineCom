import { describe, expect, it, vi } from "vitest";
import AboutPage from "./page";

const permanentRedirectMock = vi.hoisted(() => vi.fn());

vi.mock("next/navigation", () => ({
  permanentRedirect: permanentRedirectMock,
}));

describe("legacy about route", () => {
  it("permanently redirects to contacts", () => {
    AboutPage();

    expect(permanentRedirectMock).toHaveBeenCalledWith("/contacts");
  });
});
