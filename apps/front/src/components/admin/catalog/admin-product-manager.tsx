"use client";

import { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState, type FormEvent } from "react";
import {
  createAdminProduct,
  deleteAdminProduct,
  getAdminBrands,
  getAdminCategories,
  getAdminProduct,
  getAdminProductDuplicateCandidates,
  getAdminProducts,
  updateAdminProduct,
  type AdminBrandListItem,
  type AdminCategoryListItem,
  type AdminProductDetail,
  type AdminProductDuplicateCandidate,
  type AdminProductListItem,
  type AdminProductListParams,
} from "@/lib/api/admin-catalog";
import { normalizeApiError } from "@/lib/api/errors";
import { generateSlug } from "@/lib/catalog/slug";
import { AdminProductEditor } from "./admin-product-editor";
import {
  buildAdminProductCommand,
  emptyProductForm,
  formFromAdminProductDetail,
  type ProductEditorTab,
  type ProductFormState,
} from "./admin-product-editor-helpers";
import { AdminProductListPanel } from "./admin-product-list-panel";
import type { ProductListPageMeta } from "./admin-product-list-helpers";
import {
  buildDuplicateCandidateParams,
  buildProductListParams,
  loadCatalogOptionPages,
  productPageMetaFromResponse,
} from "./admin-product-manager-helpers";

const allCatalogOptionsPageSize = 60;
const defaultProductPageSize = 60;
const missingCsrfMessage = "Сессия не подтверждена. Обновите страницу и войдите снова.";

type AdminProductManagerProps = {
  csrfToken?: string | null;
};

export function AdminProductManager({ csrfToken = null }: AdminProductManagerProps) {
  const [products, setProducts] = useState<AdminProductListItem[]>([]);
  const [categories, setCategories] = useState<AdminCategoryListItem[]>([]);
  const [brands, setBrands] = useState<AdminBrandListItem[]>([]);
  const [selectedProduct, setSelectedProduct] = useState<AdminProductDetail | null>(null);
  const [form, setForm] = useState<ProductFormState>(emptyProductForm);
  const [isSlugManual, setIsSlugManual] = useState(false);
  const [activeEditorTab, setActiveEditorTab] = useState<ProductEditorTab>("main");
  const [duplicateCandidates, setDuplicateCandidates] = useState<AdminProductDuplicateCandidate[]>([]);
  const [search, setSearch] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");
  const [brandFilter, setBrandFilter] = useState("");
  const [activeFilter, setActiveFilter] = useState("");
  const [publishStatusFilter, setPublishStatusFilter] = useState("");
  const [productPage, setProductPage] = useState(1);
  const [productPageSize, setProductPageSize] = useState(defaultProductPageSize);
  const [productPageMeta, setProductPageMeta] = useState<ProductListPageMeta>({
    page: 1,
    pageSize: defaultProductPageSize,
    totalItems: 0,
    totalPages: 0,
  });
  const [isLoadingList, setIsLoadingList] = useState(false);
  const [isLoadingDetail, setIsLoadingDetail] = useState(false);
  const [isMutating, setIsMutating] = useState(false);
  const [isCheckingDuplicates, setIsCheckingDuplicates] = useState(false);
  const [alertMessage, setAlertMessage] = useState<string | null>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const listRequestSeqRef = useRef(0);
  const categoriesRequestSeqRef = useRef(0);
  const brandsRequestSeqRef = useRef(0);
  const detailRequestSeqRef = useRef(0);
  const duplicateRequestSeqRef = useRef(0);
  const editorSessionRef = useRef(0);
  const selectedProductIdRef = useRef<string | null>(null);
  const latestListParamsRef = useRef<AdminProductListParams>({});

  const listParams = useMemo<AdminProductListParams>(
    () =>
      buildProductListParams({
        activeFilter,
        brandFilter,
        categoryFilter,
        page: productPage,
        pageSize: productPageSize,
        publishStatusFilter,
        search,
      }),
    [activeFilter, brandFilter, categoryFilter, productPage, productPageSize, publishStatusFilter, search],
  );

  useEffect(() => {
    selectedProductIdRef.current = selectedProduct?.id ?? null;
  }, [selectedProduct?.id]);

  useEffect(() => {
    latestListParamsRef.current = listParams;
  }, [listParams]);

  const loadProductsForParams = useCallback(async (params: AdminProductListParams) => {
    const requestSeq = listRequestSeqRef.current + 1;
    listRequestSeqRef.current = requestSeq;
    setIsLoadingList(true);
    setAlertMessage(null);

    try {
      const response = await getAdminProducts(params);
      if (listRequestSeqRef.current !== requestSeq) return;
      setProducts(response.items);
      setProductPageMeta(productPageMetaFromResponse(response));
    } catch (error) {
      if (listRequestSeqRef.current !== requestSeq) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      if (listRequestSeqRef.current === requestSeq) {
        setIsLoadingList(false);
      }
    }
  }, []);

  const loadCategories = useCallback(async () => {
    const requestSeq = categoriesRequestSeqRef.current + 1;
    categoriesRequestSeqRef.current = requestSeq;

    try {
      const items = await loadCatalogOptionPages(
        (page, pageSize) => getAdminCategories({ page, pageSize }),
        () => categoriesRequestSeqRef.current === requestSeq,
        allCatalogOptionsPageSize,
      );
      if (!items) return;
      setCategories(items);
    } catch (error) {
      if (categoriesRequestSeqRef.current !== requestSeq) return;
      setAlertMessage(normalizeApiError(error).message);
    }
  }, []);

  const loadBrands = useCallback(async () => {
    const requestSeq = brandsRequestSeqRef.current + 1;
    brandsRequestSeqRef.current = requestSeq;

    try {
      const items = await loadCatalogOptionPages(
        (page, pageSize) => getAdminBrands({ page, pageSize }),
        () => brandsRequestSeqRef.current === requestSeq,
        allCatalogOptionsPageSize,
      );
      if (!items) return;
      setBrands(items);
    } catch (error) {
      if (brandsRequestSeqRef.current !== requestSeq) return;
      setAlertMessage(normalizeApiError(error).message);
    }
  }, []);

  useEffect(() => {
    let isCancelled = false;
    queueMicrotask(() => {
      if (!isCancelled) {
        loadCategories();
        loadBrands();
      }
    });

    return () => {
      isCancelled = true;
    };
  }, [loadBrands, loadCategories]);

  useLayoutEffect(() => {
    let isCancelled = false;
    queueMicrotask(() => {
      if (!isCancelled) {
        loadProductsForParams(listParams);
      }
    });

    return () => {
      isCancelled = true;
    };
  }, [listParams, loadProductsForParams]);

  const refreshProductList = useCallback(async () => {
    await loadProductsForParams(latestListParamsRef.current);
  }, [loadProductsForParams]);

  const resetProductPage = useCallback(() => {
    setProductPage(1);
  }, []);

  const changeSearch = useCallback(
    (value: string) => {
      resetProductPage();
      setSearch(value);
    },
    [resetProductPage],
  );

  const changeCategoryFilter = useCallback(
    (value: string) => {
      resetProductPage();
      setCategoryFilter(value);
    },
    [resetProductPage],
  );

  const changeBrandFilter = useCallback(
    (value: string) => {
      resetProductPage();
      setBrandFilter(value);
    },
    [resetProductPage],
  );

  const changeActiveFilter = useCallback(
    (value: string) => {
      resetProductPage();
      setActiveFilter(value);
    },
    [resetProductPage],
  );

  const changePublishStatusFilter = useCallback(
    (value: string) => {
      resetProductPage();
      setPublishStatusFilter(value);
    },
    [resetProductPage],
  );

  const changeProductPageSize = useCallback((pageSize: number) => {
    setProductPage(1);
    setProductPageSize(pageSize);
  }, []);

  async function selectProduct(productId: string) {
    const requestSeq = detailRequestSeqRef.current + 1;
    detailRequestSeqRef.current = requestSeq;
    editorSessionRef.current += 1;
    setIsLoadingDetail(true);
    setAlertMessage(null);
    setStatusMessage(null);
    setDuplicateCandidates([]);
    setIsCheckingDuplicates(false);
    setActiveEditorTab("main");

    try {
      const detail = await getAdminProduct(productId);
      if (detailRequestSeqRef.current !== requestSeq) return;

      setSelectedProduct(detail);
      setForm(formFromAdminProductDetail(detail));
      setIsSlugManual(true);
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
    duplicateRequestSeqRef.current += 1;
    editorSessionRef.current += 1;
    setIsLoadingDetail(false);
    setSelectedProduct(null);
    setForm(emptyProductForm);
    setIsSlugManual(false);
    setDuplicateCandidates([]);
    setIsCheckingDuplicates(false);
    setActiveEditorTab("main");
    setAlertMessage(null);
    setStatusMessage(null);
  }

  async function submitProduct(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isLoadingDetail) return;
    const capturedProductId = selectedProduct?.id ?? null;
    const capturedEditorSession = editorSessionRef.current;

    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    setIsMutating(true);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      const command = buildAdminProductCommand(form);
      const savedProduct = selectedProduct
        ? await updateAdminProduct(selectedProduct.id, command, csrfToken)
        : await createAdminProduct(command, csrfToken);

      if (!isCurrentProductMutation(capturedProductId, capturedEditorSession)) return;

      setSelectedProduct(savedProduct);
      setForm(formFromAdminProductDetail(savedProduct));
      setIsSlugManual(true);
      setStatusMessage(selectedProduct ? "Товар сохранен." : "Товар создан.");
      await refreshProductList();
    } catch (error) {
      if (!isCurrentProductMutation(capturedProductId, capturedEditorSession)) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      setIsMutating(false);
    }
  }

  async function deleteSelectedProduct() {
    if (isLoadingDetail) return;
    if (!selectedProduct) return;
    const capturedProductId = selectedProduct.id;
    const capturedEditorSession = editorSessionRef.current;

    if (!csrfToken) {
      setAlertMessage(missingCsrfMessage);
      return;
    }

    setIsMutating(true);
    setAlertMessage(null);
    setStatusMessage(null);

    try {
      await deleteAdminProduct(selectedProduct.id, csrfToken);
      if (!isCurrentProductMutation(capturedProductId, capturedEditorSession)) return;

      setSelectedProduct(null);
      setForm(emptyProductForm);
      setIsSlugManual(false);
      setDuplicateCandidates([]);
      setStatusMessage("Товар удален.");
      await refreshProductList();
    } catch (error) {
      if (!isCurrentProductMutation(capturedProductId, capturedEditorSession)) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      setIsMutating(false);
    }
  }

  async function checkDuplicateCandidates() {
    const requestSeq = duplicateRequestSeqRef.current + 1;
    duplicateRequestSeqRef.current = requestSeq;
    const capturedEditorSession = editorSessionRef.current;
    setIsCheckingDuplicates(true);
    setAlertMessage(null);

    try {
      const response = await getAdminProductDuplicateCandidates(buildDuplicateCandidateParams(form, selectedProduct?.id ?? null));
      if (duplicateRequestSeqRef.current !== requestSeq || editorSessionRef.current !== capturedEditorSession) return;
      setDuplicateCandidates(response.items);
    } catch (error) {
      if (duplicateRequestSeqRef.current !== requestSeq || editorSessionRef.current !== capturedEditorSession) return;
      setAlertMessage(normalizeApiError(error).message);
    } finally {
      if (duplicateRequestSeqRef.current === requestSeq) {
        setIsCheckingDuplicates(false);
      }
    }
  }

  function isCurrentProductMutation(capturedProductId: string | null, capturedEditorSession: number) {
    return editorSessionRef.current === capturedEditorSession && selectedProductIdRef.current === capturedProductId;
  }

  function changeProductName(name: string) {
    setForm((current) => ({
      ...current,
      name,
      slug: isSlugManual ? current.slug : generateSlug(name),
    }));
  }

  function changeProductSlug(slug: string) {
    setIsSlugManual(true);
    setForm((current) => ({ ...current, slug }));
  }

  function regenerateProductSlug() {
    setIsSlugManual(true);
    setForm((current) => ({ ...current, slug: generateSlug(current.name) }));
  }

  return (
    <div className="admin-product-manager">
      <AdminProductListPanel
        activeFilter={activeFilter}
        brandFilter={brandFilter}
        brands={brands}
        categories={categories}
        categoryFilter={categoryFilter}
        isLoadingList={isLoadingList}
        onActiveFilterChange={changeActiveFilter}
        onBrandFilterChange={changeBrandFilter}
        onCategoryFilterChange={changeCategoryFilter}
        onPageChange={setProductPage}
        onPageSizeChange={changeProductPageSize}
        onProductSelect={selectProduct}
        onPublishStatusFilterChange={changePublishStatusFilter}
        onSearchChange={changeSearch}
        onStartCreate={startCreate}
        pageMeta={productPageMeta}
        pageSize={productPageSize}
        products={products}
        publishStatusFilter={publishStatusFilter}
        search={search}
        selectedProductId={selectedProduct?.id ?? null}
      />

      <AdminProductEditor
        activeEditorTab={activeEditorTab}
        alertMessage={alertMessage}
        brands={brands}
        categories={categories}
        csrfToken={csrfToken}
        duplicateCandidates={duplicateCandidates}
        form={form}
        isCheckingDuplicates={isCheckingDuplicates}
        isLoadingDetail={isLoadingDetail}
        isMutating={isMutating}
        onCheckDuplicateCandidates={checkDuplicateCandidates}
        onDeleteSelectedProduct={deleteSelectedProduct}
        onNameChange={changeProductName}
        onProductUpdated={(product) => {
          setSelectedProduct(product);
          setIsSlugManual(true);
        }}
        onRegenerateSlug={regenerateProductSlug}
        onSetActiveEditorTab={setActiveEditorTab}
        onSlugChange={changeProductSlug}
        onSubmitProduct={submitProduct}
        selectedProduct={selectedProduct}
        setForm={setForm}
        statusMessage={statusMessage}
      />
    </div>
  );
}
