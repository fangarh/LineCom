"use client";

import { useState, type MouseEvent } from "react";
import Link from "next/link";
import Image from "next/image";
import { routes } from "@/lib/routes";
import { ThemeToggle } from "./theme-toggle";

const MOBILE_MENU_QUERY = "(max-width: 860px)";

const navItems = [
  { href: routes.home(), label: "Главная", mobileOnly: true },
  { href: routes.catalog(), label: "Каталог" },
  { href: routes.about(), label: "О нас" },
  { href: routes.delivery(), label: "Доставка" },
  { href: routes.request(), label: "Заявка" },
  { href: routes.accountRequests(), label: "Мои заявки" },
];

export function SiteHeader() {
  const [isMenuOpen, setIsMenuOpen] = useState(false);

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

  return (
    <header className="site-header">
      <div className="site-header__inner">
        <Link
          className="site-header__brand"
          href={routes.home()}
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
            priority
          />
        </Link>

        <div
          id="site-header-menu"
          className={`site-header__menu${isMenuOpen ? " site-header__menu--open" : ""}`}
        >
          <nav className="site-header__nav" aria-label="Основная навигация">
            {navItems.map((item) => (
              <Link
                key={item.href}
                className={`site-header__link${item.mobileOnly ? " site-header__link--mobile-only" : ""}`}
                href={item.href}
                onClick={closeMenu}
              >
                {item.label}
              </Link>
            ))}
          </nav>

          <div className="site-header__actions">
            <ThemeToggle />
            <Link className="button button--ghost site-header__login" href={routes.login()} onClick={closeMenu}>
              Войти
            </Link>
          </div>
        </div>
      </div>
    </header>
  );
}
