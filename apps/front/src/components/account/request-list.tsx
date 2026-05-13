"use client";

import Link from "next/link";
import type { CustomerRequestListItem } from "@/lib/api/requests";
import { formatDateTime } from "@/lib/format";
import { routes } from "@/lib/routes";

type RequestListProps = {
  requests: CustomerRequestListItem[];
  status: string;
  onStatusChange: (status: string) => void;
  onPreviewRequest: (number: string) => void;
};

const statusOptions = [
  { value: "all", label: "Все статусы" },
  { value: "new", label: "Новые" },
  { value: "in_progress", label: "В работе" },
  { value: "completed", label: "Завершенные" },
  { value: "cancelled", label: "Отмененные" },
];

export function RequestList({ requests, status, onStatusChange, onPreviewRequest }: RequestListProps) {
  return (
    <section className="account-section request-list-section" aria-labelledby="request-list-title">
      <div className="request-list-section__header">
        <div>
          <h2 id="request-list-title">История заявок</h2>
          <p>Здесь собраны обращения, отправленные из личного кабинета.</p>
        </div>

        <label className="status-filter">
          <span>Статус заявок</span>
          <select value={status} onChange={(event) => onStatusChange(event.target.value)}>
            {statusOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
      </div>

      {requests.length === 0 ? (
        <p className="empty-state">У вас пока нет заявок</p>
      ) : (
        <div className="request-list">
          {requests.map((request) => (
            <article className="request-list-card" key={request.number}>
              <div className="request-list-card__main">
                <div>
                  <p className="eyebrow">{formatDateTime(request.createdAt)}</p>
                  <h3>{request.number}</h3>
                </div>
                <span className="status-pill">{request.status.label}</span>
              </div>

              <dl className="summary-grid request-list-card__meta">
                <div>
                  <dt>Источник</dt>
                  <dd>{formatSource(request.source)}</dd>
                </div>
                <div>
                  <dt>Состав</dt>
                  <dd>{formatItemsCount(request.itemsCount)}</dd>
                </div>
              </dl>

              {request.customerComment ? <p className="request-list-card__comment">{request.customerComment}</p> : null}

              <div className="request-list-card__actions">
                <button
                  className="button button--ghost request-list-card__preview"
                  type="button"
                  onClick={() => onPreviewRequest(request.number)}
                >
                  Быстрый просмотр {request.number}
                </button>
                <Link className="button button--ghost request-list-card__link" href={routes.accountRequest(request.number)}>
                  Открыть заявку {request.number}
                </Link>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}

function formatSource(source: string): string {
  if (source === "cart") {
    return "Черновик заявки";
  }

  if (source === "quick_order") {
    return "Быстрая заявка";
  }

  return source;
}

function formatItemsCount(count: number): string {
  const mod10 = count % 10;
  const mod100 = count % 100;

  if (mod10 === 1 && mod100 !== 11) {
    return `${count} позиция`;
  }

  if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) {
    return `${count} позиции`;
  }

  return `${count} позиций`;
}
