import type { AdminBrandListItem, AdminCategoryListItem, AdminProductListItem } from "@/lib/api/admin-catalog";
import {
  formatPageRange,
  getProductIssueLabels,
  getProductStatusLabels,
  type ProductListPageMeta,
} from "./admin-product-list-helpers";

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
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
  onProductSelect: (productId: string) => void;
  onPublishStatusFilterChange: (value: string) => void;
  onSearchChange: (value: string) => void;
  onStartCreate: () => void;
  pageMeta: ProductListPageMeta;
  pageSize: number;
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
  onPageChange,
  onPageSizeChange,
  onProductSelect,
  onPublishStatusFilterChange,
  onSearchChange,
  onStartCreate,
  pageMeta,
  pageSize,
  products,
  publishStatusFilter,
  search,
  selectedProductId,
}: AdminProductListPanelProps) {
  const canGoBack = pageMeta.page > 1;
  const canGoForward = pageMeta.page < pageMeta.totalPages;

  return (
    <section className="admin-catalog-table admin-product-manager__list" aria-label="Список товаров">
      <div className="admin-product-manager__head">
        <div>
          <h2>Товары</h2>
          <p>Фильтры, таблица и быстрый выбор товара.</p>
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

      <div className="admin-product-manager__pagination" aria-label="Пагинация товаров">
        <button className="button button--secondary" disabled={!canGoBack || isLoadingList} onClick={() => onPageChange(pageMeta.page - 1)} type="button">
          Назад
        </button>
        <span className="admin-product-manager__page-range">{formatPageRange(pageMeta)}</span>
        <button className="button button--secondary" disabled={!canGoForward || isLoadingList} onClick={() => onPageChange(pageMeta.page + 1)} type="button">
          Дальше
        </button>
        <label className="admin-filter-field admin-product-manager__page-size">
          <span>На странице</span>
          <select onChange={(event) => onPageSizeChange(Number(event.target.value))} value={pageSize}>
            <option value={20}>20</option>
            <option value={40}>40</option>
            <option value={60}>60</option>
          </select>
        </label>
      </div>

      <div className="admin-product-manager__rows" aria-busy={isLoadingList}>
        {products.length ? (
          <div className="admin-product-table-wrap">
            <table className="admin-product-table">
              <thead>
                <tr>
                  <th scope="col">Товар</th>
                  <th scope="col">SKU / External ID</th>
                  <th scope="col">Категория</th>
                  <th scope="col">Бренд</th>
                  <th scope="col">Статусы</th>
                  <th scope="col">Проблемы</th>
                </tr>
              </thead>
              <tbody>
                {products.map((product) => {
                  const issueLabels = getProductIssueLabels(product);

                  return (
                    <tr className={selectedProductId === product.id ? "admin-product-table__row is-selected" : "admin-product-table__row"} key={product.id}>
                      <td data-label="Товар">
                        <button className="admin-product-table__select" onClick={() => onProductSelect(product.id)} type="button">
                          <strong>{product.name}</strong>
                          <small>{product.slug || "Нет slug"}</small>
                        </button>
                      </td>
                      <td data-label="SKU / External ID">
                        <span>{product.sku || "Без SKU"}</span>
                        <small>{product.externalId || "Без External ID"}</small>
                      </td>
                      <td data-label="Категория">
                        <span>{product.categoryName || "Нет категории"}</span>
                        <small>{product.categorySlug || "Нет slug категории"}</small>
                      </td>
                      <td data-label="Бренд">{product.brandName ?? "Без бренда"}</td>
                      <td data-label="Статусы">
                        <div className="admin-product-table__chips">
                          {getProductStatusLabels(product).map((label) => (
                            <span className="admin-product-chip" key={label}>
                              {label}
                            </span>
                          ))}
                        </div>
                      </td>
                      <td data-label="Проблемы">
                        {issueLabels.length ? (
                          <ul className="admin-product-table__issues">
                            {issueLabels.map((issue) => (
                              <li key={issue}>{issue}</li>
                            ))}
                          </ul>
                        ) : (
                          <span className="admin-product-table__ok">Нет проблем</span>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        ) : (
          <p className="empty-state">Товары не найдены.</p>
        )}
      </div>
    </section>
  );
}
