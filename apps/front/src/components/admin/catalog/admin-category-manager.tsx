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
} from "@/lib/api/admin-catalog";
import { normalizeApiError } from "@/lib/api/errors";
import { generateSlug } from "@/lib/catalog/slug";
import { AdminCategoryForm } from "./admin-category-form";
import { AdminCategoryListPanel } from "./admin-category-list-panel";
import {
  buildCategoryCommand,
  buildCategoryListParams,
  categoryFormFromDetail,
  emptyCategoryForm,
  parseCategorySortOrder,
  type CategoryFormState,
} from "./admin-category-manager-helpers";
import { AdminCategoryParentPicker } from "./admin-category-parent-picker";
import { buildCategoryTree, getBlockedParentIds } from "./admin-category-tree-helpers";

const allCategoriesPageSize = 60;

type AdminCategoryManagerProps = {
  csrfToken?: string | null;
};

export function AdminCategoryManager({ csrfToken = null }: AdminCategoryManagerProps) {
  const [categories, setCategories] = useState<AdminCategoryListItem[]>([]);
  const [allCategories, setAllCategories] = useState<AdminCategoryListItem[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<AdminCategoryDetail | null>(null);
  const [form, setForm] = useState<CategoryFormState>(emptyCategoryForm);
  const [isSlugManual, setIsSlugManual] = useState(false);
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

  const listParams = useMemo<AdminCategoryListParams>(
    () => buildCategoryListParams(search, parentFilter, activeFilter),
    [activeFilter, parentFilter, search],
  );

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
      setForm(categoryFormFromDetail(detail));
      setIsSlugManual(true);
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
    setForm(emptyCategoryForm);
    setIsSlugManual(false);
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
      const command = buildCategoryCommand(form);
      const savedCategory = selectedCategory
        ? await updateAdminCategory(selectedCategory.id, command, csrfToken)
        : await createAdminCategory(command, csrfToken);

      setSelectedCategory(savedCategory);
      setForm(categoryFormFromDetail(savedCategory));
      setIsSlugManual(true);
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
      setForm(emptyCategoryForm);
      setIsSlugManual(false);
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
      setForm(categoryFormFromDetail(movedCategory));
      setIsSlugManual(true);
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
      const sortedCategory = await sortAdminCategory(selectedCategory.id, parseCategorySortOrder(newSortOrder), csrfToken);
      setSelectedCategory(sortedCategory);
      setForm(categoryFormFromDetail(sortedCategory));
      setIsSlugManual(true);
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

  function changeCategoryName(name: string) {
    setForm((current) => ({
      ...current,
      name,
      slug: isSlugManual ? current.slug : generateSlug(name),
    }));
  }

  function changeCategorySlug(slug: string) {
    setIsSlugManual(true);
    setForm((current) => ({ ...current, slug }));
  }

  function regenerateCategorySlug() {
    setIsSlugManual(true);
    setForm((current) => ({ ...current, slug: generateSlug(current.name) }));
  }

  return (
    <div className="admin-category-manager">
      <AdminCategoryListPanel
        activeFilter={activeFilter}
        allCategories={allCategories}
        isLoadingList={isLoadingList}
        onActiveFilterChange={setActiveFilter}
        onCategorySelect={selectCategory}
        onCreateCategory={startCreate}
        onParentFilterChange={setParentFilter}
        onSearchChange={setSearch}
        parentFilter={parentFilter}
        search={search}
        selectedCategoryId={selectedId}
        treeCategories={treeCategories}
      />

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
          onNameChange={changeCategoryName}
          onRegenerateSlug={regenerateCategorySlug}
          onSlugChange={changeCategorySlug}
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
