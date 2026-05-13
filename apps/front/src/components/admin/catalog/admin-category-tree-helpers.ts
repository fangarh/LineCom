import type { AdminCategoryListItem } from "@/lib/api/admin-catalog";

export type CategoryTreeNode = {
  id: string;
  category: AdminCategoryListItem;
  children: CategoryTreeNode[];
};

export type FlatCategoryTreeNode = {
  category: AdminCategoryListItem;
  depth: number;
};

export function buildCategoryTree(categories: AdminCategoryListItem[]): CategoryTreeNode[] {
  const byId = new Map<string, CategoryTreeNode>();
  const roots: CategoryTreeNode[] = [];

  for (const category of categories) {
    byId.set(category.id, { id: category.id, category, children: [] });
  }

  for (const node of byId.values()) {
    const parent = node.category.parentId ? byId.get(node.category.parentId) : null;
    if (parent) {
      parent.children.push(node);
    } else {
      roots.push(node);
    }
  }

  sortCategoryNodes(roots);
  return roots;
}

export function flattenCategoryTree(tree: CategoryTreeNode[]): FlatCategoryTreeNode[] {
  const flat: FlatCategoryTreeNode[] = [];

  function visit(nodes: CategoryTreeNode[], depth: number) {
    for (const node of nodes) {
      flat.push({ category: node.category, depth });
      visit(node.children, depth + 1);
    }
  }

  visit(tree, 0);
  return flat;
}

export function getBlockedParentIds(tree: CategoryTreeNode[], categoryId: string | null | undefined) {
  const blockedIds = new Set<string>();
  if (!categoryId) return blockedIds;

  const selectedNode = findCategoryNode(tree, categoryId);
  if (!selectedNode) return blockedIds;

  function collect(node: CategoryTreeNode) {
    blockedIds.add(node.category.id);
    for (const child of node.children) {
      collect(child);
    }
  }

  collect(selectedNode);
  return blockedIds;
}

function findCategoryNode(nodes: CategoryTreeNode[], categoryId: string): CategoryTreeNode | null {
  for (const node of nodes) {
    if (node.category.id === categoryId) return node;

    const childMatch = findCategoryNode(node.children, categoryId);
    if (childMatch) return childMatch;
  }

  return null;
}

function sortCategoryNodes(nodes: CategoryTreeNode[]) {
  nodes.sort(compareCategoryNodes);
  for (const node of nodes) {
    sortCategoryNodes(node.children);
  }
}

function compareCategoryNodes(left: CategoryTreeNode, right: CategoryTreeNode) {
  const sortOrderDifference = left.category.sortOrder - right.category.sortOrder;
  if (sortOrderDifference !== 0) return sortOrderDifference;

  return left.category.name.localeCompare(right.category.name, "ru");
}
