"use client";

import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from "react";
import {
  createAdminCategory,
  deleteAdminCategory,
  getAdminCategories,
  getAdminCategory,
  moveAdminCategory,
  sortAdminCategory,
  updateAdminCategory,
  type AdminCategoryDetail,
  type AdminCategoryListItem,
  type AdminCategoryListParams,
  type UpsertAdminCategoryCommand,
} from "@/lib/api/admin-catalog";
import { normalizeApiError } from "@/lib/api/errors";

const allCategoriesPageSize = 60;

type AdminCategoryManagerProps = {
  csrfToken?: string | null;
};

type CategoryFormState = {
  name: string;
  slug: string;
  parentId: string;
  description: string;
  h1: string;
  seoTitle: string;
  seoDescription: string;
  sortOrder: string;
  isActive: boolean;
  isVisibleInMenu: boolean;
};

const emptyForm: CategoryFormState = {
  name: "",
  slug: "",
  parentId: "",
  description: "",
  h1: "",
  seoTitle: "",
  seoDescription: "",
  sortOrder: "0",
  isActive: true,
  isVisibleInMenu: true,
};

export function AdminCategoryManager({ csrfToken = null }: AdminCategoryManagerProps) {
  const [categories, setCategories] = useState<AdminCategoryListItem[]>([]);
  const [allCategories, setAllCategories] = useState<AdminCategoryListItem[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<AdminCategoryDetail | null>(null);
  const [form, setForm] = useState<CategoryFormState>(emptyForm);
  const [search, setSearch] = useState("");
  const [parentFilter, setParentFilter] = useState("");
  const [activeFilter, setActiveFilter] = useState("");
  const [moveParentId, setMoveParentId] = useState("");
  const [newSortOrder, setNewSortOrder] = useState("0");
  const [isLoadingList, setIsLoadingList] = useState(false);
  const [isLoadingDetail, setIsLoadingDetail] = useState(false);
  const [isMutating, setIsMutating] = useState(false);
  const [alertMessage, setAlertMessage] = useState<string | null>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const listRequestSeqRef = useRef(0);
  const allCategoriesRequestSeqRef = useRef(0);
  const detailRequestSeqRef = useRef(0);
  const latestListParamsRef = useRef<AdminCategoryListParams>({});

  const listParams = useMemo<AdminCategoryListParams>(() => {
    const params: AdminCategoryListParams = {};
    const normalizedSearch = search.trim();

    if (normalizedSearch) {
      params.search = normalizedSearch;
    }

    if (parentFilter) {
      params.parentId = parentFilter;
    }

    if (activeFilter === "true") {
      params.isActive = true;
    } else if (activeFilter === "false") {
      params.isActive = false;
    }

    return params;
  }, [activeFilter, parentFilter, search]);

  useEffect(() => {
    latestListParamsRef.current = listParams;
  }, [listParams]);

  const loadCategoriesForParams = useCallback(async (params: AdminCategoryListParams) => {
    const requestSeq = listRequestSeqRef.current + 1;
    listRequestSeqRef.current = requestSeq;
    setIsLoadingList(true);
    setAlertMessage(null);

    try {
      const response = await getAdminCategories(params);
      if (listRequestSeqRef.current !== requestSeq) return;

      setCategories(response.items);
    } catch (error) {
      if (listRequestSeqRef.current !== requestSeq) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      if (listRequestSeqRef.current === requestSeq) {
        setIsLoadingList(false);
      }
    }
  }, []);

  const loadAllCategories = useCallback(async () => {
    const requestSeq = allCategoriesRequestSeqRef.current + 1;
    allCategoriesRequestSeqRef.current = requestSeq;

    try {
      const response = await getAdminCategories({ page: 1, pageSize: allCategoriesPageSize });
      if (allCategoriesRequestSeqRef.current !== requestSeq) return;

      const items = [...response.items];

      for (let page = 2; page <= response.totalPages; page += 1) {
        const pageResponse = await getAdminCategories({ page, pageSize: allCategoriesPageSize });
        if (allCategoriesRequestSeqRef.current !== requestSeq) return;

        items.push(...pageResponse.items);
      }

      setAllCategories(items);
    } catch (error) {
      if (allCategoriesRequestSeqRef.current !== requestSeq) return;
      setAlertMessage(normalizeApiError(error).message);
    }
  }, []);

  useEffect(() => {
    let isCancelled = false;
    queueMicrotask(() => {
      if (!isCancelled) {
        void loadAllCategories();
      }
    });

    return () => {
      isCancelled = true;
    };
  }, [loadAllCategories]);

  useEffect(() => {
    let isCancelled = false;
    queueMicrotask(() => {
      if (!isCancelled) {
        void loadCategoriesForParams(listParams);
      }
    });

    return () => {
      isCancelled = true;
    };
  }, [listParams, loadCategoriesForParams]);

  const parentOptions = useMemo(
    () => allCategories.filter((category) => category.id !== selectedCategory?.id),
    [allCategories, selectedCategory?.id],
  );

  const refreshCategoryLists = useCallback(async () => {
    const allCategoriesPromise = loadAllCategories();
    await loadCategoriesForParams(latestListParamsRef.current);
    await allCategoriesPromise;
  }, [loadAllCategories, loadCategoriesForParams]);

  async function selectCategory(categoryId: string) {
    const requestSeq = detailRequestSeqRef.current + 1;
    detailRequestSeqRef.current = requestSeq;
    setIsLoadingDetail(true);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      const detail = await getAdminCategory(categoryId);
      if (detailRequestSeqRef.current !== requestSeq) return;

      setSelectedCategory(detail);
      setForm(formFromDetail(detail));
      setMoveParentId(detail.parentId ?? "");
      setNewSortOrder(String(detail.sortOrder));
    } catch (error) {
      if (detailRequestSeqRef.current !== requestSeq) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      if (detailRequestSeqRef.current === requestSeq) {
        setIsLoadingDetail(false);
      }
    }
  }

  function startCreate() {
    detailRequestSeqRef.current += 1;
    setSelectedCategory(null);
    setForm(emptyForm);
    setMoveParentId("");
    setNewSortOrder("0");
    setAlertMessage(null);
    setStatusMessage(null);
  }

  async function submitCategory(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!csrfToken) {
      setAlertMessage("Сессия не подтверждена. Обновите страницу и войдите снова.");
      return;
    }

    setIsMutating(true);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      const command = buildCommand(form);
      const savedCategory = selectedCategory
        ? await updateAdminCategory(selectedCategory.id, command, csrfToken)
        : await createAdminCategory(command, csrfToken);

      setSelectedCategory(savedCategory);
      setForm(formFromDetail(savedCategory));
      setMoveParentId(savedCategory.parentId ?? "");
      setNewSortOrder(String(savedCategory.sortOrder));
      setStatusMessage(selectedCategory ? "Категория сохранена." : "Категория создана.");
      await refreshCategoryLists();
    } catch (error) {
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      setIsMutating(false);
    }
  }

  async function deleteSelectedCategory() {
    if (!selectedCategory) return;

    if (!csrfToken) {
      setAlertMessage("Сессия не подтверждена. Обновите страницу и войдите снова.");
      return;
    }

    setIsMutating(true);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      await deleteAdminCategory(selectedCategory.id, csrfToken);
      setSelectedCategory(null);
      setForm(emptyForm);
      setStatusMessage("Категория удалена.");
      await refreshCategoryLists();
    } catch (error) {
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      setIsMutating(false);
    }
  }

  async function moveSelectedCategory() {
    if (!selectedCategory) return;

    if (!csrfToken) {
      setAlertMessage("Сессия не подтверждена. Обновите страницу и войдите снова.");
      return;
    }

    setIsMutating(true);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      const movedCategory = await moveAdminCategory(selectedCategory.id, moveParentId || null, csrfToken);
      setSelectedCategory(movedCategory);
      setForm(formFromDetail(movedCategory));
      setMoveParentId(movedCategory.parentId ?? "");
      setStatusMessage("Родитель категории обновлен.");
      await refreshCategoryLists();
    } catch (error) {
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      setIsMutating(false);
    }
  }

  async function sortSelectedCategory() {
    if (!selectedCategory) return;

    if (!csrfToken) {
      setAlertMessage("Сессия не подтверждена. Обновите страницу и войдите снова.");
      return;
    }

    setIsMutating(true);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      const sortedCategory = await sortAdminCategory(selectedCategory.id, parseSortOrder(newSortOrder), csrfToken);
      setSelectedCategory(sortedCategory);
      setForm(formFromDetail(sortedCategory));
      setMoveParentId(sortedCategory.parentId ?? "");
      setNewSortOrder(String(sortedCategory.sortOrder));
      setStatusMessage("Порядок категории обновлен.");
      await refreshCategoryLists();
    } catch (error) {
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      setIsMutating(false);
    }
  }

  const selectedId = selectedCategory?.id ?? null;

  return (
    <div className="admin-category-manager">
      <section className="admin-catalog-table admin-category-manager__list" aria-labelledby="admin-category-list-title">
        <div className="admin-category-manager__head">
          <div>
            <h2 id="admin-category-list-title">Категории</h2>
            <p>Фильтры, структура и быстрый выбор категории.</p>
          </div>
          <button className="button button--primary" onClick={startCreate} type="button">
            Новая категория
          </button>
        </div>

        <div className="admin-category-manager__filters">
          <label className="admin-filter-field">
            <span>Поиск</span>
            <input
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Название или slug"
              type="search"
              value={search}
            />
          </label>
          <label className="admin-filter-field">
            <span>Родитель</span>
            <select onChange={(event) => setParentFilter(event.target.value)} value={parentFilter}>
              <option value="">Все</option>
              {allCategories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </select>
          </label>
          <label className="admin-filter-field">
            <span>Активность</span>
            <select onChange={(event) => setActiveFilter(event.target.value)} value={activeFilter}>
              <option value="">Все</option>
              <option value="true">Активные</option>
              <option value="false">Неактивные</option>
            </select>
          </label>
        </div>

        <div className="admin-category-manager__rows" aria-busy={isLoadingList}>
          {categories.length ? (
            categories.map((category) => (
              <button
                className="admin-category-row"
                aria-pressed={selectedId === category.id}
                key={category.id}
                onClick={() => selectCategory(category.id)}
                type="button"
              >
                <span>
                  <strong>{category.name}</strong>
                  <small>{category.slug}</small>
                </span>
                <span className="admin-category-row__meta">
                  {category.isActive ? "Активна" : "Неактивна"} · {category.productsCount} товаров · {category.sortOrder}
                </span>
              </button>
            ))
          ) : (
            <p className="empty-state">Категории не найдены.</p>
          )}
        </div>
      </section>

      <section className="admin-catalog-form admin-category-manager__editor" aria-label="Редактор категории">
        <div className="admin-category-manager__head">
          <div>
            <h2>{selectedCategory ? "Редактирование категории" : "Новая категория"}</h2>
            <p className="admin-catalog-status">
              {isLoadingDetail ? "Загружаем карточку..." : selectedCategory ? selectedCategory.slug : "Заполните поля."}
            </p>
          </div>
        </div>

        {alertMessage ? (
          <p className="form-alert" role="alert">
            {alertMessage}
          </p>
        ) : null}
        {statusMessage ? <p className="form-success">{statusMessage}</p> : null}

        <form className="admin-category-form" onSubmit={submitCategory}>
          <label className="form-field">
            <span>Название</span>
            <input
              onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
              required
              value={form.name}
            />
          </label>
          <label className="form-field">
            <span>Slug</span>
            <input
              onChange={(event) => setForm((current) => ({ ...current, slug: event.target.value }))}
              required
              value={form.slug}
            />
          </label>
          <label className="form-field">
            <span>Родительская категория</span>
            <select
              onChange={(event) => setForm((current) => ({ ...current, parentId: event.target.value }))}
              value={form.parentId}
            >
              <option value="">Без родителя</option>
              {parentOptions.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </select>
          </label>
          <label className="form-field">
            <span>Описание</span>
            <textarea
              onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
              rows={4}
              value={form.description}
            />
          </label>
          <label className="form-field">
            <span>H1</span>
            <input onChange={(event) => setForm((current) => ({ ...current, h1: event.target.value }))} value={form.h1} />
          </label>
          <label className="form-field">
            <span>SEO title</span>
            <input
              onChange={(event) => setForm((current) => ({ ...current, seoTitle: event.target.value }))}
              value={form.seoTitle}
            />
          </label>
          <label className="form-field">
            <span>SEO description</span>
            <textarea
              onChange={(event) => setForm((current) => ({ ...current, seoDescription: event.target.value }))}
              rows={3}
              value={form.seoDescription}
            />
          </label>
          <label className="form-field">
            <span>Сортировка</span>
            <input
              inputMode="numeric"
              onChange={(event) => setForm((current) => ({ ...current, sortOrder: event.target.value }))}
              type="number"
              value={form.sortOrder}
            />
          </label>
          <label className="admin-category-manager__check">
            <input
              checked={form.isActive}
              onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))}
              type="checkbox"
            />
            <span>Активна</span>
          </label>
          <label className="admin-category-manager__check">
            <input
              checked={form.isVisibleInMenu}
              onChange={(event) => setForm((current) => ({ ...current, isVisibleInMenu: event.target.checked }))}
              type="checkbox"
            />
            <span>Показывать в меню</span>
          </label>

          <div className="admin-category-manager__actions">
            <button className="button button--primary" disabled={isMutating} type="submit">
              {selectedCategory ? "Сохранить" : "Создать"}
            </button>
            <button
              className="button button--ghost"
              disabled={!selectedCategory || isMutating}
              onClick={deleteSelectedCategory}
              type="button"
            >
              Удалить
            </button>
          </div>
        </form>

        <div className="admin-category-manager__move" aria-label="Перемещение и сортировка">
          <label className="form-field">
            <span>Новый родитель</span>
            <select
              disabled={!selectedCategory}
              onChange={(event) => setMoveParentId(event.target.value)}
              value={moveParentId}
            >
              <option value="">Без родителя</option>
              {parentOptions.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </select>
          </label>
          <button
            className="button button--secondary"
            disabled={!selectedCategory || isMutating}
            onClick={moveSelectedCategory}
            type="button"
          >
            Переместить
          </button>
          <label className="form-field">
            <span>Новый порядок</span>
            <input
              disabled={!selectedCategory}
              inputMode="numeric"
              onChange={(event) => setNewSortOrder(event.target.value)}
              type="number"
              value={newSortOrder}
            />
          </label>
          <button
            className="button button--secondary"
            disabled={!selectedCategory || isMutating}
            onClick={sortSelectedCategory}
            type="button"
          >
            Обновить порядок
          </button>
        </div>
      </section>
    </div>
  );
}

function formFromDetail(category: AdminCategoryDetail): CategoryFormState {
  return {
    name: category.name,
    slug: category.slug,
    parentId: category.parentId ?? "",
    description: category.description ?? "",
    h1: category.h1 ?? "",
    seoTitle: category.seoTitle ?? "",
    seoDescription: category.seoDescription ?? "",
    sortOrder: String(category.sortOrder),
    isActive: category.isActive,
    isVisibleInMenu: category.isVisibleInMenu,
  };
}

function buildCommand(form: CategoryFormState): UpsertAdminCategoryCommand {
  return {
    name: form.name.trim(),
    slug: form.slug.trim(),
    parentId: form.parentId || null,
    description: normalizeOptionalText(form.description),
    h1: normalizeOptionalText(form.h1),
    seoTitle: normalizeOptionalText(form.seoTitle),
    seoDescription: normalizeOptionalText(form.seoDescription),
    sortOrder: parseSortOrder(form.sortOrder),
    isActive: form.isActive,
    isVisibleInMenu: form.isVisibleInMenu,
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
