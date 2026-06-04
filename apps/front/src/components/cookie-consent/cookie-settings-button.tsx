"use client";

import { openCookieSettings } from "@/lib/cookie-consent";

export function CookieSettingsButton() {
  return (
    <button className="site-footer__link site-footer__button" type="button" onClick={openCookieSettings}>
      Настройки cookie
    </button>
  );
}
