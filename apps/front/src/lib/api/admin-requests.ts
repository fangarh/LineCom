import { apiJson } from "./http";
import type {
  CustomerRequestHistory,
  CustomerRequestItem,
  RequestCustomerSnapshot,
  RequestOrganizationSnapshot,
  RequestSource,
  RequestStatus,
} from "./requests";

export type AdminRequestStatusCode = "new" | "in_progress" | "completed" | "cancelled";

export type AdminRequestListItem = {
  number: string;
  status: RequestStatus;
  source: RequestSource;
  itemsCount: number;
  customer: RequestCustomerSnapshot;
  organization: RequestOrganizationSnapshot | null;
  customerComment: string | null;
  internalComment: string | null;
  createdAt: string;
  updatedAt: string;
};

export type AdminRequestListResponse = {
  items: AdminRequestListItem[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

export type AdminRequestDetail = AdminRequestListItem & {
  items: CustomerRequestItem[];
  history: CustomerRequestHistory[];
};

export type AdminRequestListParams = {
  page?: number;
  pageSize?: number;
  status?: AdminRequestStatusCode;
  number?: string;
  contact?: string;
  organization?: string;
};

export function getAdminRequests(params: AdminRequestListParams = {}) {
  const search = new URLSearchParams();
  if (params.page) search.set("page", String(params.page));
  if (params.pageSize) search.set("pageSize", String(params.pageSize));
  if (params.status) search.set("status", params.status);
  if (params.number) search.set("number", params.number);
  if (params.contact) search.set("contact", params.contact);
  if (params.organization) search.set("organization", params.organization);

  const suffix = search.toString();
  return apiJson<AdminRequestListResponse>(`/api/admin/requests${suffix ? `?${suffix}` : ""}`, {
    cache: "no-store",
  });
}

export function getAdminRequest(number: string) {
  return apiJson<AdminRequestDetail>(`/api/admin/requests/${encodeURIComponent(number)}`, {
    cache: "no-store",
  });
}

export function updateAdminRequestStatus(number: string, status: AdminRequestStatusCode, csrfToken: string) {
  return apiJson<AdminRequestDetail>(`/api/admin/requests/${encodeURIComponent(number)}/status`, {
    method: "PATCH",
    body: { status },
    csrfToken,
  });
}

export function updateAdminRequestInternalComment(number: string, internalComment: string | null, csrfToken: string) {
  return apiJson<AdminRequestDetail>(`/api/admin/requests/${encodeURIComponent(number)}/internal-comment`, {
    method: "PUT",
    body: { internalComment },
    csrfToken,
  });
}
