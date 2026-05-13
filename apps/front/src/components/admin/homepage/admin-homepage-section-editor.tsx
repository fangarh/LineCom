import type { ChangeEvent, FormEvent } from "react";
import type { AdminHomepageSection } from "@/lib/api/admin-homepage";
import { AdminHomepageItemList } from "./admin-homepage-item-list";
import { AdminHomepageTargetSearch } from "./admin-homepage-target-search";

export type AdminHomepageSectionDraft = {
  title: string;
  itemLimit: string;
  sortOrder: string;
  isActive: boolean;
};

type AdminHomepageSectionEditorProps = {
  activeSection: AdminHomepageSection | null;
  draft: AdminHomepageSectionDraft;
  isLoading: boolean;
  isMutating: boolean;
  itemSortOrders: Record<string, string>;
  onAddCategory: (categoryId: string) => void;
  onAddProduct: (productId: string) => void;
  onDraftFieldChange: (event: ChangeEvent<HTMLInputElement>) => void;
  onRemove: (itemId: string) => void;
  onSaveItemOrder: () => void;
  onSaveSection: (event: FormEvent<HTMLFormElement>) => void;
  onSortOrderChange: (itemId: string, sortOrder: string) => void;
  onToggleActive: (itemId: string, isActive: boolean) => void;
};

export function AdminHomepageSectionEditor({
  activeSection,
  draft,
  isLoading,
  isMutating,
  itemSortOrders,
  onAddCategory,
  onAddProduct,
  onDraftFieldChange,
  onRemove,
  onSaveItemOrder,
  onSaveSection,
  onSortOrderChange,
  onToggleActive,
}: AdminHomepageSectionEditorProps) {
  return (
    <section className="admin-catalog-form admin-homepage-section" aria-label="Редактор секции">
      {activeSection ? (
        <>
          <form className="admin-homepage-section" onSubmit={onSaveSection}>
            <div className="admin-category-manager__head">
              <div>
                <h2>{activeSection.title}</h2>
                <p className="admin-catalog-status">{activeSection.type}</p>
              </div>
              <button className="button" disabled={isMutating} type="submit">
                Сохранить секцию
              </button>
            </div>

            <label className="admin-filter-field">
              <span>Заголовок секции</span>
              <input name="title" onChange={onDraftFieldChange} value={draft.title} />
            </label>

            <div className="admin-homepage-section__grid">
              <label className="admin-filter-field">
                <span>Лимит</span>
                <input min="0" name="itemLimit" onChange={onDraftFieldChange} type="number" value={draft.itemLimit} />
              </label>
              <label className="admin-filter-field">
                <span>Сортировка</span>
                <input min="0" name="sortOrder" onChange={onDraftFieldChange} type="number" value={draft.sortOrder} />
              </label>
            </div>

            <label className="admin-homepage-manager__check">
              <input checked={draft.isActive} name="isActive" onChange={onDraftFieldChange} type="checkbox" />
              <span>Секция активна</span>
            </label>
          </form>

          <AdminHomepageTargetSearch
            isMutating={isMutating}
            onAddCategory={onAddCategory}
            onAddProduct={onAddProduct}
            sectionType={activeSection.type}
          />

          <AdminHomepageItemList
            isLoading={isLoading}
            isMutating={isMutating}
            itemSortOrders={itemSortOrders}
            items={activeSection.items}
            onRemove={onRemove}
            onSaveOrder={onSaveItemOrder}
            onSortOrderChange={onSortOrderChange}
            onToggleActive={onToggleActive}
          />
        </>
      ) : (
        <p className="admin-catalog-status">Секции не найдены.</p>
      )}
    </section>
  );
}
