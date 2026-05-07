import type { PublicCodeLabel } from "./catalog";
import { apiJson } from "./http";

export type RequestSource = "cart" | "quick_order";

export type RequestStatus = PublicCodeLabel;

export type CreateCustomerRequestPayload = {
  source: RequestSource;
  customerComment: string | null;
  items: Array<{
    productId: string;
    quantity: number;
    customerComment: string | null;
  }>;
};

export type CustomerRequestItem = {
  productId: string;
  productName: string;
  productSku: string | null;
  saleUnit: PublicCodeLabel;
  unitQuantity: string;
  quantity: number;
  customerComment: string | null;
};

export type CustomerRequestListItem = {
  number: string;
  status: RequestStatus;
  source: string;
  itemsCount: number;
  customerComment: string | null;
  createdAt: string;
};

export type CustomerRequestListResponse = {
  items: CustomerRequestListItem[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

export type RequestCustomerSnapshot = {
  name: string;
  email: string | null;
  phone: string | null;
};

export type RequestOrganizationSnapshot = {
  name: string;
  inn: string | null;
  contactPerson: string | null;
};

export type CustomerRequestHistory = {
  event: string;
  message: string;
  createdAt: string;
};

export type CustomerRequestDetail = {
  number: string;
  status: RequestStatus;
  source: string;
  customerComment: string | null;
  createdAt: string;
  items: CustomerRequestItem[];
  customer?: RequestCustomerSnapshot | null;
  organization?: RequestOrganizationSnapshot | null;
  history?: CustomerRequestHistory[] | null;
};

export type CustomerRequestListParams = {
  page?: number;
  pageSize?: number;
  status?: string;
};

export function createCustomerRequest(payload: CreateCustomerRequestPayload, csrfToken: string) {
  return apiJson<CustomerRequestDetail>("/api/account/requests", {
    method: "POST",
    body: payload,
    csrfToken,
  });
}

export function getCustomerRequests(params: CustomerRequestListParams = {}) {
  const search = new URLSearchParams();
  if (params.page) search.set("page", String(params.page));
  if (params.pageSize) search.set("pageSize", String(params.pageSize));
  if (params.status) search.set("status", params.status);

  const suffix = search.toString();
  return apiJson<CustomerRequestListResponse>(`/api/account/requests${suffix ? `?${suffix}` : ""}`, {
    cache: "no-store",
  });
}

export function getCustomerRequest(number: string) {
  return apiJson<CustomerRequestDetail>(`/api/account/requests/${encodeURIComponent(number)}`, {
    cache: "no-store",
  });
}
