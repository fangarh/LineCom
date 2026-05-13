"use client";

import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from "react";
import {
  createAdminAttributeOption,
  createAdminCategoryAttribute,
  deleteAdminAttributeOption,
  deleteAdminCategoryAttribute,
  getAdminCategories,
  getAdminCategoryAttributes,
  inheritAdminCategoryAttributesFromParent,
  updateAdminAttributeOption,
  updateAdminCategoryAttribute,
  type AdminAttributeOption,
  type AdminCategoryAttribute,
  type AdminCategoryListItem,
  type UpsertAdminAttributeOptionCommand,
  type UpsertAdminCategoryAttributeCommand,
} from "@/lib/api/admin-catalog";
import { normalizeApiError } from "@/lib/api/errors";
import { generateSlug } from "@/lib/catalog/slug";

const allCategoriesPageSize = 60;
const missingCsrfMessage = "Сессия не подтверждена. Обновите страницу и войдите снова.";

type AdminAttributeManagerProps = {
  csrfToken?: string | null;
};

type AttributeFormState = {
  name: string;
  code: string;
  type: string;
  unit: string;
  isRequired: boolean;
  isFilterable: boolean;
  isComparable: boolean;
  isVisibleInProduct: boolean;
  isSeoImportant: boolean;
  isUsedInGeneratedName: boolean;
  sortOrder: string;
  isActive: boolean;
};

type OptionFormState = {
  value: string;
  slug: string;
  normalizedValue: string;
  sortOrder: string;
  isActive: boolean;
};

const emptyAttributeForm: AttributeFormState = {
  name: "",
  code: "",
  type: "text",
  unit: "",
  isRequired: false,
  isFilterable: false,
  isComparable: false,
  isVisibleInProduct: true,
  isSeoImportant: false,
  isUsedInGeneratedName: false,
  sortOrder: "0",
  isActive: true,
};

const emptyOptionForm: OptionFormState = {
  value: "",
  slug: "",
  normalizedValue: "",
  sortOrder: "0",
  isActive: true,
};

const attributeTypes = [
  { value: "text", label: "Текст" },
  { value: "number", label: "Число" },
  { value: "boolean", label: "Да/нет" },
  { value: "select", label: "Список" },
];

export function AdminAttributeManager({ csrfToken = null }: AdminAttributeManagerProps) {
  const [categories, setCategories] = useState<AdminCategoryListItem[]>([]);
  const [selectedCategoryId, setSelectedCategoryId] = useState("");
  const [attributes, setAttributes] = useState<AdminCategoryAttribute[]>([]);
  const [selectedAttribute, setSelectedAttribute] = useState<AdminCategoryAttribute | null>(null);
  const [selectedOption, setSelectedOption] = useState<AdminAttributeOption | null>(null);
  const [attributeForm, setAttributeForm] = useState<AttributeFormState>(emptyAttributeForm);
  const [optionForm, setOptionForm] = useState<OptionFormState>(emptyOptionForm);
  const [isOptionSlugManual, setIsOptionSlugManual] = useState(false);
  const [isLoadingCategories, setIsLoadingCategories] = useState(false);
  const [isLoadingAttributes, setIsLoadingAttributes] = useState(false);
  const [mutatingAttributeSession, setMutatingAttributeSession] = useState<number | null>(null);
  const [mutatingOptionSession, setMutatingOptionSession] = useState<number | null>(null);
  const [alertMessage, setAlertMessage] = useState<string | null>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const categoriesRequestSeqRef = useRef(0);
  const attributesRequestSeqRef = useRef(0);
  const attributeEditorSessionRef = useRef(0);
  const optionEditorSessionRef = useRef(0);
  const attributesRef = useRef<AdminCategoryAttribute[]>([]);
  const selectedCategoryIdRef = useRef("");
  const selectedAttributeIdRef = useRef<string | null>(null);
  const selectedOptionIdRef = useRef<string | null>(null);

  const selectedCategory = useMemo(
    () => categories.find((category) => category.id === selectedCategoryId) ?? null,
    [categories, selectedCategoryId],
  );

  const selectedAttributeId = selectedAttribute?.id ?? null;
  const selectedOptionId = selectedOption?.id ?? null;
  const isPersistedSelectAttribute = selectedAttribute?.type === "select";
  const isMutatingAttribute = mutatingAttributeSession !== null;
  const isMutatingOption = mutatingOptionSession !== null;

  useEffect(() => {
    attributesRef.current = attributes;
  }, [attributes]);

  useEffect(() => {
    selectedCategoryIdRef.current = selectedCategoryId;
    selectedAttributeIdRef.current = selectedAttributeId;
    selectedOptionIdRef.current = selectedOptionId;
  }, [selectedAttributeId, selectedCategoryId, selectedOptionId]);

  const loadCategories = useCallback(async () => {
    const requestSeq = categoriesRequestSeqRef.current + 1;
    categoriesRequestSeqRef.current = requestSeq;
    setIsLoadingCategories(true);
    setAlertMessage(null);

    try {
      const response = await getAdminCategories({ page: 1, pageSize: allCategoriesPageSize });
      if (categoriesRequestSeqRef.current !== requestSeq) return;

      const items = [...response.items];
      for (let page = 2; page <= response.totalPages; page += 1) {
        const pageResponse = await getAdminCategories({ page, pageSize: allCategoriesPageSize });
        if (categoriesRequestSeqRef.current !== requestSeq) return;

        items.push(...pageResponse.items);
      }

      setCategories(items);
    } catch (error) {
      if (categoriesRequestSeqRef.current !== requestSeq) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      if (categoriesRequestSeqRef.current === requestSeq) {
        setIsLoadingCategories(false);
      }
    }
  }, []);

  const loadAttributes = useCallback(async (categoryId: string) => {
    const requestSeq = attributesRequestSeqRef.current + 1;
    attributesRequestSeqRef.current = requestSeq;
    setIsLoadingAttributes(true);
    setAlertMessage(null);

    try {
      const response = await getAdminCategoryAttributes(categoryId);
      if (attributesRequestSeqRef.current !== requestSeq) return;
      if (selectedCategoryIdRef.current !== categoryId) return;

      setAttributes(response.items);
      attributeEditorSessionRef.current += 1;
      optionEditorSessionRef.current += 1;
      setSelectedAttribute(null);
      setSelectedOption(null);
      setAttributeForm(emptyAttributeForm);
      setOptionForm(emptyOptionForm);
      setIsOptionSlugManual(false);
    } catch (error) {
      if (attributesRequestSeqRef.current !== requestSeq) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      if (attributesRequestSeqRef.current === requestSeq) {
        setIsLoadingAttributes(false);
      }
    }
  }, []);

  useEffect(() => {
    let isCancelled = false;
    queueMicrotask(() => {
      if (!isCancelled) {
        void loadCategories();
      }
    });

    return () => {
      isCancelled = true;
    };
  }, [loadCategories]);

  useEffect(() => {
    let isCancelled = false;
    queueMicrotask(() => {
      if (isCancelled) return;

      if (selectedCategoryId) {
        setMutatingAttributeSession(null);
        setMutatingOptionSession(null);
        void loadAttributes(selectedCategoryId);
      } else {
        attributesRequestSeqRef.current += 1;
        attributeEditorSessionRef.current += 1;
        optionEditorSessionRef.current += 1;
        setMutatingAttributeSession(null);
        setMutatingOptionSession(null);
        setAttributes([]);
        setSelectedAttribute(null);
        setSelectedOption(null);
        setAttributeForm(emptyAttributeForm);
        setOptionForm(emptyOptionForm);
        setIsOptionSlugManual(false);
      }
    });

    return () => {
      isCancelled = true;
    };
  }, [loadAttributes, selectedCategoryId]);

  function selectAttribute(attribute: AdminCategoryAttribute) {
    attributeEditorSessionRef.current += 1;
    optionEditorSessionRef.current += 1;
    setMutatingAttributeSession(null);
    setMutatingOptionSession(null);
    setSelectedAttribute(attribute);
    setSelectedOption(null);
    setAttributeForm(attributeFormFromDetail(attribute));
    setOptionForm(emptyOptionForm);
    setIsOptionSlugManual(false);
    setAlertMessage(null);
    setStatusMessage(null);
  }

  function startCreateAttribute() {
    attributeEditorSessionRef.current += 1;
    optionEditorSessionRef.current += 1;
    setMutatingAttributeSession(null);
    setMutatingOptionSession(null);
    setSelectedAttribute(null);
    setSelectedOption(null);
    setAttributeForm(emptyAttributeForm);
    setOptionForm(emptyOptionForm);
    setIsOptionSlugManual(false);
    setAlertMessage(null);
    setStatusMessage(null);
  }

  async function submitAttribute(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const capturedCategoryId = selectedCategoryId;
    const capturedAttributeId = selectedAttribute?.id ?? null;
    const capturedAttributeSession = attributeEditorSessionRef.current;

    if (!capturedCategoryId) {
      setAlertMessage("Выберите категорию.");
      return;
    }

    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    setMutatingAttributeSession(capturedAttributeSession);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      const command = buildAttributeCommand(attributeForm);
      const savedAttribute = selectedAttribute
        ? await updateAdminCategoryAttribute(capturedCategoryId, selectedAttribute.id, command, csrfToken)
        : await createAdminCategoryAttribute(capturedCategoryId, command, csrfToken);

      if (!isCurrentAttributeMutation(capturedCategoryId, capturedAttributeId, capturedAttributeSession)) return;

      const normalizedAttribute = mergeAttributeOptions(attributesRef.current, savedAttribute);
      setAttributes((current) => upsertAttribute(current, normalizedAttribute));
      setSelectedAttribute(normalizedAttribute);
      setAttributeForm(attributeFormFromDetail(normalizedAttribute));
      setSelectedOption(null);
      setOptionForm(emptyOptionForm);
      setIsOptionSlugManual(false);
      setStatusMessage(selectedAttribute ? "Характеристика сохранена." : "Характеристика создана.");
    } catch (error) {
      if (!isCurrentAttributeMutation(capturedCategoryId, capturedAttributeId, capturedAttributeSession)) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      if (isCurrentAttributeMutation(capturedCategoryId, capturedAttributeId, capturedAttributeSession)) {
        setMutatingAttributeSession(null);
      }
    }
  }

  async function deleteSelectedAttribute() {
    if (!selectedCategoryId || !selectedAttribute) return;
    const capturedCategoryId = selectedCategoryId;
    const capturedAttributeId = selectedAttribute.id;
    const capturedAttributeSession = attributeEditorSessionRef.current;

    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    setMutatingAttributeSession(capturedAttributeSession);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      await deleteAdminCategoryAttribute(capturedCategoryId, capturedAttributeId, csrfToken);
      if (!isCurrentAttributeMutation(capturedCategoryId, capturedAttributeId, capturedAttributeSession)) return;

      setAttributes((current) => current.filter((attribute) => attribute.id !== capturedAttributeId));
      setSelectedAttribute(null);
      setSelectedOption(null);
      setAttributeForm(emptyAttributeForm);
      setOptionForm(emptyOptionForm);
      setStatusMessage("Характеристика удалена.");
    } catch (error) {
      if (!isCurrentAttributeMutation(capturedCategoryId, capturedAttributeId, capturedAttributeSession)) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      if (isCurrentAttributeMutation(capturedCategoryId, capturedAttributeId, capturedAttributeSession)) {
        setMutatingAttributeSession(null);
      }
    }
  }

  async function inheritFromParent() {
    const capturedCategoryId = selectedCategoryId;
    const capturedAttributeSession = attributeEditorSessionRef.current;

    if (!capturedCategoryId) {
      setAlertMessage("Выберите категорию.");
      return;
    }

    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    setMutatingAttributeSession(capturedAttributeSession);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      const result = await inheritAdminCategoryAttributesFromParent(capturedCategoryId, csrfToken);
      if (!isCurrentCategoryMutation(capturedCategoryId, capturedAttributeSession)) return;

      setStatusMessage(`Добавлено: ${result.added}. Пропущено: ${result.skipped}.`);
      await loadAttributes(capturedCategoryId);
    } catch (error) {
      if (!isCurrentCategoryMutation(capturedCategoryId, capturedAttributeSession)) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      if (selectedCategoryIdRef.current === capturedCategoryId) {
        setMutatingAttributeSession(null);
      }
    }
  }

  function selectOption(option: AdminAttributeOption) {
    optionEditorSessionRef.current += 1;
    setMutatingOptionSession(null);
    setSelectedOption(option);
    setOptionForm(optionFormFromDetail(option));
    setIsOptionSlugManual(true);
    setAlertMessage(null);
    setStatusMessage(null);
  }

  function startCreateOption() {
    optionEditorSessionRef.current += 1;
    setMutatingOptionSession(null);
    setSelectedOption(null);
    setOptionForm(emptyOptionForm);
    setIsOptionSlugManual(false);
    setAlertMessage(null);
    setStatusMessage(null);
  }

  async function submitOption(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!selectedCategoryId || !selectedAttribute) return;
    const capturedCategoryId = selectedCategoryId;
    const capturedAttributeId = selectedAttribute.id;
    const capturedOptionId = selectedOption?.id ?? null;
    const capturedOptionSession = optionEditorSessionRef.current;

    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    setMutatingOptionSession(capturedOptionSession);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      const command = buildOptionCommand(optionForm);
      const savedOption = selectedOption
        ? await updateAdminAttributeOption(capturedCategoryId, capturedAttributeId, selectedOption.id, command, csrfToken)
        : await createAdminAttributeOption(capturedCategoryId, capturedAttributeId, command, csrfToken);

      if (!isCurrentOptionMutation(capturedCategoryId, capturedAttributeId, capturedOptionId, capturedOptionSession)) return;

      updateSelectedAttributeOptions(capturedAttributeId, (options) => upsertOption(options, savedOption));
      setSelectedOption(savedOption);
      setOptionForm(optionFormFromDetail(savedOption));
      setIsOptionSlugManual(true);
      setStatusMessage(selectedOption ? "Значение сохранено." : "Значение создано.");
    } catch (error) {
      if (!isCurrentOptionMutation(capturedCategoryId, capturedAttributeId, capturedOptionId, capturedOptionSession)) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      if (isCurrentOptionMutation(capturedCategoryId, capturedAttributeId, capturedOptionId, capturedOptionSession)) {
        setMutatingOptionSession(null);
      }
    }
  }

  async function deleteSelectedOption() {
    if (!selectedCategoryId || !selectedAttribute || !selectedOption) return;
    const capturedCategoryId = selectedCategoryId;
    const capturedAttributeId = selectedAttribute.id;
    const capturedOptionId = selectedOption.id;
    const capturedOptionSession = optionEditorSessionRef.current;

    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    setMutatingOptionSession(capturedOptionSession);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      await deleteAdminAttributeOption(capturedCategoryId, capturedAttributeId, capturedOptionId, csrfToken);
      if (!isCurrentOptionMutation(capturedCategoryId, capturedAttributeId, capturedOptionId, capturedOptionSession)) return;

      updateSelectedAttributeOptions(capturedAttributeId, (options) => options.filter((option) => option.id !== capturedOptionId));
      setSelectedOption(null);
      setOptionForm(emptyOptionForm);
      setIsOptionSlugManual(false);
      setStatusMessage("Значение удалено.");
    } catch (error) {
      if (!isCurrentOptionMutation(capturedCategoryId, capturedAttributeId, capturedOptionId, capturedOptionSession)) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      if (isCurrentOptionMutation(capturedCategoryId, capturedAttributeId, capturedOptionId, capturedOptionSession)) {
        setMutatingOptionSession(null);
      }
    }
  }

  function updateSelectedAttributeOptions(attributeId: string, update: (options: AdminAttributeOption[]) => AdminAttributeOption[]) {
    const currentAttribute = selectedAttributeIdRef.current === attributeId
      ? (attributesRef.current.find((attribute) => attribute.id === attributeId) ?? null)
      : null;
    if (!currentAttribute) return;

    const updatedAttribute = { ...currentAttribute, options: update(currentAttribute.options) };
    setSelectedAttribute(updatedAttribute);
    setAttributes((items) => upsertAttribute(items, updatedAttribute));
  }

  function changeOptionValue(value: string) {
    setOptionForm((current) => ({
      ...current,
      value,
      slug: isOptionSlugManual ? current.slug : generateSlug(value),
    }));
  }

  function changeOptionSlug(slug: string) {
    setIsOptionSlugManual(true);
    setOptionForm((current) => ({ ...current, slug }));
  }

  function regenerateOptionSlug() {
    setIsOptionSlugManual(true);
    setOptionForm((current) => ({ ...current, slug: generateSlug(current.value) }));
  }

  function isCurrentCategoryMutation(capturedCategoryId: string, capturedAttributeSession: number) {
    return selectedCategoryIdRef.current === capturedCategoryId && attributeEditorSessionRef.current === capturedAttributeSession;
  }

  function isCurrentAttributeMutation(
    capturedCategoryId: string,
    capturedAttributeId: string | null,
    capturedAttributeSession: number,
  ) {
    return (
      selectedCategoryIdRef.current === capturedCategoryId &&
      selectedAttributeIdRef.current === capturedAttributeId &&
      attributeEditorSessionRef.current === capturedAttributeSession
    );
  }

  function isCurrentOptionMutation(
    capturedCategoryId: string,
    capturedAttributeId: string,
    capturedOptionId: string | null,
    capturedOptionSession: number,
  ) {
    return (
      selectedCategoryIdRef.current === capturedCategoryId &&
      selectedAttributeIdRef.current === capturedAttributeId &&
      selectedOptionIdRef.current === capturedOptionId &&
      optionEditorSessionRef.current === capturedOptionSession
    );
  }

  return (
    <div className="admin-attribute-manager">
      <section className="admin-catalog-table admin-attribute-manager__list" aria-labelledby="admin-attribute-list-title">
        <div className="admin-attribute-manager__head">
          <div>
            <h2 id="admin-attribute-list-title">Характеристики</h2>
            <p>Категория, атрибуты, признаки и значения для фильтров.</p>
          </div>
          <div className="admin-attribute-manager__actions">
            <button className="button button--secondary" disabled={!selectedCategoryId || isMutatingAttribute} onClick={inheritFromParent} type="button">
              Унаследовать от родителя
            </button>
            <button className="button button--primary" disabled={!selectedCategoryId} onClick={startCreateAttribute} type="button">
              Новая характеристика
            </button>
          </div>
        </div>

        <label className="admin-filter-field admin-attribute-manager__category">
          <span>Категория</span>
          <select
            aria-busy={isLoadingCategories}
            onChange={(event) => setSelectedCategoryId(event.target.value)}
            value={selectedCategoryId}
          >
            <option value="">Выберите категорию</option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
        </label>

        <div className="admin-attribute-manager__rows" aria-busy={isLoadingAttributes}>
          {attributes.length ? (
            attributes.map((attribute) => (
              <button
                aria-pressed={selectedAttributeId === attribute.id}
                className="admin-attribute-row"
                key={attribute.id}
                onClick={() => selectAttribute(attribute)}
                type="button"
              >
                <span>
                  <strong>{attribute.name}</strong>
                  <small>
                    {attribute.code} · {attribute.type}
                    {attribute.unit ? ` · ${attribute.unit}` : ""}
                  </small>
                </span>
                <span className="admin-attribute-row__meta">
                  {attribute.productValuesCount} значений в товарах · {attribute.sortOrder}
                </span>
              </button>
            ))
          ) : (
            <p className="empty-state">
              {selectedCategory ? "Характеристики не найдены." : "Выберите категорию."}
            </p>
          )}
        </div>
      </section>

      <section className="admin-catalog-form admin-attribute-manager__editor" aria-label="Редактор характеристики">
        <div className="admin-attribute-manager__head">
          <div>
            <h2>{selectedAttribute ? "Редактирование характеристики" : "Новая характеристика"}</h2>
            <p className="admin-catalog-status">
              {selectedCategory ? selectedCategory.name : "Категория не выбрана."}
            </p>
          </div>
        </div>

        {alertMessage ? (
          <p className="form-alert" role="alert">
            {alertMessage}
          </p>
        ) : null}
        {statusMessage ? <p className="form-success">{statusMessage}</p> : null}

        <form className="admin-attribute-form" onSubmit={submitAttribute}>
          <label className="form-field">
            <span>Название</span>
            <input
              disabled={!selectedCategoryId}
              onChange={(event) => setAttributeForm((current) => ({ ...current, name: event.target.value }))}
              required
              value={attributeForm.name}
            />
          </label>
          <label className="form-field">
            <span>Код</span>
            <input
              disabled={!selectedCategoryId}
              onChange={(event) => setAttributeForm((current) => ({ ...current, code: event.target.value }))}
              required
              value={attributeForm.code}
            />
          </label>
          <label className="form-field">
            <span>Тип</span>
            <select
              disabled={!selectedCategoryId}
              onChange={(event) => {
                setAttributeForm((current) => ({ ...current, type: event.target.value }));
                setSelectedOption(null);
                setOptionForm(emptyOptionForm);
                setIsOptionSlugManual(false);
              }}
              value={attributeForm.type}
            >
              {attributeTypes.map((type) => (
                <option key={type.value} value={type.value}>
                  {type.label}
                </option>
              ))}
            </select>
          </label>
          <label className="form-field">
            <span>Единица</span>
            <input
              disabled={!selectedCategoryId}
              onChange={(event) => setAttributeForm((current) => ({ ...current, unit: event.target.value }))}
              value={attributeForm.unit}
            />
          </label>
          <label className="form-field">
            <span>Сортировка</span>
            <input
              disabled={!selectedCategoryId}
              inputMode="numeric"
              onChange={(event) => setAttributeForm((current) => ({ ...current, sortOrder: event.target.value }))}
              type="number"
              value={attributeForm.sortOrder}
            />
          </label>

          <div className="admin-attribute-manager__checks">
            <label className="admin-attribute-manager__check">
              <input
                checked={attributeForm.isRequired}
                disabled={!selectedCategoryId}
                onChange={(event) => setAttributeForm((current) => ({ ...current, isRequired: event.target.checked }))}
                type="checkbox"
              />
              <span>Обязательная</span>
            </label>
            <label className="admin-attribute-manager__check">
              <input
                checked={attributeForm.isFilterable}
                disabled={!selectedCategoryId}
                onChange={(event) => setAttributeForm((current) => ({ ...current, isFilterable: event.target.checked }))}
                type="checkbox"
              />
              <span>Фильтруемая</span>
            </label>
            <label className="admin-attribute-manager__check">
              <input
                checked={attributeForm.isComparable}
                disabled={!selectedCategoryId}
                onChange={(event) => setAttributeForm((current) => ({ ...current, isComparable: event.target.checked }))}
                type="checkbox"
              />
              <span>Сравниваемая</span>
            </label>
            <label className="admin-attribute-manager__check">
              <input
                checked={attributeForm.isVisibleInProduct}
                disabled={!selectedCategoryId}
                onChange={(event) => setAttributeForm((current) => ({ ...current, isVisibleInProduct: event.target.checked }))}
                type="checkbox"
              />
              <span>В карточке товара</span>
            </label>
            <label className="admin-attribute-manager__check">
              <input
                checked={attributeForm.isSeoImportant}
                disabled={!selectedCategoryId}
                onChange={(event) => setAttributeForm((current) => ({ ...current, isSeoImportant: event.target.checked }))}
                type="checkbox"
              />
              <span>SEO-важная</span>
            </label>
            <label className="admin-attribute-manager__check">
              <input
                checked={attributeForm.isUsedInGeneratedName}
                disabled={!selectedCategoryId}
                onChange={(event) => setAttributeForm((current) => ({ ...current, isUsedInGeneratedName: event.target.checked }))}
                type="checkbox"
              />
              <span>В названии товара</span>
            </label>
            <label className="admin-attribute-manager__check">
              <input
                checked={attributeForm.isActive}
                disabled={!selectedCategoryId}
                onChange={(event) => setAttributeForm((current) => ({ ...current, isActive: event.target.checked }))}
                type="checkbox"
              />
              <span>Активна</span>
            </label>
          </div>

          <div className="admin-attribute-manager__actions">
            <button className="button button--primary" disabled={!selectedCategoryId || isMutatingAttribute} type="submit">
              {selectedAttribute ? "Сохранить характеристику" : "Создать характеристику"}
            </button>
            <button
              className="button button--ghost"
              disabled={!selectedAttribute || isMutatingAttribute}
              onClick={deleteSelectedAttribute}
              type="button"
            >
              Удалить характеристику
            </button>
          </div>
        </form>

        {isPersistedSelectAttribute ? (
          <section className="admin-attribute-manager__options" aria-label="Редактор значения">
            <div className="admin-attribute-manager__head">
              <h2>Значения</h2>
              <button className="button button--secondary" onClick={startCreateOption} type="button">
                Новое значение
              </button>
            </div>

            <div className="admin-attribute-manager__option-rows">
              {selectedAttribute.options.length ? (
                selectedAttribute.options.map((option) => (
                  <button
                    aria-pressed={selectedOption?.id === option.id}
                    className="admin-attribute-option-row"
                    key={option.id}
                    onClick={() => selectOption(option)}
                    type="button"
                  >
                    <span>
                      <strong>{option.value}</strong>
                      <small>{option.slug}</small>
                    </span>
                    <span className="admin-attribute-row__meta">
                      {option.productValuesCount} значений в товарах · {option.sortOrder}
                    </span>
                  </button>
                ))
              ) : (
                <p className="empty-state">Значения не найдены.</p>
              )}
            </div>

            <form className="admin-attribute-option-form" onSubmit={submitOption}>
              <label className="form-field">
                <span>Значение</span>
                <input
                  onChange={(event) => changeOptionValue(event.target.value)}
                  required
                  value={optionForm.value}
                />
              </label>
              <label className="form-field">
                <span>Slug</span>
                <input
                  onChange={(event) => changeOptionSlug(event.target.value)}
                  onFocus={(event) => event.currentTarget.select()}
                  required
                  value={optionForm.slug}
                />
              </label>
              <button className="button button--ghost" onClick={regenerateOptionSlug} type="button">
                Сгенерировать заново
              </button>
              <label className="form-field">
                <span>Нормализованное значение</span>
                <input
                  onChange={(event) => setOptionForm((current) => ({ ...current, normalizedValue: event.target.value }))}
                  required
                  value={optionForm.normalizedValue}
                />
              </label>
              <label className="form-field">
                <span>Сортировка значения</span>
                <input
                  inputMode="numeric"
                  onChange={(event) => setOptionForm((current) => ({ ...current, sortOrder: event.target.value }))}
                  type="number"
                  value={optionForm.sortOrder}
                />
              </label>
              <label className="admin-attribute-manager__check">
                <input
                  checked={optionForm.isActive}
                  onChange={(event) => setOptionForm((current) => ({ ...current, isActive: event.target.checked }))}
                  type="checkbox"
                />
                <span>Активно</span>
              </label>

              <div className="admin-attribute-manager__actions">
                <button className="button button--primary" disabled={isMutatingOption} type="submit">
                  {selectedOption ? "Сохранить значение" : "Создать значение"}
                </button>
                <button
                  className="button button--ghost"
                  disabled={!selectedOption || isMutatingOption}
                  onClick={deleteSelectedOption}
                  type="button"
                >
                  Удалить значение
                </button>
              </div>
            </form>
          </section>
        ) : null}
      </section>
    </div>
  );
}

function attributeFormFromDetail(attribute: AdminCategoryAttribute): AttributeFormState {
  return {
    name: attribute.name,
    code: attribute.code,
    type: attribute.type,
    unit: attribute.unit ?? "",
    isRequired: attribute.isRequired,
    isFilterable: attribute.isFilterable,
    isComparable: attribute.isComparable,
    isVisibleInProduct: attribute.isVisibleInProduct,
    isSeoImportant: attribute.isSeoImportant,
    isUsedInGeneratedName: attribute.isUsedInGeneratedName,
    sortOrder: String(attribute.sortOrder),
    isActive: attribute.isActive,
  };
}

function optionFormFromDetail(option: AdminAttributeOption): OptionFormState {
  return {
    value: option.value,
    slug: option.slug,
    normalizedValue: option.normalizedValue,
    sortOrder: String(option.sortOrder),
    isActive: option.isActive,
  };
}

function buildAttributeCommand(form: AttributeFormState): UpsertAdminCategoryAttributeCommand {
  return {
    name: form.name.trim(),
    code: form.code.trim(),
    type: form.type,
    unit: normalizeOptionalText(form.unit),
    isRequired: form.isRequired,
    isFilterable: form.isFilterable,
    isComparable: form.isComparable,
    isVisibleInProduct: form.isVisibleInProduct,
    isSeoImportant: form.isSeoImportant,
    isUsedInGeneratedName: form.isUsedInGeneratedName,
    sortOrder: parseSortOrder(form.sortOrder),
    isActive: form.isActive,
  };
}

function buildOptionCommand(form: OptionFormState): UpsertAdminAttributeOptionCommand {
  return {
    value: form.value.trim(),
    slug: form.slug.trim(),
    normalizedValue: form.normalizedValue.trim(),
    sortOrder: parseSortOrder(form.sortOrder),
    isActive: form.isActive,
  };
}

function normalizeOptionalText(value: string) {
  const normalized = value.trim();
  return normalized || null;
}

function parseSortOrder(value: string) {
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : 0;
}

function mergeAttributeOptions(items: AdminCategoryAttribute[], savedAttribute: AdminCategoryAttribute) {
  if (savedAttribute.options.length) {
    return savedAttribute;
  }

  const existingAttribute = items.find((item) => item.id === savedAttribute.id);
  if (!existingAttribute?.options.length) {
    return savedAttribute;
  }

  return { ...savedAttribute, options: existingAttribute.options };
}

function upsertAttribute(items: AdminCategoryAttribute[], attribute: AdminCategoryAttribute) {
  const existingIndex = items.findIndex((item) => item.id === attribute.id);
  if (existingIndex === -1) {
    return [...items, attribute].sort((left, right) => left.sortOrder - right.sortOrder);
  }

  return items.map((item) => (item.id === attribute.id ? attribute : item)).sort((left, right) => left.sortOrder - right.sortOrder);
}

function upsertOption(items: AdminAttributeOption[], option: AdminAttributeOption) {
  const existingIndex = items.findIndex((item) => item.id === option.id);
  if (existingIndex === -1) {
    return [...items, option].sort((left, right) => left.sortOrder - right.sortOrder);
  }

  return items.map((item) => (item.id === option.id ? option : item)).sort((left, right) => left.sortOrder - right.sortOrder);
}
