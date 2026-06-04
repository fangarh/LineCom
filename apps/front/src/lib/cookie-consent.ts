export const COOKIE_CONSENT_VERSION = "2026-06-03";
export const COOKIE_CONSENT_STORAGE_KEY = "linecom.cookieConsent.v1";
export const COOKIE_CONSENT_CHANGE_EVENT = "linecom-cookie-consent-change";
export const COOKIE_SETTINGS_OPEN_EVENT = "linecom-cookie-settings-open";

export const optionalCookieConsentCategories = ["analytics", "marketing", "functional"] as const;

export type OptionalCookieConsentCategory = (typeof optionalCookieConsentCategories)[number];
export type CookieConsentCategory = "necessary" | OptionalCookieConsentCategory;

export type CookieConsentCategories = Record<OptionalCookieConsentCategory, boolean>;

export type CookieConsent = {
  version: string;
  updatedAt: string;
  categories: CookieConsentCategories;
};

export const emptyOptionalCookieConsent: CookieConsentCategories = {
  analytics: false,
  marketing: false,
  functional: false,
};

export const allOptionalCookieConsent: CookieConsentCategories = {
  analytics: true,
  marketing: true,
  functional: true,
};

function getStorage() {
  if (typeof window === "undefined") {
    return null;
  }

  return window.localStorage;
}

function normalizeCategories(value: unknown): CookieConsentCategories | null {
  if (!value || typeof value !== "object") {
    return null;
  }

  const input = value as Partial<Record<OptionalCookieConsentCategory, unknown>>;
  return {
    analytics: input.analytics === true,
    marketing: input.marketing === true,
    functional: input.functional === true,
  };
}

export function normalizeCookieConsent(value: unknown): CookieConsent | null {
  if (!value || typeof value !== "object") {
    return null;
  }

  const input = value as Partial<CookieConsent>;
  if (input.version !== COOKIE_CONSENT_VERSION || typeof input.updatedAt !== "string") {
    return null;
  }

  const categories = normalizeCategories(input.categories);
  if (!categories) {
    return null;
  }

  return {
    version: COOKIE_CONSENT_VERSION,
    updatedAt: input.updatedAt,
    categories,
  };
}

export function buildCookieConsent(categories: CookieConsentCategories, updatedAt = new Date().toISOString()): CookieConsent {
  return {
    version: COOKIE_CONSENT_VERSION,
    updatedAt,
    categories: { ...categories },
  };
}

export function loadCookieConsent(): CookieConsent | null {
  const storage = getStorage();
  if (!storage) {
    return null;
  }

  try {
    const raw = storage.getItem(COOKIE_CONSENT_STORAGE_KEY);
    if (!raw) {
      return null;
    }

    return normalizeCookieConsent(JSON.parse(raw));
  } catch {
    return null;
  }
}

export function saveCookieConsent(categories: CookieConsentCategories): CookieConsent {
  const consent = buildCookieConsent(categories);
  const storage = getStorage();

  if (storage) {
    storage.setItem(COOKIE_CONSENT_STORAGE_KEY, JSON.stringify(consent));
    window.dispatchEvent(new Event(COOKIE_CONSENT_CHANGE_EVENT));
  }

  return consent;
}

export function acceptAllOptionalCookies() {
  return saveCookieConsent(allOptionalCookieConsent);
}

export function rejectOptionalCookies() {
  return saveCookieConsent(emptyOptionalCookieConsent);
}

export function hasCookieConsent(category: CookieConsentCategory) {
  if (category === "necessary") {
    return true;
  }

  return loadCookieConsent()?.categories[category] === true;
}

export function openCookieSettings() {
  if (typeof window === "undefined") {
    return;
  }

  window.dispatchEvent(new Event(COOKIE_SETTINGS_OPEN_EVENT));
}
