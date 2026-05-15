"use client";

import type { ComponentProps } from "react";
import { AdminCatalogModal } from "./admin-catalog-modal";
import { AdminCategoryForm } from "./admin-category-form";
import { AdminCategoryParentPicker } from "./admin-category-parent-picker";

type AdminCategoryEditorModalProps = ComponentProps<typeof AdminCategoryForm> & {
  alertMessage: string | null;
  confirmClose: () => boolean;
  isOpen: boolean;
  moveParentId: string;
  newSortOrder: string;
  onMoveParentChange: (parentId: string) => void;
  onMoveSelectedCategory: () => void;
  onRequestClose: () => void;
  onSortOrderChange: (sortOrder: string) => void;
  onSortSelectedCategory: () => void;
  statusMessage: string | null;
};

export function AdminCategoryEditorModal({
  alertMessage,
  blockedParentIds,
  confirmClose,
  isLoadingDetail,
  isMutating,
  isOpen,
  moveParentId,
  newSortOrder,
  onMoveParentChange,
  onMoveSelectedCategory,
  onRequestClose,
  onSortOrderChange,
  onSortSelectedCategory,
  parentCategories,
  selectedCategory,
  statusMessage,
  ...formProps
}: AdminCategoryEditorModalProps) {
  const title = selectedCategory ? "Редактирование категории" : "Новая категория";
  const subtitle = isLoadingDetail
    ? "Загружаем карточку..."
    : selectedCategory
      ? selectedCategory.slug
      : "Заполните поля.";
  const isPositionDisabled = !selectedCategory || isMutating;

  return (
    <AdminCatalogModal
      closeLabel="Закрыть редактор категории"
      confirmClose={confirmClose}
      isCloseDisabled={isMutating}
      isOpen={isOpen}
      onRequestClose={onRequestClose}
      subtitle={subtitle}
      title={title}
    >
      {alertMessage ? (
        <p className="form-alert" role="alert">
          {alertMessage}
        </p>
      ) : null}
      {statusMessage ? <p className="form-success">{statusMessage}</p> : null}

      <AdminCategoryForm
        {...formProps}
        blockedParentIds={blockedParentIds}
        isLoadingDetail={isLoadingDetail}
        isMutating={isMutating}
        parentCategories={parentCategories}
        selectedCategory={selectedCategory}
        showHeader={false}
      />

      <section className="admin-category-manager__move" aria-label="Позиция">
        <div>
          <h3>Позиция</h3>
          <p className="admin-catalog-status">Родительская категория и порядок в дереве.</p>
        </div>
        <AdminCategoryParentPicker
          blockedParentIds={blockedParentIds}
          buttonLabel="Выбрать нового родителя"
          categories={parentCategories}
          disabled={isPositionDisabled}
          label="Новый родитель"
          onChange={onMoveParentChange}
          value={moveParentId}
        />
        <button
          className="button button--secondary"
          disabled={isPositionDisabled}
          onClick={onMoveSelectedCategory}
          type="button"
        >
          Переместить
        </button>
        <label className="form-field">
          <span>Новый порядок</span>
          <input
            disabled={isPositionDisabled}
            inputMode="numeric"
            onChange={(event) => onSortOrderChange(event.target.value)}
            type="number"
            value={newSortOrder}
          />
        </label>
        <button
          className="button button--secondary"
          disabled={isPositionDisabled}
          onClick={onSortSelectedCategory}
          type="button"
        >
          Обновить порядок
        </button>
      </section>
    </AdminCatalogModal>
  );
}
