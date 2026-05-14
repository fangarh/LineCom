"use client";

import Link from "next/link";
import type { AdminRequestDetail } from "@/lib/api/admin-requests";
import { formatDateTime } from "@/lib/format";
import { routes } from "@/lib/routes";

type AdminRequestPreviewDrawerProps = {
  request: AdminRequestDetail | null;
  isOpen: boolean;
  isLoading: boolean;
  error: string | null;
  onClose: () => void;
};

export function AdminRequestPreviewDrawer({
  request,
  isOpen,
  isLoading,
  error,
  onClose,
}: AdminRequestPreviewDrawerProps) {
  if (!isOpen) {
    return null;
  }

  const title = request ? `Быстрый просмотр ${request.number}` : "Быстрый просмотр заявки";

  return (
    <aside className="admin-request-preview-drawer" role="dialog" aria-modal="true" aria-labelledby="admin-request-preview-title">
      <div className="admin-request-preview-drawer__panel">
        <header className="admin-request-preview-drawer__header">
          <div>
            <p className="eyebrow">Админка</p>
            <h2 id="admin-request-preview-title">{title}</h2>
          </div>
          <button className="button button--ghost admin-request-preview-drawer__close" type="button" onClick={onClose}>
            Закрыть
          </button>
        </header>

        {isLoading ? <p className="empty-state">Загружаем заявку...</p> : null}

        {error ? (
          <p className="form-alert" role="alert">
            {error}
          </p>
        ) : null}

        {!isLoading && !error && request ? (
          <div className="admin-request-preview-drawer__body">
            <div className="admin-request-preview-drawer__summary">
              <p className="eyebrow">{formatDateTime(request.createdAt)}</p>
              <span className="status-pill">{request.status.label}</span>
            </div>

            <dl className="summary-grid admin-request-preview-drawer__timestamps">
              <SnapshotRow label="Создана" value={formatDateTime(request.createdAt)} />
              <SnapshotRow label="Обновлена" value={formatDateTime(request.updatedAt)} />
              <SnapshotRow label="Источник" value={formatSource(request.source)} />
            </dl>

            <section className="account-section admin-request-preview-drawer__section" aria-labelledby="admin-preview-customer-title">
              <h3 id="admin-preview-customer-title">Контактный снимок</h3>
              <dl className="summary-grid">
                <SnapshotRow label="Имя" value={request.customer.name} />
                <SnapshotRow label="Email" value={request.customer.email} />
                <SnapshotRow label="Телефон" value={request.customer.phone} />
              </dl>
            </section>

            {request.organization ? (
              <section className="account-section admin-request-preview-drawer__section" aria-labelledby="admin-preview-organization-title">
                <h3 id="admin-preview-organization-title">Организация</h3>
                <dl className="summary-grid">
                  <SnapshotRow label="Название" value={request.organization.name} />
                  <SnapshotRow label="ИНН" value={request.organization.inn} />
                  <SnapshotRow label="Контакт" value={request.organization.contactPerson} />
                </dl>
              </section>
            ) : null}

            <section className="account-section admin-request-preview-drawer__section" aria-labelledby="admin-preview-comments-title">
              <h3 id="admin-preview-comments-title">Комментарии</h3>
              <dl className="summary-grid">
                <SnapshotRow label="Клиент" value={request.customerComment} />
                <SnapshotRow label="Внутренний" value={request.internalComment} />
              </dl>
            </section>

            <section className="account-section admin-request-preview-drawer__section" aria-labelledby="admin-preview-items-title">
              <h3 id="admin-preview-items-title">Позиции</h3>
              {request.items.length === 0 ? (
                <p className="empty-state">В заявке нет позиций.</p>
              ) : (
                <div className="admin-request-preview-drawer__items">
                  {request.items.map((item) => (
                    <article className="request-detail-item" key={`${item.productId}:${item.productName}`}>
                      <h4>{item.productName}</h4>
                      <dl className="summary-grid request-detail-item__meta">
                        <SnapshotRow label="Артикул" value={item.productSku} />
                        <SnapshotRow label="Единица" value={item.saleUnit.label} />
                        <SnapshotRow label="Кратность" value={item.unitQuantity} />
                        <SnapshotRow label="Количество" value={`${item.quantity} ${item.saleUnit.label}`} />
                      </dl>
                      {item.customerComment ? <p>{item.customerComment}</p> : null}
                    </article>
                  ))}
                </div>
              )}
            </section>

            <section className="account-section admin-request-preview-drawer__section" aria-labelledby="admin-preview-history-title">
              <h3 id="admin-preview-history-title">История</h3>
              {request.history.length > 0 ? (
                <ol className="request-history admin-request-preview-drawer__history">
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

            <Link className="button button--primary admin-request-preview-drawer__action" href={routes.adminRequest(request.number)}>
              Открыть обработку
            </Link>
          </div>
        ) : null}
      </div>
    </aside>
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
