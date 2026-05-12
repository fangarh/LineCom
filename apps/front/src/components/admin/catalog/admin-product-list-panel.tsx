import type { AdminBrandListItem, AdminCategoryListItem, AdminProductListItem } from "@/lib/api/admin-catalog";

type AdminProductListPanelProps = {
  activeFilter: string;
  brandFilter: string;
  brands: AdminBrandListItem[];
  categories: AdminCategoryListItem[];
  categoryFilter: string;
  isLoadingList: boolean;
  onActiveFilterChange: (value: string) => void;
  onBrandFilterChange: (value: string) => void;
  onCategoryFilterChange: (value: string) => void;
  onProductSelect: (productId: string) => void;
  onPublishStatusFilterChange: (value: string) => void;
  onSearchChange: (value: string) => void;
  onStartCreate: () => void;
  products: AdminProductListItem[];
  publishStatusFilter: string;
  search: string;
  selectedProductId: string | null;
};

export function AdminProductListPanel({
  activeFilter,
  brandFilter,
  brands,
  categories,
  categoryFilter,
  isLoadingList,
  onActiveFilterChange,
  onBrandFilterChange,
  onCategoryFilterChange,
  onProductSelect,
  onPublishStatusFilterChange,
  onSearchChange,
  onStartCreate,
  products,
  publishStatusFilter,
  search,
  selectedProductId,
}: AdminProductListPanelProps) {
  return (
    <section className="admin-catalog-table admin-product-manager__list" aria-label="Список товаров">
      <div className="admin-product-manager__head">
        <div>
          <h2>Товары</h2>
          <p>Фильтры, карточки и быстрый выбор товара.</p>
        </div>
        <button className="button button--primary" onClick={onStartCreate} type="button">
          Новый товар
        </button>
      </div>

      <div className="admin-product-manager__filters">
        <label className="admin-filter-field">
          <span>Поиск</span>
          <input onChange={(event) => onSearchChange(event.target.value)} placeholder="Название, SKU или slug" type="search" value={search} />
        </label>
        <label className="admin-filter-field">
          <span>Категория</span>
          <select onChange={(event) => onCategoryFilterChange(event.target.value)} value={categoryFilter}>
            <option value="">Все</option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
        </label>
        <label className="admin-filter-field">
          <span>Бренд</span>
          <select onChange={(event) => onBrandFilterChange(event.target.value)} value={brandFilter}>
            <option value="">Все</option>
            {brands.map((brand) => (
              <option key={brand.id} value={brand.id}>
                {brand.name}
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
        <label className="admin-filter-field">
          <span>Публикация</span>
          <select onChange={(event) => onPublishStatusFilterChange(event.target.value)} value={publishStatusFilter}>
            <option value="">Все</option>
            <option value="draft">Черновик</option>
            <option value="review">Проверка</option>
            <option value="published">Опубликован</option>
            <option value="archived">Архив</option>
          </select>
        </label>
      </div>

      <div className="admin-product-manager__rows" aria-busy={isLoadingList}>
        {products.length ? (
          products.map((product) => (
            <button
              aria-pressed={selectedProductId === product.id}
              className="admin-product-row"
              key={product.id}
              onClick={() => onProductSelect(product.id)}
              type="button"
            >
              <span>
                <strong>{product.name}</strong>
                <small>
                  {product.slug}
                  {product.sku ? ` В· ${product.sku}` : ""}
                </small>
              </span>
              <span className="admin-product-row__meta">
                {product.categoryName} В· {product.brandName ?? "Без бренда"} В· {product.publishStatus} В· {product.isActive ? "активен" : "неактивен"}
              </span>
            </button>
          ))
        ) : (
          <p className="empty-state">Товары не найдены.</p>
        )}
      </div>
    </section>
  );
}
