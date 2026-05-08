"use client";

import { useEffect, useSyncExternalStore } from "react";

type Theme = "light" | "dark";

const STORAGE_KEY = "linecom.theme";
const CHANGE_EVENT = "linecom-theme-change";

function getPreferredTheme(): Theme {
  if (typeof window === "undefined") {
    return "light";
  }

  const stored = window.localStorage.getItem(STORAGE_KEY);
  if (stored === "light" || stored === "dark") {
    return stored;
  }

  if (typeof window.matchMedia !== "function") {
    return "light";
  }

  return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

function applyTheme(theme: Theme) {
  document.documentElement.dataset.theme = theme;
  document.documentElement.style.colorScheme = theme;
}

function getThemeSnapshot(): Theme {
  if (typeof document === "undefined") {
    return "light";
  }

  const theme = document.documentElement.dataset.theme;
  return theme === "dark" || theme === "light" ? theme : getPreferredTheme();
}

function subscribeThemeChange(onStoreChange: () => void) {
  window.addEventListener(CHANGE_EVENT, onStoreChange);
  window.addEventListener("storage", onStoreChange);

  return () => {
    window.removeEventListener(CHANGE_EVENT, onStoreChange);
    window.removeEventListener("storage", onStoreChange);
  };
}

function setThemePreference(theme: Theme) {
  window.localStorage.setItem(STORAGE_KEY, theme);
  applyTheme(theme);
  window.dispatchEvent(new Event(CHANGE_EVENT));
}

export function ThemeToggle() {
  const theme = useSyncExternalStore(subscribeThemeChange, getThemeSnapshot, () => "light");

  useEffect(() => {
    const preferredTheme = getPreferredTheme();
    applyTheme(preferredTheme);
    window.dispatchEvent(new Event(CHANGE_EVENT));
  }, []);

  const nextTheme = theme === "dark" ? "light" : "dark";
  const label = theme === "dark" ? "Включить светлую тему" : "Включить темную тему";

  return (
    <button
      type="button"
      className={`theme-toggle theme-toggle--${theme}`}
      aria-label={label}
      aria-pressed={theme === "dark"}
      title={label}
      onClick={() => {
        setThemePreference(nextTheme);
      }}
    >
      <span className="theme-toggle__icon theme-toggle__icon--light" aria-hidden="true">
        ☀
      </span>
      <span className="theme-toggle__knob" aria-hidden="true" />
      <span className="theme-toggle__icon theme-toggle__icon--dark" aria-hidden="true">
        ☾
      </span>
    </button>
  );
}
