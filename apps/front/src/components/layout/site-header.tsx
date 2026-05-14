"use client";

import { useState, type MouseEvent } from "react";
import Link from "next/link";
import Image from "next/image";
import { useAuth } from "@/components/auth/auth-provider";
import { useRequestDraft } from "@/components/request/request-draft-provider";
import { getDraftItemsCount } from "@/lib/request-draft/selectors";
import { routes } from "@/lib/routes";
import { ThemeToggle } from "./theme-toggle";

const MOBILE_MENU_QUERY = "(max-width: 860px)";

const navItems = [
  { href: routes.home(), label: "Главная", mobileOnly: true },
  { href: routes.catalog(), label: "Каталог" },
  { href: routes.about(), label: "О нас" },
  { href: routes.delivery(), label: "Доставка" },
  { href: routes.request(), label: "Заявка" },
];

const accountItems = [
  { href: routes.accountProfile(), label: "Профиль" },
  { href: routes.accountRequests(), label: "Мои заявки" },
];

const adminItems = [
  { href: routes.adminRequests(), label: "Заявки клиентов" },
  { href: routes.adminCatalog(), label: "Каталог админки" },
  { href: routes.adminHomepage(), label: "Главная админки" },
];

export function SiteHeader() {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isAdminMenuOpen, setIsAdminMenuOpen] = useState(false);
  const [isLoggingOut, setIsLoggingOut] = useState(false);
  const { user, logoutSession } = useAuth();
  const { state } = useRequestDraft();
  const draftItemsCount = getDraftItemsCount(state);
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
  };

  const toggleAdminMenu = () => {
    setIsAdminMenuOpen((isOpen) => !isOpen);
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
                  prefetch={item.href === routes.home() ? false : undefined}
                  onClick={closeMenu}
                >
                  <span>{item.label}</span>
                  {isRequestLink && draftItemsCount > 0 ? (
                    <span className="site-header__badge">{draftItemsCount}</span>
                  ) : null}
                </Link>
              );
            })}
            {user
              ? accountItems.map((item) => (
                  <Link key={item.href} className="site-header__link" href={item.href} onClick={closeMenu}>
                    <span>{item.label}</span>
                  </Link>
                ))
              : null}
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
              <div className="site-header__user">
                <span className="site-header__user-name">{user.name}</span>
                <button
                  className="button button--ghost site-header__logout"
                  disabled={isLoggingOut}
                  type="button"
                  onClick={handleLogout}
                >
                  Выйти
                </button>
              </div>
            ) : (
              <Link className="button button--ghost site-header__login" href={routes.login()} onClick={closeMenu}>
                Войти
              </Link>
            )}
          </div>
        </div>
      </div>
    </header>
  );
}
