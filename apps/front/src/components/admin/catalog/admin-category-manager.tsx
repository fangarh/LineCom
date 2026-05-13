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
import { AdminCategoryForm, type CategoryFormState } from "./admin-category-form";
import { AdminCategoryParentPicker } from "./admin-category-parent-picker";
import { AdminCategoryTree } from "./admin-category-tree";
import { buildCategoryTree, getBlockedParentIds } from "./admin-category-tree-helpers";

const allCategoriesPageSize = 60;

type AdminCategoryManagerProps = {
  csrfToken?: string | null;
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

  const hasActiveListFilters = Boolean(search.trim() || parentFilter || activeFilter);
  const treeCategories = hasActiveListFilters ? categories : allCategories;
  const allCategoriesTree = useMemo(() => buildCategoryTree(allCategories), [allCategories]);
  const blockedParentIds = useMemo(
    () => getBlockedParentIds(allCategoriesTree, selectedCategory?.id ?? null),
    [allCategoriesTree, selectedCategory?.id],
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

        <AdminCategoryTree
          categories={treeCategories}
          isLoading={isLoadingList}
          onCategorySelect={selectCategory}
          selectedCategoryId={selectedId}
        />
      </section>

      <section className="admin-catalog-form admin-category-manager__editor" aria-label="Редактор категории">
        {alertMessage ? (
          <p className="form-alert" role="alert">
            {alertMessage}
          </p>
        ) : null}
        {statusMessage ? <p className="form-success">{statusMessage}</p> : null}

        <AdminCategoryForm
          blockedParentIds={blockedParentIds}
          form={form}
          isLoadingDetail={isLoadingDetail}
          isMutating={isMutating}
          onDelete={deleteSelectedCategory}
          onFormChange={setForm}
          onSubmit={submitCategory}
          parentCategories={allCategories}
          selectedCategory={selectedCategory}
        />

        <div className="admin-category-manager__move" aria-label="Перемещение и сортировка">
          <AdminCategoryParentPicker
            blockedParentIds={blockedParentIds}
            buttonLabel="Выбрать нового родителя"
            categories={allCategories}
            disabled={!selectedCategory}
            label="Новый родитель"
            onChange={setMoveParentId}
            value={moveParentId}
          />
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
