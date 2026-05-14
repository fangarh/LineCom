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
} from "@/lib/api/admin-catalog";
import { normalizeApiError } from "@/lib/api/errors";
import { generateSlug } from "@/lib/catalog/slug";
import { AdminAttributeEditorPanel } from "./admin-attribute-editor-panel";
import { AdminAttributeListPanel } from "./admin-attribute-list-panel";
import {
  attributeFormFromDetail,
  buildAttributeCommand,
  buildOptionCommand,
  emptyAttributeForm,
  emptyOptionForm,
  mergeAttributeOptions,
  optionFormFromDetail,
  upsertAttribute,
  upsertOption,
  type AttributeFormState,
  type OptionFormState,
} from "./admin-attribute-manager-helpers";

const allCategoriesPageSize = 60;
const missingCsrfMessage = "Сессия не подтверждена. Обновите страницу и войдите снова.";

type AdminAttributeManagerProps = {
  csrfToken?: string | null;
};

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
      <AdminAttributeListPanel
        attributes={attributes}
        categories={categories}
        isLoadingAttributes={isLoadingAttributes}
        isLoadingCategories={isLoadingCategories}
        isMutatingAttribute={isMutatingAttribute}
        onCategoryChange={setSelectedCategoryId}
        onCreateAttribute={startCreateAttribute}
        onInheritFromParent={inheritFromParent}
        onSelectAttribute={selectAttribute}
        selectedAttributeId={selectedAttributeId}
        selectedCategory={selectedCategory}
        selectedCategoryId={selectedCategoryId}
      />

      <AdminAttributeEditorPanel
        alertMessage={alertMessage}
        attributeForm={attributeForm}
        isMutatingAttribute={isMutatingAttribute}
        isMutatingOption={isMutatingOption}
        isPersistedSelectAttribute={isPersistedSelectAttribute}
        onAttributeFormPatch={(patch) => setAttributeForm((current) => ({ ...current, ...patch }))}
        onAttributeTypeChange={(type) => {
          setAttributeForm((current) => ({ ...current, type }));
          setSelectedOption(null);
          setOptionForm(emptyOptionForm);
          setIsOptionSlugManual(false);
        }}
        onDeleteAttribute={deleteSelectedAttribute}
        onDeleteOption={deleteSelectedOption}
        onOptionFormPatch={(patch) => setOptionForm((current) => ({ ...current, ...patch }))}
        onOptionSlugChange={changeOptionSlug}
        onOptionValueChange={changeOptionValue}
        onRegenerateOptionSlug={regenerateOptionSlug}
        onSelectOption={selectOption}
        onStartCreateOption={startCreateOption}
        onSubmitAttribute={submitAttribute}
        onSubmitOption={submitOption}
        optionForm={optionForm}
        selectedAttribute={selectedAttribute}
        selectedCategory={selectedCategory}
        selectedCategoryId={selectedCategoryId}
        selectedOption={selectedOption}
        statusMessage={statusMessage}
      />
    </div>
  );
}
