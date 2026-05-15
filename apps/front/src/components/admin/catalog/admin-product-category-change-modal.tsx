import type { AdminCategoryListItem, AdminProductDetail } from "@/lib/api/admin-catalog";
import { AdminCatalogModal } from "./admin-catalog-modal";
import { AdminCategoryTreePicker } from "./admin-category-parent-picker";
import { findCategoryById, isLeafCategory, shouldShowCategoryAttributeWarning } from "./admin-product-category-change-helpers";

type AdminProductCategoryChangeModalProps = {
  alertMessage: string | null;
  categories: AdminCategoryListItem[];
  isLoadingDetail: boolean;
  isMutating: boolean;
  isOpen: boolean;
  onRequestClose: () => void;
  onSubmit: () => void;
  onTargetCategoryChange: (categoryId: string) => void;
  product: AdminProductDetail | null;
  statusMessage: string | null;
  targetCategoryId: string;
};

export function AdminProductCategoryChangeModal({
  alertMessage,
  categories,
  isLoadingDetail,
  isMutating,
  isOpen,
  onRequestClose,
  onSubmit,
  onTargetCategoryChange,
  product,
  statusMessage,
  targetCategoryId,
}: AdminProductCategoryChangeModalProps) {
  const currentCategory = product ? findCategoryById(categories, product.categoryId) : null;
  const targetCategory = findCategoryById(categories, targetCategoryId);
  const isTargetUnchanged = Boolean(product && targetCategoryId === product.categoryId);
  const canSave = Boolean(product && targetCategory && isLeafCategory(targetCategory) && !isTargetUnchanged && !isLoadingDetail && !isMutating);
  const showAttributeWarning = shouldShowCategoryAttributeWarning(product, targetCategoryId);

  return (
    <AdminCatalogModal
      closeLabel="Закрыть смену категории"
      isCloseDisabled={isMutating}
      isOpen={isOpen}
      onRequestClose={onRequestClose}
      subtitle={product ? product.name : "Загрузка товара"}
      title="Смена категории товара"
    >
      <section className="admin-product-category-change" aria-busy={isLoadingDetail || isMutating} aria-label="Быстрая смена категории товара">
        {alertMessage ? <p className="admin-catalog-alert" role="alert">{alertMessage}</p> : null}
        {statusMessage ? <p className="admin-catalog-status" role="status">{statusMessage}</p> : null}

        <div className="admin-product-category-change__summary">
          <div>
            <span>Товар</span>
            <strong>{product?.name ?? "Загрузка..."}</strong>
          </div>
          <div>
            <span>Текущая категория</span>
            <strong>{currentCategory?.name ?? product?.categoryName ?? "Категория не найдена"}</strong>
          </div>
          <div>
            <span>Новая категория</span>
            <strong>{targetCategory?.name ?? "Не выбрана"}</strong>
          </div>
        </div>

        <AdminCategoryTreePicker
          buttonLabel="Выбрать новую категорию"
          categories={categories}
          disabled={isLoadingDetail || isMutating || !product}
          getDisabledReason={() => "доступны только конечные категории"}
          isCategoryDisabled={({ category, hasChildren }) => !isLeafCategory(category, hasChildren)}
          label="Новая категория"
          onChange={onTargetCategoryChange}
          value={targetCategoryId}
        />

        {showAttributeWarning ? (
          <p className="admin-product-category-change__warning" role="alert">
            Характеристики товара будут очищены при смене категории. Проверьте карточку после сохранения.
          </p>
        ) : null}

        <div className="admin-product-category-change__actions">
          <button className="button button--secondary" disabled={isMutating} onClick={onRequestClose} type="button">
            Отмена
          </button>
          <button className="button button--primary" disabled={!canSave} onClick={onSubmit} type="button">
            {isMutating ? "Сохраняем..." : "Сохранить категорию"}
          </button>
        </div>
      </section>
    </AdminCatalogModal>
  );
}
