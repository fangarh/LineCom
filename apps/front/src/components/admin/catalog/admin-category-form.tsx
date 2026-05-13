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
};

export function AdminCategoryForm({
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
}: AdminCategoryFormProps) {
  function updateForm(patch: Partial<CategoryFormState>) {
    onFormChange({ ...form, ...patch });
  }

  return (
    <>
      <div className="admin-category-manager__head">
        <div>
          <h2>{selectedCategory ? "Редактирование категории" : "Новая категория"}</h2>
          <p className="admin-catalog-status">
            {isLoadingDetail ? "Загружаем карточку..." : selectedCategory ? selectedCategory.slug : "Заполните поля."}
          </p>
        </div>
      </div>

      <form className="admin-category-form" onSubmit={onSubmit}>
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
      </form>
    </>
  );
}
