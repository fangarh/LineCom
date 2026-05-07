"use client";

import { useState } from "react";
import type { AccountOrganization, UpsertOrganizationPayload } from "@/lib/api/account";

type OrganizationFormProps = {
  initialValue: AccountOrganization | null;
  onSubmit: (payload: UpsertOrganizationPayload) => Promise<void>;
  errorMessage?: string | null;
  successMessage?: string | null;
};

const emptyOrganization: UpsertOrganizationPayload = {
  name: "",
  inn: null,
  contactPerson: null,
  phone: null,
  email: null,
  comment: null,
};

export function OrganizationForm({ initialValue, onSubmit, errorMessage, successMessage }: OrganizationFormProps) {
  const [name, setName] = useState(initialValue?.name ?? emptyOrganization.name);
  const [inn, setInn] = useState(initialValue?.inn ?? "");
  const [contactPerson, setContactPerson] = useState(initialValue?.contactPerson ?? "");
  const [phone, setPhone] = useState(initialValue?.phone ?? "");
  const [email, setEmail] = useState(initialValue?.email ?? "");
  const [comment, setComment] = useState(initialValue?.comment ?? "");
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSubmitting(true);

    try {
      await onSubmit({
        name,
        inn: nullable(inn),
        contactPerson: nullable(contactPerson),
        phone: nullable(phone),
        email: nullable(email),
        comment: nullable(comment),
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
        <span>Название организации</span>
        <input name="organizationName" required type="text" value={name} onChange={(event) => setName(event.target.value)} />
      </label>

      <label className="form-field">
        <span>ИНН</span>
        <input name="inn" type="text" value={inn} onChange={(event) => setInn(event.target.value)} />
      </label>

      <label className="form-field">
        <span>Контактное лицо</span>
        <input
          name="contactPerson"
          type="text"
          value={contactPerson}
          onChange={(event) => setContactPerson(event.target.value)}
        />
      </label>

      <label className="form-field">
        <span>Телефон организации</span>
        <input name="organizationPhone" type="tel" value={phone} onChange={(event) => setPhone(event.target.value)} />
      </label>

      <label className="form-field">
        <span>Email организации</span>
        <input name="organizationEmail" type="email" value={email} onChange={(event) => setEmail(event.target.value)} />
      </label>

      <label className="form-field">
        <span>Комментарий</span>
        <textarea name="comment" rows={4} value={comment} onChange={(event) => setComment(event.target.value)} />
      </label>

      <button className="button button--primary form-submit" disabled={isSubmitting} type="submit">
        Сохранить организацию
      </button>
    </form>
  );
}

function nullable(value: string): string | null {
  const trimmed = value.trim();
  return trimmed ? trimmed : null;
}
