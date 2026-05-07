"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { RegisterForm } from "@/components/auth/register-form";
import { useAuth } from "@/components/auth/auth-provider";
import { register, type RegisterPayload } from "@/lib/api/auth";
import { normalizeApiError } from "@/lib/api/errors";
import { routes } from "@/lib/routes";

export function RegisterPageClient() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { setSession } = useAuth();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const returnTo = safeReturnTo(searchParams.get("returnTo"));

  async function handleSubmit(payload: RegisterPayload) {
    setErrorMessage(null);

    try {
      const session = await register(payload);
      setSession(session);
      router.push(returnTo);
    } catch (error) {
      setErrorMessage(normalizeApiError(error).message);
    }
  }

  return (
    <div className="auth-page">
      <section className="auth-card" aria-labelledby="register-title">
        <div className="auth-card__copy">
          <p className="eyebrow">Новый клиент</p>
          <h1 id="register-title">Регистрация для заявок</h1>
          <p className="lead-text">
            Укажите контактные данные. Организацию можно добавить позже в профиле, чтобы не задерживать отправку заявки.
          </p>
        </div>

        <div>
          <RegisterForm onSubmit={handleSubmit} errorMessage={errorMessage} />
          <p className="auth-switch">
            Уже есть аккаунт? <Link className="text-link" href={routes.login(returnTo)}>Войти</Link>
          </p>
        </div>
      </section>
    </div>
  );
}

function safeReturnTo(value: string | null): string {
  if (value && value.startsWith("/") && !value.startsWith("//")) {
    return value;
  }

  return routes.request();
}
