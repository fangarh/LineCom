"use client";

import type { ComponentProps } from "react";
import { AdminCatalogModal } from "./admin-catalog-modal";
import { AdminProductEditor } from "./admin-product-editor";

type AdminProductEditorModalProps = ComponentProps<typeof AdminProductEditor> & {
  confirmClose: () => boolean;
  isOpen: boolean;
  onRequestClose: () => void;
};

export function AdminProductEditorModal({
  confirmClose,
  isOpen,
  isLoadingDetail,
  onRequestClose,
  selectedProduct,
  ...editorProps
}: AdminProductEditorModalProps) {
  const title = selectedProduct ? "Редактирование товара" : "Новый товар";
  const subtitle = isLoadingDetail
    ? "Загружаем карточку..."
    : selectedProduct
      ? selectedProduct.slug
      : "Заполните основные поля.";

  return (
    <AdminCatalogModal
      closeLabel="Закрыть редактор товара"
      confirmClose={confirmClose}
      isCloseDisabled={editorProps.isMutating}
      isOpen={isOpen}
      onRequestClose={onRequestClose}
      subtitle={subtitle}
      title={title}
    >
      <AdminProductEditor
        {...editorProps}
        isLoadingDetail={isLoadingDetail}
        selectedProduct={selectedProduct}
        showHeader={false}
      />
    </AdminCatalogModal>
  );
}
