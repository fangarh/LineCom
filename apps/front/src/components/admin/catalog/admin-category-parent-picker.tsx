import { useMemo, useState, type CSSProperties } from "react";
import type { AdminCategoryListItem } from "@/lib/api/admin-catalog";
import { buildCategoryTree, flattenCategoryTree } from "./admin-category-tree-helpers";
import { formatCount } from "./admin-category-tree";

type AdminCategoryParentPickerProps = {
  label: string;
  buttonLabel: string;
  categories: AdminCategoryListItem[];
  value: string;
  blockedParentIds?: Set<string>;
  disabled?: boolean;
  onChange: (parentId: string) => void;
};

export function AdminCategoryParentPicker({
  label,
  buttonLabel,
  categories,
  value,
  blockedParentIds = new Set(),
  disabled = false,
  onChange,
}: AdminCategoryParentPickerProps) {
  const [isOpen, setIsOpen] = useState(false);
  const flatNodes = useMemo(() => flattenCategoryTree(buildCategoryTree(categories)), [categories]);
  const selectedCategory = categories.find((category) => category.id === value) ?? null;
  const availableNodes = flatNodes.filter(({ category }) => !blockedParentIds.has(category.id));

  function selectParent(parentId: string) {
    onChange(parentId);
    setIsOpen(false);
  }

  return (
    <div className="admin-category-parent-picker">
      <span className="admin-category-parent-picker__label">{label}</span>
      <button
        aria-expanded={isOpen}
        aria-label={buttonLabel}
        className="admin-category-parent-picker__trigger"
        disabled={disabled}
        onClick={() => setIsOpen((current) => !current)}
        type="button"
      >
        <span>
          <strong>{selectedCategory ? selectedCategory.name : "Без родителя"}</strong>
          {selectedCategory ? <small>{selectedCategory.slug}</small> : <small>Корневая категория</small>}
        </span>
        <span>{buttonLabel}</span>
      </button>
      {isOpen ? (
        <div className="admin-category-parent-picker__options" role="listbox" aria-label={label}>
          <button
            aria-label="Без родителя"
            aria-selected={value === ""}
            className="admin-category-parent-picker__option"
            onClick={() => selectParent("")}
            role="option"
            type="button"
          >
            <span>
              <strong>Без родителя</strong>
              <small>Корневая категория</small>
            </span>
          </button>
          {availableNodes.map(({ category, depth }) => (
            <button
              aria-label={category.name}
              aria-selected={value === category.id}
              className="admin-category-parent-picker__option"
              key={category.id}
              onClick={() => selectParent(category.id)}
              role="option"
              style={{ "--category-depth": depth } as CSSProperties}
              type="button"
            >
              <span>
                <strong>{category.name}</strong>
                <small>{category.slug}</small>
              </span>
              <span className="admin-category-parent-picker__meta">
                {category.isActive ? "активна" : "неактивна"} · {category.isVisibleInMenu ? "в меню" : "не в меню"} ·{" "}
                {formatCount(category.productsCount, ["товар", "товара", "товаров"])} ·{" "}
                {formatCount(category.childrenCount, ["подкатегория", "подкатегории", "подкатегорий"])}
              </span>
            </button>
          ))}
        </div>
      ) : null}
    </div>
  );
}
