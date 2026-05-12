import type { AdminHomepageSectionItem } from "@/lib/api/admin-homepage";

type AdminHomepageItemListProps = {
  items: AdminHomepageSectionItem[];
  isLoading: boolean;
  isMutating: boolean;
  itemSortOrders: Record<string, string>;
  onSaveOrder: () => void;
  onSortOrderChange: (itemId: string, sortOrder: string) => void;
  onToggleActive: (itemId: string, isActive: boolean) => void;
  onRemove: (itemId: string) => void;
};

export function AdminHomepageItemList({
  items,
  isLoading,
  isMutating,
  itemSortOrders,
  onSaveOrder,
  onSortOrderChange,
  onToggleActive,
  onRemove,
}: AdminHomepageItemListProps) {
  return (
    <div className="admin-homepage-section">
      <div className="admin-category-manager__head">
        <h2>Элементы</h2>
        <button
          className="button button--ghost"
          disabled={isMutating || items.length === 0}
          onClick={onSaveOrder}
          type="button"
        >
          Сохранить порядок
        </button>
      </div>

      <div className="admin-homepage-section">
        {items.map((item) => (
          <article className="admin-homepage-item" key={item.id}>
            <div>
              <strong>{item.name}</strong>
              <p className="admin-homepage-item__meta">
                {[item.slug, item.secondaryText, item.visibilityStatus].filter(Boolean).join(" · ")}
              </p>
            </div>

            <div className="admin-homepage-item__controls">
              <label className="admin-filter-field">
                <span>Сортировка</span>
                <input
                  min="0"
                  onChange={(event) => onSortOrderChange(item.id, event.target.value)}
                  type="number"
                  value={itemSortOrders[item.id] ?? String(item.sortOrder)}
                />
              </label>
              <label className="admin-homepage-manager__check">
                <input
                  checked={item.isActive}
                  disabled={isMutating}
                  onChange={(event) => onToggleActive(item.id, event.target.checked)}
                  type="checkbox"
                />
                <span>Активен</span>
              </label>
              <button className="button button--ghost" disabled={isMutating} onClick={() => onRemove(item.id)} type="button">
                Удалить
              </button>
            </div>
          </article>
        ))}

        {!isLoading && items.length === 0 ? <p className="admin-catalog-status">Элементов нет.</p> : null}
      </div>
    </div>
  );
}
