import type { CurrentUser } from "./auth";
import { apiJson } from "./http";

export type AccountOrganization = {
  name: string;
  inn: string | null;
  contactPerson: string | null;
  phone: string | null;
  email: string | null;
  comment: string | null;
};

export type AccountProfile = {
  user: CurrentUser;
  organization: AccountOrganization | null;
};

export type UpdateAccountProfilePayload = {
  name: string;
  email: string | null;
  phone: string | null;
};

export type UpsertOrganizationPayload = {
  name: string;
  inn: string | null;
  contactPerson: string | null;
  phone: string | null;
  email: string | null;
  comment: string | null;
};

export function getAccountProfile() {
  return apiJson<AccountProfile>("/api/account/profile", {
    cache: "no-store",
  });
}

export function updateAccountProfile(payload: UpdateAccountProfilePayload, csrfToken: string) {
  return apiJson<CurrentUser>("/api/account/profile", {
    method: "PUT",
    body: payload,
    csrfToken,
  });
}

export function upsertOrganization(payload: UpsertOrganizationPayload, csrfToken: string) {
  return apiJson<AccountOrganization>("/api/account/organization", {
    method: "PUT",
    body: payload,
    csrfToken,
  });
}
