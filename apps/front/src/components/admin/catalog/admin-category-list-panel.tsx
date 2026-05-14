import type { AdminCategoryListItem } from "@/lib/api/admin-catalog";
import { AdminCategoryTree } from "./admin-category-tree";

type AdminCategoryListPanelProps = {
  activeFilter: string;
  allCategories: AdminCategoryListItem[];
  isLoadingList: boolean;
  onActiveFilterChange: (activeFilter: string) => void;
  onCategorySelect: (categoryId: string) => void;
  onCreateCategory: () => void;
  onParentFilterChange: (parentFilter: string) => void;
  onSearchChange: (search: string) => void;
  parentFilter: string;
  search: string;
  selectedCategoryId: string | null;
  treeCategories: AdminCategoryListItem[];
};

export function AdminCategoryListPanel({
  activeFilter,
  allCategories,
  isLoadingList,
  onActiveFilterChange,
  onCategorySelect,
  onCreateCategory,
  onParentFilterChange,
  onSearchChange,
  parentFilter,
  search,
  selectedCategoryId,
  treeCategories,
}: AdminCategoryListPanelProps) {
  return (
    <section className="admin-catalog-table admin-category-manager__list" aria-labelledby="admin-category-list-title">
      <div className="admin-category-manager__head">
        <div>
          <h2 id="admin-category-list-title">Категории</h2>
          <p>Фильтры, структура и быстрый выбор категории.</p>
        </div>
        <button className="button button--primary" onClick={onCreateCategory} type="button">
          Новая категория
        </button>
      </div>

      <div className="admin-category-manager__filters">
        <label className="admin-filter-field">
          <span>Поиск</span>
          <input
            onChange={(event) => onSearchChange(event.target.value)}
            placeholder="Название или slug"
            type="search"
            value={search}
          />
        </label>
        <label className="admin-filter-field">
          <span>Родитель</span>
          <select onChange={(event) => onParentFilterChange(event.target.value)} value={parentFilter}>
            <option value="">Все</option>
            {allCategories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
        </label>
        <label className="admin-filter-field">
          <span>Активность</span>
          <select onChange={(event) => onActiveFilterChange(event.target.value)} value={activeFilter}>
            <option value="">Все</option>
            <option value="true">Активные</option>
            <option value="false">Неактивные</option>
          </select>
        </label>
      </div>

      <AdminCategoryTree
        categories={treeCategories}
        isLoading={isLoadingList}
        onCategorySelect={onCategorySelect}
        selectedCategoryId={selectedCategoryId}
      />
    </section>
  );
}
