"use client";

import { useEffect, useMemo, useState, useSyncExternalStore } from "react";
import {
  COOKIE_CONSENT_CHANGE_EVENT,
  COOKIE_CONSENT_STORAGE_KEY,
  COOKIE_SETTINGS_OPEN_EVENT,
  acceptAllOptionalCookies,
  emptyOptionalCookieConsent,
  loadCookieConsent,
  normalizeCookieConsent,
  optionalCookieConsentCategories,
  rejectOptionalCookies,
  saveCookieConsent,
  type CookieConsent,
  type CookieConsentCategories,
} from "@/lib/cookie-consent";
import { routes } from "@/lib/routes";

const categoryLabels: Record<keyof CookieConsentCategories, { title: string; text: string }> = {
  analytics: {
    title: "Аналитика",
    text: "Помогает понимать посещаемость и улучшать каталог без включения рекламного профилирования.",
  },
  marketing: {
    title: "Маркетинг",
    text: "Нужен для рекламных пикселей, ретаргетинга и оценки эффективности кампаний.",
  },
  functional: {
    title: "Внешние сервисы",
    text: "Позволяет подключать карты, чаты, видео и другие встраиваемые сервисы, которые могут ставить cookie.",
  },
};

type ConsentMode = "banner" | "settings" | null;

function getConsentSnapshot() {
  if (typeof window === "undefined") {
    return "";
  }

  return window.localStorage.getItem(COOKIE_CONSENT_STORAGE_KEY) ?? "";
}

function subscribeConsentChange(onStoreChange: () => void) {
  window.addEventListener(COOKIE_CONSENT_CHANGE_EVENT, onStoreChange);
  window.addEventListener("storage", onStoreChange);

  return () => {
    window.removeEventListener(COOKIE_CONSENT_CHANGE_EVENT, onStoreChange);
    window.removeEventListener("storage", onStoreChange);
  };
}

function parseConsentSnapshot(rawConsent: string): CookieConsent | null {
  if (!rawConsent) {
    return null;
  }

  try {
    return normalizeCookieConsent(JSON.parse(rawConsent));
  } catch {
    return null;
  }
}

function getStoredCategories(): CookieConsentCategories {
  return loadCookieConsent()?.categories ?? emptyOptionalCookieConsent;
}

export function CookieConsentBanner() {
  const rawConsent = useSyncExternalStore(subscribeConsentChange, getConsentSnapshot, () => "");
  const storedConsent = useMemo(() => parseConsentSnapshot(rawConsent), [rawConsent]);
  const [modeOverride, setModeOverride] = useState<ConsentMode>(null);
  const [draftCategories, setDraftCategories] = useState<CookieConsentCategories>(emptyOptionalCookieConsent);
  const mode = modeOverride ?? (storedConsent ? null : "banner");

  useEffect(() => {
    const handleSettingsOpen = () => {
      setDraftCategories(getStoredCategories());
      setModeOverride("settings");
    };

    window.addEventListener(COOKIE_SETTINGS_OPEN_EVENT, handleSettingsOpen);

    return () => {
      window.removeEventListener(COOKIE_SETTINGS_OPEN_EVENT, handleSettingsOpen);
    };
  }, []);

  const hasStoredConsent = storedConsent !== null;

  const acceptAll = () => {
    acceptAllOptionalCookies();
    setModeOverride(null);
  };

  const rejectAll = () => {
    rejectOptionalCookies();
    setModeOverride(null);
  };

  const saveSettings = () => {
    saveCookieConsent(draftCategories);
    setModeOverride(null);
  };

  const toggleCategory = (category: keyof CookieConsentCategories) => {
    setDraftCategories((current) => ({
      ...current,
      [category]: !current[category],
    }));
  };

  if (!mode) {
    return null;
  }

  return (
    <div className="cookie-consent" role="region" aria-label="Настройки cookie">
      <div className="cookie-consent__panel">
        <div className="cookie-consent__content">
          <p className="cookie-consent__eyebrow">Cookie и внешние сервисы</p>
          <h2>{mode === "settings" ? "Настройки cookie" : "Мы используем cookie"}</h2>
          <p>
            Необходимые cookie нужны для работы сайта, авторизации и защиты форм. Аналитику,
            маркетинг и внешние сервисы включаем только после вашего согласия.
          </p>
          <a className="cookie-consent__link" href={routes.cookiePolicy()}>
            Подробнее о категориях cookie
          </a>
        </div>

        {mode === "settings" ? (
          <div className="cookie-consent__settings" aria-label="Категории cookie">
            <div className="cookie-consent__required">
              <span>
                <strong>Необходимые</strong>
                <small>Всегда включены</small>
              </span>
            </div>
            {optionalCookieConsentCategories.map((category) => (
              <label key={category} className="cookie-consent__option">
                <span>
                  <strong>{categoryLabels[category].title}</strong>
                  <small>{categoryLabels[category].text}</small>
                </span>
                <input
                  type="checkbox"
                  checked={draftCategories[category]}
                  onChange={() => toggleCategory(category)}
                />
              </label>
            ))}
          </div>
        ) : null}

        <div className="cookie-consent__actions">
          {mode === "banner" ? (
            <button
              className="button button--ghost"
              type="button"
              onClick={() => {
                setDraftCategories(getStoredCategories());
                setModeOverride("settings");
              }}
            >
              Настроить
            </button>
          ) : hasStoredConsent ? (
            <button className="button button--ghost" type="button" onClick={() => setModeOverride(null)}>
              Закрыть
            </button>
          ) : null}
          <button className="button button--secondary" type="button" onClick={rejectAll}>
            Отклонить необязательные
          </button>
          {mode === "settings" ? (
            <button className="button button--primary" type="button" onClick={saveSettings}>
              Сохранить выбор
            </button>
          ) : (
            <button className="button button--primary" type="button" onClick={acceptAll}>
              Принять все
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
