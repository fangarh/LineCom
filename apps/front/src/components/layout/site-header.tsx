"use client";

import { useEffect, useState, type MouseEvent } from "react";
import Link from "next/link";
import Image from "next/image";
import { useAuth } from "@/components/auth/auth-provider";
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

const accountNavItems = [
  { href: routes.accountRequests(), label: "Мои заявки" },
  { href: routes.accountProfile(), label: "Профиль" },
];

const adminNavItems = [
  { href: routes.adminRequests(), label: "Заявки клиентов" },
  { href: routes.adminCatalog(), label: "Каталог админки" },
  { href: routes.adminHomepage(), label: "Главная админки" },
];

export function SiteHeader() {
  const { user, isRestoringSession, restoreSession, logoutSession } = useAuth();
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isLoggingOut, setIsLoggingOut] = useState(false);
  const canUseAdminNavigation = user?.role === "seller" || user?.role === "admin";

  useEffect(() => {
    restoreSession();
  }, [restoreSession]);

  const handleBrandClick = (event: MouseEvent<HTMLAnchorElement>) => {
    if (typeof window === "undefined" || !window.matchMedia(MOBILE_MENU_QUERY).matches) {
      return;
    }

    event.preventDefault();
    setIsMenuOpen((isOpen) => !isOpen);
  };

  const closeMenu = () => {
    setIsMenuOpen(false);
  };

  const handleLogout = async () => {
    setIsLoggingOut(true);

    try {
      await logoutSession();
      closeMenu();
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
          aria-busy={isRestoringSession}
        >
          <nav className="site-header__nav" aria-label="Основная навигация">
            {navItems.map((item) => (
              <Link
                key={item.href}
                className={`site-header__link${item.mobileOnly ? " site-header__link--mobile-only" : ""}`}
                href={item.href}
                prefetch={item.href === routes.home() ? false : undefined}
                onClick={closeMenu}
              >
                {item.label}
              </Link>
            ))}
            {user
              ? accountNavItems.map((item) => (
                  <Link
                    key={item.href}
                    className="site-header__link site-header__link--account"
                    href={item.href}
                    onClick={closeMenu}
                  >
                    {item.label}
                  </Link>
                ))
              : null}
            {canUseAdminNavigation
              ? adminNavItems.map((item) => (
                  <Link
                    key={item.href}
                    className="site-header__link site-header__link--admin"
                    href={item.href}
                    onClick={closeMenu}
                  >
                    {item.label}
                  </Link>
                ))
              : null}
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
            ) : isRestoringSession ? null : (
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
