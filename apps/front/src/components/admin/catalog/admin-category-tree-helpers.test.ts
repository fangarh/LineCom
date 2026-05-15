import { describe, expect, it } from "vitest";
import type { AdminCategoryListItem } from "@/lib/api/admin-catalog";
import { buildCategoryTree, flattenCategoryTree, getBlockedParentIds } from "./admin-category-tree-helpers";

const rootCategory: AdminCategoryListItem = {
  id: "cat-root",
  parentId: null,
  name: "Кабели",
  slug: "kabeli",
  sortOrder: 10,
  isActive: true,
  isVisibleInMenu: true,
  productsCount: 4,
  childrenCount: 1,
};

const childCategory: AdminCategoryListItem = {
  id: "cat-child",
  parentId: "cat-root",
  name: "Силовые кабели",
  slug: "silovye-kabeli",
  sortOrder: 20,
  isActive: false,
  isVisibleInMenu: false,
  productsCount: 2,
  childrenCount: 0,
};

const connectorCategory: AdminCategoryListItem = {
  id: "cat-connector",
  parentId: null,
  name: "Разъемы",
  slug: "razemy",
  sortOrder: 30,
  isActive: true,
  isVisibleInMenu: true,
  productsCount: 1,
  childrenCount: 0,
};

describe("admin category tree helpers", () => {
  it("builds root and child category tree sorted by sort order then name", () => {
    const earlierByName: AdminCategoryListItem = {
      ...connectorCategory,
      id: "cat-adapters",
      name: "Адаптеры",
      slug: "adaptery",
      sortOrder: 30,
    };

    const tree = buildCategoryTree([connectorCategory, childCategory, earlierByName, rootCategory]);

    expect(tree.map((node) => node.category.id)).toEqual(["cat-root", "cat-adapters", "cat-connector"]);
    expect(tree[0].children[0].id).toBe("cat-child");
  });

  it("flattens category tree with depth information", () => {
    const tree = buildCategoryTree([rootCategory, childCategory, connectorCategory]);

    expect(flattenCategoryTree(tree).map((node) => `${node.depth}:${node.hasChildren}:${node.category.id}`)).toEqual([
      "0:true:cat-root",
      "1:false:cat-child",
      "0:false:cat-connector",
    ]);
  });

  it("blocks selected category and all descendants as parent candidates", () => {
    const tree = buildCategoryTree([rootCategory, childCategory, connectorCategory]);

    expect(getBlockedParentIds(tree, "cat-root")).toEqual(new Set(["cat-root", "cat-child"]));
    expect(getBlockedParentIds(tree, null)).toEqual(new Set());
  });
});
