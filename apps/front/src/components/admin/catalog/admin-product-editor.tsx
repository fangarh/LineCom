"use client";

import type { FormEvent } from "react";
import type {
  AdminBrandListItem,
  AdminCategoryListItem,
  AdminProductDetail,
  AdminProductDuplicateCandidate,
} from "@/lib/api/admin-catalog";
import { AdminProductImagesPanel } from "./admin-product-images-panel";
import { AdminProductAttributesPanel } from "./admin-product-attributes-panel";
import { AdminProductDuplicatePanel } from "./admin-product-duplicate-panel";
import { AdminProductMainFields } from "./admin-product-main-fields";
import { AdminProductPublicationFields } from "./admin-product-publication-fields";
import { AdminProductSeoFields } from "./admin-product-seo-fields";
import {
  formFromAdminProductDetail,
  getProductEditorPanelId,
  getProductEditorTabId,
  productEditorTabs,
  type ProductEditorTab,
  type ProductFormState,
} from "./admin-product-editor-helpers";

type AdminProductEditorProps = {
  activeEditorTab: ProductEditorTab;
  alertMessage: string | null;
  brands: AdminBrandListItem[];
  categories: AdminCategoryListItem[];
  csrfToken?: string | null;
  duplicateCandidates: AdminProductDuplicateCandidate[];
  form: ProductFormState;
  isCheckingDuplicates: boolean;
  isLoadingDetail: boolean;
  isMutating: boolean;
  onCheckDuplicateCandidates: () => void;
  onDeleteSelectedProduct: () => void;
  onNameChange: (name: string) => void;
  onProductUpdated: (product: AdminProductDetail) => void;
  onRegenerateSlug: () => void;
  onSetActiveEditorTab: (tab: ProductEditorTab) => void;
  onSlugChange: (slug: string) => void;
  onSubmitProduct: (event: FormEvent<HTMLFormElement>) => void;
  selectedProduct: AdminProductDetail | null;
  setForm: (update: (current: ProductFormState) => ProductFormState) => void;
  statusMessage: string | null;
};

export function AdminProductEditor({
  activeEditorTab,
  alertMessage,
  brands,
  categories,
  csrfToken,
  duplicateCandidates,
  form,
  isCheckingDuplicates,
  isLoadingDetail,
  isMutating,
  onCheckDuplicateCandidates,
  onDeleteSelectedProduct,
  onNameChange,
  onProductUpdated,
  onRegenerateSlug,
  onSetActiveEditorTab,
  onSlugChange,
  onSubmitProduct,
  selectedProduct,
  setForm,
  statusMessage,
}: AdminProductEditorProps) {
  const activeTab = productEditorTabs.find((tab) => tab.id === activeEditorTab) ?? productEditorTabs[0];

  return (
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

      <form className="admin-product-form" onSubmit={onSubmitProduct}>
        <div className="admin-product-manager__tabs" role="tablist" aria-label="Вкладки товара">
          {productEditorTabs.map((tab) => (
            <button
              aria-controls={getProductEditorPanelId(tab.id)}
              aria-selected={activeEditorTab === tab.id}
              className="button button--ghost"
              id={getProductEditorTabId(tab.id)}
              key={tab.id}
              onClick={() => onSetActiveEditorTab(tab.id)}
              role="tab"
              type="button"
            >
              {tab.label}
            </button>
          ))}
        </div>

        <section
          aria-labelledby={getProductEditorTabId("main")}
          hidden={activeEditorTab !== "main"}
          id={getProductEditorPanelId("main")}
          role="tabpanel"
        >
          <AdminProductMainFields
            categories={categories}
            brands={brands}
            form={form}
            onNameChange={onNameChange}
            onRegenerateSlug={onRegenerateSlug}
            onSlugChange={onSlugChange}
            setForm={setForm}
          />
        </section>

        <section
          aria-labelledby={getProductEditorTabId("attributes")}
          hidden={activeEditorTab !== "attributes"}
          id={getProductEditorPanelId("attributes")}
          role="tabpanel"
        >
          {activeEditorTab === "attributes" ? (
            <AdminProductAttributesPanel
              key={selectedProduct?.id ?? "empty"}
              csrfToken={csrfToken}
              onProductUpdated={(product) => {
                onProductUpdated(product);
                setForm(() => formFromAdminProductDetail(product));
              }}
              product={selectedProduct}
            />
          ) : null}
        </section>

        <section
          aria-labelledby={getProductEditorTabId("images")}
          hidden={activeEditorTab !== "images"}
          id={getProductEditorPanelId("images")}
          role="tabpanel"
        >
          {activeEditorTab === "images" ? (
            <AdminProductImagesPanel productId={selectedProduct?.id ?? null} csrfToken={csrfToken} />
          ) : null}
        </section>

        <section
          aria-labelledby={getProductEditorTabId("seo")}
          hidden={activeEditorTab !== "seo"}
          id={getProductEditorPanelId("seo")}
          role="tabpanel"
        >
          <AdminProductSeoFields form={form} setForm={setForm} />
        </section>

        <section
          aria-labelledby={getProductEditorTabId("publication")}
          hidden={activeEditorTab !== "publication"}
          id={getProductEditorPanelId("publication")}
          role="tabpanel"
        >
          <AdminProductPublicationFields form={form} selectedProduct={selectedProduct} setForm={setForm} />
        </section>

        <AdminProductDuplicatePanel
          duplicateCandidates={duplicateCandidates}
          isCheckingDuplicates={isCheckingDuplicates}
          isLoadingDetail={isLoadingDetail}
          onCheckDuplicateCandidates={onCheckDuplicateCandidates}
        />

        <div className="admin-product-manager__actions">
          <button className="button button--primary" disabled={isMutating || isLoadingDetail} type="submit">
            {selectedProduct ? "Сохранить" : "Создать"}
          </button>
          <button
            className="button button--ghost"
            disabled={!selectedProduct || isMutating || isLoadingDetail}
            onClick={onDeleteSelectedProduct}
            type="button"
          >
            Удалить
          </button>
          <p className="admin-catalog-status">Активная вкладка: {activeTab.label}</p>
        </div>
      </form>
    </section>
  );
}
