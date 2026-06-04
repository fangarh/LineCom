"use client";

import { useEffect } from "react";
import { lineComContact } from "@/lib/contact-info";

type ContactDialogProps = {
  isOpen: boolean;
  onClose: () => void;
};

export function ContactDialog({ isOpen, onClose }: ContactDialogProps) {
  useEffect(() => {
    if (!isOpen) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) {
    return null;
  }

  return (
    <div className="contact-dialog" role="presentation" onMouseDown={onClose}>
      <section
        className="contact-dialog__panel"
        role="dialog"
        aria-modal="true"
        aria-labelledby="contact-dialog-title"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <button className="contact-dialog__close" type="button" aria-label="Закрыть" onClick={onClose}>
          ×
        </button>

        <div className="contact-dialog__head">
          <p className="eyebrow">Контакт LineCom</p>
          <h2 id="contact-dialog-title">Связаться с нами</h2>
        </div>

        <dl className="contact-dialog__details">
          <div>
            <dt>ФИО</dt>
            <dd>{lineComContact.name}</dd>
          </div>
          <div>
            <dt>Должность</dt>
            <dd>{lineComContact.role}</dd>
          </div>
          <div>
            <dt>Телефон</dt>
            <dd>
              <a href={lineComContact.phoneHref}>{lineComContact.phone}</a>
            </dd>
          </div>
        </dl>

        <div className="contact-dialog__actions">
          <a className="button button--primary" href={lineComContact.phoneHref}>
            Позвонить
          </a>
          <a className="button button--secondary" href={lineComContact.emailHref}>
            Написать
          </a>
        </div>
      </section>
    </div>
  );
}
