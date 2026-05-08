import Link from "next/link";
import type { CustomerRequestDetail } from "@/lib/api/requests";
import { formatDateTime } from "@/lib/format";
import { routes } from "@/lib/routes";

type RequestDetailProps = {
  request: CustomerRequestDetail;
};

export function RequestDetail({ request }: RequestDetailProps) {
  return (
    <article className="request-detail">
      <div className="request-detail__head">
        <div>
          <p className="eyebrow">{formatDateTime(request.createdAt)}</p>
          <h1>Заявка {request.number}</h1>
        </div>
        <span className="status-pill">{request.status.label}</span>
      </div>

      <div className="request-detail__grid">
        <section className="account-section" aria-labelledby="request-customer-title">
          <h2 id="request-customer-title">Контактный снимок</h2>
          {request.customer ? (
            <dl className="summary-grid">
              <SnapshotRow label="Имя" value={request.customer.name} />
              <SnapshotRow label="Email" value={request.customer.email} />
              <SnapshotRow label="Телефон" value={request.customer.phone} />
            </dl>
          ) : (
            <p className="empty-state">Контактный снимок не передан.</p>
          )}
        </section>

        <section className="account-section" aria-labelledby="request-organization-title">
          <h2 id="request-organization-title">Организация</h2>
          {request.organization ? (
            <dl className="summary-grid">
              <SnapshotRow label="Название" value={request.organization.name} />
              <SnapshotRow label="ИНН" value={request.organization.inn} />
              <SnapshotRow label="Контакт" value={request.organization.contactPerson} />
            </dl>
          ) : (
            <p className="empty-state">Организация не указана</p>
          )}
        </section>
      </div>

      {request.customerComment ? (
        <section className="account-section" aria-labelledby="request-comment-title">
          <h2 id="request-comment-title">Комментарий к заявке</h2>
          <p className="request-detail__comment">{request.customerComment}</p>
        </section>
      ) : null}

      <section className="account-section" aria-labelledby="request-items-title">
        <h2 id="request-items-title">Позиции</h2>
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

      <section className="account-section" aria-labelledby="request-history-title">
        <h2 id="request-history-title">История</h2>
        {request.history && request.history.length > 0 ? (
          <ol className="request-history">
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

      <Link className="button button--ghost" href={routes.accountRequests()}>
        Вернуться к списку заявок
      </Link>
    </article>
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
