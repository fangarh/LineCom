import type { AdminCategoryAttribute, AdminCategoryListItem } from "@/lib/api/admin-catalog";

type AdminAttributeListPanelProps = {
  attributes: AdminCategoryAttribute[];
  categories: AdminCategoryListItem[];
  isLoadingAttributes: boolean;
  isLoadingCategories: boolean;
  isMutatingAttribute: boolean;
  onCategoryChange: (categoryId: string) => void;
  onCreateAttribute: () => void;
  onInheritFromParent: () => void;
  onSelectAttribute: (attribute: AdminCategoryAttribute) => void;
  selectedAttributeId: string | null;
  selectedCategory: AdminCategoryListItem | null;
  selectedCategoryId: string;
};

export function AdminAttributeListPanel({
  attributes,
  categories,
  isLoadingAttributes,
  isLoadingCategories,
  isMutatingAttribute,
  onCategoryChange,
  onCreateAttribute,
  onInheritFromParent,
  onSelectAttribute,
  selectedAttributeId,
  selectedCategory,
  selectedCategoryId,
}: AdminAttributeListPanelProps) {
  return (
    <section className="admin-catalog-table admin-attribute-manager__list" aria-labelledby="admin-attribute-list-title">
      <div className="admin-attribute-manager__head">
        <div>
          <h2 id="admin-attribute-list-title">Характеристики</h2>
          <p>Категория, атрибуты, признаки и значения для фильтров.</p>
        </div>
        <div className="admin-attribute-manager__actions">
          <button className="button button--secondary" disabled={!selectedCategoryId || isMutatingAttribute} onClick={onInheritFromParent} type="button">
            Унаследовать от родителя
          </button>
          <button className="button button--primary" disabled={!selectedCategoryId} onClick={onCreateAttribute} type="button">
            Новая характеристика
          </button>
        </div>
      </div>

      <label className="admin-filter-field admin-attribute-manager__category">
        <span>Категория</span>
        <select aria-busy={isLoadingCategories} onChange={(event) => onCategoryChange(event.target.value)} value={selectedCategoryId}>
          <option value="">Выберите категорию</option>
          {categories.map((category) => (
            <option key={category.id} value={category.id}>
              {category.name}
            </option>
          ))}
        </select>
      </label>

      <div className="admin-attribute-manager__rows" aria-busy={isLoadingAttributes}>
        {attributes.length ? (
          attributes.map((attribute) => (
            <button
              aria-pressed={selectedAttributeId === attribute.id}
              className="admin-attribute-row"
              key={attribute.id}
              onClick={() => onSelectAttribute(attribute)}
              type="button"
            >
              <span>
                <strong>{attribute.name}</strong>
                <small>
                  {attribute.code} · {attribute.type}
                  {attribute.unit ? ` · ${attribute.unit}` : ""}
                </small>
              </span>
              <span className="admin-attribute-row__meta">
                {attribute.productValuesCount} значений в товарах · {attribute.sortOrder}
              </span>
            </button>
          ))
        ) : (
          <p className="empty-state">
            {selectedCategory ? "Характеристики не найдены." : "Выберите категорию."}
          </p>
        )}
      </div>
    </section>
  );
}
