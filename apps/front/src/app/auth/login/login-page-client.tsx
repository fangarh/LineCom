"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { LoginForm } from "@/components/auth/login-form";
import { useAuth } from "@/components/auth/auth-provider";
import { login, type LoginPayload } from "@/lib/api/auth";
import { normalizeApiError } from "@/lib/api/errors";
import { routes } from "@/lib/routes";
import { siteFeatures } from "@/lib/site-features";

export function LoginPageClient() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { setSession } = useAuth();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const returnTo = safeReturnTo(searchParams.get("returnTo"));

  async function handleSubmit(payload: LoginPayload) {
    setErrorMessage(null);

    try {
      const session = await login(payload);
      setSession(session);
      router.push(returnTo);
    } catch (error) {
      setErrorMessage(normalizeApiError(error).message);
    }
  }

  return (
    <div className="auth-page">
      <section className="auth-card" aria-labelledby="login-title">
        <div className="auth-card__copy">
          <p className="eyebrow">Аккаунт клиента</p>
          <h1 id="login-title">Вход в LineCom</h1>
          <p className="lead-text">
            Войдите, чтобы заполнить профиль и работать с доступными разделами личного кабинета.
          </p>
        </div>

        <div>
          <LoginForm onSubmit={handleSubmit} errorMessage={errorMessage} />
          {siteFeatures.customerRegistration ? (
            <p className="auth-switch">
              Нет аккаунта? <Link className="text-link" href={routes.register(returnTo)}>Зарегистрироваться</Link>
            </p>
          ) : null}
        </div>
      </section>
    </div>
  );
}

function safeReturnTo(value: string | null): string {
  if (value && value.startsWith("/") && !value.startsWith("//")) {
    return value;
  }

  return routes.home();
}
