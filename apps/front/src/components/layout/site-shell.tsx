import type { ReactNode } from "react";
import Link from "next/link";
import { CookieConsentBanner } from "@/components/cookie-consent/cookie-consent-banner";
import { CookieSettingsButton } from "@/components/cookie-consent/cookie-settings-button";
import { routes } from "@/lib/routes";
import { FooterLoginLink } from "./footer-login-link";
import { SiteHeader } from "./site-header";

export function SiteShell({ children }: { children: ReactNode }) {
  return (
    <div className="site-shell">
      <SiteHeader />
      <main className="site-main">{children}</main>
      <footer className="site-footer">
        <div className="site-footer__inner">
          <div>
            <strong>LineCom</strong>
            <p>Кабель, оптика и телеком-компоненты для B2B-поставок.</p>
          </div>
          <nav className="site-footer__nav" aria-label="Правовая и служебная навигация">
            <FooterLoginLink />
            <Link className="site-footer__link" href={routes.contacts()}>
              Контакты
            </Link>
            <Link className="site-footer__link" href={routes.delivery()}>
              Доставка
            </Link>
            <Link className="site-footer__link" href={routes.cookiePolicy()}>
              Cookie
            </Link>
            <CookieSettingsButton />
          </nav>
        </div>
      </footer>
      <CookieConsentBanner />
    </div>
  );
}
