"use client";

import { useState, type MouseEvent } from "react";
import Link from "next/link";
import Image from "next/image";
import { useAuth } from "@/components/auth/auth-provider";
import { useRequestDraft } from "@/components/request/request-draft-provider";
import { getDraftItemsCount } from "@/lib/request-draft/selectors";
import { routes } from "@/lib/routes";
import { siteFeatures } from "@/lib/site-features";
import { ThemeToggle } from "./theme-toggle";

const MOBILE_MENU_QUERY = "(max-width: 860px)";

const navItems = [
  { href: routes.home(), label: "Главная", mobileOnly: true },
  { href: routes.catalog(), label: "Каталог" },
  { href: routes.contacts(), label: "Контакты" },
  { href: routes.delivery(), label: "Доставка" },
  ...(siteFeatures.customerRequests ? [{ href: routes.request(), label: "Заявка" }] : []),
];

const accountItems = [
  { href: routes.accountProfile(), label: "Профиль" },
  ...(siteFeatures.customerRequests ? [{ href: routes.accountRequests(), label: "История заказов" }] : []),
];

const adminItems = [
  { href: routes.adminRequests(), label: "Заявки клиентов" },
  { href: routes.adminCatalog(), label: "Каталог админки" },
  { href: routes.adminHomepage(), label: "Главная админки" },
];

export function SiteHeader() {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isAdminMenuOpen, setIsAdminMenuOpen] = useState(false);
  const [isAccountMenuOpen, setIsAccountMenuOpen] = useState(false);
  const [isLoggingOut, setIsLoggingOut] = useState(false);
  const { user, logoutSession } = useAuth();
  const { state } = useRequestDraft();
  const draftItemsCount = siteFeatures.customerRequests ? getDraftItemsCount(state) : 0;
  const isStaff = user?.role === "seller" || user?.role === "admin";

  const handleBrandClick = (event: MouseEvent<HTMLAnchorElement>) => {
    if (typeof window === "undefined" || !window.matchMedia(MOBILE_MENU_QUERY).matches) {
      return;
    }

    event.preventDefault();
    setIsMenuOpen((isOpen) => !isOpen);
  };

  const closeMenu = () => {
    setIsMenuOpen(false);
    setIsAdminMenuOpen(false);
    setIsAccountMenuOpen(false);
  };

  const toggleAdminMenu = () => {
    setIsAccountMenuOpen(false);
    setIsAdminMenuOpen((isOpen) => !isOpen);
  };

  const toggleAccountMenu = () => {
    setIsAdminMenuOpen(false);
    setIsAccountMenuOpen((isOpen) => !isOpen);
  };

  const handleLogout = async () => {
    setIsLoggingOut(true);

    try {
      await logoutSession();
      closeMenu();
    } catch {
      // Keep the current session visible when logout fails for a non-auth reason.
    } finally {
      setIsLoggingOut(false);
    }
  };

  return (
    <header className="site-header">
      <div className="site-header__inner">
        <Link
          className="site-header__brand"
          href={routes.home()}
          prefetch={false}
          aria-label="LineCom"
          aria-controls="site-header-menu"
          aria-expanded={isMenuOpen}
          onClick={handleBrandClick}
        >
          <Image
            className="site-header__logo"
            src="/linecom-logo-full.png"
            alt="LineCom - кабель и оптоволокно"
            width={1297}
            height={373}
            preload
          />
        </Link>

        <div
          id="site-header-menu"
          className={`site-header__menu${isMenuOpen ? " site-header__menu--open" : ""}`}
        >
          <nav className="site-header__nav" aria-label="Основная навигация">
            {navItems.map((item) => {
              const isRequestLink = item.href === routes.request();

              return (
                <Link
                  key={item.href}
                  className={`site-header__link${item.mobileOnly ? " site-header__link--mobile-only" : ""}`}
                  href={item.href}
                  prefetch={item.href === routes.home() || item.href === routes.delivery() ? false : undefined}
                  onClick={closeMenu}
                >
                  <span>{item.label}</span>
                  {isRequestLink && draftItemsCount > 0 ? (
                    <span className="site-header__badge">{draftItemsCount}</span>
                  ) : null}
                </Link>
              );
            })}
            {isStaff ? (
              <div className="site-header__dropdown">
                <button
                  className="site-header__link site-header__dropdown-button"
                  type="button"
                  aria-expanded={isAdminMenuOpen}
                  aria-controls="site-header-admin-menu"
                  onClick={toggleAdminMenu}
                >
                  Администрирование
                </button>
                {isAdminMenuOpen ? (
                  <div id="site-header-admin-menu" className="site-header__dropdown-menu">
                    {adminItems.map((item) => (
                      <Link key={item.href} className="site-header__dropdown-link" href={item.href} onClick={closeMenu}>
                        {item.label}
                      </Link>
                    ))}
                  </div>
                ) : null}
              </div>
            ) : null}
          </nav>

          <div className="site-header__actions">
            <ThemeToggle />
            {user ? (
              <div className="site-header__dropdown site-header__user">
                <button
                  className="site-header__user-button"
                  type="button"
                  aria-expanded={isAccountMenuOpen}
                  aria-controls="site-header-account-menu"
                  onClick={toggleAccountMenu}
                >
                  <span className="site-header__user-name">{user.name}</span>
                </button>
                {isAccountMenuOpen ? (
                  <div id="site-header-account-menu" className="site-header__dropdown-menu site-header__dropdown-menu--account">
                    {accountItems.map((item) => (
                      <Link key={item.href} className="site-header__dropdown-link" href={item.href} onClick={closeMenu}>
                        {item.label}
                      </Link>
                    ))}
                    <button
                      className="site-header__dropdown-link site-header__dropdown-action"
                      disabled={isLoggingOut}
                      type="button"
                      onClick={handleLogout}
                    >
                      Выйти
                    </button>
                  </div>
                ) : null}
              </div>
            ) : null}
          </div>
        </div>
      </div>
    </header>
  );
}
