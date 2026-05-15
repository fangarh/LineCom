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
import { AdminCategoryEditorModal } from "./admin-category-editor-modal";
import { AdminCategoryListPanel } from "./admin-category-list-panel";
import {
  buildCategoryCommand,
  buildCategoryListParams,
  categoryFormFromDetail,
  emptyCategoryForm,
  parseCategorySortOrder,
  type CategoryFormState,
} from "./admin-category-manager-helpers";
import { buildCategoryTree, getBlockedParentIds } from "./admin-category-tree-helpers";

const allCategoriesPageSize = 60;
const missingCsrfMessage = "Сессия не подтверждена. Обновите страницу и войдите снова.";

type AdminCategoryManagerProps = {
  csrfToken?: string | null;
};

export function AdminCategoryManager({ csrfToken = null }: AdminCategoryManagerProps) {
  const [categories, setCategories] = useState<AdminCategoryListItem[]>([]);
  const [allCategories, setAllCategories] = useState<AdminCategoryListItem[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<AdminCategoryDetail | null>(null);
  const [form, setForm] = useState<CategoryFormState>(emptyCategoryForm);
  const [categoryEditorBaselineSignature, setCategoryEditorBaselineSignature] = useState(() =>
    serializeCategoryEditorSnapshot(emptyCategoryForm, "", "0"),
  );
  const [isCategoryEditorOpen, setIsCategoryEditorOpen] = useState(false);
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
  const categoryEditorSessionRef = useRef(0);
  const selectedCategoryIdRef = useRef<string | null>(null);
  const latestListParamsRef = useRef<AdminCategoryListParams>({});

  const listParams = useMemo<AdminCategoryListParams>(
    () => buildCategoryListParams(search, parentFilter, activeFilter),
    [activeFilter, parentFilter, search],
  );

  useEffect(() => {
    latestListParamsRef.current = listParams;
  }, [listParams]);

  useEffect(() => {
    selectedCategoryIdRef.current = selectedCategory?.id ?? null;
  }, [selectedCategory?.id]);

  const categoryEditorSignature = useMemo(
    () => serializeCategoryEditorSnapshot(form, moveParentId, newSortOrder),
    [form, moveParentId, newSortOrder],
  );
  const hasCategoryUnsavedChanges = categoryEditorSignature !== categoryEditorBaselineSignature;

  function resetCategoryEditorBaseline(nextForm: CategoryFormState, nextMoveParentId: string, nextSortOrder: string) {
    setCategoryEditorBaselineSignature(serializeCategoryEditorSnapshot(nextForm, nextMoveParentId, nextSortOrder));
  }

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
    categoryEditorSessionRef.current += 1;
    setIsCategoryEditorOpen(true);
    setIsLoadingDetail(true);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      const detail = await getAdminCategory(categoryId);
      if (detailRequestSeqRef.current !== requestSeq) return;

      const nextForm = categoryFormFromDetail(detail);
      const nextMoveParentId = detail.parentId ?? "";
      const nextSortOrder = String(detail.sortOrder);
      setSelectedCategory(detail);
      setForm(nextForm);
      setIsSlugManual(true);
      setMoveParentId(nextMoveParentId);
      setNewSortOrder(nextSortOrder);
      resetCategoryEditorBaseline(nextForm, nextMoveParentId, nextSortOrder);
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
    categoryEditorSessionRef.current += 1;
    setIsCategoryEditorOpen(true);
    setSelectedCategory(null);
    setForm(emptyCategoryForm);
    setIsSlugManual(false);
    setMoveParentId("");
    setNewSortOrder("0");
    resetCategoryEditorBaseline(emptyCategoryForm, "", "0");
    setAlertMessage(null);
    setStatusMessage(null);
  }

  async function submitCategory(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    const capturedCategoryId = selectedCategory?.id ?? null;
    const capturedEditorSession = categoryEditorSessionRef.current;
    setIsMutating(true);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      const command = buildCategoryCommand(form);
      const savedCategory = selectedCategory
        ? await updateAdminCategory(selectedCategory.id, command, csrfToken)
        : await createAdminCategory(command, csrfToken);

      if (!isCurrentCategoryMutation(capturedCategoryId, capturedEditorSession)) return;

      const nextForm = categoryFormFromDetail(savedCategory);
      const nextMoveParentId = savedCategory.parentId ?? "";
      const nextSortOrder = String(savedCategory.sortOrder);
      setSelectedCategory(savedCategory);
      setForm(nextForm);
      setIsSlugManual(true);
      setMoveParentId(nextMoveParentId);
      setNewSortOrder(nextSortOrder);
      resetCategoryEditorBaseline(nextForm, nextMoveParentId, nextSortOrder);
      setStatusMessage(selectedCategory ? "Категория сохранена." : "Категория создана.");
      await refreshCategoryLists();
    } catch (error) {
      if (!isCurrentCategoryMutation(capturedCategoryId, capturedEditorSession)) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      setIsMutating(false);
    }
  }

  async function deleteSelectedCategory() {
    if (!selectedCategory) return;

    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    const capturedCategoryId = selectedCategory.id;
    const capturedEditorSession = categoryEditorSessionRef.current;
    setIsMutating(true);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      await deleteAdminCategory(selectedCategory.id, csrfToken);
      if (!isCurrentCategoryMutation(capturedCategoryId, capturedEditorSession)) return;

      detailRequestSeqRef.current += 1;
      categoryEditorSessionRef.current += 1;
      setIsCategoryEditorOpen(false);
      setSelectedCategory(null);
      setForm(emptyCategoryForm);
      setIsSlugManual(false);
      setMoveParentId("");
      setNewSortOrder("0");
      resetCategoryEditorBaseline(emptyCategoryForm, "", "0");
      setStatusMessage("Категория удалена.");
      await refreshCategoryLists();
    } catch (error) {
      if (!isCurrentCategoryMutation(capturedCategoryId, capturedEditorSession)) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      setIsMutating(false);
    }
  }

  async function moveSelectedCategory() {
    if (!selectedCategory) return;

    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    const capturedCategoryId = selectedCategory.id;
    const capturedEditorSession = categoryEditorSessionRef.current;
    setIsMutating(true);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      const movedCategory = await moveAdminCategory(selectedCategory.id, moveParentId || null, csrfToken);
      if (!isCurrentCategoryMutation(capturedCategoryId, capturedEditorSession)) return;

      const nextForm = categoryFormFromDetail(movedCategory);
      const nextMoveParentId = movedCategory.parentId ?? "";
      const nextSortOrder = String(movedCategory.sortOrder);
      setSelectedCategory(movedCategory);
      setForm(nextForm);
      setIsSlugManual(true);
      setMoveParentId(nextMoveParentId);
      setNewSortOrder(nextSortOrder);
      resetCategoryEditorBaseline(nextForm, nextMoveParentId, nextSortOrder);
      setStatusMessage("Родитель категории обновлен.");
      await refreshCategoryLists();
    } catch (error) {
      if (!isCurrentCategoryMutation(capturedCategoryId, capturedEditorSession)) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      setIsMutating(false);
    }
  }

  async function sortSelectedCategory() {
    if (!selectedCategory) return;

    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    const capturedCategoryId = selectedCategory.id;
    const capturedEditorSession = categoryEditorSessionRef.current;
    setIsMutating(true);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      const sortedCategory = await sortAdminCategory(selectedCategory.id, parseCategorySortOrder(newSortOrder), csrfToken);
      if (!isCurrentCategoryMutation(capturedCategoryId, capturedEditorSession)) return;

      const nextForm = categoryFormFromDetail(sortedCategory);
      const nextMoveParentId = sortedCategory.parentId ?? "";
      const nextSortOrder = String(sortedCategory.sortOrder);
      setSelectedCategory(sortedCategory);
      setForm(nextForm);
      setIsSlugManual(true);
      setMoveParentId(nextMoveParentId);
      setNewSortOrder(nextSortOrder);
      resetCategoryEditorBaseline(nextForm, nextMoveParentId, nextSortOrder);
      setStatusMessage("Порядок категории обновлен.");
      await refreshCategoryLists();
    } catch (error) {
      if (!isCurrentCategoryMutation(capturedCategoryId, capturedEditorSession)) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      setIsMutating(false);
    }
  }

  const selectedId = selectedCategory?.id ?? null;

  function isCurrentCategoryMutation(capturedCategoryId: string | null, capturedEditorSession: number) {
    return categoryEditorSessionRef.current === capturedEditorSession && selectedCategoryIdRef.current === capturedCategoryId;
  }

  function confirmCategoryEditorClose() {
    if (!hasCategoryUnsavedChanges) return true;
    return window.confirm("Закрыть редактор без сохранения изменений?");
  }

  function closeCategoryEditor() {
    if (isMutating) return;
    detailRequestSeqRef.current += 1;
    categoryEditorSessionRef.current += 1;
    setIsLoadingDetail(false);
    setIsCategoryEditorOpen(false);
  }

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

      {alertMessage && !isCategoryEditorOpen ? (
        <p className="form-alert" role="alert">
          {alertMessage}
        </p>
      ) : null}

      <AdminCategoryEditorModal
        alertMessage={alertMessage}
        blockedParentIds={blockedParentIds}
        confirmClose={confirmCategoryEditorClose}
        form={form}
        isLoadingDetail={isLoadingDetail}
        isMutating={isMutating}
        isOpen={isCategoryEditorOpen}
        moveParentId={moveParentId}
        newSortOrder={newSortOrder}
        onDelete={deleteSelectedCategory}
        onFormChange={setForm}
        onMoveParentChange={setMoveParentId}
        onMoveSelectedCategory={moveSelectedCategory}
        onNameChange={changeCategoryName}
        onRegenerateSlug={regenerateCategorySlug}
        onRequestClose={closeCategoryEditor}
        onSlugChange={changeCategorySlug}
        onSortOrderChange={setNewSortOrder}
        onSortSelectedCategory={sortSelectedCategory}
        onSubmit={submitCategory}
        parentCategories={allCategories}
        selectedCategory={selectedCategory}
        statusMessage={statusMessage}
      />
    </div>
  );
}

function serializeCategoryEditorSnapshot(form: CategoryFormState, moveParentId: string, newSortOrder: string) {
  return JSON.stringify({ form, moveParentId, newSortOrder });
}
