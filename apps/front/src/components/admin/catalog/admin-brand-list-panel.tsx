import type { AdminBrandListItem } from "@/lib/api/admin-catalog";

type AdminBrandListPanelProps = {
  activeFilter: string;
  brands: AdminBrandListItem[];
  isLoadingList: boolean;
  onActiveFilterChange: (activeFilter: string) => void;
  onCreateBrand: () => void;
  onSearchChange: (search: string) => void;
  onSelectBrand: (brandId: string) => void;
  search: string;
  selectedBrandId: string | null;
};

export function AdminBrandListPanel({
  activeFilter,
  brands,
  isLoadingList,
  onActiveFilterChange,
  onCreateBrand,
  onSearchChange,
  onSelectBrand,
  search,
  selectedBrandId,
}: AdminBrandListPanelProps) {
  return (
    <section className="admin-catalog-table admin-brand-manager__list" aria-labelledby="admin-brand-list-title">
      <div className="admin-brand-manager__head">
        <div>
          <h2 id="admin-brand-list-title">Бренды</h2>
          <p>Фильтры, статус и быстрый выбор бренда.</p>
        </div>
        <button className="button button--primary" onClick={onCreateBrand} type="button">
          Новый бренд
        </button>
      </div>

      <div className="admin-brand-manager__filters">
        <label className="admin-filter-field">
          <span>Поиск</span>
          <input
            onChange={(event) => onSearchChange(event.target.value)}
            placeholder="Название или слаг"
            type="search"
            value={search}
          />
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

      <div className="admin-brand-manager__rows" aria-busy={isLoadingList} aria-label="Список брендов">
        {brands.length ? (
          brands.map((brand) => (
            <button
              aria-pressed={selectedBrandId === brand.id}
              className="admin-brand-row"
              key={brand.id}
              onClick={() => onSelectBrand(brand.id)}
              type="button"
            >
              <span>
                <strong>{brand.name}</strong>
                <small>{brand.slug}</small>
              </span>
              <span className="admin-brand-row__meta">
                {brand.isActive ? "Активен" : "Неактивен"} · {brand.productsCount} товаров
              </span>
            </button>
          ))
        ) : (
          <p className="empty-state">Бренды не найдены.</p>
        )}
      </div>
    </section>
  );
}
