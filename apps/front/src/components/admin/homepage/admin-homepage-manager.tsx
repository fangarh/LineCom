"use client";

import { useCallback, useEffect, useMemo, useRef, useState, type ChangeEvent, type FormEvent } from "react";
import {
  addAdminHomepageSectionItem,
  deleteAdminHomepageSectionItem,
  getAdminHomepageSections,
  updateAdminHomepageSection,
  updateAdminHomepageSectionItem,
  updateAdminHomepageSectionItemOrder,
  type AdminHomepageSection,
} from "@/lib/api/admin-homepage";
import { normalizeApiError } from "@/lib/api/errors";
import { AdminHomepageItemList } from "./admin-homepage-item-list";

type AdminHomepageManagerProps = {
  csrfToken?: string | null;
};

type SectionDraft = {
  title: string;
  itemLimit: string;
  sortOrder: string;
  isActive: boolean;
};

const emptyDraft: SectionDraft = {
  title: "",
  itemLimit: "",
  sortOrder: "",
  isActive: false,
};

export function AdminHomepageManager({ csrfToken = null }: AdminHomepageManagerProps) {
  const [sections, setSections] = useState<AdminHomepageSection[]>([]);
  const [activeSectionId, setActiveSectionId] = useState<string | null>(null);
  const [draft, setDraft] = useState<SectionDraft>(emptyDraft);
  const [itemSortOrders, setItemSortOrders] = useState<Record<string, string>>({});
  const [newTargetId, setNewTargetId] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [pendingAction, setPendingAction] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const pendingActionRef = useRef<string | null>(null);

  const activeSection = useMemo(
    () => sections.find((section) => section.id === activeSectionId) ?? sections[0] ?? null,
    [activeSectionId, sections],
  );

  const syncSectionDraft = useCallback((section: AdminHomepageSection | null) => {
    if (!section) {
      setDraft(emptyDraft);
      setItemSortOrders({});
      setNewTargetId("");
      return;
    }

    setDraft({
      title: section.title,
      itemLimit: String(section.itemLimit),
      sortOrder: String(section.sortOrder),
      isActive: section.isActive,
    });
    setItemSortOrders(Object.fromEntries(section.items.map((item) => [item.id, String(item.sortOrder)])));
    setNewTargetId("");
  }, []);

  const loadSections = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);

    try {
      const response = await getAdminHomepageSections();
      setSections(response.sections);
      setActiveSectionId((currentId) => {
        const nextSection =
          (currentId ? response.sections.find((section) => section.id === currentId) : null) ?? response.sections[0] ?? null;
        syncSectionDraft(nextSection);

        if (nextSection) {
          return nextSection.id;
        }

        return null;
      });
    } catch (error) {
      setErrorMessage(normalizeApiError(error).message);
    } finally {
      setIsLoading(false);
    }
  }, [syncSectionDraft]);

  useEffect(() => {
    loadSections();
  }, [loadSections]);

  const updateDraftField = (event: ChangeEvent<HTMLInputElement>) => {
    const { checked, name, type, value } = event.target;
    setDraft((current) => ({
      ...current,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const requireCsrf = () => {
    if (!csrfToken) {
      setErrorMessage("Сессия устарела. Войдите снова.");
      return null;
    }

    return csrfToken;
  };

  const beginPendingAction = (action: string) => {
    if (pendingActionRef.current !== null) {
      return false;
    }

    pendingActionRef.current = action;
    setPendingAction(action);
    return true;
  };

  const endPendingAction = () => {
    pendingActionRef.current = null;
    setPendingAction(null);
  };

  const saveSection = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!activeSection) return;
    const token = requireCsrf();
    if (!token) return;
    if (!beginPendingAction("section")) return;

    setErrorMessage(null);

    try {
      await updateAdminHomepageSection(
        activeSection.id,
        {
          title: draft.title.trim(),
          itemLimit: toNullableNumber(draft.itemLimit),
          sortOrder: toNullableNumber(draft.sortOrder),
          isActive: draft.isActive,
        },
        token,
      );
      await loadSections();
    } catch (error) {
      setErrorMessage(normalizeApiError(error).message);
    } finally {
      endPendingAction();
    }
  };

  const addItem = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!activeSection) return;
    const token = requireCsrf();
    const targetId = newTargetId.trim();
    if (!token || !targetId) return;
    if (!beginPendingAction("add-item")) return;

    setErrorMessage(null);

    try {
      await addAdminHomepageSectionItem(
        activeSection.id,
        {
          productId: activeSection.type === "product_list" ? targetId : null,
          categoryId: activeSection.type === "category_list" ? targetId : null,
          sortOrder: null,
          isActive: true,
        },
        token,
      );
      await loadSections();
    } catch (error) {
      setErrorMessage(normalizeApiError(error).message);
    } finally {
      endPendingAction();
    }
  };

  const saveItemOrder = async () => {
    if (!activeSection) return;
    const token = requireCsrf();
    if (!token) return;
    if (!beginPendingAction("item-order")) return;

    setErrorMessage(null);

    try {
      const itemIds = [...activeSection.items]
        .sort((left, right) => toNumber(itemSortOrders[left.id]) - toNumber(itemSortOrders[right.id]))
        .map((item) => item.id);
      await updateAdminHomepageSectionItemOrder(activeSection.id, itemIds, token);
      await loadSections();
    } catch (error) {
      setErrorMessage(normalizeApiError(error).message);
    } finally {
      endPendingAction();
    }
  };

  const toggleItemActive = async (itemId: string, isActive: boolean) => {
    if (!activeSection) return;
    const token = requireCsrf();
    if (!token) return;
    if (!beginPendingAction(itemId)) return;

    setErrorMessage(null);

    try {
      await updateAdminHomepageSectionItem(activeSection.id, itemId, { isActive }, token);
      await loadSections();
    } catch (error) {
      setErrorMessage(normalizeApiError(error).message);
    } finally {
      endPendingAction();
    }
  };

  const removeItem = async (itemId: string) => {
    if (!activeSection) return;
    const token = requireCsrf();
    if (!token) return;
    if (!beginPendingAction(itemId)) return;

    setErrorMessage(null);

    try {
      await deleteAdminHomepageSectionItem(activeSection.id, itemId, token);
      await loadSections();
    } catch (error) {
      setErrorMessage(normalizeApiError(error).message);
    } finally {
      endPendingAction();
    }
  };

  const isMutating = pendingAction !== null;
  const addButtonLabel = activeSection?.type === "category_list" ? "Добавить категорию" : "Добавить товар";
  const targetLabel = activeSection?.type === "category_list" ? "UUID категории" : "UUID товара";

  return (
    <section className="admin-catalog-shell account-section" aria-label="Управление главной страницей">
      <div className="admin-catalog-toolbar">
        <div>
          <p className="eyebrow">Админка</p>
          <h1>Главная страница</h1>
        </div>
        {activeSection ? <p className="admin-catalog-status">Секция: {activeSection.title}</p> : null}
      </div>

      {errorMessage ? (
        <p className="form-alert" role="alert">
          {errorMessage}
        </p>
      ) : null}

      <div className="admin-homepage-manager" aria-busy={isLoading || isMutating}>
        <section className="admin-catalog-table admin-homepage-section" aria-label="Секции главной страницы">
          <div className="admin-category-manager__head">
            <h2>Секции</h2>
            {isLoading ? <p className="admin-catalog-status">Загрузка...</p> : null}
          </div>

          <div className="admin-category-manager__rows">
            {sections.map((section) => (
              <button
                aria-pressed={activeSection?.id === section.id}
                className="admin-category-row"
                key={section.id}
                onClick={() => {
                  setActiveSectionId(section.id);
                  syncSectionDraft(section);
                }}
                type="button"
              >
                <span>
                  <strong>{section.title}</strong>
                  <small>{section.code}</small>
                </span>
                <span className="admin-category-row__meta">
                  {section.isActive ? "Активна" : "Скрыта"} · {section.items.length}/{section.itemLimit}
                </span>
              </button>
            ))}
          </div>
        </section>

        <section className="admin-catalog-form admin-homepage-section" aria-label="Редактор секции">
          {activeSection ? (
            <>
              <form className="admin-homepage-section" onSubmit={saveSection}>
                <div className="admin-category-manager__head">
                  <div>
                    <h2>{activeSection.title}</h2>
                    <p className="admin-catalog-status">{activeSection.type}</p>
                  </div>
                  <button className="button" disabled={isMutating} type="submit">
                    Сохранить секцию
                  </button>
                </div>

                <label className="admin-filter-field">
                  <span>Заголовок секции</span>
                  <input name="title" onChange={updateDraftField} value={draft.title} />
                </label>

                <div className="admin-homepage-section__grid">
                  <label className="admin-filter-field">
                    <span>Лимит</span>
                    <input min="0" name="itemLimit" onChange={updateDraftField} type="number" value={draft.itemLimit} />
                  </label>
                  <label className="admin-filter-field">
                    <span>Сортировка</span>
                    <input min="0" name="sortOrder" onChange={updateDraftField} type="number" value={draft.sortOrder} />
                  </label>
                </div>

                <label className="admin-homepage-manager__check">
                  <input checked={draft.isActive} name="isActive" onChange={updateDraftField} type="checkbox" />
                  <span>Секция активна</span>
                </label>
              </form>

              <form className="admin-homepage-section" onSubmit={addItem}>
                <label className="admin-filter-field">
                  <span>{targetLabel}</span>
                  <input onChange={(event) => setNewTargetId(event.target.value)} value={newTargetId} />
                </label>
                <button className="button button--ghost" disabled={isMutating} type="submit">
                  {addButtonLabel}
                </button>
              </form>

              <AdminHomepageItemList
                isLoading={isLoading}
                isMutating={isMutating}
                itemSortOrders={itemSortOrders}
                items={activeSection.items}
                onRemove={removeItem}
                onSaveOrder={saveItemOrder}
                onSortOrderChange={(itemId, sortOrder) =>
                  setItemSortOrders((current) => ({
                    ...current,
                    [itemId]: sortOrder,
                  }))
                }
                onToggleActive={toggleItemActive}
              />
            </>
          ) : (
            <p className="admin-catalog-status">Секции не найдены.</p>
          )}
        </section>
      </div>
    </section>
  );
}

function toNullableNumber(value: string) {
  if (value.trim() === "") {
    return null;
  }

  return Number(value);
}

function toNumber(value: string | undefined) {
  if (!value || value.trim() === "") {
    return 0;
  }

  return Number(value);
}
