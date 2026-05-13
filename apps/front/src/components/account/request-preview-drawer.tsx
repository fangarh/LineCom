"use client";

import Link from "next/link";
import type { CustomerRequestDetail } from "@/lib/api/requests";
import { formatDateTime } from "@/lib/format";
import { routes } from "@/lib/routes";

type RequestPreviewDrawerProps = {
  request: CustomerRequestDetail | null;
  isOpen: boolean;
  isLoading: boolean;
  error: string | null;
  onClose: () => void;
};

export function RequestPreviewDrawer({ request, isOpen, isLoading, error, onClose }: RequestPreviewDrawerProps) {
  if (!isOpen) {
    return null;
  }

  const title = request ? `Быстрый просмотр ${request.number}` : "Быстрый просмотр заявки";

  return (
    <div className="request-preview-drawer" role="presentation">
      <button
        className="request-preview-drawer__backdrop"
        type="button"
        aria-label="Закрыть быстрый просмотр"
        onClick={onClose}
      />
      <aside
        className="request-preview-drawer__panel"
        role="dialog"
        aria-modal="true"
        aria-labelledby="request-preview-drawer-title"
      >
        <div className="request-preview-drawer__header">
          <div>
            <p className="eyebrow">Личный кабинет</p>
            <h2 id="request-preview-drawer-title">{title}</h2>
          </div>
          <button className="button button--ghost request-preview-drawer__close" type="button" onClick={onClose}>
            Закрыть
          </button>
        </div>

        {isLoading ? <p className="empty-state">Загружаем заявку...</p> : null}
        {error ? (
          <p className="form-alert" role="alert">
            {error}
          </p>
        ) : null}

        {!isLoading && !error && request ? (
          <div className="request-preview-drawer__content">
            <div className="request-preview-drawer__summary">
              <div>
                <p className="eyebrow">{formatDateTime(request.createdAt)}</p>
                <h3>{request.number}</h3>
              </div>
              <span className="status-pill">{request.status.label}</span>
            </div>

            {request.customerComment ? (
              <section className="request-preview-drawer__section" aria-labelledby="request-preview-comment-title">
                <h3 id="request-preview-comment-title">Комментарий к заявке</h3>
                <p>{request.customerComment}</p>
              </section>
            ) : null}

            <section className="request-preview-drawer__section" aria-labelledby="request-preview-items-title">
              <h3 id="request-preview-items-title">Позиции</h3>
              {request.items.length === 0 ? (
                <p className="empty-state">В заявке нет позиций.</p>
              ) : (
                <div className="request-preview-drawer__items">
                  {request.items.map((item) => (
                    <article className="request-preview-drawer__item" key={`${item.productId}:${item.productName}`}>
                      <h4>{item.productName}</h4>
                      <dl className="summary-grid">
                        <SnapshotRow label="Артикул" value={item.productSku} />
                        <SnapshotRow label="Количество" value={`${item.quantity} ${item.saleUnit.label}`} />
                        <SnapshotRow label="Кратность" value={item.unitQuantity} />
                      </dl>
                      {item.customerComment ? <p>{item.customerComment}</p> : null}
                    </article>
                  ))}
                </div>
              )}
            </section>

            <section className="request-preview-drawer__section" aria-labelledby="request-preview-history-title">
              <h3 id="request-preview-history-title">История</h3>
              {request.history && request.history.length > 0 ? (
                <ol className="request-preview-drawer__history">
                  {request.history.map((event) => (
                    <li key={`${event.event}:${event.createdAt}`}>
                      <span>{formatDateTime(event.createdAt)}</span>
                      <strong>{event.message}</strong>
                    </li>
                  ))}
                </ol>
              ) : (
                <p className="empty-state">История пока содержит только создание заявки.</p>
              )}
            </section>

            <Link className="button button--ghost request-preview-drawer__link" href={routes.accountRequest(request.number)}>
              Открыть полностью
            </Link>
          </div>
        ) : null}
      </aside>
    </div>
  );
}

function SnapshotRow({ label, value }: { label: string; value: string | null | undefined }) {
  return (
    <div>
      <dt>{label}</dt>
      <dd>{value || "Не указано"}</dd>
    </div>
  );
}
