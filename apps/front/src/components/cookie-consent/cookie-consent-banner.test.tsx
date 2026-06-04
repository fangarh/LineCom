import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it } from "vitest";
import {
  COOKIE_CONSENT_STORAGE_KEY,
  COOKIE_SETTINGS_OPEN_EVENT,
  loadCookieConsent,
  saveCookieConsent,
} from "@/lib/cookie-consent";
import { CookieConsentBanner } from "./cookie-consent-banner";

describe("CookieConsentBanner", () => {
  afterEach(() => {
    localStorage.clear();
  });

  it("shows the banner until optional cookies are rejected", async () => {
    const user = userEvent.setup();
    render(<CookieConsentBanner />);

    expect(await screen.findByRole("heading", { name: "Мы используем cookie" })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Отклонить необязательные" }));

    await waitFor(() => {
      expect(screen.queryByRole("heading", { name: "Мы используем cookie" })).not.toBeInTheDocument();
    });
    expect(loadCookieConsent()?.categories).toEqual({
      analytics: false,
      marketing: false,
      functional: false,
    });
  });

  it("saves a custom category selection", async () => {
    const user = userEvent.setup();
    render(<CookieConsentBanner />);

    await user.click(await screen.findByRole("button", { name: "Настроить" }));
    await user.click(screen.getByRole("checkbox", { name: /Аналитика/i }));
    await user.click(screen.getByRole("button", { name: "Сохранить выбор" }));

    await waitFor(() => {
      expect(screen.queryByRole("heading", { name: "Настройки cookie" })).not.toBeInTheDocument();
    });
    expect(loadCookieConsent()?.categories).toEqual({
      analytics: true,
      marketing: false,
      functional: false,
    });
  });

  it("opens settings from the global settings event after consent is stored", async () => {
    saveCookieConsent({
      analytics: true,
      marketing: false,
      functional: false,
    });
    render(<CookieConsentBanner />);

    window.dispatchEvent(new Event(COOKIE_SETTINGS_OPEN_EVENT));

    expect(await screen.findByRole("heading", { name: "Настройки cookie" })).toBeInTheDocument();
    expect(screen.getByRole("checkbox", { name: /Аналитика/i })).toBeChecked();
    expect(localStorage.getItem(COOKIE_CONSENT_STORAGE_KEY)).toContain("\"analytics\":true");
  });
});
