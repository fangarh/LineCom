"use client";

import { useState } from "react";
import type { ChangePasswordPayload } from "@/lib/api/account";

type PasswordFormProps = {
  onSubmit: (payload: ChangePasswordPayload) => Promise<void>;
  errorMessage?: string | null;
};

export function PasswordForm({ onSubmit, errorMessage }: PasswordFormProps) {
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [repeatPassword, setRepeatPassword] = useState("");
  const [clientError, setClientError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setClientError(null);
    setSuccessMessage(null);

    if (newPassword !== repeatPassword) {
      setClientError("Новый пароль и повтор не совпадают.");
      return;
    }

    setIsSubmitting(true);
    try {
      await onSubmit({ currentPassword, newPassword });
      setCurrentPassword("");
      setNewPassword("");
      setRepeatPassword("");
      setSuccessMessage("Пароль изменен.");
    } catch {
      return;
    } finally {
      setIsSubmitting(false);
    }
  }

  const visibleError = clientError ?? errorMessage;

  return (
    <form className="form-panel" onSubmit={handleSubmit}>
      {visibleError ? (
        <p className="form-alert" role="alert">
          {visibleError}
        </p>
      ) : null}
      {successMessage ? <p className="form-success">{successMessage}</p> : null}

      <label className="form-field">
        <span>Текущий пароль</span>
        <input
          autoComplete="current-password"
          name="currentPassword"
          required
          type="password"
          value={currentPassword}
          onChange={(event) => setCurrentPassword(event.target.value)}
        />
      </label>

      <label className="form-field">
        <span>Новый пароль</span>
        <input
          autoComplete="new-password"
          maxLength={128}
          minLength={8}
          name="newPassword"
          required
          type="password"
          value={newPassword}
          onChange={(event) => setNewPassword(event.target.value)}
        />
      </label>

      <label className="form-field">
        <span>Повтор нового пароля</span>
        <input
          autoComplete="new-password"
          maxLength={128}
          minLength={8}
          name="repeatPassword"
          required
          type="password"
          value={repeatPassword}
          onChange={(event) => setRepeatPassword(event.target.value)}
        />
      </label>

      <button className="button button--primary form-submit" disabled={isSubmitting} type="submit">
        Сменить пароль
      </button>
    </form>
  );
}
