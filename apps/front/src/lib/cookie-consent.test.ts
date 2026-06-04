import { afterEach, describe, expect, it } from "vitest";
import {
  COOKIE_CONSENT_STORAGE_KEY,
  COOKIE_CONSENT_VERSION,
  acceptAllOptionalCookies,
  buildCookieConsent,
  emptyOptionalCookieConsent,
  hasCookieConsent,
  loadCookieConsent,
  rejectOptionalCookies,
  saveCookieConsent,
} from "./cookie-consent";

describe("cookie consent storage", () => {
  afterEach(() => {
    localStorage.clear();
  });

  it("loads no consent when storage is empty or version is outdated", () => {
    expect(loadCookieConsent()).toBeNull();

    localStorage.setItem(
      COOKIE_CONSENT_STORAGE_KEY,
      JSON.stringify({
        version: "old",
        updatedAt: "2026-01-01T00:00:00.000Z",
        categories: { analytics: true, marketing: true, functional: true },
      }),
    );

    expect(loadCookieConsent()).toBeNull();
  });

  it("persists explicit category choices", () => {
    const consent = saveCookieConsent({
      analytics: true,
      marketing: false,
      functional: true,
    });

    expect(consent.version).toBe(COOKIE_CONSENT_VERSION);
    expect(loadCookieConsent()?.categories).toEqual({
      analytics: true,
      marketing: false,
      functional: true,
    });
  });

  it("accepts and rejects optional cookies as category groups", () => {
    acceptAllOptionalCookies();

    expect(hasCookieConsent("necessary")).toBe(true);
    expect(hasCookieConsent("analytics")).toBe(true);
    expect(hasCookieConsent("marketing")).toBe(true);
    expect(hasCookieConsent("functional")).toBe(true);

    rejectOptionalCookies();

    expect(loadCookieConsent()?.categories).toEqual(emptyOptionalCookieConsent);
    expect(hasCookieConsent("analytics")).toBe(false);
  });

  it("builds versioned consent with a stable timestamp when provided", () => {
    expect(buildCookieConsent(emptyOptionalCookieConsent, "2026-06-03T00:00:00.000Z")).toEqual({
      version: COOKIE_CONSENT_VERSION,
      updatedAt: "2026-06-03T00:00:00.000Z",
      categories: emptyOptionalCookieConsent,
    });
  });
});
