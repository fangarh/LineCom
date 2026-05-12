import { apiJson } from "./http";

type Id = string;

export type AdminHomepageSectionType = "product_list" | "category_list";

export type AdminHomepageSectionsResponse = {
  sections: AdminHomepageSection[];
};

export type AdminHomepageSection = {
  id: Id;
  code: string;
  title: string;
  type: AdminHomepageSectionType;
  itemLimit: number;
  sortOrder: number;
  isActive: boolean;
  items: AdminHomepageSectionItem[];
};

export type AdminHomepageSectionItem = {
  id: Id;
  productId: Id | null;
  categoryId: Id | null;
  name: string;
  slug: string | null;
  secondaryText: string | null;
  sortOrder: number;
  isActive: boolean;
  visibilityStatus: string;
};

export type UpdateAdminHomepageSectionCommand = {
  title?: string | null;
  itemLimit?: number | null;
  sortOrder?: number | null;
  isActive?: boolean | null;
};

export type AddAdminHomepageSectionItemCommand = {
  productId?: Id | null;
  categoryId?: Id | null;
  sortOrder?: number | null;
  isActive?: boolean | null;
};

export type UpdateAdminHomepageSectionItemCommand = {
  sortOrder?: number | null;
  isActive?: boolean | null;
};

export function getAdminHomepageSections() {
  return apiJson<AdminHomepageSectionsResponse>("/api/admin/homepage/sections", {
    cache: "no-store",
  });
}

export function updateAdminHomepageSection(
  sectionId: Id,
  command: UpdateAdminHomepageSectionCommand,
  csrfToken: string,
) {
  return apiJson<AdminHomepageSection>(`/api/admin/homepage/sections/${encodeURIComponent(sectionId)}`, {
    method: "PUT",
    body: command,
    csrfToken,
  });
}

export function addAdminHomepageSectionItem(
  sectionId: Id,
  command: AddAdminHomepageSectionItemCommand,
  csrfToken: string,
) {
  return apiJson<AdminHomepageSectionItem>(`/api/admin/homepage/sections/${encodeURIComponent(sectionId)}/items`, {
    method: "POST",
    body: command,
    csrfToken,
  });
}

export function updateAdminHomepageSectionItemOrder(sectionId: Id, itemIds: Id[], csrfToken: string) {
  return apiJson<AdminHomepageSectionsResponse>(
    `/api/admin/homepage/sections/${encodeURIComponent(sectionId)}/items/order`,
    {
      method: "PUT",
      body: { itemIds },
      csrfToken,
    },
  );
}

export function updateAdminHomepageSectionItem(
  sectionId: Id,
  itemId: Id,
  command: UpdateAdminHomepageSectionItemCommand,
  csrfToken: string,
) {
  return apiJson<AdminHomepageSectionItem>(
    `/api/admin/homepage/sections/${encodeURIComponent(sectionId)}/items/${encodeURIComponent(itemId)}`,
    {
      method: "PUT",
      body: command,
      csrfToken,
    },
  );
}

export function deleteAdminHomepageSectionItem(sectionId: Id, itemId: Id, csrfToken: string) {
  return apiJson<void>(
    `/api/admin/homepage/sections/${encodeURIComponent(sectionId)}/items/${encodeURIComponent(itemId)}`,
    {
      method: "DELETE",
      csrfToken,
    },
  );
}
