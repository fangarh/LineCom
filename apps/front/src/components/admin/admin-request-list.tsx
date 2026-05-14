"use client";

import Link from "next/link";
import type { AdminRequestListItem, AdminRequestStatusCode } from "@/lib/api/admin-requests";
import { formatDateTime } from "@/lib/format";
import { routes } from "@/lib/routes";

export type AdminRequestListFilters = {
  status: "all" | AdminRequestStatusCode;
  number: string;
  contact: string;
  organization: string;
};

type AdminRequestListProps = {
  requests: AdminRequestListItem[];
  filters: AdminRequestListFilters;
  onFiltersChange: (filters: AdminRequestListFilters) => void;
  onPreviewRequest: (number: string) => void;
};

const statusOptions: Array<{ value: AdminRequestListFilters["status"]; label: string }> = [
  { value: "all", label: "Все статусы" },
  { value: "new", label: "Новые" },
  { value: "in_progress", label: "В работе" },
  { value: "completed", label: "Завершенные" },
  { value: "cancelled", label: "Отмененные" },
];

export function AdminRequestList({ requests, filters, onFiltersChange, onPreviewRequest }: AdminRequestListProps) {
  const updateFilter = (patch: Partial<AdminRequestListFilters>) => {
    onFiltersChange({ ...filters, ...patch });
  };

  return (
    <section className="admin-requests account-section" aria-labelledby="admin-requests-title">
      <div className="admin-requests__header">
        <div>
          <p className="eyebrow">Админка</p>
          <h1 id="admin-requests-title">Заявки</h1>
        </div>
      </div>

      <div className="admin-requests__filters" aria-label="Фильтры заявок">
        <label className="admin-filter-field">
          <span>Статус</span>
          <select
            value={filters.status}
            onChange={(event) => updateFilter({ status: event.target.value as AdminRequestListFilters["status"] })}
          >
            {statusOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>

        <label className="admin-filter-field">
          <span>Номер</span>
          <input
            type="search"
            value={filters.number}
            onChange={(event) => updateFilter({ number: event.target.value })}
            placeholder="ЗК26-0001"
          />
        </label>

        <label className="admin-filter-field">
          <span>Контакт</span>
          <input
            type="search"
            value={filters.contact}
            onChange={(event) => updateFilter({ contact: event.target.value })}
            placeholder="Имя, телефон или email"
          />
        </label>

        <label className="admin-filter-field">
          <span>Организация</span>
          <input
            type="search"
            value={filters.organization}
            onChange={(event) => updateFilter({ organization: event.target.value })}
            placeholder="Название или ИНН"
          />
        </label>
      </div>

      {requests.length === 0 ? (
        <p className="empty-state">Заявки не найдены</p>
      ) : (
        <div className="admin-request-list">
          {requests.map((request) => (
            <article className="admin-request-card" key={request.number}>
              <div className="admin-request-card__head">
                <div>
                  <p className="eyebrow">{formatDateTime(request.createdAt)}</p>
                  <h2>{request.number}</h2>
                </div>
                <span className="status-pill">{request.status.label}</span>
              </div>

              <dl className="summary-grid admin-request-card__meta">
                <div>
                  <dt>Клиент</dt>
                  <dd>{request.customer.name}</dd>
                </div>
                <div>
                  <dt>Контакт</dt>
                  <dd>{formatContact(request.customer)}</dd>
                </div>
                <div>
                  <dt>Организация</dt>
                  <dd>{request.organization?.name ?? "Не указана"}</dd>
                </div>
                <div>
                  <dt>Состав</dt>
                  <dd>{formatItemsCount(request.itemsCount)}</dd>
                </div>
                <div>
                  <dt>Источник</dt>
                  <dd>{formatSource(request.source)}</dd>
                </div>
              </dl>

              {request.customerComment ? (
                <p className="admin-request-card__comment">{request.customerComment}</p>
              ) : null}

              {request.internalComment ? (
                <p className="admin-request-card__comment admin-request-card__comment--internal">
                  {request.internalComment}
                </p>
              ) : null}

              <div className="admin-request-card__actions">
                <button
                  className="button button--secondary admin-request-card__preview"
                  type="button"
                  onClick={() => onPreviewRequest(request.number)}
                >
                  Быстрый просмотр {request.number}
                </button>
                <Link className="button button--ghost admin-request-card__link" href={routes.adminRequest(request.number)}>
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

function formatContact(customer: AdminRequestListItem["customer"]): string {
  return customer.email ?? customer.phone ?? "Не указан";
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
