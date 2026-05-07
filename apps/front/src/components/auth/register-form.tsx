"use client";

import { useState } from "react";
import type { RegisterPayload } from "@/lib/api/auth";

type RegisterFormProps = {
  onSubmit: (payload: RegisterPayload) => Promise<void>;
  errorMessage?: string | null;
};

export function RegisterForm({ onSubmit, errorMessage }: RegisterFormProps) {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [password, setPassword] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSubmitting(true);

    try {
      await onSubmit({
        name,
        email: nullable(email),
        phone: nullable(phone),
        password,
      });
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
        <span>Имя</span>
        <input
          autoComplete="name"
          name="name"
          required
          type="text"
          value={name}
          onChange={(event) => setName(event.target.value)}
        />
      </label>

      <label className="form-field">
        <span>Email</span>
        <input
          autoComplete="email"
          name="email"
          type="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
        />
      </label>

      <label className="form-field">
        <span>Телефон</span>
        <input
          autoComplete="tel"
          name="phone"
          type="tel"
          value={phone}
          onChange={(event) => setPhone(event.target.value)}
        />
      </label>

      <label className="form-field">
        <span>Пароль</span>
        <input
          autoComplete="new-password"
          minLength={8}
          name="password"
          required
          type="password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
        />
      </label>

      <button className="button button--primary form-submit" disabled={isSubmitting} type="submit">
        Зарегистрироваться
      </button>
    </form>
  );
}

function nullable(value: string): string | null {
  const trimmed = value.trim();
  return trimmed ? trimmed : null;
}
