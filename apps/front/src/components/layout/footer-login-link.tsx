"use client";

import Link from "next/link";
import { useAuth } from "@/components/auth/auth-provider";
import { routes } from "@/lib/routes";

export function FooterLoginLink() {
  const { user } = useAuth();

  if (user) {
    return null;
  }

  return (
    <Link className="site-footer__link" href={routes.login()}>
      Войти
    </Link>
  );
}
