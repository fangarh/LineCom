"use client";

import { useState } from "react";
import type { UpdateAccountProfilePayload } from "@/lib/api/account";

type ProfileFormProps = {
  initialValue: UpdateAccountProfilePayload;
  onSubmit: (payload: UpdateAccountProfilePayload) => Promise<void>;
  errorMessage?: string | null;
  successMessage?: string | null;
};

export function ProfileForm({ initialValue, onSubmit, errorMessage, successMessage }: ProfileFormProps) {
  const [name, setName] = useState(initialValue.name);
  const [email, setEmail] = useState(initialValue.email ?? "");
  const [phone, setPhone] = useState(initialValue.phone ?? "");
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSubmitting(true);

    try {
      await onSubmit({
        name,
        email: nullable(email),
        phone: nullable(phone),
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
      {successMessage ? <p className="form-success">{successMessage}</p> : null}

      <label className="form-field">
        <span>Имя</span>
        <input name="name" required type="text" value={name} onChange={(event) => setName(event.target.value)} />
      </label>

      <label className="form-field">
        <span>Email</span>
        <input name="email" type="email" value={email} onChange={(event) => setEmail(event.target.value)} />
      </label>

      <label className="form-field">
        <span>Телефон</span>
        <input name="phone" type="tel" value={phone} onChange={(event) => setPhone(event.target.value)} />
      </label>

      <button className="button button--primary form-submit" disabled={isSubmitting} type="submit">
        Сохранить профиль
      </button>
    </form>
  );
}

function nullable(value: string): string | null {
  const trimmed = value.trim();
  return trimmed ? trimmed : null;
}
