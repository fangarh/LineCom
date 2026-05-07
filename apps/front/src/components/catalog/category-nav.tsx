import Link from "next/link";
import type { PublicCategoryTreeItem } from "@/lib/api/catalog";
import { routes } from "@/lib/routes";

type CategoryNavProps = {
  items: PublicCategoryTreeItem[];
};

export function CategoryNav({ items }: CategoryNavProps) {
  if (items.length === 0) {
    return <p className="empty-state">Категории пока не опубликованы.</p>;
  }

  return (
    <nav className="category-nav" aria-label="Категории каталога">
      <CategoryList items={items} />
    </nav>
  );
}

function CategoryList({ items }: CategoryNavProps) {
  return (
    <ul>
      {items.map((item) => (
        <li key={item.id}>
          <Link href={routes.category(item.slug)}>
            <span>{item.name}</span>
            {item.description ? <small>{item.description}</small> : null}
          </Link>
          {item.children.length > 0 ? <CategoryList items={item.children} /> : null}
        </li>
      ))}
    </ul>
  );
}
