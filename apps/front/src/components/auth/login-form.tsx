"use client";

import { useState } from "react";
import type { LoginPayload } from "@/lib/api/auth";

type LoginFormProps = {
  onSubmit: (payload: LoginPayload) => Promise<void>;
  errorMessage?: string | null;
};

export function LoginForm({ onSubmit, errorMessage }: LoginFormProps) {
  const [login, setLogin] = useState("");
  const [password, setPassword] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSubmitting(true);

    try {
      await onSubmit({ login, password });
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form className="form-panel" onSubmit={handleSubmit}>
      {errorMessage ? (
        <p className="form-alert" role="alert">
          {errorMessage}
        </p>
      ) : null}

      <label className="form-field">
        <span>Email или телефон</span>
        <input
          autoComplete="username"
          name="login"
          required
          type="text"
          value={login}
          onChange={(event) => setLogin(event.target.value)}
        />
      </label>

      <label className="form-field">
        <span>Пароль</span>
        <input
          autoComplete="current-password"
          name="password"
          required
          type="password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
        />
      </label>

      <button className="button button--primary form-submit" disabled={isSubmitting} type="submit">
        Войти
      </button>
    </form>
  );
}
