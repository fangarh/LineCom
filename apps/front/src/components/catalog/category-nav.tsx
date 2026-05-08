import Link from "next/link";
import type { PublicCategoryTreeItem } from "@/lib/api/catalog";
import { routes } from "@/lib/routes";

type CategoryNavProps = {
  items: PublicCategoryTreeItem[];
  activeSlug?: string;
};

type CategoryListProps = CategoryNavProps & {
  level?: number;
};

export function CategoryNav({ items, activeSlug }: CategoryNavProps) {
  if (items.length === 0) {
    return <p className="empty-state">Категории пока не опубликованы.</p>;
  }

  return (
    <nav className="category-nav" aria-label="Категории каталога">
      <CategoryList items={items} activeSlug={activeSlug} />
    </nav>
  );
}

function CategoryList({ items, activeSlug, level = 0 }: CategoryListProps) {
  return (
    <ul className={level > 0 ? "category-nav__children" : undefined}>
      {items.map((item) => (
        <li key={item.id} className={hasActiveDescendant(item, activeSlug) ? "category-nav__item--active-branch" : undefined}>
          <Link
            className={item.slug === activeSlug ? "category-nav__link category-nav__link--active" : "category-nav__link"}
            href={routes.category(item.slug)}
            aria-current={item.slug === activeSlug ? "page" : undefined}
          >
            <span>{item.name}</span>
            {item.description ? <small>{item.description}</small> : null}
          </Link>
          {item.children.length > 0 ? (
            <CategoryList items={item.children} activeSlug={activeSlug} level={level + 1} />
          ) : null}
        </li>
      ))}
    </ul>
  );
}

function hasActiveDescendant(item: PublicCategoryTreeItem, activeSlug: string | undefined): boolean {
  if (!activeSlug) {
    return false;
  }

  return item.children.some((child) => child.slug === activeSlug || hasActiveDescendant(child, activeSlug));
}
