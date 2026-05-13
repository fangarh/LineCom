import type { CSSProperties, ReactNode } from "react";
import type { AdminCategoryListItem } from "@/lib/api/admin-catalog";
import { buildCategoryTree, flattenCategoryTree } from "./admin-category-tree-helpers";

type AdminCategoryTreeProps = {
  categories: AdminCategoryListItem[];
  isLoading?: boolean;
  selectedCategoryId: string | null;
  onCategorySelect: (categoryId: string) => void;
};

export function AdminCategoryTree({
  categories,
  isLoading = false,
  selectedCategoryId,
  onCategorySelect,
}: AdminCategoryTreeProps) {
  const flatNodes = flattenCategoryTree(buildCategoryTree(categories));

  return (
    <div className="admin-category-tree" aria-busy={isLoading} aria-label="Дерево категорий" role="tree">
      {flatNodes.length ? (
        flatNodes.map(({ category, depth }) => {
          const isSelected = selectedCategoryId === category.id;

          return (
            <button
              aria-level={depth + 1}
              aria-selected={isSelected}
              className="admin-category-tree__button"
              data-selected={isSelected}
              key={category.id}
              onClick={() => onCategorySelect(category.id)}
              role="treeitem"
              style={{ "--category-depth": depth } as CSSProperties}
              type="button"
            >
              <span className="admin-category-tree__main">
                <strong>{category.name}</strong>
                <small>{category.slug}</small>
              </span>
              <span className="admin-category-tree__badges">
                <CategoryBadge>{category.isActive ? "активна" : "неактивна"}</CategoryBadge>
                <CategoryBadge>{category.isVisibleInMenu ? "в меню" : "не в меню"}</CategoryBadge>
              </span>
              <span className="admin-category-tree__meta">
                {formatCount(category.productsCount, ["товар", "товара", "товаров"])}
                {" · "}
                {formatCount(category.childrenCount, ["подкатегория", "подкатегории", "подкатегорий"])}
              </span>
            </button>
          );
        })
      ) : (
        <p className="empty-state">Категории не найдены.</p>
      )}
    </div>
  );
}

function CategoryBadge({ children }: { children: ReactNode }) {
  return <span className="admin-category-tree__badge">{children}</span>;
}

export function formatCount(count: number, forms: [string, string, string]) {
  const normalized = Math.abs(count);
  const lastTwo = normalized % 100;
  const last = normalized % 10;
  const form = lastTwo >= 11 && lastTwo <= 14 ? forms[2] : last === 1 ? forms[0] : last >= 2 && last <= 4 ? forms[1] : forms[2];

  return `${count} ${form}`;
}
