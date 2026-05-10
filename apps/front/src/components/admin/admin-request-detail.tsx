"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import type {
  AdminRequestDetail as AdminRequestDetailModel,
  AdminRequestStatusCode,
} from "@/lib/api/admin-requests";
import { formatDateTime } from "@/lib/format";
import { routes } from "@/lib/routes";

type AdminRequestDetailProps = {
  request: AdminRequestDetailModel;
  onStatusSave: (status: AdminRequestStatusCode) => void | Promise<void>;
  onInternalCommentSave: (comment: string) => void | Promise<void>;
  isStatusSaving: boolean;
  isCommentSaving: boolean;
  canSave: boolean;
  actionMessage: string | null;
};

const statusOptions: Array<{ value: AdminRequestStatusCode; label: string }> = [
  { value: "new", label: "Новая" },
  { value: "in_progress", label: "В работе" },
  { value: "completed", label: "Завершена" },
  { value: "cancelled", label: "Отменена" },
];

export function AdminRequestDetail({
  request,
  onStatusSave,
  onInternalCommentSave,
  isStatusSaving,
  isCommentSaving,
  canSave,
  actionMessage,
}: AdminRequestDetailProps) {
  const [selectedStatus, setSelectedStatus] = useState<AdminRequestStatusCode>(
    request.status.code as AdminRequestStatusCode,
  );
  const [internalComment, setInternalComment] = useState(request.internalComment ?? "");

  useEffect(() => {
    setSelectedStatus(request.status.code as AdminRequestStatusCode);
    setInternalComment(request.internalComment ?? "");
  }, [request]);

  return (
    <article className="admin-request-detail">
      <Link className="button button--ghost admin-request-detail__back" href={routes.adminRequests()}>
        Вернуться к списку заявок
      </Link>

      <div className="admin-request-detail__head">
        <div>
          <p className="eyebrow">{formatDateTime(request.createdAt)}</p>
          <h1>Заявка {request.number}</h1>
        </div>
        <span className="status-pill">{request.status.label}</span>
      </div>

      <dl className="summary-grid admin-request-detail__timestamps">
        <SnapshotRow label="Создана" value={formatDateTime(request.createdAt)} />
        <SnapshotRow label="Обновлена" value={formatDateTime(request.updatedAt)} />
        <SnapshotRow label="Источник" value={formatSource(request.source)} />
      </dl>

      <div className="admin-request-detail__layout">
        <div className="admin-request-detail__main">
          <div className="admin-request-detail__grid">
            <section className="account-section" aria-labelledby="admin-request-customer-title">
              <h2 id="admin-request-customer-title">Контактный снимок</h2>
              <dl className="summary-grid">
                <SnapshotRow label="Имя" value={request.customer.name} />
                <SnapshotRow label="Email" value={request.customer.email} />
                <SnapshotRow label="Телефон" value={request.customer.phone} />
              </dl>
            </section>

            {request.organization ? (
              <section className="account-section" aria-labelledby="admin-request-organization-title">
                <h2 id="admin-request-organization-title">Организация</h2>
                <dl className="summary-grid">
                  <SnapshotRow label="Название" value={request.organization.name} />
                  <SnapshotRow label="ИНН" value={request.organization.inn} />
                  <SnapshotRow label="Контакт" value={request.organization.contactPerson} />
                </dl>
              </section>
            ) : null}
          </div>

          <section className="account-section" aria-labelledby="admin-request-comment-title">
            <h2 id="admin-request-comment-title">Комментарий клиента</h2>
            {request.customerComment ? (
              <p className="request-detail__comment">{request.customerComment}</p>
            ) : (
              <p className="empty-state">Комментарий не указан.</p>
            )}
          </section>

          <section className="account-section" aria-labelledby="admin-request-items-title">
            <h2 id="admin-request-items-title">Позиции</h2>
            {request.items.length === 0 ? (
              <p className="empty-state">В заявке нет позиций.</p>
            ) : (
              <div className="request-detail__items">
                {request.items.map((item) => (
                  <article className="request-detail-item" key={`${item.productId}:${item.productName}`}>
                    <div>
                      <h3>{item.productName}</h3>
                      <dl className="summary-grid request-detail-item__meta">
                        <SnapshotRow label="Артикул" value={item.productSku} />
                        <SnapshotRow label="Единица" value={item.saleUnit.label} />
                        <SnapshotRow label="Кратность" value={item.unitQuantity} />
                        <SnapshotRow label="Количество" value={`${item.quantity} ${item.saleUnit.label}`} />
                      </dl>
                    </div>
                    {item.customerComment ? <p>{item.customerComment}</p> : null}
                  </article>
                ))}
              </div>
            )}
          </section>

          <section className="account-section" aria-labelledby="admin-request-history-title">
            <h2 id="admin-request-history-title">История</h2>
            {request.history && request.history.length > 0 ? (
              <ol className="request-history">
                {request.history.map((event) => (
                  <li key={`${event.event}:${event.createdAt}:${event.message}`}>
                    <span>{formatDateTime(event.createdAt)}</span>
                    <strong>{event.message}</strong>
                  </li>
                ))}
              </ol>
            ) : (
              <p className="empty-state">История пока содержит только создание заявки.</p>
            )}
          </section>
        </div>

        <aside className="account-section admin-request-processing" aria-labelledby="admin-request-processing-title">
          <h2 id="admin-request-processing-title">Обработка</h2>

          {actionMessage ? (
            <p className="form-success" role="status">
              {actionMessage}
            </p>
          ) : null}

          {!canSave ? (
            <p className="form-alert" role="alert">
              Сессия не подтверждена. Обновите страницу и войдите снова.
            </p>
          ) : null}

          <form
            className="admin-request-processing__form"
            onSubmit={(event) => {
              event.preventDefault();
              onStatusSave(selectedStatus);
            }}
          >
            <label className="form-field">
              <span>Статус</span>
              <select
                value={selectedStatus}
                onChange={(event) => setSelectedStatus(event.target.value as AdminRequestStatusCode)}
              >
                {statusOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <button className="button button--primary" type="submit" disabled={!canSave || isStatusSaving}>
              Сохранить статус
            </button>
          </form>

          <form
            className="admin-request-processing__form"
            onSubmit={(event) => {
              event.preventDefault();
              onInternalCommentSave(internalComment);
            }}
          >
            <label className="form-field">
              <span>Внутренний комментарий</span>
              <textarea
                rows={6}
                value={internalComment}
                onChange={(event) => setInternalComment(event.target.value)}
              />
            </label>
            <button className="button button--secondary" type="submit" disabled={!canSave || isCommentSaving}>
              Сохранить комментарий
            </button>
          </form>
        </aside>
      </div>
    </article>
  );
}

function SnapshotRow({ label, value }: { label: string; value: string | number | null | undefined }) {
  return (
    <div>
      <dt>{label}</dt>
      <dd>{value || "Не указано"}</dd>
    </div>
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
