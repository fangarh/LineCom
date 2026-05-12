"use client";

import { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState, type FormEvent } from "react";
import {
  createAdminProduct,
  deleteAdminProduct,
  getAdminBrands,
  getAdminCategories,
  getAdminCategoryAttributes,
  getAdminProduct,
  getAdminProductDuplicateCandidates,
  getAdminProducts,
  updateAdminProduct,
  updateAdminProductAttributes,
  type AdminBrandListItem,
  type AdminCategoryAttribute,
  type AdminCategoryListItem,
  type AdminProductDetail,
  type AdminProductAttributeValue,
  type AdminProductDuplicateCandidate,
  type AdminProductListItem,
  type AdminProductListParams,
  type UpdateAdminProductAttributesCommand,
  type UpsertAdminProductCommand,
} from "@/lib/api/admin-catalog";
import { normalizeApiError } from "@/lib/api/errors";
import { AdminProductImagesPanel } from "./admin-product-images-panel";

const allCatalogOptionsPageSize = 60;
const missingCsrfMessage = "Сессия не подтверждена. Обновите страницу и войдите снова.";

type AdminProductManagerProps = {
  csrfToken?: string | null;
};

type ProductEditorTab = "main" | "attributes" | "images" | "seo" | "publication";

type ProductFormState = {
  categoryId: string;
  brandId: string;
  name: string;
  slug: string;
  sku: string;
  externalId: string;
  description: string;
  shortDescription: string;
  availabilityStatus: string;
  saleUnit: string;
  unitQuantity: string;
  sortOrder: string;
  h1: string;
  seoTitle: string;
  seoDescription: string;
  publishStatus: string;
  isActive: boolean;
};

const emptyForm: ProductFormState = {
  categoryId: "",
  brandId: "",
  name: "",
  slug: "",
  sku: "",
  externalId: "",
  description: "",
  shortDescription: "",
  availabilityStatus: "in_stock",
  saleUnit: "шт",
  unitQuantity: "1",
  sortOrder: "0",
  h1: "",
  seoTitle: "",
  seoDescription: "",
  publishStatus: "draft",
  isActive: true,
};

const editorTabs: { id: ProductEditorTab; label: string }[] = [
  { id: "main", label: "Основное" },
  { id: "attributes", label: "Характеристики" },
  { id: "images", label: "Изображения" },
  { id: "seo", label: "SEO" },
  { id: "publication", label: "Публикация" },
];

export function AdminProductManager({ csrfToken = null }: AdminProductManagerProps) {
  const [products, setProducts] = useState<AdminProductListItem[]>([]);
  const [categories, setCategories] = useState<AdminCategoryListItem[]>([]);
  const [brands, setBrands] = useState<AdminBrandListItem[]>([]);
  const [selectedProduct, setSelectedProduct] = useState<AdminProductDetail | null>(null);
  const [form, setForm] = useState<ProductFormState>(emptyForm);
  const [activeEditorTab, setActiveEditorTab] = useState<ProductEditorTab>("main");
  const [duplicateCandidates, setDuplicateCandidates] = useState<AdminProductDuplicateCandidate[]>([]);
  const [search, setSearch] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");
  const [brandFilter, setBrandFilter] = useState("");
  const [activeFilter, setActiveFilter] = useState("");
  const [publishStatusFilter, setPublishStatusFilter] = useState("");
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

  const listParams = useMemo<AdminProductListParams>(() => {
    const params: AdminProductListParams = {};
    const normalizedSearch = search.trim();

    if (normalizedSearch) params.search = normalizedSearch;
    if (categoryFilter) params.categoryId = categoryFilter;
    if (brandFilter) params.brandId = brandFilter;
    if (activeFilter === "true") params.isActive = true;
    if (activeFilter === "false") params.isActive = false;
    if (publishStatusFilter) params.publishStatus = publishStatusFilter;

    return params;
  }, [activeFilter, brandFilter, categoryFilter, publishStatusFilter, search]);

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
      const response = await getAdminCategories({ page: 1, pageSize: allCatalogOptionsPageSize });
      if (categoriesRequestSeqRef.current !== requestSeq) return;
      const items = [...response.items];

      for (let page = 2; page <= response.totalPages; page += 1) {
        const pageResponse = await getAdminCategories({ page, pageSize: allCatalogOptionsPageSize });
        if (categoriesRequestSeqRef.current !== requestSeq) return;
        items.push(...pageResponse.items);
      }

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
      const response = await getAdminBrands({ page: 1, pageSize: allCatalogOptionsPageSize });
      if (brandsRequestSeqRef.current !== requestSeq) return;
      const items = [...response.items];

      for (let page = 2; page <= response.totalPages; page += 1) {
        const pageResponse = await getAdminBrands({ page, pageSize: allCatalogOptionsPageSize });
        if (brandsRequestSeqRef.current !== requestSeq) return;
        items.push(...pageResponse.items);
      }

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
      setForm(formFromDetail(detail));
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
    setForm(emptyForm);
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
      const command = buildCommand(form);
      const savedProduct = selectedProduct
        ? await updateAdminProduct(selectedProduct.id, command, csrfToken)
        : await createAdminProduct(command, csrfToken);

      if (!isCurrentProductMutation(capturedProductId, capturedEditorSession)) return;

      setSelectedProduct(savedProduct);
      setForm(formFromDetail(savedProduct));
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
      setForm(emptyForm);
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
      const response = await getAdminProductDuplicateCandidates({
        name: normalizeOptionalText(form.name),
        categoryId: form.categoryId || null,
        brandId: form.brandId || null,
        sku: normalizeOptionalText(form.sku),
        externalId: normalizeOptionalText(form.externalId),
        slug: normalizeOptionalText(form.slug),
        excludeProductId: selectedProduct?.id ?? null,
        limit: 5,
      });
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

  const selectedId = selectedProduct?.id ?? null;
  const activeTab = editorTabs.find((tab) => tab.id === activeEditorTab) ?? editorTabs[0];

  return (
    <div className="admin-product-manager">
      <section className="admin-catalog-table admin-product-manager__list" aria-label="Список товаров">
        <div className="admin-product-manager__head">
          <div>
            <h2>Товары</h2>
            <p>Фильтры, карточки и быстрый выбор товара.</p>
          </div>
          <button className="button button--primary" onClick={startCreate} type="button">
            Новый товар
          </button>
        </div>

        <div className="admin-product-manager__filters">
          <label className="admin-filter-field">
            <span>Поиск</span>
            <input onChange={(event) => setSearch(event.target.value)} placeholder="Название, SKU или slug" type="search" value={search} />
          </label>
          <label className="admin-filter-field">
            <span>Категория</span>
            <select onChange={(event) => setCategoryFilter(event.target.value)} value={categoryFilter}>
              <option value="">Все</option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </select>
          </label>
          <label className="admin-filter-field">
            <span>Бренд</span>
            <select onChange={(event) => setBrandFilter(event.target.value)} value={brandFilter}>
              <option value="">Все</option>
              {brands.map((brand) => (
                <option key={brand.id} value={brand.id}>
                  {brand.name}
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
          <label className="admin-filter-field">
            <span>Публикация</span>
            <select onChange={(event) => setPublishStatusFilter(event.target.value)} value={publishStatusFilter}>
              <option value="">Все</option>
              <option value="draft">Черновик</option>
              <option value="review">Проверка</option>
              <option value="published">Опубликован</option>
              <option value="archived">Архив</option>
            </select>
          </label>
        </div>

        <div className="admin-product-manager__rows" aria-busy={isLoadingList}>
          {products.length ? (
            products.map((product) => (
              <button
                aria-pressed={selectedId === product.id}
                className="admin-product-row"
                key={product.id}
                onClick={() => selectProduct(product.id)}
                type="button"
              >
                <span>
                  <strong>{product.name}</strong>
                  <small>
                    {product.slug}
                    {product.sku ? ` · ${product.sku}` : ""}
                  </small>
                </span>
                <span className="admin-product-row__meta">
                  {product.categoryName} · {product.brandName ?? "Без бренда"} · {product.publishStatus} · {product.isActive ? "активен" : "неактивен"}
                </span>
              </button>
            ))
          ) : (
            <p className="empty-state">Товары не найдены.</p>
          )}
        </div>
      </section>

      <section className="admin-catalog-form admin-product-manager__editor" aria-label="Редактор товара">
        <div className="admin-product-manager__head">
          <div>
            <h2>{selectedProduct ? "Редактирование товара" : "Новый товар"}</h2>
            <p className="admin-catalog-status">
              {isLoadingDetail ? "Загружаем карточку..." : selectedProduct ? selectedProduct.slug : "Заполните основные поля."}
            </p>
          </div>
        </div>

        {alertMessage ? (
          <p className="form-alert" role="alert">
            {alertMessage}
          </p>
        ) : null}
        {statusMessage ? <p className="form-success">{statusMessage}</p> : null}

        <form className="admin-product-form" onSubmit={submitProduct}>
          <div className="admin-product-manager__tabs" role="tablist" aria-label="Вкладки товара">
            {editorTabs.map((tab) => (
              <button
                aria-controls={getEditorPanelId(tab.id)}
                aria-selected={activeEditorTab === tab.id}
                className="button button--ghost"
                id={getEditorTabId(tab.id)}
                key={tab.id}
                onClick={() => setActiveEditorTab(tab.id)}
                role="tab"
                type="button"
              >
                {tab.label}
              </button>
            ))}
          </div>

          <section
            aria-labelledby={getEditorTabId("main")}
            hidden={activeEditorTab !== "main"}
            id={getEditorPanelId("main")}
            role="tabpanel"
          >
            <ProductMainFields categories={categories} brands={brands} form={form} setForm={setForm} />
          </section>

          <section
            aria-labelledby={getEditorTabId("attributes")}
            hidden={activeEditorTab !== "attributes"}
            id={getEditorPanelId("attributes")}
            role="tabpanel"
          >
            {activeEditorTab === "attributes" ? (
              <ProductAttributesPanel
                key={selectedProduct?.id ?? "empty"}
                csrfToken={csrfToken}
                onProductUpdated={(product) => {
                  setSelectedProduct(product);
                  setForm(formFromDetail(product));
                }}
                product={selectedProduct}
              />
            ) : null}
          </section>

          <section
            aria-labelledby={getEditorTabId("images")}
            hidden={activeEditorTab !== "images"}
            id={getEditorPanelId("images")}
            role="tabpanel"
          >
            {activeEditorTab === "images" ? (
              <AdminProductImagesPanel productId={selectedProduct?.id ?? null} csrfToken={csrfToken} />
            ) : null}
          </section>

          <section
            aria-labelledby={getEditorTabId("seo")}
            hidden={activeEditorTab !== "seo"}
            id={getEditorPanelId("seo")}
            role="tabpanel"
          >
            <ProductSeoFields form={form} setForm={setForm} />
          </section>

          <section
            aria-labelledby={getEditorTabId("publication")}
            hidden={activeEditorTab !== "publication"}
            id={getEditorPanelId("publication")}
            role="tabpanel"
          >
            <ProductPublicationFields form={form} selectedProduct={selectedProduct} setForm={setForm} />
          </section>

          <section className="admin-product-manager__duplicates" aria-label="Кандидаты дублей">
            <div className="admin-product-manager__head">
              <h2>Дубли</h2>
            <button
              className="button button--secondary"
              disabled={isCheckingDuplicates || isLoadingDetail}
              onClick={checkDuplicateCandidates}
              type="button"
            >
                Проверить дубли
              </button>
            </div>
            {duplicateCandidates.length ? (
              <table className="admin-product-manager__duplicate-table">
                <tbody>
                  {duplicateCandidates.map((candidate) => (
                    <tr key={candidate.id}>
                      <td>
                        <strong>{candidate.name}</strong>
                        <small>{candidate.slug}</small>
                      </td>
                      <td>{candidate.sku ?? "Без SKU"}</td>
                      <td>{candidate.categoryName}</td>
                      <td>{candidate.brandName ?? "Без бренда"}</td>
                      <td>{Math.round(candidate.similarity * 100)}%</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : (
              <p className="admin-catalog-status">Кандидаты не загружены.</p>
            )}
          </section>

          <div className="admin-product-manager__actions">
            <button className="button button--primary" disabled={isMutating || isLoadingDetail} type="submit">
              {selectedProduct ? "Сохранить" : "Создать"}
            </button>
            <button
              className="button button--ghost"
              disabled={!selectedProduct || isMutating || isLoadingDetail}
              onClick={deleteSelectedProduct}
              type="button"
            >
              Удалить
            </button>
            <p className="admin-catalog-status">Активная вкладка: {activeTab.label}</p>
          </div>
        </form>
      </section>
    </div>
  );
}

function ProductMainFields({
  brands,
  categories,
  form,
  setForm,
}: {
  brands: AdminBrandListItem[];
  categories: AdminCategoryListItem[];
  form: ProductFormState;
  setForm: (update: (current: ProductFormState) => ProductFormState) => void;
}) {
  return (
    <div className="admin-product-form__grid">
      <label className="form-field">
        <span>Категория</span>
        <select onChange={(event) => setForm((current) => ({ ...current, categoryId: event.target.value }))} required value={form.categoryId}>
          <option value="">Выберите категорию</option>
          {categories.map((category) => (
            <option key={category.id} value={category.id}>
              {category.name}
            </option>
          ))}
        </select>
      </label>
      <label className="form-field">
        <span>Бренд</span>
        <select onChange={(event) => setForm((current) => ({ ...current, brandId: event.target.value }))} value={form.brandId}>
          <option value="">Без бренда</option>
          {brands.map((brand) => (
            <option key={brand.id} value={brand.id}>
              {brand.name}
            </option>
          ))}
        </select>
      </label>
      <label className="form-field">
        <span>Название</span>
        <input onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} required value={form.name} />
      </label>
      <label className="form-field">
        <span>Slug</span>
        <input onChange={(event) => setForm((current) => ({ ...current, slug: event.target.value }))} required value={form.slug} />
      </label>
      <label className="form-field">
        <span>SKU</span>
        <input onChange={(event) => setForm((current) => ({ ...current, sku: event.target.value }))} value={form.sku} />
      </label>
      <label className="form-field">
        <span>External ID</span>
        <input onChange={(event) => setForm((current) => ({ ...current, externalId: event.target.value }))} value={form.externalId} />
      </label>
      <label className="form-field">
        <span>Краткое описание</span>
        <textarea
          onChange={(event) => setForm((current) => ({ ...current, shortDescription: event.target.value }))}
          rows={3}
          value={form.shortDescription}
        />
      </label>
      <label className="form-field">
        <span>Описание</span>
        <textarea onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} rows={4} value={form.description} />
      </label>
      <label className="form-field">
        <span>Наличие</span>
        <select onChange={(event) => setForm((current) => ({ ...current, availabilityStatus: event.target.value }))} value={form.availabilityStatus}>
          <option value="in_stock">В наличии</option>
          <option value="preorder">Под заказ</option>
          <option value="out_of_stock">Нет в наличии</option>
        </select>
      </label>
      <label className="form-field">
        <span>Единица продажи</span>
        <input onChange={(event) => setForm((current) => ({ ...current, saleUnit: event.target.value }))} required value={form.saleUnit} />
      </label>
      <label className="form-field">
        <span>Количество в единице</span>
        <input onChange={(event) => setForm((current) => ({ ...current, unitQuantity: event.target.value }))} required value={form.unitQuantity} />
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
    </div>
  );
}

type ProductAttributeFormState = {
  valueText: string;
  valueNumber: string;
  valueBoolean: boolean;
  attributeOptionId: string;
};

type ProductAttributeEditorRow = {
  attributeId: string;
  name: string;
  type: string;
  unit: string | null;
  options: AdminCategoryAttribute["options"];
};

function ProductAttributesPanel({
  csrfToken,
  onProductUpdated,
  product,
}: {
  csrfToken?: string | null;
  onProductUpdated: (product: AdminProductDetail) => void;
  product: AdminProductDetail | null;
}) {
  const [categoryAttributes, setCategoryAttributes] = useState<AdminCategoryAttribute[]>([]);
  const [values, setValues] = useState<Record<string, ProductAttributeFormState>>(() =>
    valuesFromAttributeRows(product?.attributes ?? []),
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
  const attributeRows = categoryAttributes
    .filter((attribute) => attribute.isActive)
    .map<ProductAttributeEditorRow>((attribute) => ({
      attributeId: attribute.id,
      name: attribute.name,
      type: attribute.type,
      unit: attribute.unit,
      options: attribute.options.filter((option) => option.isActive),
    }));

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
      const attributeValues = attributeRows
        .map((attribute) => commandFromAttribute(attribute, values[attribute.attributeId]))
        .filter((value) => value !== null);
      const command: UpdateAdminProductAttributesCommand = {
        values: attributeValues,
      };
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
            const value = values[attribute.attributeId] ?? emptyAttributeValue();

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

function ProductSeoFields({
  form,
  setForm,
}: {
  form: ProductFormState;
  setForm: (update: (current: ProductFormState) => ProductFormState) => void;
}) {
  return (
    <div className="admin-product-form__grid">
      <label className="form-field">
        <span>H1</span>
        <input onChange={(event) => setForm((current) => ({ ...current, h1: event.target.value }))} value={form.h1} />
      </label>
      <label className="form-field">
        <span>SEO title</span>
        <input onChange={(event) => setForm((current) => ({ ...current, seoTitle: event.target.value }))} value={form.seoTitle} />
      </label>
      <label className="form-field">
        <span>SEO description</span>
        <textarea
          onChange={(event) => setForm((current) => ({ ...current, seoDescription: event.target.value }))}
          rows={3}
          value={form.seoDescription}
        />
      </label>
    </div>
  );
}

function ProductPublicationFields({
  form,
  selectedProduct,
  setForm,
}: {
  form: ProductFormState;
  selectedProduct: AdminProductDetail | null;
  setForm: (update: (current: ProductFormState) => ProductFormState) => void;
}) {
  const canPublish = selectedProduct?.readiness.canPublish ?? false;
  const issues = selectedProduct?.readiness.issues ?? [];

  return (
    <div className="admin-product-form__grid">
      <label className="form-field">
        <span>Статус публикации</span>
        <select onChange={(event) => setForm((current) => ({ ...current, publishStatus: event.target.value }))} value={form.publishStatus}>
          <option value="draft">Черновик</option>
          <option value="review">Проверка</option>
          <option value="published">Опубликован</option>
          <option value="archived">Архив</option>
        </select>
      </label>
      <label className="admin-product-manager__check">
        <input
          checked={form.isActive}
          onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))}
          type="checkbox"
        />
        <span>Активен</span>
      </label>
      <div className="admin-product-manager__readiness">
        <strong>{canPublish ? "Можно опубликовать" : "Нельзя опубликовать"}</strong>
        {issues.length ? (
          <ul>
            {issues.map((issue) => (
              <li key={issue.code}>{issue.message}</li>
            ))}
          </ul>
        ) : (
          <p className="admin-catalog-status">Проблем готовности нет.</p>
        )}
      </div>
    </div>
  );
}

function formFromDetail(product: AdminProductDetail): ProductFormState {
  return {
    categoryId: product.categoryId,
    brandId: product.brandId ?? "",
    name: product.name,
    slug: product.slug,
    sku: product.sku ?? "",
    externalId: product.externalId ?? "",
    description: product.description ?? "",
    shortDescription: product.shortDescription ?? "",
    availabilityStatus: product.availabilityStatus,
    saleUnit: product.saleUnit,
    unitQuantity: product.unitQuantity,
    sortOrder: String(product.sortOrder),
    h1: product.h1 ?? "",
    seoTitle: product.seoTitle ?? "",
    seoDescription: product.seoDescription ?? "",
    publishStatus: product.publishStatus,
    isActive: product.isActive,
  };
}

function buildCommand(form: ProductFormState): UpsertAdminProductCommand {
  return {
    categoryId: form.categoryId || null,
    brandId: form.brandId || null,
    name: form.name.trim(),
    slug: form.slug.trim(),
    sku: normalizeOptionalText(form.sku),
    externalId: normalizeOptionalText(form.externalId),
    description: normalizeOptionalText(form.description),
    shortDescription: normalizeOptionalText(form.shortDescription),
    availabilityStatus: form.availabilityStatus,
    saleUnit: form.saleUnit.trim(),
    unitQuantity: form.unitQuantity.trim(),
    publishStatus: form.publishStatus,
    isActive: form.isActive,
    seoTitle: normalizeOptionalText(form.seoTitle),
    seoDescription: normalizeOptionalText(form.seoDescription),
    h1: normalizeOptionalText(form.h1),
    sortOrder: parseSortOrder(form.sortOrder),
  };
}

function valuesFromAttributeRows(rows: AdminProductAttributeValue[]) {
  return rows.reduce<Record<string, ProductAttributeFormState>>((values, row) => {
    values[row.attributeId] = valueFromProductAttributeValue(row);
    return values;
  }, {});
}

function valueFromProductAttributeValue(row: AdminProductAttributeValue): ProductAttributeFormState {
  return {
    valueText: row.valueText ?? "",
    valueNumber: row.valueNumber === null ? "" : String(row.valueNumber),
    valueBoolean: row.valueBoolean ?? false,
    attributeOptionId: row.attributeOptionId ?? "",
  };
}

function emptyAttributeValue(): ProductAttributeFormState {
  return {
    valueText: "",
    valueNumber: "",
    valueBoolean: false,
    attributeOptionId: "",
  };
}

function commandFromAttribute(row: ProductAttributeEditorRow, value: ProductAttributeFormState | undefined) {
  const currentValue = value ?? emptyAttributeValue();

  if (row.type === "number") {
    const valueNumber = parseNullableNumber(currentValue.valueNumber);
    if (valueNumber === null) return null;

    return {
      attributeId: row.attributeId,
      valueNumber,
    };
  }

  if (row.type === "boolean") {
    return {
      attributeId: row.attributeId,
      valueBoolean: currentValue.valueBoolean,
    };
  }

  if (row.type === "select") {
    if (!currentValue.attributeOptionId) return null;

    return {
      attributeId: row.attributeId,
      attributeOptionId: currentValue.attributeOptionId,
    };
  }

  const valueText = normalizeOptionalText(currentValue.valueText);
  if (valueText === null) return null;

  return {
    attributeId: row.attributeId,
    valueText,
  };
}

function parseNullableNumber(value: string) {
  if (!value.trim()) return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function attributeLabel(attribute: ProductAttributeEditorRow) {
  return attribute.name;
}

function normalizeOptionalText(value: string) {
  const normalized = value.trim();
  return normalized || null;
}

function parseSortOrder(value: string) {
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : 0;
}

function getEditorTabId(tab: ProductEditorTab) {
  return `admin-product-${tab}-tab`;
}

function getEditorPanelId(tab: ProductEditorTab) {
  return `admin-product-${tab}-panel`;
}
