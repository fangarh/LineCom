"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import {
  getAdminCategoryAttributes,
  updateAdminProductAttributes,
  type AdminCategoryAttribute,
  type AdminProductDetail,
} from "@/lib/api/admin-catalog";
import { normalizeApiError } from "@/lib/api/errors";
import {
  buildAdminProductAttributesCommand,
  buildProductAttributeEditorRows,
  buildProductAttributeEditorState,
  emptyProductAttributeValue,
  type ProductAttributeEditorRow,
  type ProductAttributeFormState,
} from "./admin-product-editor-helpers";

const missingCsrfMessage = "Сессия не подтверждена. Обновите страницу и войдите снова.";

type AdminProductAttributesPanelProps = {
  csrfToken?: string | null;
  onProductUpdated: (product: AdminProductDetail) => void;
  product: AdminProductDetail | null;
};

export function AdminProductAttributesPanel({ csrfToken, onProductUpdated, product }: AdminProductAttributesPanelProps) {
  const [categoryAttributes, setCategoryAttributes] = useState<AdminCategoryAttribute[]>([]);
  const [values, setValues] = useState<Record<string, ProductAttributeFormState>>(
    () => buildProductAttributeEditorState([], product?.attributes ?? []).values,
  );
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [alertMessage, setAlertMessage] = useState<string | null>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const requestSeqRef = useRef(0);
  const operationSeqRef = useRef(0);
  const isMountedRef = useRef(false);
  const productIdRef = useRef<string | null>(null);
  const productId = product?.id ?? null;
  const productCategoryId = product?.categoryId ?? null;
  const attributeRows = buildProductAttributeEditorRows(categoryAttributes);

  useEffect(() => {
    isMountedRef.current = true;

    return () => {
      isMountedRef.current = false;
      operationSeqRef.current += 1;
    };
  }, []);

  useEffect(() => {
    operationSeqRef.current += 1;
    productIdRef.current = productId;
  }, [product?.attributes, productId]);

  const loadCategoryAttributes = useCallback(async (categoryId: string, productId: string) => {
    const requestSeq = requestSeqRef.current + 1;
    requestSeqRef.current = requestSeq;
    setIsLoading(true);
    setAlertMessage(null);

    try {
      const response = await getAdminCategoryAttributes(categoryId);
      if (requestSeqRef.current !== requestSeq || productIdRef.current !== productId) return;
      setCategoryAttributes(response.items);
    } catch (error) {
      if (requestSeqRef.current !== requestSeq) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      if (requestSeqRef.current === requestSeq) {
        setIsLoading(false);
      }
    }
  }, []);

  useEffect(() => {
    requestSeqRef.current += 1;
    if (!productId || !productCategoryId) return undefined;

    let isCancelled = false;
    queueMicrotask(() => {
      if (!isCancelled) {
        loadCategoryAttributes(productCategoryId, productId);
      }
    });

    return () => {
      isCancelled = true;
    };
  }, [loadCategoryAttributes, productCategoryId, productId]);

  async function saveAttributes() {
    if (!product) return;
    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    const operationProductId = product.id;
    const operationSeq = operationSeqRef.current;
    const isCurrentOperation = () =>
      isMountedRef.current &&
      operationSeqRef.current === operationSeq &&
      productIdRef.current === operationProductId;

    setIsSaving(true);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      const command = buildAdminProductAttributesCommand(attributeRows, values);
      const updatedProduct = await updateAdminProductAttributes(product.id, command, csrfToken);
      if (!isCurrentOperation()) return;
      onProductUpdated(updatedProduct);
      setStatusMessage("Характеристики сохранены.");
    } catch (error) {
      if (!isCurrentOperation()) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      if (isCurrentOperation()) {
        setIsSaving(false);
      }
    }
  }

  if (!product) {
    return <p className="admin-catalog-status">Выберите товар.</p>;
  }

  return (
    <div className="admin-product-attributes" aria-busy={isLoading}>
      {alertMessage ? (
        <p className="form-alert" role="alert">
          {alertMessage}
        </p>
      ) : null}
      {statusMessage ? <p className="form-success">{statusMessage}</p> : null}

      {attributeRows.length ? (
        <div className="admin-product-attributes__grid">
          {attributeRows.map((attribute) => {
            const value = values[attribute.attributeId] ?? emptyProductAttributeValue();

            return (
              <ProductAttributeControl
                attribute={attribute}
                key={attribute.attributeId}
                onChange={(nextValue) =>
                  setValues((current) => ({
                    ...current,
                    [attribute.attributeId]: nextValue,
                  }))
                }
                value={value}
              />
            );
          })}
        </div>
      ) : (
        <p className="empty-state">Характеристики не заданы.</p>
      )}

      <div className="admin-product-manager__actions">
        <button
          className="button button--secondary"
          disabled={isSaving || isLoading || !attributeRows.length}
          onClick={saveAttributes}
          type="button"
        >
          Сохранить характеристики
        </button>
      </div>
    </div>
  );
}

function ProductAttributeControl({
  attribute,
  onChange,
  value,
}: {
  attribute: ProductAttributeEditorRow;
  onChange: (value: ProductAttributeFormState) => void;
  value: ProductAttributeFormState;
}) {
  if (attribute.type === "number") {
    return (
      <label className="form-field">
        <span>{attributeLabel(attribute)}</span>
        <input
          onChange={(event) => onChange({ ...value, valueNumber: event.target.value })}
          type="number"
          value={value.valueNumber}
        />
      </label>
    );
  }

  if (attribute.type === "boolean") {
    return (
      <label className="admin-product-manager__check admin-product-attributes__check">
        <input
          checked={value.valueBoolean}
          onChange={(event) => onChange({ ...value, valueBoolean: event.target.checked })}
          type="checkbox"
        />
        <span>{attributeLabel(attribute)}</span>
      </label>
    );
  }

  if (attribute.type === "select") {
    return (
      <label className="form-field">
        <span>{attributeLabel(attribute)}</span>
        <select
          onChange={(event) => onChange({ ...value, attributeOptionId: event.target.value })}
          value={value.attributeOptionId}
        >
          <option value="">Не выбрано</option>
          {attribute.options.map((option) => (
            <option key={option.id} value={option.id}>
              {option.value}
            </option>
          ))}
        </select>
      </label>
    );
  }

  return (
    <label className="form-field">
      <span>{attributeLabel(attribute)}</span>
      <input onChange={(event) => onChange({ ...value, valueText: event.target.value })} type="text" value={value.valueText} />
    </label>
  );
}

function attributeLabel(attribute: ProductAttributeEditorRow) {
  return attribute.name;
}
