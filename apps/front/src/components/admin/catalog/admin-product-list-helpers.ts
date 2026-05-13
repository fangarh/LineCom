import type { AdminProductListItem } from "@/lib/api/admin-catalog";

export type ProductListPageMeta = {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

const publishStatusLabels: Record<string, string> = {
  draft: "Черновик",
  review: "Проверка",
  published: "Опубликован",
  archived: "Архив",
};

export function formatPageRange(meta: ProductListPageMeta) {
  if (meta.totalItems <= 0) return "0 из 0";

  const start = (meta.page - 1) * meta.pageSize + 1;
  const end = Math.min(meta.page * meta.pageSize, meta.totalItems);

  return `${start}-${end} из ${meta.totalItems}`;
}

export function getPublishStatusLabel(status: string) {
  return publishStatusLabels[status] ?? status;
}

export function getProductStatusLabels(product: AdminProductListItem) {
  return [
    product.isActive ? "Активен" : "Неактивен",
    getPublishStatusLabel(product.publishStatus),
    product.readiness.canPublish ? "Готов к публикации" : "Нельзя публиковать",
  ];
}

export function getProductIssueLabels(product: AdminProductListItem) {
  const issues: string[] = [];

  if (!product.categoryName || !product.categorySlug) issues.push("Нет категории");
  if (!product.slug) issues.push("Нет slug");
  issues.push(...product.readiness.issues.map((issue) => issue.message));

  return issues;
}
