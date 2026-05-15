import type { FormEvent } from "react";
import type { AdminCategoryDetail, AdminCategoryListItem } from "@/lib/api/admin-catalog";
import { AdminCategoryParentPicker } from "./admin-category-parent-picker";

export type CategoryFormState = {
  name: string;
  slug: string;
  parentId: string;
  description: string;
  h1: string;
  seoTitle: string;
  seoDescription: string;
  sortOrder: string;
  isActive: boolean;
  isVisibleInMenu: boolean;
};

type AdminCategoryFormProps = {
  activePanel?: AdminCategoryFormPanel | "all";
  form: CategoryFormState;
  selectedCategory: AdminCategoryDetail | null;
  isLoadingDetail: boolean;
  isMutating: boolean;
  parentCategories: AdminCategoryListItem[];
  blockedParentIds: Set<string>;
  onFormChange: (form: CategoryFormState) => void;
  onNameChange: (name: string) => void;
  onRegenerateSlug: () => void;
  onSlugChange: (slug: string) => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onDelete: () => void;
  showHeader?: boolean;
};

export type AdminCategoryFormPanel = "main" | "seo" | "actions";

export function AdminCategoryForm({
  activePanel = "all",
  form,
  selectedCategory,
  isLoadingDetail,
  isMutating,
  parentCategories,
  blockedParentIds,
  onFormChange,
  onNameChange,
  onRegenerateSlug,
  onSlugChange,
  onSubmit,
  onDelete,
  showHeader = true,
}: AdminCategoryFormProps) {
  const isTabbed = activePanel !== "all";

  function updateForm(patch: Partial<CategoryFormState>) {
    onFormChange({ ...form, ...patch });
  }

  return (
    <>
      {showHeader ? (
        <div className="admin-category-manager__head">
          <div>
            <h2>{selectedCategory ? "Редактирование категории" : "Новая категория"}</h2>
            <p className="admin-catalog-status">
              {isLoadingDetail ? "Загружаем карточку..." : selectedCategory ? selectedCategory.slug : "Заполните поля."}
            </p>
          </div>
        </div>
      ) : null}

      <form className="admin-category-form admin-category-editor__form" onSubmit={onSubmit}>
        <section
          aria-labelledby={isTabbed ? "admin-category-tab-main" : "admin-category-section-main"}
          className="admin-category-editor__section"
          hidden={isTabbed && activePanel !== "main"}
          id={isTabbed ? "admin-category-tabpanel-main" : undefined}
          role={isTabbed ? "tabpanel" : undefined}
        >
          <div className="admin-category-editor__section-head">
            <h3 id="admin-category-section-main">Основное</h3>
            <p className="admin-catalog-status">Название, адрес и место категории в основной структуре.</p>
          </div>

          <label className="form-field">
            <span>Название</span>
            <input onChange={(event) => onNameChange(event.target.value)} required value={form.name} />
          </label>
          <label className="form-field">
            <span>Slug</span>
            <input onChange={(event) => onSlugChange(event.target.value)} onFocus={(event) => event.currentTarget.select()} required value={form.slug} />
          </label>
          <button className="button button--ghost" onClick={onRegenerateSlug} type="button">
            Сгенерировать заново
          </button>
          <AdminCategoryParentPicker
            blockedParentIds={blockedParentIds}
            buttonLabel="Выбрать родителя"
            categories={parentCategories}
            label="Родительская категория"
            onChange={(parentId) => updateForm({ parentId })}
            value={form.parentId}
          />
          <label className="form-field">
            <span>Описание</span>
            <textarea onChange={(event) => updateForm({ description: event.target.value })} rows={4} value={form.description} />
          </label>
        </section>

        <section
          aria-labelledby={isTabbed ? "admin-category-tab-seo" : "admin-category-section-seo"}
          className="admin-category-editor__section"
          hidden={isTabbed && activePanel !== "seo"}
          id={isTabbed ? "admin-category-tabpanel-seo" : undefined}
          role={isTabbed ? "tabpanel" : undefined}
        >
          <div className="admin-category-editor__section-head">
            <h3 id="admin-category-section-seo">SEO и меню</h3>
            <p className="admin-catalog-status">Отображение в каталоге, меню и поисковых сниппетах.</p>
          </div>

          <label className="form-field">
            <span>H1</span>
            <input onChange={(event) => updateForm({ h1: event.target.value })} value={form.h1} />
          </label>
          <label className="form-field">
            <span>SEO title</span>
            <input onChange={(event) => updateForm({ seoTitle: event.target.value })} value={form.seoTitle} />
          </label>
          <label className="form-field">
            <span>SEO description</span>
            <textarea onChange={(event) => updateForm({ seoDescription: event.target.value })} rows={3} value={form.seoDescription} />
          </label>
          <label className="form-field">
            <span>Сортировка</span>
            <input
              inputMode="numeric"
              onChange={(event) => updateForm({ sortOrder: event.target.value })}
              type="number"
              value={form.sortOrder}
            />
          </label>
          <div className="admin-category-editor__checks">
            <label className="admin-category-manager__check">
              <input
                checked={form.isActive}
                onChange={(event) => updateForm({ isActive: event.target.checked })}
                type="checkbox"
              />
              <span>Активна</span>
            </label>
            <label className="admin-category-manager__check">
              <input
                checked={form.isVisibleInMenu}
                onChange={(event) => updateForm({ isVisibleInMenu: event.target.checked })}
                type="checkbox"
              />
              <span>Показывать в меню</span>
            </label>
          </div>
        </section>

        <section
          aria-labelledby={isTabbed ? "admin-category-tab-actions" : "admin-category-section-actions"}
          className="admin-category-editor__section admin-category-editor__section--actions"
          hidden={isTabbed && activePanel !== "actions"}
          id={isTabbed ? "admin-category-tabpanel-actions" : undefined}
          role={isTabbed ? "tabpanel" : undefined}
        >
          <div className="admin-category-editor__section-head">
            <h3 id="admin-category-section-actions">Действия</h3>
            <p className="admin-catalog-status">Сохранение изменений или удаление выбранной категории.</p>
          </div>

          <div className="admin-category-manager__actions">
            <button className="button button--primary" disabled={isMutating} type="submit">
              {selectedCategory ? "Сохранить" : "Создать"}
            </button>
            <button
              className="button button--ghost"
              disabled={!selectedCategory || isMutating}
              onClick={onDelete}
              type="button"
            >
              Удалить
            </button>
          </div>
        </section>
      </form>
    </>
  );
}
