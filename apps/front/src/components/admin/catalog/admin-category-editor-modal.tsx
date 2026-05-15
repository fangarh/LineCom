"use client";

import { useState, type ComponentProps, type KeyboardEvent } from "react";
import { AdminCatalogModal } from "./admin-catalog-modal";
import { AdminCategoryForm, type AdminCategoryFormPanel } from "./admin-category-form";
import { AdminCategoryParentPicker } from "./admin-category-parent-picker";

type CategoryEditorTab = AdminCategoryFormPanel | "position";

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

const CATEGORY_EDITOR_TABS: { id: CategoryEditorTab; label: string }[] = [
  { id: "main", label: "Основное" },
  { id: "seo", label: "SEO и меню" },
  { id: "position", label: "Позиция" },
  { id: "actions", label: "Действия" },
];

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
  const editorKey = `${isOpen ? "open" : "closed"}:${selectedCategory?.id ?? "new"}`;
  const [activeTabState, setActiveTabState] = useState<{ editorKey: string; tab: CategoryEditorTab }>({
    editorKey,
    tab: "main",
  });
  const activeTab = activeTabState.editorKey === editorKey ? activeTabState.tab : "main";
  const isPositionDisabled = !selectedCategory || isMutating;

  function focusTab(tabId: CategoryEditorTab) {
    document.getElementById(`admin-category-tab-${tabId}`)?.focus();
  }

  function selectTab(tabId: CategoryEditorTab) {
    setActiveTabState({ editorKey, tab: tabId });
  }

  function handleTabKeyDown(event: KeyboardEvent<HTMLButtonElement>, tabId: CategoryEditorTab) {
    const currentIndex = CATEGORY_EDITOR_TABS.findIndex((tab) => tab.id === tabId);
    if (currentIndex === -1) {
      return;
    }

    if (event.key === "ArrowRight" || event.key === "ArrowDown") {
      event.preventDefault();
      const nextTab = CATEGORY_EDITOR_TABS[(currentIndex + 1) % CATEGORY_EDITOR_TABS.length].id;
      selectTab(nextTab);
      focusTab(nextTab);
    }

    if (event.key === "ArrowLeft" || event.key === "ArrowUp") {
      event.preventDefault();
      const previousTab = CATEGORY_EDITOR_TABS[(currentIndex - 1 + CATEGORY_EDITOR_TABS.length) % CATEGORY_EDITOR_TABS.length].id;
      selectTab(previousTab);
      focusTab(previousTab);
    }
  }

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
      <div className="admin-category-editor">
        {alertMessage ? (
          <p className="form-alert" role="alert">
            {alertMessage}
          </p>
        ) : null}
        {statusMessage ? <p className="form-success">{statusMessage}</p> : null}

        <div aria-label="Разделы редактора категории" className="admin-category-editor__tabs" role="tablist">
          {CATEGORY_EDITOR_TABS.map((tab) => (
            <button
              aria-controls={`admin-category-tabpanel-${tab.id}`}
              aria-selected={activeTab === tab.id}
              className="admin-category-editor__tab"
              id={`admin-category-tab-${tab.id}`}
              key={tab.id}
              onClick={() => selectTab(tab.id)}
              onKeyDown={(event) => handleTabKeyDown(event, tab.id)}
              role="tab"
              tabIndex={activeTab === tab.id ? 0 : -1}
              type="button"
            >
              {tab.label}
            </button>
          ))}
        </div>

        {activeTab !== "position" ? (
          <AdminCategoryForm
            {...formProps}
            activePanel={activeTab}
            blockedParentIds={blockedParentIds}
            isLoadingDetail={isLoadingDetail}
            isMutating={isMutating}
            parentCategories={parentCategories}
            selectedCategory={selectedCategory}
            showHeader={false}
          />
        ) : null}

        <section
          aria-labelledby="admin-category-tab-position"
          className="admin-category-editor__section admin-category-editor__section--position"
          hidden={activeTab !== "position"}
          id="admin-category-tabpanel-position"
          role="tabpanel"
        >
          <div className="admin-category-editor__section-head">
            <h3 id="admin-category-section-position">Позиция</h3>
            <p className="admin-catalog-status">Перемещение категории и порядок в дереве.</p>
          </div>
          <div className="admin-category-editor__position-grid">
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
          </div>
        </section>
      </div>
    </AdminCatalogModal>
  );
}
