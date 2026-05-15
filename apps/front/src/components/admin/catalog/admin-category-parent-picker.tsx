import { useMemo, useState, type CSSProperties } from "react";
import type { AdminCategoryListItem } from "@/lib/api/admin-catalog";
import { buildCategoryTree, flattenCategoryTree, type FlatCategoryTreeNode } from "./admin-category-tree-helpers";
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

type EmptyCategoryOption = {
  title: string;
  description: string;
  ariaLabel: string;
};

type AdminCategoryTreePickerProps = {
  label: string;
  buttonLabel: string;
  categories: AdminCategoryListItem[];
  value: string;
  className?: string;
  emptyOption?: EmptyCategoryOption | null;
  emptySelection?: EmptyCategoryOption;
  unavailableSelection?: EmptyCategoryOption;
  disabled?: boolean;
  isCategoryHidden?: (node: FlatCategoryTreeNode) => boolean;
  isCategoryDisabled?: (node: FlatCategoryTreeNode) => boolean;
  getDisabledReason?: (node: FlatCategoryTreeNode) => string;
  onChange: (categoryId: string) => void;
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
  return (
    <AdminCategoryTreePicker
      buttonLabel={buttonLabel}
      categories={categories}
      disabled={disabled}
      emptyOption={{
        ariaLabel: "Без родителя",
        title: "Без родителя",
        description: "Корневая категория",
      }}
      label={label}
      isCategoryHidden={({ category }) => blockedParentIds.has(category.id)}
      onChange={onChange}
      value={value}
    />
  );
}

export function AdminCategoryTreePicker({
  label,
  buttonLabel,
  categories,
  value,
  className,
  emptyOption = null,
  emptySelection = {
    ariaLabel: "Выбрать категорию",
    title: "Выберите категорию",
    description: "Категория не выбрана",
  },
  unavailableSelection = {
    ariaLabel: "Категория недоступна",
    title: "Категория недоступна",
    description: "Выберите актуальную категорию",
  },
  disabled = false,
  isCategoryHidden = () => false,
  isCategoryDisabled = () => false,
  getDisabledReason = () => "Недоступно для выбора",
  onChange,
}: AdminCategoryTreePickerProps) {
  const [isOpen, setIsOpen] = useState(false);
  const flatNodes = useMemo(() => flattenCategoryTree(buildCategoryTree(categories)), [categories]);
  const selectedCategory = categories.find((category) => category.id === value) ?? null;
  const availableNodes = flatNodes.filter((node) => !isCategoryHidden(node));
  const selectedTitle = selectedCategory ? selectedCategory.name : value ? unavailableSelection.title : emptySelection.title;
  const selectedDescription = selectedCategory
    ? selectedCategory.slug
    : value
      ? unavailableSelection.description
      : emptySelection.description;

  function selectValue(categoryId: string) {
    onChange(categoryId);
    setIsOpen(false);
  }

  return (
    <div className={["admin-category-parent-picker", className].filter(Boolean).join(" ")}>
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
          <strong>{selectedTitle}</strong>
          <small>{selectedDescription}</small>
        </span>
        <span>{buttonLabel}</span>
      </button>
      {isOpen ? (
        <div className="admin-category-parent-picker__options" role="listbox" aria-label={label}>
          {emptyOption ? (
            <button
              aria-label={emptyOption.ariaLabel}
              aria-selected={value === ""}
              className="admin-category-parent-picker__option"
              onClick={() => selectValue("")}
              role="option"
              type="button"
            >
              <span>
                <strong>{emptyOption.title}</strong>
                <small>{emptyOption.description}</small>
              </span>
            </button>
          ) : null}
          {availableNodes.map((node) => {
            const { category, depth } = node;
            const isOptionDisabled = isCategoryDisabled(node);

            return (
              <button
                aria-disabled={isOptionDisabled}
                aria-label={category.name}
                aria-selected={value === category.id}
                className="admin-category-parent-picker__option"
                disabled={isOptionDisabled}
                key={category.id}
                onClick={() => selectValue(category.id)}
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
                  {isOptionDisabled ? ` · ${getDisabledReason(node)}` : ""}
                </span>
              </button>
            );
          })}
        </div>
      ) : null}
    </div>
  );
}
