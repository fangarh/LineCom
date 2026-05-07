"use client";

import Link from "next/link";
import { useState, type FormEvent } from "react";
import { formatSku } from "@/lib/format";
import { routes } from "@/lib/routes";
import { getDraftItemsCount, isDraftEmpty } from "@/lib/request-draft/selectors";
import { useRequestDraft } from "./request-draft-provider";

type RequestDraftViewProps = {
  onSubmit: () => Promise<void> | void;
};

export function RequestDraftView({ onSubmit }: RequestDraftViewProps) {
  const { state, dispatch } = useRequestDraft();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const empty = isDraftEmpty(state);
  const itemsCount = getDraftItemsCount(state);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (empty || isSubmitting) {
      return;
    }

    setIsSubmitting(true);
    try {
      await onSubmit();
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="request-page" aria-labelledby="request-title">
      <div className="catalog-intro request-intro">
        <div>
          <p className="eyebrow">Заявка</p>
          <h1 id="request-title">Черновик заявки</h1>
          <p className="lead-text">
            Проверьте позиции, количество и комментарии перед отправкой. После входа менеджер получит заявку и
            уточнит наличие, сроки и условия поставки.
          </p>
        </div>
        <div className="request-total" aria-label="Сводка заявки">
          <span>Позиций</span>
          <strong>{state.items.length}</strong>
          <span>Единиц продажи</span>
          <strong>{itemsCount}</strong>
        </div>
      </div>

      <form className="request-draft" onSubmit={handleSubmit}>
        <div className="request-draft__items">
          {empty ? (
            <p className="empty-state">В заявке пока нет товаров</p>
          ) : (
            state.items.map((item) => (
              <article className="request-item" key={item.productId}>
                <div className="request-item__main">
                  <div>
                    <p className="eyebrow">Позиция</p>
                    <h2>
                      <Link href={routes.product(item.slug)}>{item.productName}</Link>
                    </h2>
                    <p className="muted-text">{formatSku(item.productSku)}</p>
                  </div>

                  <button
                    className="button button--ghost"
                    type="button"
                    aria-label={`Удалить ${item.productName}`}
                    onClick={() => dispatch({ type: "removeItem", productId: item.productId })}
                  >
                    Удалить
                  </button>
                </div>

                <dl className="summary-grid request-item__specs">
                  <div>
                    <dt>Единица</dt>
                    <dd>
                      {item.saleUnit.label}, {item.unitQuantity}
                    </dd>
                  </div>
                </dl>

                <div className="request-fields">
                  <label>
                    <span>Количество</span>
                    <input
                      aria-label={`Количество для ${item.productName}`}
                      defaultValue={item.quantity}
                      min={1}
                      step={1}
                      type="number"
                      onChange={(event) =>
                        dispatch({
                          type: "setQuantity",
                          productId: item.productId,
                          quantity: event.currentTarget.valueAsNumber,
                        })
                      }
                    />
                  </label>

                  <label>
                    <span>Комментарий к позиции</span>
                    <textarea
                      defaultValue={item.customerComment}
                      rows={3}
                      onChange={(event) =>
                        dispatch({
                          type: "setItemComment",
                          productId: item.productId,
                          customerComment: event.currentTarget.value,
                        })
                      }
                    />
                  </label>
                </div>
              </article>
            ))
          )}
        </div>

        <aside className="request-draft__aside">
          <div className="product-detail__section request-submit-panel">
            <h2>Данные к заявке</h2>
            <label className="request-comment">
              <span>Общий комментарий</span>
              <textarea
                defaultValue={state.customerComment}
                rows={5}
                onChange={(event) =>
                  dispatch({ type: "setCustomerComment", customerComment: event.currentTarget.value })
                }
              />
            </label>
            <button className="button button--primary request-submit" type="submit" disabled={empty || isSubmitting}>
              Отправить заявку
            </button>
          </div>
        </aside>
      </form>
    </section>
  );
}
