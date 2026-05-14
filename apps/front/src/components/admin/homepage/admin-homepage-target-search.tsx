"use client";

import { useEffect, useRef, useState } from "react";
import {
  getAdminCategories,
  getAdminProducts,
  type AdminCategoryListItem,
  type AdminProductListItem,
} from "@/lib/api/admin-catalog";
import type { AdminHomepageSectionType } from "@/lib/api/admin-homepage";
import { normalizeApiError } from "@/lib/api/errors";
import { describeHomepageTargetVisibility } from "./admin-homepage-visibility";

type AdminHomepageTargetSearchProps = {
  isMutating: boolean;
  sectionType: AdminHomepageSectionType;
  onAddCategory: (categoryId: string) => void;
  onAddProduct: (productId: string) => void;
};

type SearchState = {
  categories: AdminCategoryListItem[];
  error: string | null;
  key: string;
  products: AdminProductListItem[];
};

const emptySearchState: SearchState = {
  categories: [],
  error: null,
  key: "",
  products: [],
};

const publishStatusLabels: Record<string, string> = {
  archived: "Архив",
  draft: "Черновик",
  published: "Опубликован",
  review: "Проверка",
};

export function AdminHomepageTargetSearch({
  isMutating,
  sectionType,
  onAddCategory,
  onAddProduct,
}: AdminHomepageTargetSearchProps) {
  const [query, setQuery] = useState("");
  const [searchState, setSearchState] = useState<SearchState>(emptySearchState);
  const requestIdRef = useRef(0);

  const trimmedQuery = query.trim();
  const isProductList = sectionType === "product_list";
  const searchKey = `${sectionType}:${trimmedQuery}`;
  const hasSearchQuery = trimmedQuery.length > 0;
  const hasCurrentSearchState = searchState.key === searchKey;
  const products = hasCurrentSearchState && isProductList ? searchState.products : [];
  const categories = hasCurrentSearchState && !isProductList ? searchState.categories : [];
  const searchError = hasCurrentSearchState ? searchState.error : null;
  const isSearching = hasSearchQuery && !hasCurrentSearchState;
  const label = isProductList ? "Поиск товара" : "Поиск категории";

  useEffect(() => {
    const requestId = requestIdRef.current + 1;
    requestIdRef.current = requestId;

    if (!trimmedQuery) {
      return;
    }

    const searchPromise = isProductList
      ? getAdminProducts({ search: trimmedQuery, page: 1, pageSize: 10 }).then((response) => {
          if (requestIdRef.current === requestId) {
            setSearchState({
              categories: [],
              error: null,
              key: searchKey,
              products: response.items,
            });
          }
        })
      : getAdminCategories({ search: trimmedQuery, page: 1, pageSize: 10 }).then((response) => {
          if (requestIdRef.current === requestId) {
            setSearchState({
              categories: response.items,
              error: null,
              key: searchKey,
              products: [],
            });
          }
        });

    searchPromise
      .catch((error) => {
        if (requestIdRef.current === requestId) {
          setSearchState({
            categories: [],
            error: normalizeApiError(error).message,
            key: searchKey,
            products: [],
          });
        }
      });
  }, [isProductList, searchKey, trimmedQuery]);

  return (
    <section className="admin-homepage-section admin-homepage-target-search" aria-label="Добавление элемента секции">
      <div className="admin-category-manager__head">
        <div>
          <h2>Добавить элемент</h2>
          <p className="admin-catalog-status">Найдите позицию в каталоге.</p>
        </div>
      </div>

      <label className="admin-filter-field">
        <span>{label}</span>
        <input
          autoComplete="off"
          onChange={(event) => setQuery(event.target.value)}
          placeholder={isProductList ? "Название, артикул или externalId" : "Название категории"}
          value={query}
        />
      </label>

      {searchError ? (
        <p className="form-alert" role="alert">
          {searchError}
        </p>
      ) : null}

      {isSearching ? <p className="admin-catalog-status">Поиск...</p> : null}

      <div className="admin-homepage-search-results">
        {isProductList
          ? products.map((product) => (
              <ProductSearchResult
                isMutating={isMutating}
                key={product.id}
                onAdd={() => onAddProduct(product.id)}
                product={product}
              />
            ))
          : categories.map((category) => (
              <CategorySearchResult
                category={category}
                isMutating={isMutating}
                key={category.id}
                onAdd={() => onAddCategory(category.id)}
              />
            ))}
      </div>

      {!isSearching && trimmedQuery && products.length === 0 && categories.length === 0 && !searchError ? (
        <p className="admin-catalog-status">Ничего не найдено.</p>
      ) : null}
    </section>
  );
}

function ProductSearchResult({
  isMutating,
  product,
  onAdd,
}: {
  isMutating: boolean;
  product: AdminProductListItem;
  onAdd: () => void;
}) {
  const skuText = product.sku ?? product.externalId ?? "без артикула";
  const publishStatus = publishStatusLabels[product.publishStatus] ?? product.publishStatus;
  const visibility = describeHomepageTargetVisibility({
    type: "product",
    isActive: product.isActive,
    publishStatus: product.publishStatus,
    slug: product.slug,
    categoryName: product.categoryName,
  });

  return (
    <article className="admin-homepage-search-result">
      <div>
        <p className="admin-homepage-search-result__type">Товар</p>
        <strong>{product.name}</strong>
        <p className="admin-homepage-item__meta">
          {[product.slug || "нет slug", skuText, product.categoryName || "без категории"].join(" · ")}
        </p>
        <p className="admin-homepage-item__meta">
          {product.isActive ? "Активен" : "Неактивен"} · {publishStatus} · {visibility}
        </p>
      </div>
      <button className="button button--ghost" disabled={isMutating} onClick={onAdd} type="button">
        Добавить {product.name}
      </button>
    </article>
  );
}

function CategorySearchResult({
  category,
  isMutating,
  onAdd,
}: {
  category: AdminCategoryListItem;
  isMutating: boolean;
  onAdd: () => void;
}) {
  const visibility = describeHomepageTargetVisibility({
    type: "category",
    isActive: category.isActive,
    slug: category.slug,
    isVisibleInMenu: category.isVisibleInMenu,
  });

  return (
    <article className="admin-homepage-search-result">
      <div>
        <p className="admin-homepage-search-result__type">Категория</p>
        <strong>{category.name}</strong>
        <p className="admin-homepage-item__meta">
          {[category.slug || "нет slug", `${category.productsCount} товаров`, `${category.childrenCount} подкатегорий`].join(
            " · ",
          )}
        </p>
        <p className="admin-homepage-item__meta">
          {category.isActive ? "Активна" : "Неактивна"} · {category.isVisibleInMenu ? "В меню" : "Не в меню"} · {visibility}
        </p>
      </div>
      <button className="button button--ghost" disabled={isMutating} onClick={onAdd} type="button">
        Добавить {category.name}
      </button>
    </article>
  );
}
