import Link from "next/link";
import Image from "next/image";
import { routes } from "@/lib/routes";
import { ThemeToggle } from "./theme-toggle";

const navItems = [
  { href: routes.catalog(), label: "Каталог" },
  { href: routes.request(), label: "Заявка" },
  { href: routes.accountRequests(), label: "Мои заявки" },
];

export function SiteHeader() {
  return (
    <header className="site-header">
      <div className="site-header__inner">
        <Link className="site-header__brand" href={routes.home()} aria-label="LineCom">
          <Image
            className="site-header__logo"
            src="/linecom-logo-full.png"
            alt="LineCom - кабель и оптоволокно"
            width={1297}
            height={373}
            priority
          />
        </Link>

        <nav className="site-header__nav" aria-label="Основная навигация">
          {navItems.map((item) => (
            <Link key={item.href} className="site-header__link" href={item.href}>
              {item.label}
            </Link>
          ))}
        </nav>

        <div className="site-header__actions">
          <ThemeToggle />
          <Link className="button button--ghost site-header__login" href={routes.login()}>
            Войти
          </Link>
        </div>
      </div>
    </header>
  );
}
