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
import { AdminHomepageSectionEditor, type AdminHomepageSectionDraft } from "./admin-homepage-section-editor";
import { AdminHomepageSectionList } from "./admin-homepage-section-list";

type AdminHomepageManagerProps = {
  csrfToken?: string | null;
};

const emptyDraft: AdminHomepageSectionDraft = {
  title: "",
  itemLimit: "",
  sortOrder: "",
  isActive: false,
};

export function AdminHomepageManager({ csrfToken = null }: AdminHomepageManagerProps) {
  const [sections, setSections] = useState<AdminHomepageSection[]>([]);
  const [activeSectionId, setActiveSectionId] = useState<string | null>(null);
  const [draft, setDraft] = useState<AdminHomepageSectionDraft>(emptyDraft);
  const [itemSortOrders, setItemSortOrders] = useState<Record<string, string>>({});
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
      return;
    }

    setDraft({
      title: section.title,
      itemLimit: String(section.itemLimit),
      sortOrder: String(section.sortOrder),
      isActive: section.isActive,
    });
    setItemSortOrders(Object.fromEntries(section.items.map((item) => [item.id, String(item.sortOrder)])));
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
    let isActive = true;

    getAdminHomepageSections()
      .then((response) => {
        if (!isActive) return;

        setSections(response.sections);
        setActiveSectionId((currentId) => {
          const nextSection =
            (currentId ? response.sections.find((section) => section.id === currentId) : null) ?? response.sections[0] ?? null;
          syncSectionDraft(nextSection);

          return nextSection?.id ?? null;
        });
      })
      .catch((error) => {
        if (isActive) {
          setErrorMessage(normalizeApiError(error).message);
        }
      })
      .finally(() => {
        if (isActive) {
          setIsLoading(false);
        }
      });

    return () => {
      isActive = false;
    };
  }, [syncSectionDraft]);

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

  const addItem = async (targetId: string) => {
    if (!activeSection) return;
    const token = requireCsrf();
    if (!token) return;
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
        <AdminHomepageSectionList
          activeSectionId={activeSection?.id ?? null}
          isLoading={isLoading}
          onSelect={(section) => {
            setActiveSectionId(section.id);
            syncSectionDraft(section);
          }}
          sections={sections}
        />

        <AdminHomepageSectionEditor
          activeSection={activeSection}
          draft={draft}
          isLoading={isLoading}
          isMutating={isMutating}
          itemSortOrders={itemSortOrders}
          onAddCategory={addItem}
          onAddProduct={addItem}
          onDraftFieldChange={updateDraftField}
          onRemove={removeItem}
          onSaveItemOrder={saveItemOrder}
          onSaveSection={saveSection}
          onSortOrderChange={(itemId, sortOrder) =>
            setItemSortOrders((current) => ({
              ...current,
              [itemId]: sortOrder,
            }))
          }
          onToggleActive={toggleItemActive}
        />
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
